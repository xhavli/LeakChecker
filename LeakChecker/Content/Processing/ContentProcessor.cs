using System.Diagnostics;
using System.Globalization;
using System.Text;
using LeakChecker.Format;
using LeakChecker.Format.Detection;
using LeakChecker.Logging;
using LeakChecker.Logging.FileLogging;
using LeakChecker.Utilities.Extensions;

namespace LeakChecker.Content.Processing;

public class ContentProcessor
{
    private char _delimiter;
    private long _linesRead;
    private long _recordsProcessed;
    private long _readerPosition;
    private readonly Stopwatch _sw = new();
    private readonly Encoding _encoding;
    private readonly StreamReader _reader;
    private readonly IFileLogger _logger;
    private readonly FileStats _fileStats;

    private ContentProcessor(char delimiter, Encoding encoding, StreamReader reader, IFileLogger logger, FileStats fileStats)
    {
        _delimiter = delimiter;
        _encoding = encoding;
        _reader = reader;
        _logger = logger;
        _fileStats = fileStats;
    }

    public static async Task<ContentProcessor> CreateAsync(
        char delimiter, IFileLogger logger, FileStats stats, Encoding? encoding = null)
    {
        if (encoding == null)
        {
            await logger.Log("No encoding specified to ContentDetector. Set UTF-8 without BOM as default.", 
                LogLevel.Warning, LogContext.Content);
            encoding ??= new UTF8Encoding(false);
        }
        
        string filePath = logger.SubjectFilePath;
        var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var reader = new StreamReader(stream, encoding, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        
        return new ContentProcessor(delimiter, encoding, reader, logger, stats);
    }
    
    public async Task ProcessFile()
    {
        _sw.Start();
        await _logger.LogContentHeader();

        while (await _reader.ReadLineWithEndingAsync() is { } line)
        {
            if (_recordsProcessed >= 150) break;    //todo remove this 
            
            int bytesRead = _encoding.GetByteCount(line);
            line = line.ReplaceLineEndings("").Trim();
            
            if (string.IsNullOrWhiteSpace(line) || string.IsNullOrEmpty(line)) continue;
            
            if (line.StartsWith("INSERT INTO", StringComparison.OrdinalIgnoreCase))
            {
                await ProcessSqlInsert();
                continue;
            }
            
            // if looks like a JSON    //TODO
            if (line.StartsWith('{') || line.StartsWith('[')) {}    // test if it really is a json and parse it
            // if looks like a HTML    //TODO
            if (line.Contains("<html", StringComparison.OrdinalIgnoreCase) || 
                line.Contains("<body", StringComparison.OrdinalIgnoreCase)) {}  // // test if it really is a html and parse it

            await ProcessCsvFile();

            if (_recordsProcessed >= 150) break;
        }

        _fileStats.LinesRead = _linesRead;
        _fileStats.RecordsCount = _recordsProcessed;
        Console.WriteLine();
        await _logger.Log($"Content processing finished successfully. Time taken: {_sw.Elapsed}, current DateTime: " +
                       $"{DateTime.Now.ToString("F", CultureInfo.InvariantCulture)}", LogLevel.Success, LogContext.Content);
        await _logger.Log($"Lines processed count: {_recordsProcessed}");
    }

    private async Task ProcessSqlInsert()
    {
        long sqlInsertStart = _readerPosition;
        
        // Reset to before INSERT INTO
        _reader.AdjustPosition(sqlInsertStart);
        
        // Detect schema & get end position
        Stopwatch sw = Stopwatch.StartNew();
        var schema = await SqlInsertDetector.DetectFormat(_reader, _logger);
        Console.WriteLine();
        await _logger.Log($"SQL INSERT schema created in {sw.Elapsed}");
        Console.WriteLine();
        
        // Process INSERT block fully (this will advance reader internally)
        SqlInsertProcessor processor = new(schema, _reader, _logger);
        _reader.AdjustPosition(sqlInsertStart);
        var result = await processor.ProcessSqlInsert();
        
        // Reader is now at end of INSERT block -> realign
        _readerPosition += result.bytesRead;
        _recordsProcessed += result.recordProcessed;
        _reader.AdjustPosition(_readerPosition);
        
        _fileStats.Formats.Add(FormatEnum.SqlInsert);
    }

    private async Task ProcessCsvFile()
    {
        long csvFormatStarted = _readerPosition;
            
        _reader.AdjustPosition(csvFormatStarted);
        
        // Detect schema & get end position
        Stopwatch csvFormatSw = Stopwatch.StartNew();
        var csvSchema = await CsvFileDetector.DetectFormat(_delimiter, _reader, _logger);
        // csvSchema = CsvCredentialAssigner.Assign(csvSchema);
        Console.WriteLine();
        await _logger.Log($"-- CSV file schema created in {csvFormatSw.Elapsed} --");
        Console.WriteLine();
            
        _reader.AdjustPosition(csvFormatStarted);
        CsvFileProcessor csvFileProcessor = new(csvSchema, _logger);
        var csvResult = await csvFileProcessor.ProcessCsvFile(_reader);
        
        // Reader is now at end of INSERT block -> realign
        _linesRead += csvResult.linesRead;
        _readerPosition += csvResult.bytesRead;
        _recordsProcessed += csvResult.recordProcessed;
        _reader.AdjustPosition(_readerPosition);
        
        _fileStats.Formats.Add(FormatEnum.Csv);
    }
}