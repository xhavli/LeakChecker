using LeakChecker.DataParser.Content.Parse;
using LeakChecker.DataParser.Encodings;
using LeakChecker.DataParser.Encodings.Conversion;
using LeakChecker.DataParser.Encodings.Detection;
using LeakChecker.DataParser.Helpers.FileHelp;
using LeakChecker.DataParser.Helpers.Settings;
using LeakChecker.DataParser.Logging;
using LeakChecker.DataParser.Logging.Execution;
using LeakChecker.DataParser.Logging.Parse;
using LeakChecker.DataParser.Stats.Execution;
using LeakChecker.DataParser.Stats.Parse;

namespace LeakChecker.DataParser.Orchestration;

public class ParsingOrchestrator(
    string filePath,
    ISettings settings,
    FileHelper fileHelper,
    ExecutionStats stats,
    ExecutionLogger logger,
    IParseLoggerFactory parseLoggerFactory)
{
    public async Task RunAsync()
    {
        logger.Log("Started: " + Path.GetFileName(filePath), LogLevel.Info, LogContext.Parsing);
        
        if (!fileHelper.CanParse(filePath))
            return;
        
        using var parseLogger = parseLoggerFactory.CreateAsync(filePath);
        ParseStats parseStats = new ParseStats(stats.ExecutionId, parseLogger, filePath);

        try
        {
            if (FileHelper.IsExcel(filePath))
            {
                ExcelParser excelParser = new(filePath, parseLogger, parseStats, settings);
                await excelParser.ParseAsync();
            }
            else
            {
                string parsePath;
                if (!settings.Environment.StartsWith("Development", StringComparison.OrdinalIgnoreCase))
                {
                    EncodingDetector encodingDetector = new(filePath, parseLogger, parseStats);
                    List<EncodingSegment> encodingSegments = await encodingDetector.DetectEncodingSegments();
                    
                    parsePath = await EncodingConverter.ConvertFileToUtf8(encodingSegments, parseLogger);
                }
                else
                {
                    logger.Log($"Skipping encoding detection and conversion in {settings.Environment} environment", LogLevel.Info, LogContext.Parsing);
                    parsePath = parseLogger.SubjectFilePath;
                }

                await fileHelper.IsReadable(parsePath);

                using ContentParser contentParser = new(parsePath, parseLogger, parseStats, settings);
                await contentParser.ParseAsync();
            }

            parseLogger.LogParseStats(parseStats);
            await settings.Database.SaveParseOne(parseStats);
            await settings.Database.UpsertDashboardStats(parseStats);

            stats.Update(parseStats);
        
            logger.Log($"Finished: {parseStats.FileName}", LogLevel.Success, LogContext.Parsing);
        }
        catch (Exception e)
        {
            logger.Log($"{parseStats.ParseId} : {parseStats.FileName}: {e}", LogLevel.Failure, LogContext.Parsing);
        }
        finally
        {
            // Comment this if you want to keep extracted files
            if (File.Exists(parseLogger.SubjectFilePath) &&
                parseLogger.SubjectFilePath.StartsWith(settings.TmpDirectory, StringComparison.Ordinal))
            {
                 File.Delete(parseLogger.SubjectFilePath);
            }
            
            // Comment this if you want to keep encoding normalized files
            if (File.Exists(parseLogger.SubjectTmpFilePath) &&
                parseLogger.SubjectTmpFilePath.StartsWith(settings.TmpDirectory, StringComparison.Ordinal))
            {
                File.Delete(parseLogger.SubjectTmpFilePath);
            }
        }
    }
}