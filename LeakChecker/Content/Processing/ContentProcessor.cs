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
    private const long ParseLimit = long.MaxValue;

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

        while (await _reader.ReadLineWithEndingAsync() is { } originLine)
        {
            if (_recordsRead >= ParseLimit) break;
            
            string line = originLine.Trim();
            
            if (IsTrashOrEmpty(line))
            {
                _linesRead++;
                _readerPosition += _encoding.GetByteCount(originLine);
                continue;
            }

            if (IsSqlCreateTable(line))
            {
                _linesRead++;
                _readerPosition += _encoding.GetByteCount(originLine);
                await SkipSqlCreateTable();
                continue;
            }
            
            if (IsSqlInsertValues(line))
            {
                await ProcessSqlInsert();
                continue;
            }
            
            if (line.StartsWith('{') || line.StartsWith('[')) {} // looks like a JSON - test if it really is a json and parse it
            if (line.Contains("<html", StringComparison.OrdinalIgnoreCase) ||   // looks like an HTML
                line.Contains("<body", StringComparison.OrdinalIgnoreCase)) {}  // test if it really is a html and parse it

            // Fallback
            await ProcessCsvFile();
        }

        _stats.LinesRead = _linesRead;
        _stats.BytesRead = _readerPosition;
        _stats.RecordsRead = _recordsRead;

        await _logger.Log($"Content processing finished successfully. Time taken: {_sw.Elapsed}, current DateTime: " +
                       $"{DateTime.Now.ToString("F", CultureInfo.InvariantCulture)}", LogLevel.Success, LogContext.Content);
        await _logger.Log($"Lines processed count: {_linesRead:N0}");
    }

    private async Task ProcessSqlInsert()
    {
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
        ParsingState result = await processor.ProcessSqlInsert(_linesRead, ParseLimit);
        
        _recordsRead += result.RecordsRead;
        _linesRead += result.LinesRead;
        _readerPosition += result.BytesRead;
        
        _stats.Formats.Add(FormatEnum.SqlInsert);
        _stats.Delimiters.Add(',');
    }
    
    private async Task SkipSqlCreateTable()
    {
        bool inSingle = false;
        bool inDouble = false;
        bool inBacktick = false;
        bool inBracket = false; // [identifier] for T-SQL

        while (await _reader.ReadLineWithEndingAsync() is { } line)
        {
            _linesRead++;

            _readerPosition += _encoding.GetByteCount(line);
            line = line.TrimOuterWhiteSpace();

            ReadOnlySpan<char> span = line.AsSpan();

            for (int i = 0; i < span.Length; i++)
            {
                char ch = span[i];

                // toggle string/identifier states
                switch (ch)
                {
                    case '\'':
                        if (!inDouble && !inBacktick && !inBracket)
                            inSingle = !inSingle;
                        break;

                    case '"':
                        if (!inSingle && !inBacktick && !inBracket)
                            inDouble = !inDouble;
                        break;

                    case '`':
                        if (!inSingle && !inDouble && !inBracket)
                            inBacktick = !inBacktick;
                        break;

                    case '[':
                        if (!inSingle && !inDouble && !inBacktick)
                            inBracket = true;
                        break;

                    case ']':
                        if (inBracket)
                            inBracket = false;
                        break;

                    case ';':
                        // this ends CREATE TABLE *only if not inside quotes / identifiers*
                        if (!inSingle && !inDouble && !inBacktick && !inBracket)
                            return;
                        break;
                }
            }
        }
    }

    private async Task ProcessCsvFile()
    {
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
            await _logger.Log("Delimiter detection failed. Setting default delimiter [:]", LogLevel.Warning, LogContext.Delimiter);
            delimiter = ';';
        }

        // Reset reader before CSV start
        _reader.AdjustPosition(csvFormatStart);
        
        // Detect schema
        Stopwatch sw = Stopwatch.StartNew();
        var schema = await CsvFileDetector.DetectFormat(_linesRead, delimiter, _reader, _logger, CsvSamplesLimit, ThresholdPercent);
        await _logger.Log($"CSV file schema created in {sw.Elapsed}\n");
        
        // Parse CSV file
        _reader.AdjustPosition(csvFormatStart);
        CsvFileProcessor csvFileProcessor = new(schema, _logger);
        ParsingState result;
        if (schema.Values.All(v => v == ItemEnum.Other) || schema.Count == 0)    // if nothing reliable detected
        {
            result = await csvFileProcessor.ProcessCsvFile(_linesRead, delimiter, _reader, CsvSamplesLimit);
            _recordsRead += result.RecordsRead;
            _linesRead += result.LinesRead;
            _readerPosition += result.BytesRead;
            
            return;
        }
        result = await csvFileProcessor.ProcessCsvFile(_linesRead, delimiter, _reader, ParseLimit);

        _recordsRead += result.RecordsRead;
        _linesRead += result.LinesRead;
        _readerPosition += result.BytesRead;
        
        _stats.Formats.Add(FormatEnum.Csv);
        _stats.Delimiters.Add(delimiter);
    }

    // INSERT [modifiers] INTO <table_name> [ (columns...) ] VALUES
    // ( literal , literal , literal , ... );
    private static bool IsSqlInsertValues(string line)
    {
        return line.StartsWith("INSERT ", StringComparison.OrdinalIgnoreCase) &&
               line.IndexOf("INTO", StringComparison.OrdinalIgnoreCase) >= "INSERT ".Length &&
               line.Contains("VALUES", StringComparison.OrdinalIgnoreCase);
    }
    
    // CREATE [modifiers] TABLE [table_modifiers] <table_name>
    // ( <column> <data_type> [column_constraint] ,
    //   <column> <data_type> [column_constraint] ,
    //   ...
    // ) [table_options];
    private static bool IsSqlCreateTable(string line)
    {
        return line.StartsWith("CREATE ", StringComparison.OrdinalIgnoreCase) &&
               line.IndexOf("TABLE", StringComparison.OrdinalIgnoreCase) >= "CREATE ".Length;
    }

    private static bool IsTrashOrEmpty(string line)
    {
        return string.IsNullOrWhiteSpace(line) ||
               line.Replace(" ", "").All(ch => char.GetUnicodeCategory(ch) == UnicodeCategory.DashPunctuation); // Sql comment boundary
        
        if (string.IsNullOrWhiteSpace(line)) return true;

        StringBuilder sb = new();

        foreach (char ch in line)
        {
            switch (ch)
            {
                case '-':
                    continue;
                case ' ':
                    continue;
                default:
                    sb.Append(ch);
                    return false;
            }
        }
        
        return sb.Length == 0;
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
