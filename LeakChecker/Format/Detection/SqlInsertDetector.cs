using System.Text;
using LeakChecker.Content;
using LeakChecker.Content.Detection;
using LeakChecker.Logging;
using LeakChecker.Logging.FileLogging;

namespace LeakChecker.Format.Detection;

public static class SqlInsertDetector
{
    private const char Delimiter = ',';

    public static async Task<Dictionary<int, ItemEnum>> DetectFormat(
        StreamReader reader, IFileLogger logger, int detectSamples = 53, int threshold = 50)
    {
        bool inInsert = false;
        int parenDepth = 0, expectedColumns = 0;
        List<string> sqlHeaders = new();
        StringBuilder stringBuilder = new();

        int samplesCount = 0;
        var analyzer = new SchemaHeuristic();

        while (await reader.ReadLineAsync() is { } line)
        {
            if (samplesCount == detectSamples) break;
            
            string trimmed = line.Trim();

            if (!inInsert)
            {
                if (trimmed.StartsWith("INSERT INTO", StringComparison.OrdinalIgnoreCase))
                {
                    inInsert = true;

                    // Extract header columns
                    int openParen = trimmed.IndexOf('(');
                    int closeParen = trimmed.IndexOf(')');

                    if (openParen > 0 && closeParen > openParen)
                    {
                        // Extract SQL Insert subject
                        string subject = string.Empty;
                        int insertIntoPos = trimmed.IndexOf("INSERT INTO", StringComparison.OrdinalIgnoreCase);
                        if (insertIntoPos >= 0 && openParen > insertIntoPos)
                        {
                            subject = trimmed.Substring(insertIntoPos + "INSERT INTO".Length,
                                    openParen - (insertIntoPos + "INSERT INTO".Length))
                                .Trim(' ', '`', '"');
                        }

                        // Extract column list
                        string columnList = trimmed.Substring(openParen + 1, closeParen - openParen - 1);
                        expectedColumns = columnList.Split(Delimiter).Select(c => c.Trim()).Count();
                        sqlHeaders = columnList
                            .Split(Delimiter)
                            .Select(c => c.Trim().Trim('`'))
                            .ToList();

                        await logger.LogSqlInsertHeader(subject, sqlHeaders, trimmed);
                    }


                    // Jump to VALUES
                    int valuesPos = trimmed.IndexOf("VALUES", StringComparison.OrdinalIgnoreCase);
                    if (valuesPos >= 0)
                    {
                        line = line.Substring(valuesPos + 6);
                    }
                    else
                    {
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

                            // Validate column count
                            if (row.Length != expectedColumns)
                            {
                                await logger.Log(
                                    $"Incorrect Sql Insert: Columns count mismatch. Expected {expectedColumns}, got {row.Length}. Sql Insert = {tuple}",
                                    LogLevel.Warning, LogContext.Content);
                            }

                            samplesCount++;

                            List<SchemaHeuristicRecord> linePatterns = new();
                            
                            Console.WriteLine($"SQL insert sample {samplesCount}: {tuple}");
                            for (int j = 0; j < row.Length; j++)
                            {
                                string value = row[j];
                                if (string.IsNullOrEmpty(value) || string.IsNullOrWhiteSpace(value)) continue;
                                ItemEnum item = await ContentDetector.DetectToken(value, logger);
                                Console.WriteLine($"[{j}] {item} = {value}");

                                linePatterns.Add(new SchemaHeuristicRecord
                                {
                                    Attribute = item,
                                    Position = j,
                                    DelimitersInside = value.Count(ch => ch == Delimiter)
                                });
                            }

                            analyzer.AddLinePatterns(linePatterns);
                            Console.WriteLine();
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

        Console.WriteLine();
        Console.Write("Sql Header is: "); //TODO remove this
        foreach (var header in sqlHeaders)
        {
            Console.Write(header + ", ");
        }
        Console.WriteLine();
        Console.WriteLine();

        await logger.LogFinalSchema(schema);
        return schema;
    }

    public static string[] ParseTuple(string tuple)
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