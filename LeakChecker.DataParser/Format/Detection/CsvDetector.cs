using System.Diagnostics;
using LeakChecker.DataParser.Content;
using LeakChecker.DataParser.Content.Detection;
using LeakChecker.DataParser.Format.Schema;
using LeakChecker.DataParser.Logging.Parse;
using LeakChecker.DataParser.Utilities.Extensions;

namespace LeakChecker.DataParser.Format.Detection;

public static class CsvDetector
{
    public static async Task<Dictionary<int, ItemEnum >> DetectSchema(ParsingContext parsingContext)
    {
        IParseLogger logger = parsingContext.Logger;
        StreamReader reader = parsingContext.Reader;
        char delimiter = parsingContext.Delimiter;
        int samplesLimit = parsingContext.SamplesLimit;
        long startLine = parsingContext.StartLine;
        int threshold = parsingContext.Threshold;
        
        
        int samplesCount = 0;
        SchemaHeuristic analyzer = new();
        await logger.LogSchemaDetectionHeader();

        Stopwatch sw = Stopwatch.StartNew();
        while (await reader.ReadLineAsync() is { } line)
        {
            if (samplesCount == samplesLimit) break;

            line = line.Trim()
                .TrimOuterParentheses()
                .TrimOuterParenthesesWithComma();    // For undetected SQL INSERT
            
            if (string.IsNullOrWhiteSpace(line)) { continue; }
            
            samplesCount++;
            await logger.LogSample($"CSV file sample {samplesCount} on line {startLine + samplesCount}: {line}");
            var linePatterns = await ContentDetector.DetectLine(line, delimiter, logger);
            int lineDelimitersCount = line.Count(ch => ch == delimiter);
            analyzer.AddLinePatterns(linePatterns, lineDelimitersCount);
        }
        
        await logger.LogHeuristicData(analyzer);
        await logger.LogDominantSchema(analyzer, threshold);
        
        var schema = analyzer.GetDominantSchema(threshold);
        var assigned = CredentialAssigner.Assign(schema);
        
        await logger.LogFinalSchema(assigned);
        await logger.Log($"Csv file schema created in {sw.Elapsed}\n");
        
        return assigned;
    }
}