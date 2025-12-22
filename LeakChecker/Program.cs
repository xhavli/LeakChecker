using System.Globalization;
using System.Text;
using System.Threading.Channels;
using LeakChecker.Content.Detection.RecognitionService;
using LeakChecker.Content.Processing;
using LeakChecker.Data;
using LeakChecker.Encodings;
using LeakChecker.Encodings.Conversion;
using LeakChecker.Encodings.Detection;
using LeakChecker.Logging;
using LeakChecker.Logging.ExecutionLogging;
using LeakChecker.Logging.FileLogging;
using LeakChecker.Utilities.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LeakChecker;

public static class Program
{
    private static ExecutionLogger? ExecutionLogger { get; set; }

    public static async Task<int> Main()
    {
        const string configJson = "appsettings.json";
        AppConfig config;
        try
        {
            config = AppConfigParser.LoadFromFile(configJson);
        }
        catch (Exception e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            await Console.Error.WriteLineAsync($"[EXCEPTION] [CONFIG] {e.Message}");
            await Console.Error.WriteLineAsync(e.ToString());
            Console.ResetColor();
            Console.WriteLine("Program will exit with exit code 1.");
            return 1;
        }
        
        var services = new ServiceCollection();
        services.AddSingleton(config);
        
        services.AddSingleton<IFileLoggerFactory, FileLoggerFactory>();
        services.AddSingleton<ExecutionLogger>();
        
        var provider = services.BuildServiceProvider();
        ExecutionLogger = provider.GetRequiredService<ExecutionLogger>();
        var loggerFactory = provider.GetRequiredService<IFileLoggerFactory>();
        Guid executionId = Guid.NewGuid();
        var stats = new ExecutionStats(executionId, ExecutionLogger.ExecutionStart);
        
        PythonNerService pythonNerService = new PythonNerService(ExecutionLogger);
        try
        {
            // await pythonNerService.Start(config.PythonNerService, config.PythonNerServArgs);
            await pythonNerService.WaitForStart(config.CsharpPort, config.PythonPort, config.ConnectionTimeout);
        }
        catch (Exception e)
        {
            await ExecutionLogger.Log(e.Message, LogLevel.Failure, LogContext.PythonNerService);
            return 1;
        }
        
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Encoding utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false); // false = no BOM
        Console.InputEncoding = utf8;   // Enforce UTF8 encoding which can handle cyrilic characters 
        Console.OutputEncoding = utf8;
        
        var data = FilePaths.FilesPathsUtf8;
        
        int threads = config.ThreadsCapacity;   // Degree of parallelism
        int capacity = config.ChannelCapacity;  // Channel capacity

        // Bounded channel = backpressure + stable memory
        var channel = Channel.CreateBounded<string>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleWriter = true,
            SingleReader = false
        });

        // Producer: enqueue file paths
        var producer = Task.Run(async () =>
        {
            try
            {
                foreach (var filePath in data)
                {
                    await channel.Writer.WriteAsync(filePath);
                }

                channel.Writer.TryComplete();
            }
            catch (Exception ex)
            {
                channel.Writer.TryComplete(ex);
                throw;
            }
        });

        // Consumers: process files with bounded concurrency (threads)
        var consumers = Enumerable.Range(0, threads).Select(_ => Task.Run(async () =>
        {
            await foreach (var filePath in channel.Reader.ReadAllAsync())
            {
                try
                {
                    ValidateFileReadability(filePath);
                }
                catch (Exception e)
                {
                    await ExecutionLogger.Log(e.Message, LogLevel.Warning, LogContext.Main);
                    continue;
                }

                Guid parseId = Guid.NewGuid();
                DateTime parseStart = DateTime.Now;
                using var parseLogger = await loggerFactory.CreateAsync(parseId, executionId, parseStart, filePath);

                FileStats parseStats = new()
                {
                    ParseId = parseId,
                    ExecutionId = executionId,
                    ParseStart = parseStart,
                    FileName = Path.GetFileName(filePath),
                    FilePath = filePath,
                    FileSize = new FileInfo(filePath).Length,
                };

                try
                {
                    EncodingDetector encodingDetector = new(parseLogger, parseStats);
                    List<EncodingSegment> encodingSegments = await encodingDetector.DetectFileEncodings();
                    
                    await EncodingConverter.ConvertFileToUtf8(parseLogger, encodingSegments);
                    
                    using ContentProcessor contentProcessor = await ContentProcessor.CreateAsync(parseLogger, parseStats, utf8, config.SchemaThreshold);
                    await contentProcessor.ProcessFile();

                    parseStats.ParseEnd = DateTime.Now;
                    await parseLogger.LogFileStats(parseStats);
                    
                    lock (stats)
                    {
                        stats.MalformedRecordsRead += parseStats.MalformedRecordsRead;
                        stats.RecordsParsed += parseStats.RecordsRead;
                        stats.LinesParsed += parseStats.LinesRead;
                        stats.BytesParsed += parseStats.BytesRead;
                        stats.FilesParsed.Add(parseStats.ParseId);
                    }
                }
                catch (Exception e)
                {
                    await ExecutionLogger.Log($"{parseId} : {parseLogger.SubjectFileName}: {e}", LogLevel.Failure, LogContext.Main);
                }
                finally
                {
                    try
                    {
                        File.Delete(parseLogger.SubjectTmpFilePath);
                    }
                    catch (Exception e)
                    {
                        await ExecutionLogger.Log($"{parseId} : {parseLogger.SubjectFileName}: {e}", LogLevel.Warning, LogContext.Main);
                    }
                }
            }
        })).ToArray();

        await Task.WhenAll(consumers.Append(producer));
        await pythonNerService.Stop();
        
        Console.WriteLine();
        stats.ExecutionEnd = DateTime.Now;
        await ExecutionLogger.LogExecutionStats(stats);
        Console.WriteLine();
        
        await ExecutionLogger.Log($"Execution finished successfully. Parsed {data.Length} files. Current DateTime is " +
                         $"{DateTime.Now.ToString("F", CultureInfo.InvariantCulture)}", LogLevel.Success, LogContext.Main);
        await ExecutionLogger.Log("Program will exit with exit code 0");
        return 0;
    }

    private static void ValidateFileReadability(string filePath)
    {
        try
        {
            using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read);
        }
        catch (FileNotFoundException)
        {
            throw new FileNotFoundException($"File in path: '{filePath}' does not exists.", filePath);
        }
        catch (UnauthorizedAccessException)
        {
            throw new UnauthorizedAccessException(
                $"{AppDomain.CurrentDomain.FriendlyName} does not have permission to read the file in path: '{filePath}'.");
        }
        catch (IOException)
        {
            throw new IOException($"File in path: '{filePath}' is locked or in use.");
        }
    }
}