using LeakChecker.Common.Enums;
using LeakChecker.DataParser.Content.Parse;
using LeakChecker.DataParser.Encodings;
using LeakChecker.DataParser.Encodings.Conversion;
using LeakChecker.DataParser.Encodings.Detection;
using LeakChecker.DataParser.Helpers.FileHelp;
using LeakChecker.DataParser.Helpers.Settings;
using LeakChecker.DataParser.Interfaces;
using LeakChecker.DataParser.Logging.Execution;
using LeakChecker.DataParser.Logging.Parse;
using LeakChecker.DataParser.Stats.Parse;

namespace LeakChecker.DataParser.Orchestration.Parsing;

public class ParsingOrchestrator(
    string filePath,
    ISettings settings,
    FileHelper fileHelper,
    Guid executionId,
    ExecutionLogger logger,
    IParseLoggerFactory parseLoggerFactory,
    IDatabase database)
{
    public async Task<ParseStats> RunAsync()
    {
        using var parseLogger = parseLoggerFactory.Create(filePath);
        ParseStats parseStats = new ParseStats(executionId, parseLogger.ParseId, filePath);
        
        logger.Log($"Started: {parseStats.ParseId} - {Path.GetFileName(filePath)}", LogLevel.Info, LogContext.Parsing);
        
        if (!fileHelper.CanParse(filePath))
            return parseStats;

        try
        {
            if (FileHelper.IsExcel(filePath))
            {
                ExcelParser excelParser = new(filePath, parseLogger, parseStats, settings, database);
                await excelParser.ParseAsync();
            }
            else
            {
                string parsePath;
                if (settings.Environment != EnvironmentType.Development)
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

                using ContentParser contentParser = new(parsePath, parseLogger, parseStats, settings, database);
                await contentParser.ParseAsync();
            }

            parseLogger.LogParseStats(parseStats);
            await database.SaveParseOne(parseStats);
            await database.UpsertDashboardStats(parseStats);
            
            logger.Log($"Finished: {parseStats.FileName}", LogLevel.Success, LogContext.Parsing);
            return parseStats;
        }
        catch (Exception e)
        {
            logger.Log($"{parseStats.ParseId} : {parseStats.FileName}: {e}", LogLevel.Failure, LogContext.Parsing);
            return parseStats;
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