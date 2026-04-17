using System.Globalization;
using System.Threading.Channels;
using LeakChecker.DataParser.Content.Detection.RecognitionService;
using LeakChecker.DataParser.Content.Parse;
using LeakChecker.DataParser.Data;
using LeakChecker.DataParser.Encodings;
using LeakChecker.DataParser.Encodings.Conversion;
using LeakChecker.DataParser.Encodings.Detection;
using LeakChecker.DataParser.Helpers;
using LeakChecker.DataParser.Helpers.ArchiveExtraction;
using LeakChecker.DataParser.Helpers.FileHelp;
using LeakChecker.DataParser.Helpers.Settings;
using LeakChecker.DataParser.Logging;
using LeakChecker.DataParser.Logging.Execution;
using LeakChecker.DataParser.Logging.Parse;
using LeakChecker.DataParser.Stats.Execution;
using LeakChecker.DataParser.Stats.Parse;

namespace LeakChecker.DataParser.Orchestration;

public sealed class ExecutionOrchestrator(
    ISettings settings,
    FileHelper fileHelper,
    ArchiveExtractor archiveExtractor,
    PythonNerService pythonNerService,
    ExecutionLogger logger,
    IParseLoggerFactory parseLoggerFactory)
{
    private static readonly Guid ExecutionId = Guid.NewGuid();
    private readonly ExecutionStats _stats = new(ExecutionId, logger.ExecutionStart);
    
    public async Task<int> RunAsync()
    {
        try
        {
            await pythonNerService.Start();
            await pythonNerService.WaitStart();
        }
        catch (Exception e)
        {
            logger.Log(e.Message, LogLevel.Failure, LogContext.PythonNerService);
            return 1;
        }

        try
        {
            // var inputPaths = fileHelper.GetInputFiles();
            // var allPaths = await archiveExtractor.ExtractArchives(inputPaths);
            var paths = fileHelper.AreAccessible(allPaths);
            
            var channel = Channel.CreateBounded<string>(new BoundedChannelOptions(settings.ChannelCapacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleWriter = true,
                SingleReader = false
            });

            var producer = Task.Run(async () =>
            {
                try
                {
                    foreach (var path in paths)
                    {
                        await channel.Writer.WriteAsync(path);
                    }

                    channel.Writer.TryComplete();
                }
                catch (Exception ex)
                {
                    channel.Writer.TryComplete(ex);
                    throw;
                }
            });

            var consumers = Enumerable.Range(0, settings.ThreadsCapacity).Select(_ => Task.Run(async () =>
                {
                    await foreach (var filePath in channel.Reader.ReadAllAsync())
                    {
                        await RunParseAsync(filePath);
                    }
                }))
                .ToArray();

            await Task.WhenAll(consumers.Append(producer));

            _stats.ExecutionEnd = DateTime.Now;
            logger.LogExecutionStats(_stats);
            
            logger.Log($"Execution finished successfully. Parsed {paths.Count()} files. Current DateTime is " + 
                       $"{DateTime.Now.ToString("F", CultureInfo.InvariantCulture)}", LogLevel.Success, LogContext.Orchestrator);
            
            logger.Log("Program will exit with exit code 0");
            return 0;
        }
        catch (Exception e)
        {
            logger.Log(e.ToString(), LogLevel.Failure, LogContext.Orchestrator);
            return 1;
        }
        finally
        {
            await pythonNerService.Stop();
            fileHelper.RemoveEmptyDirectories();
        }
    }

    private async Task RunParseAsync(string filePath)
    {
        logger.Log("Started: " + Path.GetFileName(filePath), LogLevel.Info, LogContext.Parsing);

        var isAccessible = fileHelper.IsAccessible(filePath);
        var isTextual = fileHelper.IsTextual(filePath);
        var isExcel = FileHelper.IsExcel(filePath);

        if (!isAccessible || !(isTextual || isExcel))
            return;
        
        using var parseLogger = await parseLoggerFactory.CreateAsync(filePath);
        ParseStats parseStats = new ParseStats(ExecutionId, parseLogger, filePath);

        try
        {
            if (FileHelper.IsExcel(filePath))
            {
                await ExcelParser.ParseAsync(filePath, parseLogger, parseStats, settings);
            }
            else
            {
                EncodingDetector encodingDetector = new(filePath, parseLogger, parseStats);
                List<EncodingSegment> encodingSegments = await encodingDetector.DetectEncodingSegments();

                string parsePath = await EncodingConverter.ConvertFileToUtf8(encodingSegments, parseLogger);
                
                await fileHelper.IsReadable(parsePath);

                using ParsingOrchestrator parsingOrchestrator = new(parsePath, parseLogger, parseStats, settings);
                await parsingOrchestrator.ParseAsync();
            }

            await parseLogger.LogParseStats(parseStats);

            lock (_stats)
            {
                _stats.FilesParsed.Add(parseStats.ParseId);
                _stats.LinesParsed += parseStats.LinesRead;
                _stats.BytesParsed += parseStats.BytesRead;
                _stats.RecordsParsed += parseStats.RecordsRead;
                _stats.MalformedRecordsRead += parseStats.MalformedRead;
            }
        
            logger.Log($"Finished: {parseStats.FileName}", LogLevel.Success, LogContext.Parsing);
        }
        catch (Exception e)
        {
            logger.Log($"{parseStats.ParseId} : {parseStats.FileName}: {e}", LogLevel.Failure, LogContext.Orchestrator);
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