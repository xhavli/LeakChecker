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

        var isAccessible = fileHelper.IsAccessible(filePath);
        var isTextual = fileHelper.IsTextual(filePath);
        var isExcel = FileHelper.IsExcel(filePath);

        if (!isAccessible || !(isTextual || isExcel))
            return;
        
        using var parseLogger = await parseLoggerFactory.CreateAsync(filePath);
        ParseStats parseStats = new ParseStats(stats.ExecutionId, parseLogger, filePath);

        try
        {
            if (FileHelper.IsExcel(filePath))
            {
                await ExcelParser.ParseAsync(filePath, parseLogger, parseStats, settings);
            }
            else
            {
                // EncodingDetector encodingDetector = new(filePath, parseLogger, parseStats);
                // List<EncodingSegment> encodingSegments = await encodingDetector.DetectEncodingSegments();
                //
                // string parsePath = await EncodingConverter.ConvertFileToUtf8(encodingSegments, parseLogger);
                string parsePath = parseLogger.SubjectFilePath;
                
                await fileHelper.IsReadable(parsePath);

                using ContentParser contentParser = new(parsePath, parseLogger, parseStats, settings);
                await contentParser.ParseAsync();
            }

            await parseLogger.LogParseStats(parseStats);
            // await DatabaseFacade.SaveParseOne(parseStats);

            lock (stats)
            {
                stats.FilesParsed.Add(parseStats.ParseId);
                stats.LinesParsed += parseStats.LinesRead;
                stats.BytesParsed += parseStats.BytesRead;
                stats.RecordsParsed += parseStats.RecordsRead;
                stats.MalformedRecordsRead += parseStats.MalformedRead;
            }
        
            logger.Log($"Finished: {parseStats.FileName}", LogLevel.Success, LogContext.Parsing);
        }
        catch (Exception e)
        {
            logger.Log($"{parseStats.ParseId} : {parseStats.FileName}: {e}", LogLevel.Failure, LogContext.Execution);
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