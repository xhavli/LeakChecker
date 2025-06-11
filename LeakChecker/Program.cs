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
        string temporaryDirectory = Config.TemporaryDirectory;
        string pythonScriptPath = Config.EncodingDetector.ScriptPath;
        int accuracyPercent = Config.EncodingDetector.AccuracyPercent;
        string fileName = Path.GetFileName(filePath);
        List<string> detectedEncodings = new List<string>();
        
        try
        {
            // 1. Open the file
            await using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            long fileSize = fs.Length;

            // 2. Calculate sampling positions and chunk size
            double spacing = 100.0 / accuracyPercent;
            long chunkSize = (long)Math.Ceiling(fileSize * (accuracyPercent / 100.0) / accuracyPercent);
            chunkSize = Math.Min(chunkSize, 2 * SizeEnum.Gigabyte - 1);
            chunkSize = Math.Max(chunkSize, SizeEnum.Megabyte);

            for (int i = 0; i < accuracyPercent; i++)
            {
                long offset = (long)Math.Round(i * spacing * chunkSize);
                if (offset >= fileSize) break; // Don't read past EOF

                fs.Seek(offset, SeekOrigin.Begin);
                int bytesToRead = (int)Math.Min(chunkSize, fileSize - offset);
                byte[] buffer = new byte[bytesToRead];
                int bytesRead = await fs.ReadAsync(buffer, 0, bytesToRead);

                if (bytesRead == 0) continue;

                // 3. Write this chunk to a temporary file
                string tmpPath = Path.Combine(temporaryDirectory, fileName + "_" + i + "_" + Guid.NewGuid());
                await File.WriteAllBytesAsync(tmpPath, buffer.Take(bytesRead).ToArray());

                try
                {
                    // 4. Detect encoding for this chunk using Python
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
                    process.EnableRaisingEvents = false;
                    process.Start();

                    string? result = await process.StandardOutput.ReadLineAsync();
                    string stderr = await process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync();

                    if (!string.IsNullOrWhiteSpace(stderr))
                        Logger.LogError($"[PYTHON] {filePath}: {stderr.Trim()}");

                    if (!string.IsNullOrWhiteSpace(result))
                        detectedEncodings.Add(result.Trim());
                }
                catch (Exception ex)
                {
                    Logger.LogError($"[CHUNK EXCEPTION] '{filePath}': {ex.Message}");
                }
                finally
                {
                    File.Delete(tmpPath);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"[EXCEPTION] {filePath}: {ex.Message}");
        }

        // 5. Pick the most common detected encoding, or "unknown"
        if (detectedEncodings.Count == 0)
            return "Unknown";

        var best = detectedEncodings
            .GroupBy(s => s)
            .OrderByDescending(g => g.Count())
            .First().Key;

        return best;
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