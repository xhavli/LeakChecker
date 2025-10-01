using System.Diagnostics;
using System.Globalization;
using System.Text;
using LeakChecker.EncodingDetection;
using LeakChecker.ContentDetection;
using LeakChecker.ContentDetection.RecognitionService;
using LeakChecker.FormatDetection;
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
        int success = 0;

        var filePaths = FilesDelimiters.FilesEncodingsDictionary.Keys.ToList();
        var tasks = filePaths.Select(async filePath =>
        {
            if (!File.Exists(filePath))
            {
                await execLogger.Log($"File not found: {filePath}", LogLevel.Warning);
                return;
            }
            
            try
            {
                FileLogger fileLogger = new FileLogger(filePath, config.LogDirectory);
                FileStats fileStats = new()
                {
                    FilePath = filePath,
                    FileName = fileLogger.SubjectFileName,
                    FileBytes = fileLogger.SubjectFileBytes,
                    ProcessingStart = fileLogger.ProcessingStart,
                };
                
                // TODO tmp unused, this is correct way which will be developed later
                // var encodingSegments = await encodingDetector.DetectEncodingFromFilePath(file);
                // foreach (var segment in encodingSegments.OrderBy(s => s.StartOffset))
                // {
                //     Encoded encoding;
                //     await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                //     fileStream.Seek(segment.StartOffset, SeekOrigin.Begin);
                //     
                //     try
                //     {
                //         encoding = Encoded.GetEncoding(segment.Encoded);
                //     }
                //     catch (Exception e)
                //     {
                //         await file.Log(LogLevel.Error, e.Message);
                //         await file.Log(LogLevel.Warning, $"Encoded set to default [{Encoded.UTF8.WebName}]");
                //         encoding = Encoded.UTF8;
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
                Encoding encoding = await EncodingDetector.DetectEncodingFromStream(fileLogger);   // TODO delete this method
                
                // TODO format detector will detect pattern of content with delimiters
                // TODO detect format like first line starts with INSERT INTO...
                // TODO create a pattern how content looks like
                char delimiter = await FormatDetector.DetectDelimiterFromFile(fileLogger);
                if (delimiter == FilesDelimiters.FilesEncodingsDictionary[filePath])
                {
                    success++;
                }
                else
                {
                    await fileLogger.Log("Detected delimiter not match", LogLevel.Warning, LogContext.Format);
                    delimiter = FilesDelimiters.FilesEncodingsDictionary[filePath];
                }

                ContentDetector contentDetector = new(fileLogger, fileStats);
                await contentDetector.ProcessFile(encoding, delimiter);
                
                Console.WriteLine();
                fileStats.ProcessingEnd = DateTime.Now;
                fileStats.PrintFileStats();
                Console.WriteLine();
                
            }
            catch (Exception e)
            {
                await execLogger.Log($"{filePath}: {e}", LogLevel.Exception, LogContext.Main);
            }
        });

        await Task.WhenAll(tasks);
        
        // pythonNerService.Stop();
        
        await execLogger.Log($"Format success rate is {success}/{FilesDelimiters.FilesEncodingsDictionary.Keys.Count}");
        await execLogger.Log($"Execution finished successfully. Time taken {sw.Elapsed}, current DateTime is " +
                         $"{DateTime.Now.ToString("F", CultureInfo.InvariantCulture)}", LogLevel.Success, LogContext.Main);
        await execLogger.Log("Program will exit with code 0");
        Environment.Exit(0);
    }
}