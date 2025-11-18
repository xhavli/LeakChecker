using LeakChecker.Content;
using LeakChecker.Content.Detection;
using LeakChecker.Logging.FileLogging;
using LeakChecker.Utilities.Extensions;

namespace LeakChecker.Format.Detection;

public static class CsvFileDetector
{
    public static async Task<Dictionary<int, ItemEnum >> DetectFormat(
        char delimiter, StreamReader reader, IFileLogger logger, int detectSamples = 107, int threshold = 50)
    {
        int samplesCount = 0;
        SchemaHeuristic analyzer = new();

        while (await reader.ReadLineAsync() is { } line)
        {
            samplesCount++;
            if (samplesCount == detectSamples) break;

            line = line.Trim().TrimOuterParenthesesAndComma();
            Console.WriteLine();
            Console.WriteLine($"CSV file sample {samplesCount}: {line}");
            if (string.IsNullOrWhiteSpace(line) || string.IsNullOrEmpty(line)) { continue; }
            analyzer.AddLinePatterns(await ContentDetector.DetectLine(line, delimiter, logger));
        }
        
        await logger.LogHeuristicData(analyzer, threshold);
        return analyzer.GetDominantSchema(threshold);
    }
}