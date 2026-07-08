using System.Diagnostics;
using LeakChecker.Common.Enums;
using LeakChecker.DataParser.Content;
using LeakChecker.DataParser.Content.Detection;
using LeakChecker.DataParser.Format.Schema;
using LeakChecker.DataParser.Helpers.Extensions;
using LeakChecker.DataParser.Logging;
using LeakChecker.DataParser.Logging.Parse;

namespace LeakChecker.DataParser.Format.Detection;

public static class CsvDetector
{
    public static async Task<Dictionary<int, ItemType >> DetectSchema(ParsingContext parsingContext)
    {
        IParseLogger logger = parsingContext.Logger;
        StreamReader reader = parsingContext.Reader;
        char delimiter = parsingContext.Delimiter;
        int samplesLimit = parsingContext.Settings.CsvSamples;
        long startLine = parsingContext.StartLine;
        int threshold = parsingContext.Settings.SchemaThreshold;
        
        int linesRead = 0;
        int samplesCount = 0;
        SchemaHeuristic analyzer = new();
        logger.LogSchemaDetectionHeader();

        Stopwatch sw = Stopwatch.StartNew();
        while (await reader.ReadLineWithEndingAsync() is { } line)
        {
            linesRead++;
            
            if (line.IsSqlInsert())
            {
                logger.Log($"Detected SQL Insert while sampling CSV file on line {startLine + linesRead}: {line}. " + 
                           $"Returning back to recompute schema.", LogLevel.Warning);
                break;
            }
            
            if (samplesCount == samplesLimit)
                break;

            if (string.IsNullOrWhiteSpace(line))
                continue;
            
            line = line.Trim()
                .TrimOuterParentheses()
                .TrimOuterParenthesesWithComma();   // For undetected SQL INSERT
            
            samplesCount++;
            logger.LogSample($"CSV file sample {samplesCount} on line {startLine + linesRead}: {line}");
            
            var linePatterns = await ContentDetector.DetectLine(line, delimiter, logger);
            int delimitersCount = line.Count(ch => ch == delimiter);
            analyzer.AddLinePatterns(linePatterns, delimitersCount);
        }
        
        logger.LogHeuristicData(analyzer);
        logger.LogDominantSchema(analyzer, threshold);
        
        var original = analyzer.GetDominantSchema(threshold);
        var assigned = CredentialAssigner.Assign(original);
        
        logger.LogFinalSchema(assigned);
        logger.Log($"CSV file schema created in {sw.Elapsed}\n");
        
        parsingContext.Stats.Schemas.Add(assigned);
        parsingContext.Stats.Context.Add(Path.GetFileNameWithoutExtension(parsingContext.Stats.SourcePath));
        
        return assigned;
    }
}