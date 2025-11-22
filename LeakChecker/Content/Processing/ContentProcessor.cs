using System.Diagnostics;
using System.Globalization;
using System.Text;
using LeakChecker.Format;
using LeakChecker.Format.Detection;
using LeakChecker.Logging;
using LeakChecker.Logging.FileLogging;
using LeakChecker.Utilities.Extensions;

namespace LeakChecker.Content.Processing;

public class ContentProcessor : IDisposable
{
    private long _linesRead;
    private long _recordsRead;
    private long _readerPosition;
    private readonly Stopwatch _sw = new();
    private readonly Encoding _encoding;
    private readonly StreamReader _reader;
    private readonly IFileLogger _logger;
    private readonly FileStats _stats;
    private const int SqlSamplesLimit = 31;
    private const int CsvSamplesLimit = 103;
    private const int ThresholdPercent = 50;

    private ContentProcessor(Encoding encoding, StreamReader reader, IFileLogger logger, FileStats stats)
    {
        _encoding = encoding;
        _reader = reader;
        _logger = logger;
        _stats = stats;
    }

    public static async Task<ContentProcessor> CreateAsync(IFileLogger logger, FileStats stats, Encoding? encoding = null)
    {
        await logger.LogContentHeader();
        if (encoding == null)
        {
            await logger.Log("No encoding specified to ContentDetector. Set UTF-8 without BOM as default.", 
                            LogLevel.Warning, LogContext.Content);
            encoding ??= new UTF8Encoding(false);
        }
        
        string filePath = logger.SubjectFilePath;   //TODO remove this only for test purposes
        // string filePath = logger.SubjectTmpFilePath;    //TODO use this in deployment
        var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var reader = new StreamReader(stream, encoding, detectEncodingFromByteOrderMarks: false);
        
        return new ContentProcessor(encoding, reader, logger, stats);
    }
    
    public async Task ProcessFile()
    {
        _sw.Start();
        await _logger.Log("Content parsing started.\n");

        while (await _reader.ReadLineAsync() is { } line)
        {
            // if (_recordsRead >= 150) break;
            // if (_recordsRead >= 1_000_000) break; 

            _linesRead++;
            // line = line.ReplaceLineEndings("").Trim();
            if (string.IsNullOrWhiteSpace(line) || string.IsNullOrEmpty(line)) { continue; }
            
            if (line.StartsWith("INSERT INTO", StringComparison.OrdinalIgnoreCase))
            {
                await ProcessSqlInsert();
                continue;
            }
            
            if (line.StartsWith('{') || line.StartsWith('[')) {} // looks like a JSON - test if it really is a json and parse it
            if (line.Contains("<html", StringComparison.OrdinalIgnoreCase) ||   // looks like an HTML
                line.Contains("<body", StringComparison.OrdinalIgnoreCase)) {}  // test if it really is a html and parse it

            await ProcessCsvFile();
        }

        _stats.LinesRead = _linesRead;
        _stats.BytesRead = _readerPosition;
        _stats.RecordsCount = _recordsRead;

        Console.WriteLine();
        await _logger.Log($"Content processing finished successfully. Time taken: {_sw.Elapsed}, current DateTime: " +
                       $"{DateTime.Now.ToString("F", CultureInfo.InvariantCulture)}", LogLevel.Success, LogContext.Content);
        await _logger.Log($"Lines processed count: {_linesRead:N0}");
    }

    private async Task ProcessSqlInsert()
    {
        _linesRead--;
        long sqlInsertStart = _readerPosition;
        
        // Reset reader before INSERT INTO
        _reader.AdjustPosition(sqlInsertStart);
        
        // Detect schema
        Stopwatch sw = Stopwatch.StartNew();
        var schema = await SqlInsertDetector.DetectFormat(_linesRead, _reader, _logger, SqlSamplesLimit, ThresholdPercent);
        await _logger.Log($"SQL INSERT schema created in {sw.Elapsed}\n");

        // Parse SQL INSERT block with header
        _reader.AdjustPosition(sqlInsertStart);
        SqlInsertProcessor processor = new(schema, _reader, _logger);
        ParsingState result = await processor.ProcessSqlInsert(_linesRead);
        
        _recordsRead += result.RecordsRead;
        _linesRead += result.LinesRead;
        _readerPosition += result.BytesRead;
        
        _stats.Formats.Add(FormatEnum.SqlInsert);
        _stats.Delimiters.Add(',');
    }

    private async Task ProcessCsvFile()
    {
        _linesRead--;
        long csvFormatStart = _readerPosition;

        char delimiter;
        var delimiterResult = DelimiterHeuristic.Analyze(_reader);
        if (delimiterResult.BestDelimiter.HasValue)
        { 
            delimiter = delimiterResult.BestDelimiter.Value;
            await _logger.LogDelimiterHeuristic(delimiterResult, count: 5);
        }
        else
        {
            await _logger.Log("Delimiter detection failed. Setting default delimiter ':'", LogLevel.Warning, LogContext.Delimiter);
            delimiter = ';';
        }

        _reader.AdjustPosition(csvFormatStart);
        
        // Detect schema
        Stopwatch sw = Stopwatch.StartNew();
        var schema = await CsvFileDetector.DetectFormat(_linesRead, delimiter, _reader, _logger, CsvSamplesLimit, ThresholdPercent);
        await _logger.Log($"CSV file schema created in {sw.Elapsed}\n");
        
        // Parse CSV file
        _reader.AdjustPosition(csvFormatStart);
        CsvFileProcessor csvFileProcessor = new(schema, _logger);
        ParsingState result;
        if (schema.Values.All(v => v == ItemEnum.Other))    // if nothing reliable detected
        {
            result = await csvFileProcessor.ProcessCsvFile(_linesRead, delimiter, _reader, CsvSamplesLimit);
            _recordsRead += result.RecordsRead;
            _linesRead += result.LinesRead;
            _readerPosition += result.BytesRead;
            
            return;
        }
        result = await csvFileProcessor.ProcessCsvFile(_linesRead, delimiter, _reader, long.MaxValue);

        _recordsRead += result.RecordsRead;
        _linesRead += result.LinesRead;
        _readerPosition += result.BytesRead;
        
        _stats.Formats.Add(FormatEnum.Csv);
        _stats.Delimiters.Add(delimiter);
    }

    public void Dispose()
    {
        _reader.Dispose();
        _reader.BaseStream.Dispose();
    }
}

public class ParsingState
{
    public long RecordsRead = 0;
    public long LinesRead = 0;
    public long BytesRead = 0;
}
