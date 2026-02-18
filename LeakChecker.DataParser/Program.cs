using System.Globalization;
using System.Text;
using System.Threading.Channels;
using LeakChecker.DataParser.Content.Detection.RecognitionService;
using LeakChecker.DataParser.Content.Parsing;
using LeakChecker.DataParser.Data;
using LeakChecker.DataParser.Logging;
using LeakChecker.DataParser.Logging.Execution;
using LeakChecker.DataParser.Logging.Parse;
using LeakChecker.DataParser.Utilities;
using LeakChecker.DataParser.Utilities.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LeakChecker.DataParser;

public static class Program
{
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
            Console.WriteLine("Program will exit with exit code 1.");
            Console.ResetColor();
            return 1;
        }
        
        var services = new ServiceCollection();
        services.AddSingleton(config);
        
        services.AddSingleton<IParseLoggerFactory, ParseLoggerFactory>();
        services.AddSingleton<ExecutionLogger>();
        
        var provider = services.BuildServiceProvider();
        var executionLogger = provider.GetRequiredService<ExecutionLogger>();
        var loggerFactory = provider.GetRequiredService<IParseLoggerFactory>();
        Guid executionId = Guid.NewGuid();
        var stats = new ExecutionStats(executionId, executionLogger.ExecutionStart);
        var fileHelper = new FileHelper(executionLogger);
        
        PythonNerService pythonNerService = new PythonNerService(executionLogger);
        try
        {
            // await pythonNerService.Start(config.PythonNerService, config.PythonNerServArgs);
            await pythonNerService.WaitForStart(config.CsharpPort, config.PythonPort, config.ConnectionTimeout);
        }
        catch (Exception e)
        {
            await executionLogger.Log(e.Message, LogLevel.Failure, LogContext.PythonNerService);
            return 1;
        }
        
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Encoding utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false); // false = no BOM
        Console.InputEncoding = utf8;   // Enforce UTF8 encoding which can handle cyrilic characters 
        Console.OutputEncoding = utf8;
        
        var data = FilePaths.TestFiles;
        
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
                if (!await fileHelper.IsAccessible(filePath) || !await fileHelper.IsSupported(filePath)) continue;

                using var parseLogger = await loggerFactory.CreateAsync(executionId, filePath);
                ParseStats parseStats = ParseStats.Create(executionId, parseLogger, filePath);

                try
                {
                    if (FileHelper.IsExcel(filePath))
                    {
                        // Avoid encoding conversion of zipped XML file
                        await ExcelParser.ParseFile(filePath, parseLogger, parseStats, config.SchemaThreshold);
                    }
                    else
                    {
                        // EncodingDetector encodingDetector = new(parseLogger, parseStats);
                        // List<EncodingSegment> encodingSegments = await encodingDetector.DetectFileEncodings();
                        //
                        // await EncodingConverter.ConvertFileToUtf8(parseLogger, encodingSegments);

                        await fileHelper.IsReadable(filePath);
                        
                        string parseFile = parseLogger.SubjectFilePath;   //TODO remove this only for test purposes
                        // string parseFile = parseLogger.SubjectTmpFilePath;    //TODO use this in deployment
                        using ContentParser contentParser = await ContentParser.CreateAsync(parseFile, parseLogger, parseStats, utf8, config.SchemaThreshold);
                        await contentParser.ProcessFile();
                    }

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
                    await executionLogger.Log($"{parseLogger.ParseId} : {parseLogger.SubjectFileName}: {e}", LogLevel.Failure, LogContext.Main);
                }
                finally
                {
                    // try
                    // {
                    //     File.Delete(parseLogger.SubjectTmpFilePath);
                    // }
                    // catch (Exception e)
                    // {
                    //     await ExecutionLogger.Log($"{parseId} : {parseLogger.SubjectFileName}: {e}", LogLevel.Warning, LogContext.Main);
                    // }
                }

                await executionLogger.Log($"Finished: {parseLogger.SubjectFileName}", LogLevel.Success, LogContext.Parsing);
            }
        })).ToArray();

        await Task.WhenAll(consumers.Append(producer));
        await pythonNerService.Stop();
        
        stats.ExecutionEnd = DateTime.Now;
        await executionLogger.LogExecutionStats(stats);
        
        await executionLogger.Log($"Execution finished successfully. Parsed {data.Length} files. Current DateTime is " +
                         $"{DateTime.Now.ToString("F", CultureInfo.InvariantCulture)}", LogLevel.Success, LogContext.Main);
        await executionLogger.Log("Program will exit with exit code 0");
        return 0;
    }
}