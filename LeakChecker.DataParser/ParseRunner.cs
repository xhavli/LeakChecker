using System.Globalization;
using System.Threading.Channels;
using LeakChecker.DataParser.Content.Detection.RecognitionService;
using LeakChecker.DataParser.Content.Parse;
using LeakChecker.DataParser.Data;
using LeakChecker.DataParser.Encodings;
using LeakChecker.DataParser.Encodings.Conversion;
using LeakChecker.DataParser.Encodings.Detection;
using LeakChecker.DataParser.Logging;
using LeakChecker.DataParser.Logging.Execution;
using LeakChecker.DataParser.Logging.Parse;
using LeakChecker.DataParser.Stats.Execution;
using LeakChecker.DataParser.Stats.Parse;
using LeakChecker.DataParser.Utilities;
using LeakChecker.DataParser.Utilities.ArchiveExtraction;
using LeakChecker.DataParser.Utilities.Settings;

namespace LeakChecker.DataParser;

public sealed class ParserRunner(
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
            await logger.Log(e.Message, LogLevel.Failure, LogContext.PythonNerService);
            return 1;
        }

        try
        {
            var inputPaths = fileHelper.GetInputFiles();
            var paths = await archiveExtractor.ExtractArchives(inputPaths);
            
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
                        await ParseFileAsync(filePath);
                    }
                }))
                .ToArray();

            await Task.WhenAll(consumers.Append(producer));

            _stats.ExecutionEnd = DateTime.Now;
            await logger.LogExecutionStats(_stats);
            
            await logger.Log($"Execution finished successfully. Parsed {paths.Count()} files. Current DateTime is " + 
                             $"{DateTime.Now.ToString("F", CultureInfo.InvariantCulture)}", LogLevel.Success, LogContext.ParseRunner);
            
            await logger.Log("Program will exit with exit code 0");
            return 0;
        }
        catch (Exception e)
        {
            await logger.Log(e.ToString(), LogLevel.Failure, LogContext.ParseRunner);
            return 1;
        }
        finally
        {
            await pythonNerService.Stop();
            await fileHelper.RemoveEmptyDirectories();
        }
    }

    private async Task ParseFileAsync(string filePath)
    {
        await logger.Log("Started: " + Path.GetFileName(filePath), LogLevel.Info, LogContext.Parsing);

        if (!await fileHelper.IsAccessible(filePath) ||
            !await fileHelper.IsTextual(filePath) ||
            !FileHelper.IsExcel(filePath))
            return;

        using var parseLogger = await parseLoggerFactory.CreateAsync(filePath);
        ParseStats parseStats = new ParseStats(ExecutionId, parseLogger, filePath);

        try
        {
            if (FileHelper.IsExcel(filePath))
            {
                await ExcelParser.ParseFile(filePath, parseLogger, parseStats, settings.SchemaThreshold);
            }
            else
            {
                EncodingDetector encodingDetector = new(filePath, parseLogger, parseStats);
                List<EncodingSegment> encodingSegments = await encodingDetector.DetectEncodingSegments();

                string parsePath = await EncodingConverter.ConvertFileToUtf8(encodingSegments, parseLogger);
                
                await fileHelper.IsReadable(parsePath);

                using ContentParser contentParser = new(parsePath, parseLogger, parseStats, settings);
                await contentParser.ParseFile();
            }

            await parseLogger.LogParseStats(parseStats);

            lock (_stats)
            {
                _stats.FilesParsed.Add(parseStats.ParseId);
                _stats.LinesParsed += parseStats.LinesRead;
                _stats.BytesParsed += parseStats.BytesRead;
                _stats.RecordsParsed += parseStats.RecordsRead;
                _stats.MalformedRecordsRead += parseStats.MalformedRecordsRead;
            }
        }
        catch (Exception e)
        {
            await logger.Log($"{parseStats.ParseId} : {parseStats.FileName}: {e}", LogLevel.Failure, LogContext.ParseRunner);
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

        await logger.Log($"Finished: {parseStats.FileName}", LogLevel.Success, LogContext.Parsing);
    }
}