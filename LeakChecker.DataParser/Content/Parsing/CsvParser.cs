using System.Text;
using LeakChecker.DataParser.Logging;
using LeakChecker.DataParser.Logging.Parse;
using LeakChecker.DataParser.Utilities.Extensions;

namespace LeakChecker.DataParser.Content.Parsing;

public class CsvParser(ParsingContext parsingContext)
{
    private readonly IParseLogger _logger = parsingContext.Logger;
    private readonly Dictionary<int, ItemEnum> _schema = parsingContext.Schema;
    
    public async Task<ParsingState> ProcessFile()
    {
        StreamReader reader = parsingContext.Reader;
        long startLine = parsingContext.StartLine;
        char delimiter = parsingContext.Delimiter;
        long parseLimit = parsingContext.ParseLimit;
        int malformedLimit = parsingContext.MalformedLimit;
        
        Encoding encoding = reader.CurrentEncoding;
        int expectedFields = _schema.Count == 0 ? 0 : _schema.Keys.Max() + 1;
        
        int malformedRecordsSequence = 0;
        int malformedRecordsRead = 0;
        long recordsRead = 0;
        long linesRead = 0;
        long bytesRead = 0;

        while (await reader.ReadLineWithEndingAsync() is { } line)
        {
            linesRead++;
            bytesRead += encoding.GetByteCount(line);
        
            line = line.Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;

            // Console.WriteLine($"\nCSV file parsing line {startLine + linesRead}: {line}");

            string[] row = line.Split(delimiter);
            
            if (row.Length != expectedFields)
            {
                await _logger.Log($"Bad row length at line {startLine + linesRead}: expected {expectedFields}, got {row.Length} content: {line}", LogLevel.Warning);
                malformedRecordsRead++;
                malformedRecordsSequence++;
                if (malformedRecordsSequence >= malformedLimit)
                {
                    await _logger.Log($"Parsing reach malformed limit {malformedLimit}. Returning back to recompute schema", LogLevel.Warning, LogContext.Parsing);
                    break;
                }
                continue;
            }

            await ParseRow(delimiter.ToString(), row);
            malformedRecordsSequence = 0;
            recordsRead++;

            if (recordsRead == parseLimit) break;
        }
        
        return new ParsingState
        {
            MalformedRecordsRead = malformedRecordsRead,
            RecordsRead = recordsRead,
            LinesRead = linesRead,
            BytesRead = bytesRead,
        };
    }

    private async Task ParseRow(string delimiter, string[] row)
    {
        Dictionary<ItemEnum, List<string>> record = new();
        
        int i = 0;
        while (i < row.Length)
        {
            string value = row[i].Trim();
            if (string.IsNullOrWhiteSpace(value))
            {
                i++;
                continue;
            }

            if (!_schema.TryGetValue(i, out var itemType))
            {
                // No schema for this field -> log warning
                await _logger.Log($"Unmapped CSV field[{i}] = {value}", LogLevel.Warning, LogContext.Parsing);
                //todo exception to higher logic to search for new pattern
                i++;
                continue;
            }
            
            int nextIndex = i + 1;
            while (nextIndex < row.Length && 
                   _schema.TryGetValue(nextIndex, out var nextType) && 
                   nextType == ItemEnum.Previous)
            {
                string nextValue = row[nextIndex].Trim();
                if (!string.IsNullOrEmpty(nextValue))
                {
                    value += delimiter;
                    value += nextValue;
                }
                nextIndex++;
            }

            if (!record.TryGetValue(itemType, out var list))
            {
                list = new List<string>();
                record[itemType] = list;
            }

            list.Add(value);
            
            // TODO: forward to content storage
            // Console.WriteLine($"[{i}] {schemaEntry} = {value}");
            
            i = nextIndex;
        }
    }
}
