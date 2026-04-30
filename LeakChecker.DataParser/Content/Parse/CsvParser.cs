using System.Text;
using LeakChecker.Common.Enums;
using LeakChecker.DataParser.Database;
using LeakChecker.DataParser.Helpers.Extensions;
using LeakChecker.DataParser.Logging;
using LeakChecker.DataParser.Logging.Parse;
using MongoDB.Bson;

namespace LeakChecker.DataParser.Content.Parse;

public class CsvParser(ParsingContext parsingContext)
{
    private readonly char _delimiter = parsingContext.Delimiter;
    private readonly ObjectId _parseId = parsingContext.Stats.ParseId;
    private readonly IParseLogger _logger = parsingContext.Logger;
    private readonly Dictionary<int, ItemEnum> _schema = parsingContext.Schema;
    private readonly List<Dictionary<ItemEnum, List<string>>> _cachedRecords = new(2001);
    private readonly IDatabase _database = parsingContext.Settings.Database;

    private const int FlushThreshold = 2000;

    public async Task<ParsingResult> Parse()
    {
        StreamReader reader = parsingContext.Reader;
        long startLine = parsingContext.StartLine;
        long parseLimit = parsingContext.ParseLimit;
        int malformedLimit = parsingContext.MalformedLimit;
        
        Encoding encoding = reader.CurrentEncoding;
        int expectedFields = _schema.Count == 0 ? 0 : _schema.Keys.Max() + 1;
        
        long linesRead = 0;
        long bytesRead = 0;
        long recordsRead = 0;
        int malformedRead = 0;
        int malformedReadSequence = 0;
        
        while (await reader.ReadLineWithEndingAsync() is { } line)
        {
            if (line.IsSqlInsert())
            {
                await _logger.Log($"Detected SQL Insert while parsing CSV file on line {startLine + linesRead}: {line}. " +
                                  $"Returning back to recompute schema.", LogLevel.Warning, LogContext.Parsing);
                break;
            }
            
            linesRead++;
            bytesRead += encoding.GetByteCount(line);
        
            if (string.IsNullOrWhiteSpace(line))
                continue;

            line = line.Trim();
            string[] row = line.Split(_delimiter);
            
            if (row.Length != expectedFields)
            {
                await _logger.Log($"Bad row length at line {startLine + linesRead}: expected {expectedFields}, " +
                                  $"got {row.Length} content: {line}", LogLevel.Warning);

                malformedRead++;
                if (++malformedReadSequence >= malformedLimit)
                {
                    await _logger.Log($"Parsing reach malformed limit {malformedLimit}. " +
                                      $"Returning back to recompute schema.", LogLevel.Warning, LogContext.Parsing);
                    break;
                }
                
                continue;
            }

            await ParseRow(row);
            if (_cachedRecords.Count > FlushThreshold)
            {
                await _database.SaveIdentityMany(_cachedRecords, _parseId);
                _cachedRecords.Clear();
            }
            
            recordsRead++;
            malformedReadSequence = 0;

            if (recordsRead == parseLimit)
                break;
        }
        
        await _database.SaveIdentityMany(_cachedRecords, _parseId);
        
        return new ParsingResult
        {
            LinesRead = linesRead,
            BytesRead = bytesRead,
            RecordsRead = recordsRead,
            MalformedRead = malformedRead,
        };
    }

    private async Task ParseRow(string[] row)
    {
        Dictionary<ItemEnum, List<string>> record = new();
        
        int i = 0;
        while (i < row.Length)
        {
            string raw = row[i];
            if (string.IsNullOrWhiteSpace(raw))
            {
                i++;
                continue;
            }

            if (!_schema.TryGetValue(i, out ItemEnum itemType))
            {
                await _logger.Log($"Unmapped CSV field[{i}] = {raw.Trim()}", LogLevel.Warning, LogContext.Parsing);
                i++;
                continue;
            }
            
            string value = BuildValue(row, i, out int nextIndex);

            if (!record.TryGetValue(itemType, out var list))
                record[itemType] = list = new List<string>();

            list.Add(value);
            i = nextIndex;
        }

        // Forward to content storage
        _cachedRecords.Add(record);
    }
    
    private string BuildValue(string[] row, int startIndex, out int nextIndex)
    {
        string firstValue = row[startIndex].Trim();
        StringBuilder? sb = null;

        nextIndex = startIndex + 1;
        while (nextIndex < row.Length &&
               _schema.TryGetValue(nextIndex, out ItemEnum nextType) &&
               nextType == ItemEnum.Previous)
        {
            string nextValue = row[nextIndex].Trim();
            if (!string.IsNullOrWhiteSpace(nextValue))
            {
                sb ??= new StringBuilder(firstValue);
                sb.Append(_delimiter);
                sb.Append(nextValue);
            }

            nextIndex++;
        }

        return sb?.ToString() ?? firstValue;
    }
}
