using System.Diagnostics;
using System.Globalization;
using System.Text;
using LeakChecker.Format;
using LeakChecker.Format.Detection;
using LeakChecker.Logging;
using LeakChecker.Logging.FileLogging;
using LeakChecker.Utilities.Extensions;

namespace LeakChecker.Content.Parsing;

public class ContentParser : IDisposable
{
    // Parsing state
    private long _malformedRecordsRead;
    private long _recordsRead;
    private long _linesRead;
    private long _readerPosition;
    // Data read
    private readonly Encoding _encoding;
    private readonly StreamReader _reader;
    // Logging and Statistics
    private readonly Stopwatch _sw = new();
    private readonly IFileLogger _logger;
    private readonly FileStats _stats;
    // Default constants
    private const int SqlSamplesLimit = 31;
    private const int CsvSamplesLimit = 103;
    // private const long ParseLimit = 150;
    private const long ParseLimit = long.MaxValue;
    private readonly int _thresholdPercent;

    private bool _possibleAsciiTable;

    private ContentParser(int thresholdPercent, Encoding encoding, StreamReader reader, IFileLogger logger, FileStats stats)
    {
        _thresholdPercent = thresholdPercent;
        _encoding = encoding;
        _reader = reader;
        _logger = logger;
        _stats = stats;
    }

    public static async Task<ContentParser> CreateAsync(IFileLogger logger, FileStats stats, Encoding? encoding, int thresholdPercent)
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
        
        return new ContentParser(thresholdPercent, encoding, reader, logger, stats);
    }
    
    public async Task ProcessFile()
    {
        _sw.Start();

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
                _possibleAsciiTable = false;
                continue;
            }
            
            if (IsSqlInsertValues(line))
            {
                await ProcessSqlInsert();
                _possibleAsciiTable = false;
                continue;
            }
            
            if (line.StartsWith('{') || line.StartsWith('[')) {} // looks like a JSON - test if it really is a json and parse it
            
            if (line.Contains("<html", StringComparison.OrdinalIgnoreCase) ||   // looks like an HTML
                line.Contains("<body", StringComparison.OrdinalIgnoreCase)) {}  // test if it really is a html and parse it

            // Fallback
            await ProcessCsvFile();
        }

        _stats.MalformedRecordsRead = _malformedRecordsRead;
        _stats.RecordsRead = _recordsRead;
        _stats.LinesRead = _linesRead;
        _stats.BytesRead = _readerPosition;

        await _logger.Log($"Content processing finished successfully. Time taken: {_sw.Elapsed}", LogLevel.Success, LogContext.Content);
    }

    private async Task ProcessSqlInsert()
    {
        long sqlInsertStart = _readerPosition;
        
        // Reset reader before INSERT INTO
        _reader.AdjustPosition(sqlInsertStart);
        
        // Detect schema
        Stopwatch sw = Stopwatch.StartNew();
        var schema = await SqlInsertDetector.DetectFormat(_linesRead, _reader, _logger, SqlSamplesLimit, _thresholdPercent);
        await _logger.Log($"SQL INSERT schema created in {sw.Elapsed}\n");

        // Parse SQL INSERT block with header
        _reader.AdjustPosition(sqlInsertStart);
        SqlInsertParser parser = new(schema, _reader, _logger);
        ParsingState result = await parser.ProcessSqlInsert(_linesRead, SqlSamplesLimit, ParseLimit);
        UpdateParsingState(result);
        
        _stats.Formats.Add(FormatEnum.SqlInsert);
        _stats.Delimiters.Add(',');
    }

    private async Task ProcessCsvFile()
    {
        long csvFormatStart = _readerPosition;

        // Detect delimiter
        char delimiter;
        var delimiterResult = DelimiterHeuristic.Analyze(_reader);
        if (delimiterResult.BestDelimiter.HasValue)
        { 
            delimiter = delimiterResult.BestDelimiter.Value;
            await _logger.LogDelimiterHeuristic(delimiterResult, count: 5);
        }
        else
        {
            delimiter = ':';
            await _logger.Log($"Delimiter detection failed. Setting default delimiter [{delimiter}]", LogLevel.Warning, LogContext.Delimiter);
        }

        // Reset reader before CSV start
        _reader.AdjustPosition(csvFormatStart);
        
        // Detect schema
        Stopwatch sw = Stopwatch.StartNew();
        var schema = await CsvFileDetector.DetectFormat(_linesRead, delimiter, _reader, _logger, CsvSamplesLimit, _thresholdPercent);
        await _logger.Log($"CSV file schema created in {sw.Elapsed}\n");
        
        // Parse CSV file
        _reader.AdjustPosition(csvFormatStart);
        CsvParser csvParser = new(schema, _reader, _logger);
        ParsingState result;
        if (schema.Values.All(v => v == ItemEnum.Other) || schema.Count == 0)    // if nothing reliable detected
        {
            result = await csvParser.ProcessCsvFile(_linesRead, delimiter, CsvSamplesLimit, CsvSamplesLimit);
            result.LinesRead = 0;
            result.RecordsRead = 0;
            result.MalformedRecordsRead = CsvSamplesLimit;
            UpdateParsingState(result);
            
            return;
        }
        result = await csvParser.ProcessCsvFile(_linesRead, delimiter, CsvSamplesLimit, ParseLimit);
        UpdateParsingState(result);
        
        if (_possibleAsciiTable && delimiter == '|')
        {
            _stats.Formats.Add(FormatEnum.AsciiTable);
        }
        else
        { 
            _stats.Formats.Add(FormatEnum.Csv);
        }
        _stats.Delimiters.Add(delimiter);
        _possibleAsciiTable = false;
    }

    // INSERT [modifiers] INTO <table_name> [ (columns...) ] VALUES
    // ( literal , literal , literal , literal ),
    // ( ... );
    private static bool IsSqlInsertValues(string line)
    {
        var start = line.IndexOf("INSERT ", StringComparison.OrdinalIgnoreCase);
        if (start != 0)
            return false;

        var into = line.IndexOf(" INTO ", StringComparison.OrdinalIgnoreCase);
        if (into < 0)
            return false;

        var values = line.IndexOf(" VALUES", StringComparison.OrdinalIgnoreCase);
        if (values < 0)
            return false;

        return start < into && into < values;
    }
    
    // CREATE [modifiers] TABLE [modifiers] <table_name>
    // ( <column> <data_type> [constraint] ,
    //   <column> <data_type> [constraint] ,
    //   ...
    // ) [options];
    private static bool IsSqlCreateTable(string line)
    {
        var create = line.IndexOf("CREATE ", StringComparison.OrdinalIgnoreCase);
        if (create != 0)
            return false;

        var table = line.IndexOf(" TABLE ", StringComparison.OrdinalIgnoreCase);
        if (table < 0)
            return false;

        return create < table;
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
            line = line.Trim();

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

    private bool IsTrashOrEmpty(string line)
    {
        line = line.Trim();
        if (string.IsNullOrWhiteSpace(line)) return true;

        if (line.First() == line.Last() &&
            line.All(c => c == '+' || char.GetUnicodeCategory(c) == UnicodeCategory.DashPunctuation))
        {
            _possibleAsciiTable = true;
            return true;
        }
        
        if (line.LastIndexOf("--", StringComparison.Ordinal) == 0 && _stats.Formats.Last() == FormatEnum.SqlInsert) return true;    // Sql comment
        if (line == ";" && _stats.Formats.Last() == FormatEnum.SqlInsert) return true;
        
        return line.Replace(" ", "").All(ch => char.GetUnicodeCategory(ch) == UnicodeCategory.DashPunctuation 
                                     && _stats.Formats.Last() == FormatEnum.SqlInsert); // Sql comment boundary
    }

    private void UpdateParsingState(ParsingState state)
    {
        _malformedRecordsRead += state.MalformedRecordsRead;
        _recordsRead += state.RecordsRead;
        _linesRead += state.LinesRead;
        _readerPosition += state.BytesRead;
    }
    
    public void Dispose()
    {
        _reader.Dispose();
        _reader.BaseStream.Dispose();
    }
}