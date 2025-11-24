using System.Text;
using LeakChecker.Logging;
using LeakChecker.Logging.FileLogging;
using LeakChecker.Utilities.Extensions;

namespace LeakChecker.Content.Processing;

public class CsvFileProcessor(Dictionary<int, ItemEnum> schema, IFileLogger logger)
{
    public async Task<ParsingState> ProcessCsvFile(long startLine, char delimiter, StreamReader reader, long parseLimit)
    {
        Encoding encoding = reader.CurrentEncoding;
        int expectedFields = schema.Count == 0 ? 0 : schema.Keys.Max() + 1;
        
        long recordsRead = 0;
        long linesRead = 0;
        long bytesRead = 0;

        while (await reader.ReadLineWithEndingAsync() is { } line)
        {
            linesRead++;
            int lineBytes = encoding.GetByteCount(line);
        
            line = line.TrimOuterWhiteSpace();
            if (string.IsNullOrWhiteSpace(line)) { continue; }

            Console.WriteLine($"CSV file parsing line {startLine + linesRead}: {line}");

            string[] row = line.Split(delimiter);
            
            int actualFields = row.Length;
            if (actualFields != expectedFields)
            {
                await logger.Log($"Bad row length on line {startLine + linesRead}: expected {expectedFields}, got {actualFields} content: {line}", LogLevel.Warning);
                // todo return (recordsProcessed, bytesRead, linesRead); //need to be tested
                continue;
            }

            await ParseRow(delimiter, row);
            bytesRead += lineBytes;
            recordsRead++;

            if (recordsRead == parseLimit) break;
        }
        
        return new ParsingState
        {
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
                string warnVal = row[i].Trim();
                await logger.Log($"Unmapped CSV field[{i}] = {warnVal}", LogLevel.Warning, LogContext.Parsing);
                //todo exception to higher logic to search for new pattern
                i++;
            }
        }
    }
}
