using System.Diagnostics;
using System.Text;
using System.Text.Json;
using LeakChecker.ContentDetector;
using LeakChecker.EncodingDetection;
using LeakChecker.FileTracking;
using LeakChecker.FormatDetection;
using LeakChecker.Tests;
using LeakChecker.Utilities;

namespace LeakChecker;

public class Program
{
    private static AppConfig Config { get; set; } = null!;
    private static readonly object ConsoleLock = new();

    public static async Task Main()
    {
        Config = AppConfig.ParseAppConfig();
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        // EncodingDetector.PrintSupportedEncodings(); Environment.Exit(0);
        EncodingDetector encodingDetector = new EncodingDetector();
        FormatDetector formatDetector = new FormatDetector();
        ContentDetector.ContentDetector contentDetector = new ContentDetector.ContentDetector();
        
        int success = 0;
        
        Logger.LogInfo("Program started at: " + DateTime.Now.ToString("HH:mm:ss"));
        Stopwatch sw = Stopwatch.StartNew();

        var filePaths = FilesDelimiters.FilesEncodingsDictionary.Keys.ToList();
        var tasks = filePaths.Select(async filePath =>
        {
            if (!File.Exists(filePath))
            {
                Logger.LogWarning($"[MISSING] {filePath}");
                return;
            }

            try
            {
                FileContext file = new FileContext(filePath, Config.LogDirectory);
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
                Encoding encoding = await encodingDetector.DetectEncodingFromOneStream(file);   // TODO delete this method
                
                // TODO format detector will detect pattern of content with delimiters
                // TODO detect format like first line starts with INSERT INTO...
                // TODO create a pattern how content looks like
                string delimiter = await formatDetector.DetectDelimiter(file);
                if (delimiter == FilesDelimiters.FilesEncodingsDictionary[filePath])
                {
                    success++;
                }
                else
                {
                    await file.Log("Detected delimiter not match", LogLevel.Warning, LogContext.Delimiter);
                }
                
                await contentDetector.ProcessFile(file, delimiter, encoding);
                return;


                string firstLine = string.Empty;
                var url = $"http://localhost:8000/?text={Uri.EscapeDataString(firstLine)}";
                using HttpClient client = new HttpClient();
                try
                {
                    HttpResponseMessage response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                    string result = await response.Content.ReadAsStringAsync();
                    
                    List<PresidioEntity>? entities = JsonSerializer.Deserialize<List<PresidioEntity>>(result);
                    // Sort by score DESC, then start ASC, then end ASC
                    var sorted = entities
                        .OrderByDescending(e => e.Score)
                        .ThenBy(e => e.Start)
                        .ThenBy(e => e.End)
                        .ToList();
                    
                    var filtered = new List<PresidioEntity>();

                    lock (ConsoleLock)
                    {
                        // Print with text fragment from firstLine
                        Logger.LogInfo($"[OUTPUT] {filePath} -> \"{firstLine?.Trim()}\"");
                        foreach (var current in sorted)
                        {
                            bool overlaps = filtered.Any(existing =>
                                current.Start < existing.End && existing.Start < current.End);

                            if (!overlaps)
                            {
                                filtered.Add(current);
                            }
                        }

                        // Print filtered results
                        foreach (var entity in sorted)
                        {
                            string fragment = firstLine.Substring(entity.Start, entity.End - entity.Start);
                            Console.WriteLine($"{entity.EntityType} [{entity.Start}-{entity.End}], {fragment}, score={entity.Score:F2}");
                        }
                    }
                }
                catch (HttpRequestException e)
                {
                    Logger.LogError($"[EXCEPTION] response error from FastAPI: " + e.Message);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"[EXCEPTION] [MAIN] '{filePath}': {ex.Message}");
            }
        });

        await Task.WhenAll(tasks);
        
        sw.Stop();
        Logger.LogInfo($"Success rate is {success}/{FilesDelimiters.FilesEncodingsDictionary.Keys.Count}");
        Logger.LogInfo($"Time taken {sw.Elapsed}, current time is {DateTime.Now:T}");
        Logger.LogSuccess("Program successfully finished with exit code 0");
    }
}