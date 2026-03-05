using System.Diagnostics;
using System.Text;
using LeakChecker.DataParser.Format;
using LeakChecker.DataParser.Format.Detection;
using LeakChecker.DataParser.Logging;
using LeakChecker.DataParser.Logging.Parse;
using LeakChecker.DataParser.Utilities.Extensions;

namespace LeakChecker.DataParser.Content.Parsing;

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
    private readonly IParseLogger _logger;
    private readonly ParseStats _stats;
    // Default constants
    private const int SqlSamplesLimit = 31;
    private const int CsvSamplesLimit = 103;
    // private const long ParseLimit = 150;
    private const long ParseLimit = long.MaxValue;
    private readonly int _thresholdPercent;

    private bool _possibleAsciiTable;

    public ContentParser(string filePath, IParseLogger logger, ParseStats stats, int thresholdPercent)
    {
        _encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        _reader = new StreamReader(stream, _encoding, detectEncodingFromByteOrderMarks: false);
        _logger = logger;
        _stats = stats;
        _thresholdPercent = thresholdPercent;
    }

    public async Task ProcessFile()
    {
        await _logger.LogContentHeader();
        _sw.Start();

        while (await _reader.ReadLineWithEndingAsync() is { } originLine)
        {
            if (_recordsRead >= ParseLimit) break;
            
            string line = originLine.Trim();
            
            if (line.IsTrashOrEmpty())
            {
                _linesRead++;
                _readerPosition += _encoding.GetByteCount(originLine);
                if (line.IsPossibleAsciiTable()) _possibleAsciiTable = true;
                continue;
            }

            if (line.IsSqlCreateTable())
            {
                _linesRead++;
                _readerPosition += _encoding.GetByteCount(originLine);
                await SkipSqlCreateTable();
                _possibleAsciiTable = false;
                continue;
            }
            
            if (line.IsSqlInsertValues())
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
        var schema = await SqlInsertDetector.DetectFormat(_linesRead, _reader, _logger, SqlSamplesLimit, _thresholdPercent);

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
        var schema = await CsvFileDetector.DetectFormat(_linesRead, delimiter, _reader, _logger, CsvSamplesLimit, _thresholdPercent);
        
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
            _stats.Formats.Add(FormatEnum.AsciiTable);
        else
            _stats.Formats.Add(FormatEnum.Csv);
        _stats.Delimiters.Add(delimiter);
    }

    private async Task SkipSqlCreateTable()
    {
        bool inSingle = false;  // `identifier` for MySQL
        bool inDouble = false;  // "identifier" for PostgreSQL
        bool inBracket = false; // [identifier] for SQL Server, T-SQL
        bool inBacktick = false;

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