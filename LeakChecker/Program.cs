using System.Diagnostics;
using System.Text;
using LeakChecker.Tests;
using LeakChecker.Utilities;
using EncodingDetector = LeakChecker.EncodingDetection.EncodingDetector;    // TODO naming and register AppConfig in DI

namespace LeakChecker;

public class Program
{
    private static AppConfig Config { get; set; } = null!;

    public static async Task Main()
    {
        Config = AppConfig.ParseAppConfig();
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        // EncodingDetector.PrintSupportedEncodings(); Environment.Exit(0);
        int success = 0;
        
        Logger.LogInfo("Program started at: " + DateTime.Now.ToString("HH:mm:ss"));
        Stopwatch sw = Stopwatch.StartNew();

        var filePaths = FilesEncodings.FilesEncodingsDictionary.Keys.ToList();
        var tasks = filePaths.Select(async filePath =>
        {
            if (!File.Exists(filePath))
            {
                Logger.LogWarning($"[MISSING] {filePath}");
                return;
            }

            try
            {
                string detectedEncoding = await EncodingDetector.DetectEncoding(filePath);
                Encoding dotnetEncoding = Encoding.GetEncoding(detectedEncoding);
                if (detectedEncoding == FilesEncodings.FilesEncodingsDictionary[filePath])
                {
                    success++;
                    Logger.LogSuccess($"[MATCH] {filePath} [{detectedEncoding}]");
                }
                else
                {
                    Logger.LogWarning($"[MISMATCH] {filePath} detected [{detectedEncoding}] " +
                                           $"correct [{FilesEncodings.FilesEncodingsDictionary[filePath]}]");
                }
                
                using var reader = new StreamReader(filePath, dotnetEncoding);
                string? firstLine = await reader.ReadLineAsync();
                // Console.WriteLine($"[OUTPUT] {filePath} -> \"{firstLine?.Trim()}\"");
            }
            catch (Exception ex)
            {
                Logger.LogError($"[EXCEPTION] {filePath}: {ex.Message}");
            }
        });

        await Task.WhenAll(tasks);
        sw.Stop();

        Logger.LogInfo($"Success rate is {success}/{FilesEncodings.FilesEncodingsDictionary.Keys.Count}");
        Logger.LogInfo($"Time taken {sw.Elapsed}");
        Logger.LogSuccess("Program successfully finished with exit code 0");
    }
}