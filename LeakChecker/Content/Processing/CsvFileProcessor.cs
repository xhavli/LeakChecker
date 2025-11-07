using System.Text;
using LeakChecker.Logging;
using LeakChecker.Logging.FileLogging;
using LeakChecker.Utilities.Extensions;

namespace LeakChecker.Content.Processing;

public class CsvFileProcessor(Dictionary<int, ItemEnum> schema, IFileLogger logger)
{
    public async Task<(long recordProcessed, long bytesRead, long linesRead)> ProcessCsvFile(
        StreamReader reader, char delimiter = ':')
    {
        int expectedFields = schema.Count == 0 ? 0 : schema.Keys.Max() + 1;
        long bytesRead = 0, linesRead = 0, recordsProcessed = 0;
        Encoding encoding = reader.CurrentEncoding;

        while (await reader.ReadLineWithEndingAsync() is { } line)
        {
            // bytesRead += encoding.GetByteCount(line);
            int lineBytes = encoding.GetByteCount(line);
            linesRead++;
        
            line = line.ReplaceLineEndings("").Trim();
            if (string.IsNullOrWhiteSpace(line) || string.IsNullOrEmpty(line)) { continue; }

            Console.WriteLine($"CSV file line {recordsProcessed} processing: {line}");

            string[] row = SplitCsvLine(line, delimiter);
            
            int actualFields = row.Length;
            if (actualFields != expectedFields)
            {
                await logger.Log($"Bad row length: actual {actualFields}, expected {expectedFields}, line {line}", LogLevel.Warning);
                // return (recordsProcessed, bytesRead, linesRead); //need to be tested
                continue;
            }

            await ProcessRow(delimiter, row);
            bytesRead += lineBytes;
            recordsProcessed++;

            Console.WriteLine();
            if (recordsProcessed == 150) break; //TODO remove
        }

        return (recordsProcessed, bytesRead, linesRead);
    }

    private async Task ProcessRow(char delimiter, string[] row)
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
                Console.WriteLine($"[{i}] {schemaEntry} = {value}");
                i = nextIndex;
            }
            else
            {
                // No schema for this field -> log warning
                string warnVal = row[i].Trim();
                await logger.Log($"Unmapped CSV field[{i}] = {warnVal}", LogLevel.Warning, LogContext.Processing);
                //todo exception to higher logic to search for new pattern
                i++;
            }
        }
    }

    private static string[] SplitCsvLine(string line, char delimiter)
    {
        List<string> result = new();
        StringBuilder current = new();
        bool inQuote = false;

        foreach (char c in line)
        {
            if (c == '"')
            {
                inQuote = !inQuote;
                continue;
            }

            if (c == delimiter && !inQuote)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        result.Add(current.ToString());
        return result.ToArray();
    }
}
