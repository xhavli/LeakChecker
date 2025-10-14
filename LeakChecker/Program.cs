using System.Diagnostics;
using System.Globalization;
using System.Text;
using LeakChecker.Content.Detection.RecognitionService;
using LeakChecker.Content.Processing;
using LeakChecker.Format;
using LeakChecker.Logging;
using LeakChecker.Logging.ExecutionLogging;
using LeakChecker.Logging.FileLogging;
using LeakChecker.Utilities;
using LeakChecker.Tests;

namespace LeakChecker;

public static class Program
{
    public static async Task Main()
    {
        Stopwatch sw = Stopwatch.StartNew();
        
        AppConfig config = AppConfig.ParseAppConfig();
        ExecutionLogger execLogger = new ExecutionLogger(config);
        
        PythonNerService pythonNerService = new PythonNerService(execLogger);
        // await pythonNerService.Start();
        await pythonNerService.WaitForStart();
        
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Encoding utf8 = new UTF8Encoding(false); // false = no BOM
        Console.OutputEncoding = utf8;  // Enforce UTF8 encoding which can handle cyrilic characters 
        
        int success = 0;

        var data = FilesDelimiters.FilesDelimitersDictUtf8;
        var filePaths = data.Keys.ToList();
        var tasks = filePaths.Select(async filePath =>
        {
            if (!File.Exists(filePath))
            {
                await execLogger.Log($"File not found: {filePath}", LogLevel.Warning);
                return;
            }
            
            FileLogger fileLogger = await FileLogger.CreateAsync(filePath, config);
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
                
                // TODO test this and use this
                //  var result = DelimiterHeuristic.Analyze(filePath, maxLines: 10_000);
                //  Console.WriteLine($"Best delimiter: {(result.BestDelimiter is '\t' ? "\\t" : result.BestDelimiter?.ToString() ?? "none")}");
                //  Console.WriteLine($"Sampled {result.SampledLines} lines (~{result.SampledBytes} chars)");
                //  foreach (var c in result.Candidates.Take(10))
                //      Console.WriteLine(c);
                //
                //  return;
                
                char delimiter = await FormatDetector.DetectDelimiterFromFile(fileLogger);
                if (delimiter == data[filePath])
                {
                    success++;
                }
                else
                {
                    await fileLogger.Log("Detected delimiter not match. Setting predefined", LogLevel.Warning, LogContext.Format);
                    delimiter = data[filePath];
                }

                ContentProcessor contentProcessor = await ContentProcessor.CreateAsync(delimiter, fileLogger, fileStats);
                await contentProcessor.ProcessFile();

                Console.WriteLine();
                fileStats.ProcessingEnd = DateTime.Now;
                fileStats.PrintFileStats();
                Console.WriteLine();

            }
            catch (Exception e)
            {
                await execLogger.Log($"{filePath}: {e}", LogLevel.Exception, LogContext.Main);
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
        
        await execLogger.Log($"Delimiter success rate is {success}/{data.Keys.Count}");
        await execLogger.Log($"Execution finished successfully. {data.Keys.Count} files processed. Time taken {sw.Elapsed}, current DateTime is " +
                         $"{DateTime.Now.ToString("F", CultureInfo.InvariantCulture)}", LogLevel.Success, LogContext.Main);
        await execLogger.Log("Program will exit with code 0");
        Environment.Exit(0);
    }
}