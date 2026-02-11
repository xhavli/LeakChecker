using LeakChecker.Content;
using LeakChecker.Content.Detection;
using LeakChecker.Format.Schema;
using LeakChecker.Logging.Parse;
using LeakChecker.Utilities.Extensions;

namespace LeakChecker.Format.Detection;

public static class CsvFileDetector
{
    public static async Task<Dictionary<int, ItemEnum >> DetectFormat(
        long startLine, char delimiter, StreamReader reader, IParseLogger logger, int detectSamples, int threshold)
    {
        int samplesCount = 0;
        SchemaHeuristic analyzer = new();
        await logger.LogSchemaDetectionHeader();

        while (await reader.ReadLineAsync() is { } line)
        {
            if (samplesCount == detectSamples) break;

            line = line.Trim()
                .TrimEnclosingChars()
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
        
        return assigned;
    }
}