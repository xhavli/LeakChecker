using System.Text;
using LeakChecker.Logging;
using LeakChecker.Logging.FileLogging;
using LeakChecker.Utilities.Extensions;

namespace LeakChecker.Content.Processing;

public class CsvFileProcessor(Dictionary<int, (ItemEnum Attribute, int DelimiterSpan)> schema)
{
    public async Task<(long recordProcessed, long bytesRead)> ProcessCsvFile(
        StreamReader reader, FileLogger logger, char delimiter = ':')
    {
        long bytesRead = 0;
        long recordsProcessed = 0;
        Encoding encoding = reader.CurrentEncoding;

        while (await reader.ReadLineWithEndingAsync() is { } line)
        {
            bytesRead += encoding.GetByteCount(line);
            recordsProcessed++;
        
            line = line.ReplaceLineEndings("").Trim();
            if (string.IsNullOrWhiteSpace(line) || string.IsNullOrEmpty(line)) { continue; }

            Console.WriteLine($"CSV file line {recordsProcessed} processing: {line}");

            string[] fields = SplitCsvLine(line, delimiter);
            await ProcessRecord(delimiter, fields, logger);

            Console.WriteLine();
            if (recordsProcessed == 150) break;
        }

        return (recordsProcessed, bytesRead);
    }

    private async Task ProcessRecord(char delimiter, string[] fields, FileLogger logger)
    {
        int i = 0;
        while (i < fields.Length)
        {
            string value = fields[i].Trim();

            // If current field has schema
            if (schema.TryGetValue(i, out var schemaEntry))
            {
                // Merge undefined fields that follow
                int nextIndex = i + 1;
                while (nextIndex < fields.Length && !schema.ContainsKey(nextIndex))
                {
                    string nextVal = fields[nextIndex].Trim();
                    if (!string.IsNullOrEmpty(nextVal))
                        value += delimiter + nextVal;
                    nextIndex++;
                }

                Console.WriteLine($"[{i}] {schemaEntry.Attribute} = {value}");
                i = nextIndex;
            }
            else
            {
                // No schema for this field -> log warning
                string warnVal = fields[i].Trim();
                await logger.Log($"Unmapped CSV field[{i}] = {warnVal}", LogLevel.Warning, LogContext.Processing);
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
