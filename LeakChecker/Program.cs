using System.Diagnostics;
using System.Globalization;
using System.Text;
using LeakChecker.EncodingDetection;
using LeakChecker.ContentDetection;
using LeakChecker.ContentDetection.ItemRecognition;
using LeakChecker.ContentDetection.RecognitionService;
using LeakChecker.FormatDetection;
using LeakChecker.Logging;
using LeakChecker.Logging.ExecutionLogging;
using LeakChecker.Logging.FileLogging;
using LeakChecker.Utilities;
using LeakChecker.Tests;
using Microsoft.Recognizers.Text.DateTime;

namespace LeakChecker;

public static class Program
{
    public static async Task Main()
    {
        Stopwatch sw = Stopwatch.StartNew();
        
        AppConfig config = AppConfig.ParseAppConfig();
        ExecutionLogger logger = new ExecutionLogger(config);
        
        PythonNerService pythonNerService = new PythonNerService(logger);
        // await pythonNerService.Start();
        await pythonNerService.WaitForStart();
        
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        int success = 0;

        var filePaths = FilesDelimiters.FilesEncodingsDictionary.Keys.ToList();
        var tasks = filePaths.Select(async filePath =>
        {
            if (!File.Exists(filePath))
            {
                await logger.Log($"File not found: {filePath}", LogLevel.Warning);
                return;
            }
            
            try
            {
                FileLogger file = new FileLogger(filePath, config.LogDirectory);
                // TODO tmp unused, this is correct way which will be developed later
                // var encodingSegments = await encodingDetector.DetectEncodingFromFilePath(file);
                // foreach (var segment in encodingSegments.OrderBy(s => s.StartOffset))
                // {
                //     Encoding encoding;
                //     await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                //     fileStream.Seek(segment.StartOffset, SeekOrigin.Begin);
                //     
                //     try
                //     {
                //         encoding = Encoding.GetEncoding(segment.EncodingName);
                //     }
                //     catch (Exception e)
                //     {
                //         await file.Log(LogLevel.Error, e.Message);
                //         await file.Log(LogLevel.Warning, $"Encoding set to default [{Encoding.UTF8.WebName}]");
                //         encoding = Encoding.UTF8;
                //     }
                //     
                //     using var reader = new StreamReader(fileStream, encoding, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
                //     
                //     long bytesToRead = segment.Length;
                //     long bytesRead = 0;
                //
                //     while (bytesRead < bytesToRead)
                //     {
                //         var line = await reader.ReadLineAsync();
                //         
                //         if (line == null) continue;  //???
                //         int readBytesByLine = encoding.GetByteCount(line);
                //         bytesRead += readBytesByLine;
                //         
                //         //formatDetector.DetectDelimiter()
                //     }
                // }
                
                
                // TODO tmp used, for experimental purposes
                Encoding encoding = await EncodingDetector.DetectEncodingFromStream(file);   // TODO delete this method
                
                // TODO format detector will detect pattern of content with delimiters
                // TODO detect format like first line starts with INSERT INTO...
                // TODO create a pattern how content looks like
                string delimiter = await FormatDetector.DetectDelimiterFromFile(file);
                if (delimiter == FilesDelimiters.FilesEncodingsDictionary[filePath])
                {
                    success++;
                }
                else
                {
                    await file.Log("Detected delimiter not match", LogLevel.Warning, LogContext.Delimiter);
                    delimiter = FilesDelimiters.FilesEncodingsDictionary[filePath];
                }
                
                await ContentDetector.ProcessFile(file, encoding, delimiter);
            }
            catch (Exception e)
            {
                await logger.Log($"{filePath}: {e.Message}", LogLevel.Exception, LogContext.Main);
            }
        });

        await Task.WhenAll(tasks);
        
        // pythonNerService.Stop();
        
        await logger.Log($"Delimiter success rate is {success}/{FilesDelimiters.FilesEncodingsDictionary.Keys.Count}");
        await logger.Log($"Execution finished successfully. Time taken {sw.Elapsed}, current DateTime is " +
                         $"{DateTime.Now.ToString("F", CultureInfo.InvariantCulture)}", LogLevel.Success, LogContext.Main);
        await logger.Log("Program will exit with code 0");
        Environment.Exit(0);
    }
}