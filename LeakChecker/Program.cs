using System.Diagnostics;
using System.Text;
using LeakChecker.EncodingDetection;
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
        EncodingDetector.VerifySupportedEncodings();
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
                string pythonEnc = await DetectEncodingFromPython(filePath);
                string normalizedEnc = NormalizePythonEncoding(pythonEnc);
                Encoding encoding = MapNormalizedEncoding(normalizedEnc);
                if (pythonEnc == FilesEncodings.FilesEncodingsDictionary[filePath] &&
                    EncodingMapper.EncodingMap.ContainsKey(normalizedEnc))
                {
                    success++;
                    Logger.LogSuccess($"[MATCH] {filePath} [{pythonEnc}]");
                }
                else
                {
                    Logger.LogWarning($"[MISMATCH] {filePath} detected [{pythonEnc}] " +
                                           $"correct [{FilesEncodings.FilesEncodingsDictionary[filePath]}]");
                }
                
                using var reader = new StreamReader(filePath, encoding);
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

        Logger.LogInfo($"Success rate with {Config.EncodingDetector.AccuracyPercent}% accuracy is " +
                               $"{success}/{FilesEncodings.FilesEncodingsDictionary.Keys.Count - 2}");
        Logger.LogInfo($"Time taken {sw.Elapsed}");
        Logger.LogSuccess("Program successfully finished with exit code 0");
    }

    
    
    private static async Task<string> DetectEncodingFromPython(string filePath)
    {
        string pythonScriptPath = Config.EncodingDetector.ScriptPath;
        string tmpPath = String.Empty;
        string? result = String.Empty;
        
        try
        {
            tmpPath = Path.GetTempFileName();   // Windows C:\Users\<USER-NICKNAME>\AppData\Local\Temp\tmp*.tmp
            byte[] sample = SampleFileChunksByAccuracy(filePath);
            await File.WriteAllBytesAsync(tmpPath, sample);

            var psi = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = $"\"{pythonScriptPath}\" detect \"{tmpPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process();
            process.StartInfo = psi;
            process.EnableRaisingEvents = true;
            process.Start();

            result = await process.StandardOutput.ReadLineAsync();
            string stderr = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (!string.IsNullOrWhiteSpace(stderr))
                Logger.LogError($"[PYTHON] {filePath}: {stderr.Trim()}");
        }
        catch (Exception ex)
        {
            Logger.LogError($"[EXCEPTION] {filePath}: {ex.Message}");
        }
        finally
        {
            File.Delete(tmpPath);
        }
        
        return string.IsNullOrWhiteSpace(result) ? "unknown" : result.Trim();
    }
    
    private static byte[] SampleFileChunksByAccuracy(string filePath)
    {
        int accuracyPercent = Config.EncodingDetector.AccuracyPercent;
        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        long fileSize = fs.Length;

        if (fileSize > SizeEnum.Gigabyte)
        {
            Logger.LogWarning("File is larger than [1GB]. It may take a long time " +
                                   $"and cause performance issues when parsing large files. File '{filePath}'");
        }
        if (accuracyPercent >= 100)
        {
            // Full file
            byte[] all = new byte[fileSize];    //TODO C# limit 2gb
            fs.ReadExactly(all, 0, (int)fileSize);
            return all;
        }
        long totalSampleBytes = (long)(fileSize * (accuracyPercent / 100.0));
        int chunkSize = 4096; // Recommended min size per chunk for encoding detection
        int chunkCount = Math.Max(1, (int)(totalSampleBytes / chunkSize));
        chunkSize = (int)(totalSampleBytes / chunkCount);
        byte[] sample = new byte[chunkCount * chunkSize];
        long spacing = fileSize / chunkCount;

        for (int i = 0; i < chunkCount; i++)
        {
            long position = spacing * i;
            fs.Seek(position, SeekOrigin.Begin);
            int bytesRead = fs.Read(sample, i * chunkSize, chunkSize);
            if (bytesRead < chunkSize)
            {
                Array.Clear(sample, i * chunkSize + bytesRead, chunkSize - bytesRead);
            }
        }

        return sample;
    }

    private static string NormalizePythonEncoding(string pythonEncoding)
    {
        return pythonEncoding.Trim().ToLowerInvariant().Replace("_", "").Replace("-", "");
    }

    private static Encoding MapNormalizedEncoding(string normalizedEncoding)
    {
        if (string.IsNullOrWhiteSpace(normalizedEncoding))
            return Encoding.Default;

        if (EncodingMapper.EncodingMap.TryGetValue(normalizedEncoding, out var dotNetName))
        {
            try { return Encoding.GetEncoding(dotNetName); }
            catch { Logger.LogWarning($"Encoding '{normalizedEncoding}' not supported" +
                                           $"in Encoding.GetEncodings()"); }
        }

        Logger.LogWarning($"Encoding '{normalizedEncoding}' not found in EncodingMap, " +
                               $"falling back to Encoding.Default - " + Encoding.Default.EncodingName);
        return Encoding.Default;
    }
}