using System.Globalization;
using System.Text;
using LeakChecker.EncodingDetection;
using LeakChecker.Utilities;

namespace LeakChecker.FileTracking;

public class FileContext : IDisposable
{
    public string Path { get; }
    private readonly DateTime startTime;
    private StreamWriter Writer { get; set; }


    public FileContext(string originFilePath, string logDirectory)
    {
        startTime = DateTime.Now;
        Path = originFilePath;
        
        string nameTimeStamp = $"{startTime:yyyy-M-dTHH-mm-ss}";
        string reportFileName = $"{nameTimeStamp}_{System.IO.Path.GetFileName(Path)}.txt";
        string reportFilePath = System.IO.Path.Combine(logDirectory, reportFileName);

        Writer = new StreamWriter(reportFilePath, append: true, encoding: Encoding.UTF8);
        Writer.AutoFlush = true;
        
        CreateReportHeader();
    }

    private async Task WriteLineAsync(string message)
    {
        await Writer.WriteLineAsync(message);
    }
    
    public async Task Log(LogLevel level, string message)
    {
        switch (level)
        {
            case LogLevel.Info:
                await LogInfo(message);
                break;
            case LogLevel.Warning:
                await LogWarning(message);
                break;
            case LogLevel.Success:
                await LogSuccess(message);
                break;
            case LogLevel.Error:
                await LogError(message);
                break;
            default:
                Console.WriteLine("[UNKNOWN] " + message);
                break;
        }
    }
    
    private async Task LogInfo(string content)
    {
        string level = "[INFO]";
        string log = $"[{DateTime.Now:T}] {level} {content}";
        await WriteLineAsync(log);
        Console.ForegroundColor = ConsoleColor.DarkBlue;
        await Console.Error.WriteLineAsync($"{level} {content}");
        Console.ResetColor();
    }
    
    private async Task LogWarning(string content)
    {
        string level = "[WARNING]";
        string log = $"[{DateTime.Now:T}] {level} {content}";
        await WriteLineAsync(log);
        Console.ForegroundColor = ConsoleColor.Yellow;
        await Console.Error.WriteLineAsync($"{level} {content}");
        Console.ResetColor();
    }

    private async Task LogSuccess(string content)
    {
        string level = "[SUCCESS]";
        string log = $"[{DateTime.Now:T}] {level} {content}";
        await WriteLineAsync(log);
        Console.ForegroundColor = ConsoleColor.Green;
        await Console.Error.WriteLineAsync($"{level} {content}");
        Console.ResetColor();
    }

    private async Task LogError(string content)
    {
        string level = "[ERROR]";
        string log = $"[{DateTime.Now:T}] {level} {content}";
        await WriteLineAsync(log);
        Console.ForegroundColor = ConsoleColor.Red;
        await Console.Error.WriteLineAsync($"{level} {content}");
        Console.ResetColor();
    }

    private void CreateReportHeader()
    {
        string timeStamp = startTime.ToString("F", CultureInfo.InvariantCulture);
        FileInfo fileInfo = new FileInfo(Path);
        long sizeInBytes = fileInfo.Length;
        double sizeMB = sizeInBytes / (1024.0 * 1024);
        double sizeGB = sizeInBytes / (1024.0 * 1024 * 1024);

        Writer.WriteLine($"File processing start at: {timeStamp}");
        Writer.WriteLine($"File name: {fileInfo.Name}");
        Writer.WriteLine($"File path: {fileInfo.FullName}");
        Writer.WriteLine($"File size: {sizeGB:F2} GB / {sizeMB:F2} MB / {sizeInBytes} bytes ");
    }
    
    public async Task LogEncodingProcessingStart()
    {
        await WriteLineAsync("");
        await WriteLineAsync("---------------------------");
        await WriteLineAsync("[X] ENCODING PROCESSING [X]");
        await WriteLineAsync("---------------------------");
        await Log(LogLevel.Info, "Encoding processing started");

    }
    
    public async Task LogEncodingStats(List<EncodingSegment> segments)
    {
        await WriteLineAsync("");
        await WriteLineAsync("--------------------------------------");
        await WriteLineAsync("[X] ENCODING PROCESSING STATISTICS [X]");
        await WriteLineAsync("--------------------------------------");
        
        await WriteLineAsync($"Encoding segments count: {segments.Count}");
        int distinctEncodingCount = segments
            .Select(s => s.EncodingName)
            .Distinct()
            .Count();
        await WriteLineAsync($"Different encodings count: {distinctEncodingCount}");
        
        var largestSegment = segments
            .MaxBy(s => s.Length);
        await WriteLineAsync($"Largest encoding segment: {largestSegment!.ShowByte()}");
        
        await WriteLineAsync("-");
        await WriteLineAsync("Encodings by segment count:");
        var encodingCounts = segments
            .GroupBy(s => s.EncodingName)
            .OrderByDescending(g => g.Count());
        
        foreach (var group in encodingCounts)
        {
            await WriteLineAsync($"{group.Key,-15} : {group.Count()} segments");
        }
        
        await WriteLineAsync("-");
        await WriteLineAsync("Encodings by total size in bytes:");
        var encodingSizes = segments
            .GroupBy(s => s.EncodingName)
            .OrderByDescending(g => g.Sum(s => s.Length));
        
        foreach (var group in encodingSizes)
        {
            long totalBytes = group.Sum(s => s.Length);
            await WriteLineAsync($"{group.Key,-15} : {totalBytes:N0} bytes");
        }
    }

    public async Task LogEncodingDetail(List<EncodingSegment> segments)
    {
        await WriteLineAsync("");
        await WriteLineAsync("-----------------------------------");
        await WriteLineAsync("[X] ENCODING PROCESSING DETAILS [X]");
        await WriteLineAsync("-----------------------------------");
        
        foreach (var segment in segments)
        {
            await WriteLineAsync(segment.ShowByte());
        }
    }
    
    public void Dispose()
    {
        Writer.Flush();
        Writer.Close();
        Writer.Dispose();
    }
}