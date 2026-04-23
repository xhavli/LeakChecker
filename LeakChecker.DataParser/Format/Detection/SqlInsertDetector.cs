using System.Diagnostics;
using System.Text;
using LeakChecker.DataParser.Content;
using LeakChecker.DataParser.Content.Detection;
using LeakChecker.DataParser.Format.Schema;
using LeakChecker.DataParser.Helpers.Extensions;
using LeakChecker.DataParser.Logging;
using LeakChecker.DataParser.Logging.Parse;

namespace LeakChecker.DataParser.Format.Detection;

public static class SqlInsertDetector
{
    private const string Insert = "INSERT";
    private const string Into = "INTO";
    private const string Values = "VALUES";
    private const char Delimiter = ',';

    public static async Task<Dictionary<int, ItemEnum>> DetectSchema(ParsingContext parsingContext)
    {
        IParseLogger logger = parsingContext.Logger;
        StreamReader reader = parsingContext.Reader;
        int threshold = parsingContext.Settings.SchemaThreshold;
        long linesRead = parsingContext.StartLine;
        int samplesLimit = parsingContext.Settings.SqlSamples;
        
        int parenDepth = 0;
        int samplesRead = 0;
        int expectedCols = 0;
        StringBuilder sb = new();

        Stopwatch sw = Stopwatch.StartNew();
        var analyzer = new SchemaHeuristic();
        await logger.LogSchemaDetectionHeader();

        bool inQuote = false;
        SqlInsertHeader? header = null;
        string tempLine = string.Empty;
        while (await reader.ReadLineAsync() is { } line)
        {
            linesRead++;
            
            if (string.IsNullOrWhiteSpace(line))
                continue;
            
            if (samplesRead >= samplesLimit)
                break;
            
            line = line.Trim();
            
            if (header is null)
            {
                if (tempLine.Length > 4096)
                    tempLine = tempLine[^2048..];
                
                tempLine += line;
                tempLine += ' ';
                
                header = TryParseSqlHeader(tempLine);
                if (header is null)
                    continue;
                
                expectedCols = header.Headers.Count;
                await logger.LogSqlInsertHeader(header);

                if (string.IsNullOrWhiteSpace(header.ValuesTail))
                    continue;
                
                line = header.ValuesTail.Trim();
            }
            
            // If inside VALUES
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '\'')
                {
                    // Handle doubled single quote '' as escape
                    if (inQuote && i + 1 < line.Length && line[i + 1] == '\'')
                    {
                        sb.Append('\'');
                        sb.Append('\'');
                        i++; // skip the second quote
                        continue;
                    }
                    
                    inQuote = !inQuote;
                    sb.Append(c);
                    continue;
                }

                if (!inQuote)
                {
                    if (c == '(')
                    {
                        if (parenDepth == 0)
                            sb.Clear();
                        
                        sb.Append(c);
                        parenDepth++;
                        continue;
                    }

                    if (c == ')')
                    {
                        parenDepth--;
                        sb.Append(c);

                        if (parenDepth == 0)
                        {
                            samplesRead++;
                            
                            // Extract tuple (row)
                            string tuple = sb.ToString().Trim(',', ';', ' ');
                            string[] row = TupleToArray(tuple);
                            
                            // Validate column length
                            if (row.Length != expectedCols)
                            {
                                await logger.Log($"Bad row length on line {linesRead}: expected {expectedCols}, " +
                                                 $"got {row.Length} content: {tuple}", LogLevel.Warning);
                            }
                            
                            await logger.LogSample($"SQL insert sample {samplesRead} on line {linesRead}: {tuple}");

                            var linePatterns = await AnalyzeRow(row, logger);

                            int delimitersCount = tuple.Count(ch => ch == Delimiter);
                            analyzer.AddLinePatterns(linePatterns, delimitersCount);
                            
                            sb.Clear();

                            // End of SQL INSERT
                            if (line.IsSqlInsertEnd())
                                break;
                        }

                        continue;
                    }
                }

                if (parenDepth > 0 || inQuote)
                    sb.Append(c);
            }
            
            // End of SQL INSERT
            if (line.IsSqlInsertEnd())
                break;
        }

        await logger.LogHeuristicData(analyzer);
        await logger.LogDominantSchema(analyzer, threshold);
        
        var original = analyzer.GetDominantSchema(threshold);
        var guessed = HeaderGuesser.GuessColumns(header!.Headers);
        var assigned = HeaderGuesser.BindGuessed(original, guessed);

        await logger.LogFinalSchema(assigned);
        await logger.Log($"SQL Insert schema created in {sw.Elapsed}\n");
        
        parsingContext.Stats.Schemas.Add(assigned);
        parsingContext.Stats.Context.Add(header.Subject);

        return assigned;
    }

    private static SqlInsertHeader? TryParseSqlHeader(string line)
    {
        line = line.Trim();

        int insertPos = line.IndexOf(Insert, StringComparison.OrdinalIgnoreCase);
        if (insertPos == -1)
            return null;

        int intoPos = line.IndexOf(Into, insertPos, StringComparison.OrdinalIgnoreCase);
        if (intoPos == -1)
            return null;

        int openParen = line.IndexOf('(', intoPos);
        if (openParen == -1)
            return null;

        int closeParen = line.IndexOf(')', openParen + 1);
        if (closeParen == -1)
            return null;

        int valuesPos = line.IndexOf(Values, closeParen, StringComparison.OrdinalIgnoreCase);
        if (valuesPos == -1)
            return null;

        string subject = line[(intoPos + Into.Length)..openParen]
            .Trim(' ', '`', '"', '[', ']');

        string columnList = line[(openParen + 1)..closeParen];

        List<string> headers = columnList
            .Split(Delimiter)
            .Select(h => h.Trim(' ', '`', '"', '[', ']'))
            .ToList();

        string valuesTail = line[(valuesPos + Values.Length)..];
        string fullHeader = line.Substring(insertPos, valuesPos - insertPos + Values.Length); //include VALUES

        return new SqlInsertHeader
        {
            Subject = subject,
            Headers = headers,
            FullHeader = fullHeader,
            ValuesTail = valuesTail,
        };
    }
    
    private static string[] TupleToArray(string tuple)
    {
        if (string.IsNullOrEmpty(tuple))
            return [];

        int start = 0;
        int end = tuple.Length;

        if (tuple[0] == '(' && tuple[^1] == ')')
        {
            start = 1;
            end--;
        }

        bool inQuote = false;
        var sb = new StringBuilder();
        var fields = new List<string>();

        for (int i = start; i < end; i++)
        {
            char c = tuple[i];

            if (c == '\'')
            {
                if (inQuote && i + 1 < end && tuple[i + 1] == '\'')
                {
                    sb.Append('\''); // escaped quote: ''
                    i++;
                }
                else
                {
                    inQuote = !inQuote;
                }

                continue;
            }

            if (c == Delimiter && !inQuote)
            {
                fields.Add(sb.ToString().Trim());
                sb.Clear();
                continue;
            }

            sb.Append(c);
        }

        fields.Add(sb.ToString().Trim());
        return fields.ToArray();
    }

    private static async Task<List<SchemaHeuristicRecord>> AnalyzeRow(string[] row, IParseLogger logger)
    {
        List<SchemaHeuristicRecord> linePatterns = new();
                            
        for (int j = 0; j < row.Length; j++)
        {
            string value = row[j];
                                
            if (string.IsNullOrWhiteSpace(value))
                continue;
                                
            ItemEnum item = await ContentDetector.DetectToken(value, logger);
            // Console.WriteLine($"[{j}] {item} = {value}");

            linePatterns.Add(new SchemaHeuristicRecord
            {
                Position = j,
                Attribute = item,
                DelimitersInside = value.Count(ch => ch == Delimiter)
            });
        }

        return linePatterns;
    }
}