using System.Diagnostics;
using System.Globalization;
using System.Text;
using LeakChecker.Content.Detection;
using LeakChecker.Content.Detection.RecognitionService;
using LeakChecker.Content.Processing;
using LeakChecker.Data;
using LeakChecker.Encodings;
using LeakChecker.Encodings.Detection;
using LeakChecker.Format;
using LeakChecker.Logging;
using LeakChecker.Logging.ExecutionLogging;
using LeakChecker.Logging.FileLogging;
using LeakChecker.Utilities;
using LeakChecker.Utilities.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LeakChecker;

public static class Program
{
    private static ExecutionLogger? ExecLogger { get; set; }

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
        
        object consoleLock = new object();
        Stopwatch sw = Stopwatch.StartNew();
        
        var services = new ServiceCollection();
        services.AddSingleton(config);
        
        services.AddSingleton<IFileLoggerFactory, FileLoggerFactory>();
        services.AddSingleton<ExecutionLogger>();
        
        var provider = services.BuildServiceProvider();
        ExecLogger = provider.GetRequiredService<ExecutionLogger>();
        var loggerFactory = provider.GetRequiredService<IFileLoggerFactory>();
        
        PythonNerService pythonNerService = new PythonNerService(ExecLogger);
        // await pythonNerService.Start();
        // await pythonNerService.WaitForStart();   //TODO
        
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Encoding utf8 = new UTF8Encoding(false); // false = no BOM
        Console.InputEncoding = utf8;   // Enforce UTF8 encoding which can handle cyrilic characters 
        Console.OutputEncoding = utf8; 
        
        int success = 0;

        var data = FilesDelimiters.FilesDelimitersDictUtf8;
        var filePaths = data.Keys.ToList();
        var tasks = filePaths.Select(async filePath =>
        {
            try
            {
                ValidateFileReadability(filePath);
            }
            catch (Exception e)
            {
                await ExecLogger.Log(e.Message, LogLevel.Exception);
                return;
            }
            
            var fileLogger = await loggerFactory.CreateAsync(filePath);
            FileStats fileStats = new()
            {
                FilePath = filePath,
                FileName = fileLogger.SubjectFileName,
                FileBytes = fileLogger.SubjectFileBytes,
                ProcessingStart = fileLogger.ProcessingStart,
            };

            try
            {
                // TODO
                //  EncodingDetector encDetector = new(fileLogger, fileStats);
                //  List<EncodingSegment> encodingSegments = await encDetector.DetectFileEncodings();
                //  fileStats.EncodingSegments = encodingSegments;
                //  await EncodingConverter.ConvertFileToUtf8(fileLogger, encodingSegments);
                
                char delimiter = data[filePath];    //TODO
                //TODO use this
                // var result = DelimiterHeuristic.Analyze(filePath, maxLines: 100_000);
                // if (result.BestDelimiter != null)
                // {
                //     delimiter = (char)result.BestDelimiter;
                // }
                // else
                // {
                //     await fileLogger.Log("Delimiter detection failed. Setting default delimiter ':'",
                //          LogLevel.Warning, LogContext.Format);
                //     // delimiter = ':'; TODO use after remove FileDelimiters.cs
                //     delimiter = data[filePath];
                // }
                
                //TODO delete this only for testing purposes
                // lock (consoleLock)
                // {
                //  if (delimiter == data[filePath])
                //  {
                //      success++;
                //      Console.ForegroundColor = ConsoleColor.Green;
                //      Console.WriteLine($"\n{fileStats.FileName} delimiter match '{data[filePath]}'");
                //      Console.ResetColor();
                //  }
                //  else
                //  {
                //      Console.ForegroundColor = ConsoleColor.Red;
                //      Console.WriteLine($"\n{fileStats.FileName} delimiter '{delimiter}' not match '{data[filePath]}'");
                //      Console.ResetColor();
                //  }
                //  Console.WriteLine($"Sampled {result.SampledLines} lines (~{result.SampledBytes} chars)");
                //  foreach (var c in result.Candidates.Take(10))
                //      Console.WriteLine(c);
                // }

                ContentProcessor contentProcessor = await ContentProcessor.CreateAsync(delimiter, fileLogger, fileStats);
                await contentProcessor.ProcessFile();

                Console.WriteLine();
                fileStats.ProcessingEnd = DateTime.Now;
                fileStats.PrintFileStats();
                Console.WriteLine();

            }
            catch (Exception e)
            {
                await ExecLogger.Log($"{filePath}: {e}", LogLevel.Exception, LogContext.Main);
            }
            finally
            {
                // if (!File.Exists(fileLogger.SubjectTmpFilePath))
                // {
                //     await execLogger.Log($"TMP File not found: {filePath}", LogLevel.Warning);
                // }
                // else
                // {
                //     File.Delete(fileLogger.SubjectTmpFilePath);
                // }
            }
        });

        await Task.WhenAll(tasks);
        
        // pythonNerService.Stop();
        
        await ExecLogger.Log($"Delimiter success rate is {success}/{data.Keys.Count}");
        await ExecLogger.Log($"Execution finished successfully. {data.Keys.Count} files processed. Time taken {sw.Elapsed}, current DateTime is " +
                         $"{DateTime.Now.ToString("F", CultureInfo.InvariantCulture)}", LogLevel.Success, LogContext.Main);
        await ExecLogger.Log("Program will exit with exit code 0");
        return 0;
    }

    private static void ValidateFileReadability(string filePath)
    {
        try
        {
            using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read);
            //TODO is it necessary?
            // using var reader = new StreamReader(stream);
            // for (int i = 0; i < 10; i++)
            // {
            //     _ = reader.ReadLine();
            // }
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