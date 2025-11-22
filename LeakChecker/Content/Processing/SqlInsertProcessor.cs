using System.Text;
using LeakChecker.Format.Detection;
using LeakChecker.Logging;
using LeakChecker.Logging.FileLogging;
using LeakChecker.Utilities.Extensions;

namespace LeakChecker.Content.Processing;

public class SqlInsertProcessor(Dictionary<int, ItemEnum> schema, StreamReader reader, IFileLogger logger)
{
    public async Task<ParsingState> ProcessSqlInsert(long startLine)
    {
        StringBuilder stringBuilder = new();
        Encoding encoding = reader.CurrentEncoding;
        int expectedFields = schema.Count == 0 ? 0 : schema.Keys.Max() + 1;

        int parenDepth = 0;
        bool inQuote = false;
        bool afterValues = false;
        
        long recordsRead = 0;
        long linesRead = 0;
        long bytesRead = 0;
        
        while (await reader.ReadLineWithEndingAsync() is { } line)
        {
            recordsRead++;
            linesRead++;
            bytesRead += encoding.GetByteCount(line);
            line = line.ReplaceLineEndings("").Trim();
            
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
                        stringBuilder.Append('\''); // escaped quote
                        i++;
                        continue;
                    }
                    inQuote = !inQuote;
                    stringBuilder.Append(c);
                    continue;
                }

                if (!inQuote)
                {
                    if (c == '(')
                    {
                        if (parenDepth == 0) stringBuilder.Clear();
                        stringBuilder.Append(c);
                        parenDepth++;
                        continue;
                    }
                    
                    if (c == ')')
                    {
                        stringBuilder.Append(c);
                        parenDepth--;

                        if (parenDepth == 0)
                        {
                            string tuple = stringBuilder.ToString().Trim(',', ';', ' ');
                            
                            Console.WriteLine();
                            Console.WriteLine($"SQL insert parsing line {startLine + linesRead}: {tuple}");
                            string[] row = SqlInsertDetector.ParseTuple(tuple);
                            
                            int actualFields = row.Length;
                            if (actualFields != expectedFields)
                            {
                                await logger.Log($"Bad row length: actual {actualFields}, expected {expectedFields}, " +
                                                 $"line {startLine + linesRead}: {line}", LogLevel.Warning);
                                // return (recordsProcessed, bytesRead, linesRead); //return in the middle
                                continue;
                            }
                            
                            await ParseRow(row);

                            // if (recordsProcessed == 150) return (recordsProcessed, bytesRead, linesRead);
                            
                            stringBuilder.Clear();
                        }
                        continue;
                    }
                }

                if (parenDepth > 0 || inQuote)
                {
                    stringBuilder.Append(c);
                }
            }

            // Stop when end of insert block is reached
            if (line.EndsWith(");") || line.EndsWith(") ;") || line.EndsWith(")\t;"))
                break;
        }

        return new ParsingState
        {
            RecordsRead = recordsRead,
            LinesRead = linesRead,
            BytesRead = bytesRead,
        };
    }

    private async Task ParseRow(string[] row)
    {
        for (int i = 0; i < row.Length; i++)
        {
            string value = row[i];

            if (!schema.TryGetValue(i, out var schemaEntry))
            {
                await logger.Log($"Unmapped field[{i}] = {value}", LogLevel.Warning, LogContext.Parsing);
                //todo exception to higher logic for non valid sql insert and search for new pattern
                continue;
            }

            // TODO: forward to content storage
            Console.WriteLine($"[{i}] {schemaEntry} = {value}");
        }
    }
}