using System.Diagnostics;
using System.Globalization;
using System.Threading.Channels;
using LeakChecker.Common.Enums;
using LeakChecker.DataParser.Content.Detection.RecognitionService;
using LeakChecker.DataParser.Data;
using LeakChecker.DataParser.Helpers.FileHelp;
using LeakChecker.DataParser.Helpers.Settings;
using LeakChecker.DataParser.Logging;
using LeakChecker.DataParser.Logging.Execution;
using LeakChecker.DataParser.Logging.Parse;
using LeakChecker.DataParser.Stats.Execution;

namespace LeakChecker.DataParser.Orchestration;

public sealed class ExecutionOrchestrator(
    ISettings settings,
    FileHelper fileHelper,
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
            IEnumerable<string> paths;
            if (settings.Environment.StartsWith("Development", StringComparison.OrdinalIgnoreCase))
            {
                logger.Log($"Paths loaded from {nameof(FilePaths.Utf8)} in {settings.Environment} environment");
                paths = FilePaths.Utf8;
            }
            else
            {
                paths = await fileHelper.GetPathsFromInputDirectory();
            }

            
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
                        ParsingOrchestrator parser = new ParsingOrchestrator(filePath, settings, fileHelper, _stats, logger, parseLoggerFactory);
                        await parser.RunAsync();
                    }
                }))
                .ToArray();

            await Task.WhenAll(consumers.Append(producer));

            _stats.ExecutionEnd = DateTime.Now;
            logger.LogExecutionStats(_stats);
            await settings.Database.SaveExecutionOne(_stats);
            
            logger.Log($"Execution finished successfully. Parsed {paths.Count()} files. Current DateTime is " + 
                       $"{DateTime.Now.ToString("F", CultureInfo.InvariantCulture)}", LogLevel.Success, LogContext.Execution);

            logger.Log($"Creating MongoDB indexes for {CollectionType.Identities} collection.");
            Stopwatch sw = Stopwatch.StartNew();
            await settings.Database.CreateIndexes();
            logger.Log($"MongoDB indexes created in {sw.Elapsed}.", LogLevel.Success, LogContext.Execution);
            
            logger.Log("Program will exit with exit code 0");
            return 0;
        }
        catch (Exception e)
        {
            logger.Log(e.ToString(), LogLevel.Failure, LogContext.Execution);
            return 1;
        }
        finally
        {
            await pythonNerService.Stop();
            fileHelper.RemoveEmptyDirectories();
        }
    }
}