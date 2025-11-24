using LeakChecker.Content;
using LeakChecker.Content.Detection;
using LeakChecker.Logging.FileLogging;
using LeakChecker.Utilities.Extensions;

namespace LeakChecker.Format.Detection;

public static class CsvFileDetector
{
    public static async Task<Dictionary<int, ItemEnum >> DetectFormat(
        long startLine, char delimiter, StreamReader reader, IFileLogger logger, int detectSamples, int threshold)
    {
        int samplesCount = 0;
        SchemaHeuristic analyzer = new();
        await logger.LogSchemaDetectionHeader();

        while (await reader.ReadLineAsync() is { } line)
        {
            if (samplesCount == detectSamples) break;
            
            line = line.TrimOuterWhiteSpace().TrimOuterParenthesesAndComma().TrimOuterQuotes();
            samplesCount++;
            await logger.LogSample($"CSV file sample {samplesCount} on line {startLine + samplesCount}: {line}");
            if (string.IsNullOrWhiteSpace(line)) { continue; }
            analyzer.AddLinePatterns(await ContentDetector.DetectLine(line, delimiter, logger));
        }
        
        await logger.LogHeuristicData(analyzer);
        await logger.LogDominantSchema(analyzer, threshold);
        
        var schema = analyzer.GetDominantSchema(threshold);
        var assigned = CsvCredentialAssigner.Assign(schema);
        await logger.LogFinalSchema(assigned);
        
        return assigned;
    }
}