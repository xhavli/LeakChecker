using System.Text;
using LeakChecker.Logging;
using LeakChecker.Logging.Parse;
using LeakChecker.Utilities.Extensions;

namespace LeakChecker.Content.Parsing;

public class CsvParser(Dictionary<int, ItemEnum> schema, StreamReader reader, IParseLogger logger)
{
    public async Task<ParsingState> ProcessCsvFile(long startLine, char delimiter, int malformedLimit, long parseLimit)
    {
        Encoding encoding = reader.CurrentEncoding;
        int expectedFields = schema.Count == 0 ? 0 : schema.Keys.Max() + 1;
        
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
                await logger.Log($"Bad row length at line {startLine + linesRead}: expected {expectedFields}, got {row.Length} content: {line}", LogLevel.Warning);
                malformedRecordsRead++;
                malformedRecordsSequence++;
                if (malformedRecordsSequence >= malformedLimit)
                {
                    await logger.Log($"Parsing reach malformed limit {malformedLimit}. Returning back to recompute schema", LogLevel.Warning, LogContext.Parsing);
                    break;
                }
                continue;
            }

            await ParseRow(delimiter, row);
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

    private async Task ParseRow(char delimiter, string[] row)
    {
        int i = 0;
        
        while (i < row.Length)
        {
            string value = row[i].Trim();

            if (schema.TryGetValue(i, out var schemaEntry))
            {
                int nextIndex = i + 1;
                while (nextIndex < row.Length && schema.TryGetValue(nextIndex, out var cont) && cont == ItemEnum.Previous)
                {
                    string nextVal = row[nextIndex].Trim();
                    if (!string.IsNullOrEmpty(nextVal))
                        value += delimiter + nextVal;
                    nextIndex++;
                }

                // TODO: forward to content storage
                // Console.WriteLine($"[{i}] {schemaEntry} = {value}");
                i = nextIndex;
            }
            else
            {
                // No schema for this field -> log warning
                await logger.Log($"Unmapped CSV field[{i}] = {value}", LogLevel.Warning, LogContext.Parsing);
                //todo exception to higher logic to search for new pattern
                i++;
            }
        }
    }
}
