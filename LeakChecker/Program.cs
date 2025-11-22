using System.Diagnostics;
using System.Globalization;
using System.Text;
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
    private static ExecutionLogger? ExeLogger { get; set; }

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

        Stopwatch sw = Stopwatch.StartNew();
        
        var services = new ServiceCollection();
        services.AddSingleton(config);
        
        services.AddSingleton<IFileLoggerFactory, FileLoggerFactory>();
        services.AddSingleton<ExecutionLogger>();
        
        var provider = services.BuildServiceProvider();
        ExeLogger = provider.GetRequiredService<ExecutionLogger>();
        var loggerFactory = provider.GetRequiredService<IFileLoggerFactory>();
        Guid executionId = Guid.NewGuid();
        var stats = new ExecutionStats(executionId);
        
        PythonNerService pythonNerService = new PythonNerService(ExeLogger);
        // await pythonNerService.Start();
        await pythonNerService.WaitForStart();   //TODO
        
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Encoding utf8 = new UTF8Encoding(false); // false = no BOM
        Console.InputEncoding = utf8;   // Enforce UTF8 encoding which can handle cyrilic characters 
        Console.OutputEncoding = utf8;

        var data = FilesDelimiters.FilesDelimitersDictRaw;
        var filePaths = data.Keys.ToList();
        var tasks = filePaths.Select(async filePath =>
        {
            try
            {
                ValidateFileReadability(filePath);
            }
            catch (Exception e)
            {
                await ExeLogger.Log(e.Message, LogLevel.Exception);
                return;
            }
            
            Guid parsingId = Guid.NewGuid();
            DateTime parseStart = DateTime.Now;
            using var parseLogger = await loggerFactory.CreateAsync(parsingId, executionId, parseStart, filePath);
            FileStats parseStats = new()
            {
                ParseId = parsingId,
                ExecutionId = executionId,
                ParseStart = parseStart,
                FileName = Path.GetFileName(filePath),
                FilePath = filePath,
                FileSize = new FileInfo(filePath).Length,
            };

            try
            {
                EncodingDetector encDetector = new(fileLogger, fileStats);
                List<EncodingSegment> encodingSegments = await encDetector.DetectFileEncodings();
                fileStats.EncodingSegments = encodingSegments;
                await EncodingConverter.ConvertFileToUtf8(fileLogger, encodingSegments);
                
                using ContentProcessor contentProcessor = await ContentProcessor.CreateAsync(fileLogger, fileStats, utf8);
                using ContentProcessor contentProcessor = await ContentProcessor.CreateAsync(parseLogger, parseStats, utf8);
                await contentProcessor.ProcessFile();

                parseStats.ParseEnd = DateTime.Now;
                await parseLogger.LogFileStats(parseStats);
                
                stats.FilesParsed.Add(parseStats.ParseId);
                stats.BytesParsed += parseStats.BytesRead;
                stats.LinesParsed += parseStats.RecordsCount;
            }
            catch (Exception e)
            {
                await ExeLogger.Log($"{filePath}: {e}", LogLevel.Exception, LogContext.Main);
            }
            finally
            {
                if (File.Exists(parseLogger.SubjectTmpFilePath))
                {
                    File.Delete(parseLogger.SubjectTmpFilePath);
                }
                else
                {
                    await ExeLogger.Log($"Temporary parse file for delete not found: {filePath}", LogLevel.Warning);
                }
            }
        });

        await Task.WhenAll(tasks);
        
        // pythonNerService.Stop();
        
        Console.WriteLine();
        stats.ParsingEnd = DateTime.Now;
        await ExeLogger.LogExecutionStats(stats);
        Console.WriteLine();
        
        await ExeLogger.Log($"Execution finished successfully. {data.Keys.Count} files processed. Time taken {sw.Elapsed}, current DateTime is " +
                         $"{DateTime.Now.ToString("F", CultureInfo.InvariantCulture)}", LogLevel.Success, LogContext.Main);
        await ExeLogger.Log("Program will exit with exit code 0");
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