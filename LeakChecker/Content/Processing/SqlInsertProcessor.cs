using System.Text;
using LeakChecker.Content.Detection;
using LeakChecker.Format.Detection;
using LeakChecker.Logging;
using LeakChecker.Logging.FileLogging;
using LeakChecker.Utilities.Extensions;

namespace LeakChecker.Content.Processing;

public class SqlInsertProcessor(
    Dictionary<int, (ItemEnum Attribute, int DelimiterSpan)> schema, StreamReader reader, IFileLogger logger)
{
    public async Task<(long recordProcessed, long bytesRead)> ProcessSqlInsert()
    {
        int parenDepth = 0;
        long bytesRead = 0;
        long recordsProcessed = 0;
        StringBuilder tupleBuilder = new();
        Encoding encoding = reader.CurrentEncoding;

        bool afterValues = false;
        bool inQuote = false;

        while (await reader.ReadLineWithEndingAsync() is { } line)
        {
            bytesRead += encoding.GetByteCount(line);
            line = line.ReplaceLineEndings("").Trim();

            // string line = line.Trim();

            // Detect when VALUES starts
            if (!afterValues)
            {
                int valuesPos = line.IndexOf("VALUES", StringComparison.OrdinalIgnoreCase);
                if (valuesPos >= 0)
                {
                    // start processing *after* VALUES keyword
                    line = line.Substring(valuesPos + 6);
                    afterValues = true;
                }
                else
                {
                    // still in header -> skip line
                    continue;
                }
            }

            // Normal tuple parsing (respects inQuote)
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '\'')
                {
                    if (inQuote && i + 1 < line.Length && line[i + 1] == '\'')
                    {
                        tupleBuilder.Append('\''); // escaped quote
                        i++;
                        continue;
                    }
                    inQuote = !inQuote;
                    tupleBuilder.Append(c);
                    continue;
                }

                if (!inQuote)
                {
                    if (c == '(')
                    {
                        if (parenDepth == 0) tupleBuilder.Clear();
                        tupleBuilder.Append(c);
                        parenDepth++;
                        continue;
                    }
                    
                    if (c == ')')
                    {
                        tupleBuilder.Append(c);
                        parenDepth--;

                        if (parenDepth == 0)
                        {
                            string tuple = tupleBuilder.ToString().Trim(',', ';', ' ');
                            recordsProcessed++;
                            
                            Console.WriteLine();
                            Console.WriteLine($"SQL insert {recordsProcessed} processing: {tuple}");
                            string[] row = SqlInsertDetector.ParseTuple(tuple);
                            await ProcessRow(row);

                            if (recordsProcessed == 150) return (recordsProcessed, bytesRead);
                            
                            tupleBuilder.Clear();
                        }
                        continue;
                    }
                }

                if (parenDepth > 0 || inQuote)
                {
                    tupleBuilder.Append(c);
                }
            }

            // Stop when end of insert block is reached
            if (line.EndsWith(");", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }
        }

        return (recordsProcessed, bytesRead);
    }

    private async Task ProcessRow(string[] row)
    {
        for (int i = 0; i < row.Length; i++)
        {
            string value = row[i];

            if (!schema.TryGetValue(i, out var schemaEntry))
            {
                await logger.Log($"Unmapped field[{i}] = {value}", LogLevel.Warning, LogContext.Processing);
                continue;
            }

            // TODO: forward to content storage
            Console.WriteLine($"[{i}] {schemaEntry.Attribute} = {value}");
        }
    }
}