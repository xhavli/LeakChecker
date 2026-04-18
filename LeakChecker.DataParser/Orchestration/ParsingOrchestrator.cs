using System.Diagnostics;
using System.Text;
using LeakChecker.DataParser.Content;
using LeakChecker.DataParser.Content.Parse;
using LeakChecker.DataParser.Format;
using LeakChecker.DataParser.Format.Detection;
using LeakChecker.DataParser.Helpers.Extensions;
using LeakChecker.DataParser.Helpers.Settings;
using LeakChecker.DataParser.Logging;
using LeakChecker.DataParser.Logging.Parse;
using LeakChecker.DataParser.Stats.Parse;

namespace LeakChecker.DataParser.Orchestration;

public class ParsingOrchestrator : IDisposable
{
    // Parsing state
    private long _linesRead;
    private long _recordsRead;
    private long _malformedRead;
    private long _readerPosition;
    // Data read
    private readonly Encoding _encoding;
    private readonly StreamReader _reader;
    // Logging and Statistics
    private readonly IParseStats _stats;
    private readonly IParseLogger _logger;
    // Default constants
    private static int _sqlSamplesLimit;
    private static int _csvSamplesLimit;
    private const long ParseLimit = long.MaxValue;
    private readonly int _schemaThreshold;

    private bool _possibleAsciiTable;

    public ParsingOrchestrator(string filePath, IParseLogger logger, IParseStats stats, ISettings settings)
    {
        _encoding = settings.DefaultUtf8;
        var stream = File.OpenRead(filePath);
        _reader = new StreamReader(stream, _encoding, detectEncodingFromByteOrderMarks: false);
        _logger = logger;
        _stats = stats;
        _schemaThreshold = settings.SchemaThreshold;
        _csvSamplesLimit = settings.CsvSamples;
        _sqlSamplesLimit = settings.SqlSamples;
    }

    public async Task ParseAsync()
    {
        await _logger.LogContentHeader();
        Stopwatch sw = Stopwatch.StartNew();

        while (await _reader.ReadLineWithEndingAsync() is { } originalLine)
        {
            if (_recordsRead >= ParseLimit)
                break;
            
            string line = originalLine.Trim();
            
            if (line.IsTrashOrEmpty())
            {
                _linesRead++;
                _readerPosition += _encoding.GetByteCount(originalLine);
                if (line.IsPossibleAsciiTable())
                    _possibleAsciiTable = true;
                
                continue;
            }

            if (line.IsSqlCreateTable())
            {
                _linesRead++;
                _readerPosition += _encoding.GetByteCount(originalLine);
                await SkipSqlCreateTable();
                _possibleAsciiTable = false;
                continue;
            }
            
            if (line.IsSqlInsert())
            {
                await ParseSqlInsert();
                _possibleAsciiTable = false;
                continue;
            }
            
            if (line.StartsWith('{') || line.StartsWith('[')) {} // looks like a JSON - test if it really is a JSON and parse it
            
            if (line.Contains("<html", StringComparison.OrdinalIgnoreCase) ||   // looks like an HTML
                line.Contains("<body", StringComparison.OrdinalIgnoreCase)) {}  // test if it really is an HTML and parse it

            // Fallback to CSV format
            await ParseCsv();
        }

        _stats.LinesRead = _linesRead;
        _stats.RecordsRead = _recordsRead;
        _stats.BytesRead = _readerPosition;
        _stats.MalformedRead = _malformedRead;

        await _logger.Log($"Content parsing finished successfully. Time taken: {sw.Elapsed}", LogLevel.Success, LogContext.Parsing);
    }

    private async Task ParseSqlInsert()
    {
        long sqlInsertStart = _readerPosition;
        
        // Reset reader before INSERT INTO
        _reader.AdjustPosition(sqlInsertStart);
        
        // Detect schema
        ParsingContext detectionContext = new ParsingContext
        {
            Reader = _reader,
            Logger = _logger,
            Stats = _stats,
            StartLine = _linesRead,
            SamplesLimit = _sqlSamplesLimit,
            Threshold = _schemaThreshold,
        };
        var schema = await SqlInsertDetector.DetectSchema(detectionContext);

        // Parse SQL INSERT block with header
        _reader.AdjustPosition(sqlInsertStart);
        ParsingContext parsingContext = new ParsingContext
        {
            Reader = _reader,
            Logger = _logger,
            Stats = _stats,
            Schema = schema,
            StartLine = _linesRead,
            ParseLimit = ParseLimit,
            MalformedLimit = _sqlSamplesLimit,
        };
        SqlInsertParser parser = new(parsingContext);
        ParsingResult result = await parser.Parse();
        UpdateParsingState(result);
        
        _stats.Formats.Add(FormatEnum.SqlInsert);
        _stats.Delimiters.Add(',');
    }

    private async Task ParseCsv()
    {
        long csvFormatStart = _readerPosition;

        // Detect delimiter
        char delimiter;
        var delimiterResult = DelimiterHeuristic.Analyze(_reader);
        if (delimiterResult.BestDelimiter.HasValue)
        { 
            delimiter = delimiterResult.BestDelimiter.Value;
            await _logger.LogDelimiterHeuristic(delimiterResult);
        }
        else
        {
            delimiter = ':';
            await _logger.Log($"Delimiter detection failed. Setting default delimiter [{delimiter}]", LogLevel.Warning, LogContext.Delimiter);
        }

        // Reset reader before CSV start
        _reader.AdjustPosition(csvFormatStart);
        
        // Detect schema
        ParsingContext detectionContext = new ParsingContext
        {
            Reader = _reader,
            Logger = _logger,
            Stats = _stats,
            Delimiter = delimiter,
            StartLine = _linesRead,
            SamplesLimit = _csvSamplesLimit,
            Threshold = _schemaThreshold,
        };
        var schema = await CsvDetector.DetectSchema(detectionContext);
        
        // Parse CSV file
        _reader.AdjustPosition(csvFormatStart);
        ParsingContext parsingContext = new ParsingContext
        {
            Reader = _reader,
            Logger = _logger,
            Stats = _stats,
            Schema = schema,
            Delimiter = delimiter,
            StartLine = _linesRead,
            ParseLimit = ParseLimit,
            MalformedLimit = _csvSamplesLimit,
        };
        CsvParser parser = new(parsingContext);
        ParsingResult result = await parser.Parse();
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

    private void UpdateParsingState(ParsingResult result)
    {
        _linesRead += result.LinesRead;
        _recordsRead += result.RecordsRead;
        _readerPosition += result.BytesRead;
        _malformedRead += result.MalformedRead;

        // Obfuscation fix caused by mojibake and encoding errors
        if (_readerPosition <= _reader.BaseStream.Length)
        {
            _reader.AdjustPosition(_readerPosition);
        }
        else
        {
            
            _logger.Log($"UpdateParsingState _readerPosition is greater than file size in bytes. {_readerPosition} - {_reader.BaseStream.Length} " +
                        $"= {_readerPosition - _reader.BaseStream.Length} bytes. Setting BaseStream.Length", LogLevel.Warning, LogContext.Parsing);
            _readerPosition = _reader.BaseStream.Length;
        }
    }
    
    public void Dispose()
    {
        _reader.Dispose();
        _reader.BaseStream.Dispose();
    }
}