using System.Text;
using LeakChecker.Content;
using LeakChecker.Content.Detection;
using LeakChecker.Format.Schema;
using LeakChecker.Logging;
using LeakChecker.Logging.FileLogging;

namespace LeakChecker.Format.Detection;

public static class SqlInsertDetector
{
    private const char Delimiter = ',';

    public static async Task<Dictionary<int, ItemEnum>> DetectFormat(
        long startLine, StreamReader reader, IFileLogger logger, int detectSamples, int threshold)
    {
        bool inInsert = false;
        int parenDepth = 0, expectedColumns = 0;
        List<string> sqlHeaders = new();
        StringBuilder stringBuilder = new();

        int samplesCount = 0;
        var analyzer = new SchemaHeuristic();
        await logger.LogSchemaDetectionHeader();

        while (await reader.ReadLineAsync() is { } line)
        {
            if (samplesCount == detectSamples) break;
            
            string trimmed = line.Trim();

            if (!inInsert)
            {
                // Detect INSERT [...] INTO
                int intoPos = trimmed.IndexOf("INTO", StringComparison.OrdinalIgnoreCase);

                if (trimmed.StartsWith("INSERT ", StringComparison.OrdinalIgnoreCase) &&
                    intoPos >= "INSERT ".Length)
                {
                    // Find column list parentheses
                    int openParen = trimmed.IndexOf('(', intoPos);
                    int closeParen = trimmed.IndexOf(')', openParen + 1);

                    if (openParen > intoPos && closeParen > openParen)
                    {
                        inInsert = true;

                        // Extract subject (table name)
                        string subject = trimmed
                            .Substring(intoPos + "INTO".Length, openParen - (intoPos + "INTO".Length))
                            .Trim(' ', '`', '"', '[', ']');

                        // Extract columns
                        string columnList = trimmed.Substring(openParen + 1, closeParen - openParen - 1);

                        sqlHeaders = columnList
                            .Split(Delimiter)
                            .Select(h => h.Trim(' ', '`', '"', '[', ']'))
                            .ToList();

                        expectedColumns = sqlHeaders.Count;

                        await logger.LogSqlInsertHeader(subject, sqlHeaders, trimmed);

                        // Move to VALUES part, if present
                        int valuesPos = trimmed.IndexOf("VALUES", StringComparison.OrdinalIgnoreCase);
                        if (valuesPos >= 0)
                        {
                            line = trimmed.Substring(valuesPos + "VALUES".Length);
                        }
                        else
                        {
                            continue; // Wait for VALUES on following lines
                        }
                    }
                    else
                    {
                        // No column list -> skip
                        continue;
                    }
                }
            }

            // If we're inside VALUES
            bool inQuote = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '\'')
                {
                    // Handle doubled single quote '' as escape
                    if (inQuote && i + 1 < line.Length && line[i + 1] == '\'')
                    {
                        stringBuilder.Append('\'');
                        i++; // skip the second quote
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
                            // Extract tuple (row)
                            string tuple = stringBuilder.ToString().Trim(',', ';', ' ');
                            string[] row = ParseTuple(tuple);

                            samplesCount++;
                            
                            // Validate column count
                            if (row.Length != expectedColumns)
                            {
                                await logger.Log($"Bad row length on line {startLine}: expected {expectedColumns}, got {row.Length} content: {tuple}", 
                                    LogLevel.Warning);
                            }
                            
                            List<SchemaHeuristicRecord> linePatterns = new();

                            await logger.LogSample($"SQL insert sample {samplesCount} on line {startLine + samplesCount}: {tuple}");
                            for (int j = 0; j < row.Length; j++)
                            {
                                string value = row[j];
                                if (string.IsNullOrEmpty(value) || string.IsNullOrWhiteSpace(value)) continue;
                                ItemEnum item = await ContentDetector.DetectToken(value, logger);
                                // Console.WriteLine($"[{j}] {item} = {value}");

                                linePatterns.Add(new SchemaHeuristicRecord
                                {
                                    Attribute = item,
                                    Position = j,
                                    DelimitersInside = value.Count(ch => ch == Delimiter)
                                });
                            }
                            
                            int lineDelimitersCount = line.Count(ch => ch == Delimiter) - 1;    // -1 because value is exact number of columns, not delimiters
                            analyzer.AddLinePatterns(linePatterns, lineDelimitersCount);
                            stringBuilder.Clear();

                            // End of Sql INSERT
                            if (trimmed.EndsWith(");") || trimmed.EndsWith(") ;") || trimmed.EndsWith(")\t;")) { break; }
                        }

                        continue;
                    }
                }

                if (parenDepth > 0 || inQuote) { stringBuilder.Append(c); }
            }
            
            // End of Sql INSERT
            if (trimmed.EndsWith(");") || trimmed.EndsWith(") ;") || trimmed.EndsWith(")\t;")) { break; }
        }

        await logger.LogHeuristicData(analyzer);
        await logger.LogDominantSchema(analyzer, threshold);
        
        var schema = analyzer.GetDominantSchema(threshold);
        var guessed = SqlHeaderGuesser.GuessColumns(sqlHeaders);

        foreach (var (idx, guess) in guessed)
        {
            if (!schema.TryGetValue(idx, out var existing) || 
                existing == ItemEnum.Other || existing == ItemEnum.Null)
            {
                schema[idx] = guess;
            }
        }

        await logger.LogFinalSchema(schema);
        return schema;
    }

    private static string[] ParseTuple(string tuple)
    {
        if (tuple.StartsWith('(') && tuple.EndsWith(')')) 
            tuple = tuple.Substring(1, tuple.Length - 2);

        bool inQuote = false;
        List<string> fields = new();
        StringBuilder stringBuilder = new();

        for (int i = 0; i < tuple.Length; i++)
        {
            char c = tuple[i];
            if (c == '\'')
            {
                if (inQuote && i + 1 < tuple.Length && tuple[i + 1] == '\'')
                {
                    stringBuilder.Append('\'');
                    i++;
                }
                else
                {
                    inQuote = !inQuote;
                }
            }
            else if (c == Delimiter && !inQuote)
            {
                fields.Add(stringBuilder.ToString().Trim().Trim('\''));
                stringBuilder.Clear();
            }
            else
            {
                stringBuilder.Append(c);
            }
        }
        if (stringBuilder.Length > 0)
            fields.Add(stringBuilder.ToString().Trim().Trim('\''));

        return fields.ToArray();
    }
}