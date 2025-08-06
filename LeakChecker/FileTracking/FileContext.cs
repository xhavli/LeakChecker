using System.Globalization;
using System.Text;
using LeakChecker.ContentDetector;
using LeakChecker.EncodingDetection;

namespace LeakChecker.FileTracking;

public class FileContext : IDisposable
{
    public string Path { get; }
    private readonly DateTime _startTime;
    private StreamWriter Writer { get; set; }

    public FileContext(string originFilePath, string logDirectory)
    {
        _startTime = DateTime.Now;
        Path = originFilePath;
        
        string nameTimeStamp = $"{_startTime:yyyy-M-dTHH-mm-ss}";
        string reportFileName = $"{nameTimeStamp}_{System.IO.Path.GetFileName(Path)}.txt";
        string reportFilePath = System.IO.Path.Combine(logDirectory, reportFileName);

        Writer = new StreamWriter(reportFilePath, append: true, encoding: Encoding.UTF8);
        Writer.AutoFlush = true;
        
        CreateReportHeader();
    }

    private async Task LogAsync(string message)
    {
        await Writer.WriteLineAsync(message);
    }
    
    public async Task Log(string message, LogLevel level = LogLevel.Info, LogContext? context = null )
    {
        ConsoleColor consoleColor = Console.ForegroundColor;
        switch (level)
        {
            case LogLevel.Info:
                consoleColor = ConsoleColor.DarkBlue;
                break;
            case LogLevel.Warning:
                consoleColor = ConsoleColor.Yellow;
                break;
            case LogLevel.Success:
                consoleColor = ConsoleColor.Green;
                break;
            case LogLevel.Exception:
                consoleColor = ConsoleColor.Red;
                break;
            default:
                Console.ResetColor();
                break;
        }

        string log = context is null ? $"[{DateTime.Now:T}] {level.GetString()} {message}"
                                     : $"[{DateTime.Now:T}] {level.GetString()} {context.Value.GetString()} {message}";
        await LogAsync(log);
        Console.ForegroundColor = consoleColor;
        Console.WriteLine($"'{Path}' {log}");
        Console.ResetColor();
    }

    private void CreateReportHeader()
    {
        string timeStamp = _startTime.ToString("F", CultureInfo.InvariantCulture);
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
        await LogAsync("");
        await LogAsync("---------------------------");
        await LogAsync("[X] ENCODING PROCESSING [X]");
        await LogAsync("---------------------------");
        await Log("Encoding processing started");
    }

    public async Task LogEncodingStats(List<EncodingSegment> segments)
    {
        await LogAsync("");
        await LogAsync("--------------------------------------");
        await LogAsync("[X] ENCODING PROCESSING STATISTICS [X]");
        await LogAsync("--------------------------------------");
        
        await LogAsync($"Encoding segments count: {segments.Count}");
        int distinctEncodingCount = segments
            .Select(s => s.EncodingName)
            .Distinct()
            .Count();
        await LogAsync($"Different encodings count: {distinctEncodingCount}");
        
        var largestSegment = segments
            .MaxBy(s => s.Length);
        await LogAsync($"Largest encoding segment: {largestSegment!.ShowByte()}");
        
        await LogAsync("-");
        await LogAsync("Encodings by segment count:");
        var encodingCounts = segments
            .GroupBy(s => s.EncodingName)
            .OrderByDescending(g => g.Count());
        
        foreach (var group in encodingCounts)
        {
            await LogAsync($"{group.Key,-15} : {group.Count()} segments");
        }
        
        await LogAsync("-");
        await LogAsync("Encodings by total size in bytes:");
        var encodingSizes = segments
            .GroupBy(s => s.EncodingName)
            .OrderByDescending(g => g.Sum(s => s.Length));
        
        foreach (var group in encodingSizes)
        {
            long totalBytes = group.Sum(s => s.Length);
            await LogAsync($"{group.Key,-15} : {totalBytes:N0} bytes");
        }
    }

    public async Task LogEncodingDetails(List<EncodingSegment> segments)
    {
        await LogAsync("");
        await LogAsync("-----------------------------------");
        await LogAsync("[X] ENCODING PROCESSING DETAILS [X]");
        await LogAsync("-----------------------------------");
        
        foreach (var segment in segments)
        {
            await LogAsync(segment.ShowByte());
        }
    }
    
    public async Task LogContentProcessingStart()
    {
        await LogAsync("");
        await LogAsync("--------------------------");
        await LogAsync("[X] CONTENT PROCESSING [X]");
        await LogAsync("--------------------------");
        await Log("Content processing started");
    }
    
    public async Task LogContentStats(Dictionary<RecordAttribute, int> contentResults)
    {
        await LogAsync("");
        await LogAsync("-------------------------------------");
        await LogAsync("[X] CONTENT PROCESSING STATISTICS [X]");
        await LogAsync("-------------------------------------");
        
        await LogAsync("Content attributes found:");
        var contentStats = contentResults.OrderByDescending(x => x.Value);
        string log;
        foreach (var kvp in contentStats)
        {
            log = $"{kvp.Key}: {kvp.Value}";
            await LogAsync(log);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(log);
            Console.ResetColor();
        }
    }

    public void Dispose()
    {
        Writer.Flush();
        Writer.Close();
        Writer.Dispose();
    }
}