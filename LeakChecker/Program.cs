using System.Diagnostics;
using System.Text;
using LeakChecker.EncodingDetector;
using LeakChecker.Tests;
using LeakChecker.Utilities;

namespace LeakChecker;

public class Program
{
    private static AppConfig Config { get; set; } = null!;

    public static async Task Main()
    {
        Config = AppConfig.ParseAppConfiguration();
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        int success = 0;
        
        bool showEncsAndExit = false;
        if (showEncsAndExit)
        {
            Console.WriteLine("List of supported encodings after provide " +
                              "Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);");
            PrintSupportedEncodings();
            Environment.Exit(0);
        }
        
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
                    EncodingMaper.EncodingMap.ContainsKey(normalizedEnc))
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
        Logger.LogInfo("Program successfully finished with exit code 0");
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
            Logger.LogWarning("File is larger than [1GB]. It may take a long time" +
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

        if (EncodingMaper.EncodingMap.TryGetValue(normalizedEncoding, out var dotNetName))
        {
            try { return Encoding.GetEncoding(dotNetName); }
            catch { Logger.LogWarning($"Encoding '{normalizedEncoding}' not supported" +
                                           $"in Encoding.GetEncodings()"); }
        }

        Logger.LogWarning($"Encoding '{normalizedEncoding}' not found in EncodingMap, " +
                               $"falling back to Encoding.Default - " + Encoding.Default.EncodingName);
        return Encoding.Default;
    }

    // Source https://learn.microsoft.com/en-us/dotnet/api/system.text.encodinginfo.codepage?view=net-10.0
    private static void PrintSupportedEncodings()
    {
        // Print the header.
        Console.Write( "Info.CodePage      " );
        Console.Write( "Info.Name                    " );
        Console.Write( "Info.DisplayName" );
        Console.WriteLine();

        // Display the EncodingInfo names for every encoding, and compare with the equivalent Encoding names.
        var sortedEncodings = Encoding.GetEncodings()
            .OrderBy(ei => ei.Name);
        
        foreach( EncodingInfo ei in sortedEncodings)  {
            Encoding e = ei.GetEncoding();

            Console.Write( "{0,-15}", ei.CodePage );
            Console.Write(ei.CodePage == e.CodePage ? "    " : "*** ");

            Console.Write( "{0,-25}", ei.Name );
            Console.Write(ei.CodePage == e.CodePage ? "    " : "*** ");

            Console.Write( "{0,-25}", ei.DisplayName );
            Console.Write(ei.CodePage == e.CodePage ? "    " : "*** ");

            Console.WriteLine();
        }
    }
}