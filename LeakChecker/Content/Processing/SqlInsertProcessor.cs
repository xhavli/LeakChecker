using System.Text;
using LeakChecker.Logging;
using LeakChecker.Logging.FileLogging;
using LeakChecker.Utilities.Extensions;

namespace LeakChecker.Content.Processing;

public class SqlInsertProcessor(Dictionary<int, ItemEnum> schema, StreamReader reader, IFileLogger logger)
{
    public async Task<ParsingState> ProcessSqlInsert(long startLine, long parseLimit)
    {
        StringBuilder stringBuilder = new();
        Encoding encoding = reader.CurrentEncoding;

        int expectedFields = schema.Count == 0 ? 0 : schema.Keys.Max() + 1;

        bool afterValues = false;
        bool inQuote = false;
        int parenDepth = 0;

        long recordsRead = 0;
        long linesRead = 0;
        long bytesRead = 0;

        while (await reader.ReadLineWithEndingAsync() is { } line)
        {
            linesRead++;
            bytesRead += encoding.GetByteCount(line);

            line = line.TrimOuterWhiteSpace();

            // Detect VALUES
            if (!afterValues)
            {
                const string valuesKeyword = "VALUES";
                int pos = line.IndexOf(valuesKeyword, StringComparison.OrdinalIgnoreCase);

                if (pos < 0)
                    continue;

                line = line[(pos + valuesKeyword.Length)..];
                afterValues = true;
            }

            // Parse characters
            for (int i = 0; i < line.Length; i++)
            {
                char ch = line[i];

                // Escaped SQL quote '' SHOULD NOT BE UNESCAPED HERE
                if (ch == '\'' && inQuote)
                {
                    if (i + 1 < line.Length && line[i + 1] == '\'')
                    {
                        stringBuilder.Append("''"); // keep original SQL escape
                        i++;
                        continue;
                    }
                }

                // Normal quote toggler
                if (ch == '\'')
                {
                    inQuote = !inQuote;
                    stringBuilder.Append(ch);
                    continue;
                }

                // Parentheses tracking (only outside string literals)
                if (!inQuote)
                {
                    if (ch == '(')
                    {
                        if (parenDepth == 0)
                            stringBuilder.Clear();

                        parenDepth++;
                        stringBuilder.Append(ch);
                        continue;
                    }

                    if (ch == ')')
                    {
                        parenDepth--;
                        stringBuilder.Append(ch);

                        // completed tuple
                        if (parenDepth == 0)
                        {
                            string rawTuple = stringBuilder.ToString().Trim();

                            string[] row = ParseTuple(rawTuple);

                            Console.WriteLine($"\nSQL insert parsing line {startLine + linesRead}: {line}");
                            if (row.Length != expectedFields)
                            {
                                await logger.Log($"Bad row length on line {startLine + linesRead}: expected {expectedFields}, got {row.Length} content: {line}", LogLevel.Warning);
                                continue;
                            }

                            await ParseRow(row);
                            recordsRead++;

                            if (recordsRead == parseLimit)
                            {
                                return new ParsingState
                                {
                                    RecordsRead = recordsRead,
                                    LinesRead = linesRead,
                                    BytesRead = bytesRead,
                                };
                            }
                        }

                        continue;
                    }
                }

                // Normal character
                if (parenDepth > 0)
                    stringBuilder.Append(ch);
            }

            // End of Sql INSERT
            if (line.EndsWith(");") || line.EndsWith(") ;") || line.EndsWith(")\t;")) { break; }
        }

        return new ParsingState
        {
            RecordsRead = recordsRead,
            LinesRead = linesRead,
            BytesRead = bytesRead,
        };
    }

    private static string[] ParseTuple(string tuple)
    {
        tuple = tuple.Trim();

        // remove wrapping parentheses
        if (tuple.StartsWith("(") && tuple.EndsWith(")"))
            tuple = tuple.Substring(1, tuple.Length - 2);

        List<string> fields = new();
        StringBuilder sb = new();

        bool inQuote = false;

        for (int i = 0; i < tuple.Length; i++)
        {
            char ch = tuple[i];

            // escaped SQL quotes, keep them as text for ParseField
            if (ch == '\'' && inQuote)
            {
                if (i + 1 < tuple.Length && tuple[i + 1] == '\'')
                {
                    sb.Append("''");
                    i++;
                    continue;
                }
            }

            // toggle quoting
            if (ch == '\'')
            {
                inQuote = !inQuote;
                sb.Append(ch);
                continue;
            }

            // comma outside quotes = field separator
            if (ch == ',' && !inQuote)
            {
                fields.Add(ParseField(sb.ToString()));
                sb.Clear();
                continue;
            }

            sb.Append(ch);
        }

        // last field
        if (sb.Length > 0)
            fields.Add(ParseField(sb.ToString()));

        return fields.ToArray();
    }

    private static string ParseField(string raw)
    {
        raw = raw.Trim();

        // strip SQL quotes
        if (raw is ['\'', _, ..] && raw[^1] == '\'')
            raw = raw.Substring(1, raw.Length - 2);

        // decode SQL escaping
        raw = raw.Replace("''", "'");

        return raw;
    }

    private async Task ParseRow(string[] row)
    {
        for (int i = 0; i < row.Length; i++)
        {
            string value = row[i];

            if (!schema.TryGetValue(i, out var schemaEntry))
            {
                await logger.Log($"Unmapped field[{i}] = {value}", LogLevel.Warning, LogContext.Parsing);
                continue;
            }

            // TODO: forward to content storage
            Console.WriteLine($"[{i}] {schemaEntry} = {value}");
        }
    }
}
