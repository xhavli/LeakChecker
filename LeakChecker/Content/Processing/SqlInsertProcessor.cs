using System.Text;
using LeakChecker.Content.Detection;
using LeakChecker.Format.Detection;
using LeakChecker.Logging;
using LeakChecker.Logging.FileLogging;
using LeakChecker.Utilities.Extensions;

namespace LeakChecker.Content.Processing;

public class SqlInsertProcessor(Dictionary<int, ItemEnum> schema, StreamReader reader, IFileLogger logger)
{
    public async Task<(long recordProcessed, long bytesRead, long linesRead)> ProcessSqlInsert()
    {
        int expectedFields = schema.Count == 0 ? 0 : schema.Keys.Max() + 1;
        long bytesRead = 0, linesRead = 0, recordsProcessed = 0;
        StringBuilder tupleBuilder = new();
        Encoding encoding = reader.CurrentEncoding;

        int parenDepth = 0;
        bool afterValues = false;
        bool inQuote = false;

        while (await reader.ReadLineWithEndingAsync() is { } line)
        {
            bytesRead += encoding.GetByteCount(line);
            line = line.ReplaceLineEndings("").Trim();
            linesRead++;

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
                            
                            Console.WriteLine();
                            Console.WriteLine($"SQL insert {recordsProcessed} processing: {tuple}");
                            string[] row = SqlInsertDetector.ParseTuple(tuple);
                            
                            int actualFields = row.Length;
                            if (actualFields != expectedFields)
                            {
                                await logger.Log($"Bad row length: actual {actualFields}, expected {expectedFields}, line {line}", LogLevel.Warning);
                                // return (recordsProcessed, bytesRead, linesRead); //return in the middle
                                continue;
                            }
                            
                            await ProcessRow(row);
                            recordsProcessed++;

                            if (recordsProcessed == 150) return (recordsProcessed, bytesRead, linesRead);
                            
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

        return (recordsProcessed, bytesRead, linesRead);
    }

    private async Task ProcessRow(string[] row)
    {
        for (int i = 0; i < row.Length; i++)
        {
            string value = row[i];

            if (!schema.TryGetValue(i, out var schemaEntry))
            {
                await logger.Log($"Unmapped field[{i}] = {value}", LogLevel.Warning, LogContext.Processing);
                //todo exception to higher logic for non valid sql insert and search for new pattern
                continue;
            }

            // TODO: forward to content storage
            Console.WriteLine($"[{i}] {schemaEntry} = {value}");
        }
    }
}