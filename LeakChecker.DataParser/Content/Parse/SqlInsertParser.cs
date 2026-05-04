using System.Text;
using LeakChecker.Common.Enums;
using LeakChecker.DataParser.Database;
using LeakChecker.DataParser.Helpers.Extensions;
using LeakChecker.DataParser.Logging;
using LeakChecker.DataParser.Logging.Parse;
using MongoDB.Bson;

namespace LeakChecker.DataParser.Content.Parse;

public class SqlInsertParser(ParsingContext parsingContext)
{
    private const string Values = "VALUES";
    private readonly ObjectId _parseId = parsingContext.Stats.ParseId;
    private readonly IParseLogger _logger = parsingContext.Logger;
    private readonly Dictionary<int, ItemEnum> _schema = parsingContext.Schema;
    private readonly List<Dictionary<ItemEnum, List<string>>> _cachedRecords = new();
    private readonly IDatabase _database = parsingContext.Settings.Database;
    
    public async Task<ParsingResult> Parse()
    {
        StreamReader reader = parsingContext.Reader;
        long startLine = parsingContext.StartLine;
        long parseLimit = parsingContext.ParseLimit;
        int malformedLimit = parsingContext.MalformedLimit;
    
        StringBuilder stringBuilder = new();
        Encoding encoding = reader.CurrentEncoding;

        int expectedFields = _schema.Count == 0 ? 0 : _schema.Keys.Max() + 1;

        int parenDepth = 0;
        bool inQuote = false;
        bool afterValues = false;

        long bytesRead = 0;
        long linesRead = 0;
        long recordsRead = 0;
        int malformedRead = 0;
        int malformedReadSequence = 0;

        while (await reader.ReadLineWithEndingAsync() is { } line)
        {
            linesRead++;
            bytesRead += encoding.GetByteCount(line);

            if (string.IsNullOrWhiteSpace(line))
                continue;
            
            line = line.Trim();

            // Detect VALUES position
            if (!afterValues)
            {
                int valuesPos = line.IndexOf(Values, StringComparison.OrdinalIgnoreCase);

                if (valuesPos < 0)    // Wait for VALUES on another line
                    continue;

                line = line.Substring(valuesPos + Values.Length);
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

                            string[] row = TupleToArray(rawTuple);

                            // Console.WriteLine($"\nSQL insert parsing line {startLine + linesRead}: {line}");
                            if (row.Length != expectedFields)
                            {
                                _logger.Log($"Bad row length on line {startLine + linesRead}: expected {expectedFields}, got {row.Length} content: {line}", LogLevel.Warning);
                                
                                malformedRead++;
                                malformedReadSequence++;

                                if (malformedReadSequence >= malformedLimit)
                                {
                                    _logger.Log($"Parsing reach malformed limit {malformedLimit}. Returning back to recompute schema", LogLevel.Warning, LogContext.Parsing);
                                    break;
                                }
                                
                                continue;
                            }

                            await ParseRow(row);
                            if (_cachedRecords.Count > 2000)
                            {
                                await _database.SaveIdentityMany(_cachedRecords, _parseId);
                                _cachedRecords.Clear();
                            }
                            malformedReadSequence = 0;
                            recordsRead++;

                            if (recordsRead == parseLimit)
                            {
                                return new ParsingResult
                                {
                                    RecordsRead = recordsRead,
                                    LinesRead = linesRead,
                                    BytesRead = bytesRead,
                                    MalformedRead = malformedRead,
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

            // End of SQL INSERT
            if (line.IsSqlInsertEnd())
                break;
        }

        await _database.SaveIdentityMany(_cachedRecords, _parseId);
        
        return new ParsingResult
        {
            LinesRead = linesRead,
            BytesRead = bytesRead,
            RecordsRead = recordsRead,
            MalformedRead = malformedRead,
        };
    }

    private static string[] TupleToArray(string tuple)
    {
        tuple = tuple.Trim();

        // remove wrapping parentheses
        if (tuple.StartsWith('(') && tuple.EndsWith(')'))
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
                fields.Add(Normalize(sb.ToString()));
                sb.Clear();
                continue;
            }

            sb.Append(ch);
        }

        // last field
        if (sb.Length > 0)
            fields.Add(Normalize(sb.ToString()));

        return fields.ToArray();
    }

    private static string Normalize(string raw)
    {
        raw = raw.Trim();
        
        if (string.Equals(raw, "NULL", StringComparison.OrdinalIgnoreCase))
            return string.Empty;

        // strip SQL quotes
        if (raw is ['\'', _, ..] && raw[^1] == '\'')
            raw = raw.Substring(1, raw.Length - 2);

        // decode SQL escaping
        raw = raw.Replace("''", "'");

        return raw;
    }

    private async Task ParseRow(string[] row)
    {
        Dictionary<ItemEnum, List<string>> record = new();
        
        for (int i = 0; i < row.Length; i++)
        {
            string value = row[i];
            
            if (string.IsNullOrWhiteSpace(value))
                continue;
            
            if (!_schema.TryGetValue(i, out var itemType))
            {
                _logger.Log($"Unmapped field[{i}] = {value}", LogLevel.Warning, LogContext.Parsing);
                continue;
            }
            
            if (!record.TryGetValue(itemType, out var list))
            {
                list = new List<string>();
                record[itemType] = list;
            }

            list.Add(value);

            // Console.WriteLine($"[{i}] {schemaEntry} = {row[i]}");
        }
        
        // Forward to content storage
        _cachedRecords.Add(record);
    }
}
