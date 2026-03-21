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
using LeakChecker.DataParser.Utilities;
using LeakChecker.DataParser.Utilities.ArchiveExtraction;
using LeakChecker.DataParser.Utilities.Settings;

namespace LeakChecker.DataParser;

public sealed class ParserRunner(
    ISettings settings,
    IParseLoggerFactory parseLoggerFactory,
    ExecutionLogger executionLogger,
    FileHelper fileHelper,
    PythonNerService pythonNerService)
{
    public async Task<int> RunAsync()
    {
        Guid executionId = Guid.NewGuid();
        var stats = new ExecutionStats(executionId, executionLogger.ExecutionStart);

        try
        {
            await pythonNerService.Start();
            await pythonNerService.WaitStart();
        }
        catch (Exception e)
        {
            await executionLogger.Log(e.Message, LogLevel.Failure, LogContext.PythonNerService);
            return 1;
        }

        try
        {
            var inputPaths = FilePaths.Original;
            var paths = await ArchiveExtractor.ExtractArchives(inputPaths, settings.TmpDirectory);

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
                        await ParseFileAsync(executionId, stats, filePath);
                    }
                }))
                .ToArray();

            await Task.WhenAll(consumers.Append(producer));

            stats.ExecutionEnd = DateTime.Now;
            await executionLogger.LogExecutionStats(stats);
            await executionLogger.Log($"Execution finished successfully. Parsed {paths.Count()} files. Current DateTime is " +
                                       $"{DateTime.Now.ToString("F", CultureInfo.InvariantCulture)}", LogLevel.Success, LogContext.ParseRunner);
            await executionLogger.Log("Program will exit with exit code 0");
            return 0;
        }
        catch (Exception e)
        {
            await executionLogger.Log(e.ToString(), LogLevel.Failure, LogContext.ParseRunner);
            return 1;
        }
        finally
        {
            await pythonNerService.Stop();
        }
    }

    private async Task ParseFileAsync(Guid executionId, ExecutionStats stats, string filePath)
    {
        await executionLogger.Log("Started: " + Path.GetFileName(filePath), LogLevel.Info, LogContext.Parsing);

        if (!await fileHelper.IsAccessible(filePath) || !await fileHelper.IsSupported(filePath))
            return;

        using var parseLogger = await parseLoggerFactory.CreateAsync(filePath);
        ParseStats parseStats = new ParseStats(executionId, parseLogger, filePath);

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
                

                await EncodingConverter.ConvertFileToUtf8(encodingSegments, parseLogger, parseStats);


                await fileHelper.IsReadable(filePath);

                string parseFile = parseLogger.SubjectFilePath;
                using ContentParser contentParser = new (parseFile, parseLogger, parseStats, settings);
                await contentParser.ParseFile();
            }

            await parseLogger.LogParseStats(parseStats);

            lock (stats)
            {
                stats.FilesParsed.Add(parseStats.ParseId);
                stats.LinesParsed += parseStats.LinesRead;
                stats.BytesParsed += parseStats.BytesRead;
                stats.RecordsParsed += parseStats.RecordsRead;
                stats.MalformedRecordsRead += parseStats.MalformedRecordsRead;
            }
        }
        catch (Exception e)
        {
            await executionLogger.Log($"{parseStats.ParseId} : {parseStats.FileName}: {e}", LogLevel.Failure, LogContext.ParseRunner);
        }
        finally
        {
            // if (parseLogger.SubjectFilePath.StartsWith(_config.TmpDirectory, StringComparison.OrdinalIgnoreCase))
            //     File.Delete(parseLogger.SubjectFilePath);
            //
            // if (parseLogger.SubjectTmpFilePath.StartsWith(_config.TmpDirectory, StringComparison.OrdinalIgnoreCase))
            //     File.Delete(parseLogger.SubjectTmpFilePath);
        }

        await executionLogger.Log($"Finished: {parseStats.FileName}", LogLevel.Success, LogContext.Parsing);
    }
}