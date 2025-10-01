using System.Globalization;
using System.Text;
using LeakChecker.ContentDetection;
using LeakChecker.EncodingDetection;
using LeakChecker.FormatDetection;

namespace LeakChecker.Logging.FileLogging;

public class FileLogger : IDisposable
{
    public string SubjectFilePath { get; }
    public string SubjectFileName { get; }
    public long SubjectFileBytes { get; private set; }
    public readonly DateTime ProcessingStart = DateTime.Now;
    private StreamWriter Writer { get; }

    public FileLogger(string subjectFilePath, string logDirectory)
    {
        SubjectFilePath = subjectFilePath;
        SubjectFileName = Path.GetFileName(SubjectFilePath);
        
        string nameTimeStamp = $"{ProcessingStart:yyyy-M-dTHH-mm-ss}";
        string reportFileName = $"{nameTimeStamp}_{SubjectFileName}.txt";
        string reportFilePath = Path.Combine(logDirectory, reportFileName);

        Writer = new StreamWriter(reportFilePath, append: true, encoding: Encoding.UTF8);
        Writer.AutoFlush = true;
        
        CreateReportHeader();
    }

    private async Task LogAsync(string message)
    {
        await Writer.WriteLineAsync(message);
    }
    
    public async Task Log(string? message, LogLevel level = LogLevel.Info, LogContext? context = null )
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
        Console.WriteLine($"'{SubjectFilePath}' {log}");
        Console.ResetColor();
    }

    private void CreateReportHeader()
    {
        string timeStamp = ProcessingStart.ToString("F", CultureInfo.InvariantCulture);
        FileInfo fileInfo = new FileInfo(SubjectFilePath);
        SubjectFileBytes = fileInfo.Length;
        double sizeMB = SubjectFileBytes / (1024.0 * 1024);
        double sizeGB = SubjectFileBytes / (1024.0 * 1024 * 1024);

        Writer.WriteLine($"File processing start at: {timeStamp}");
        Writer.WriteLine($"File name: {fileInfo.Name}");
        Writer.WriteLine($"File path: {fileInfo.FullName}");
        Writer.WriteLine($"File size: {sizeGB:F2} GB / {sizeMB:F2} MB / {SubjectFileBytes:N0} bytes ");
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
            .Select(s => s.Encoded)
            .Distinct()
            .Count();
        await LogAsync($"Different encodings count: {distinctEncodingCount}");
        
        var largestSegment = segments
            .MaxBy(s => s.Length);
        await LogAsync($"Largest encoding segment: {largestSegment!.ShowByte()}");
        
        await LogAsync("-");
        await LogAsync("Encodings by segment count:");
        var encodingCounts = segments
            .GroupBy(s => s.Encoded)
            .OrderByDescending(g => g.Count());
        
        foreach (var group in encodingCounts)
        {
            await LogAsync($"{group.Key,-15} : {group.Count()} segments");
        }
        
        await LogAsync("-");
        await LogAsync("Encodings by total size in bytes:");
        var encodingSizes = segments
            .GroupBy(s => s.Encoded)
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
    
    public async Task LogFormatProcessingStart()
    {
        await LogAsync("");
        await LogAsync("---------------------------");
        await LogAsync("[X] FORMAT PROCESSING [X]");
        await LogAsync("---------------------------");
        await Log("Format processing started");
    }
    
    public async Task LogContentProcessingStart()
    {
        await LogAsync("");
        await LogAsync("--------------------------");
        await LogAsync("[X] CONTENT PROCESSING [X]");
        await LogAsync("--------------------------");
        await Log("Content processing started");
    }
    
    public async Task LogContentStats(Dictionary<ItemEnum, int> contentResults)
    {
        await LogAsync("");
        await LogAsync("-------------------------------------");
        await LogAsync("[X] CONTENT PROCESSING STATISTICS [X]");
        await LogAsync("-------------------------------------");
        
        await LogAsync("Content attributes found:");
        var contentStats = contentResults.OrderByDescending(x => x.Value);
        foreach (var kvp in contentStats)
        {
            string log = $"{kvp.Key}: {kvp.Value}";
            await LogAsync(log);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(log);
            Console.ResetColor();
        }
    }

    public async Task LogSqlInsertHeader(string subject, string columnList, string fullHeader)
    {
        await Log(fullHeader);
        await LogAsync($"Sql Insert subject = {subject}");
        var columnNames = columnList.Split(',');
        for (int i = 0; i < columnNames.Length; i++)
        {
            await LogAsync($"[{i}] {columnNames[i].Trim()}");
        }

        await LogAsync("");
    }
    
    public async Task LogContentHeuristic(HeuristicAnalyzer analyzer, double threshold = 50.0)
    {
        await LogAsync("Heuristic data stats:");
        Console.WriteLine("Heuristic data stats:");
        foreach (var kvp in analyzer.PositionCounts.OrderBy(x => x.Key))
        {
            int totalAtPosition = kvp.Value.Sum();
            await LogAsync($"   Position {kvp.Key}: (total {totalAtPosition})");
            Console.WriteLine($"   Position {kvp.Key}: (total {totalAtPosition})");

            var records = Enumerable.Range(0, analyzer.AttributeCount)
                .Where(i => kvp.Value[i] > 0)
                .Select(i => new
                {
                    Attribute = (ItemEnum)i,
                    Count = kvp.Value[i]
                })
                .OrderByDescending(r => r.Count);

            foreach (var rec in records)
            {
                double percent = (double)rec.Count / totalAtPosition * 100.0;
                string log = $"      {rec.Attribute} = {rec.Count} ({percent:0.##}%)";
                await LogAsync(log);
                Console.WriteLine($"      {rec.Attribute} = {rec.Count} ({percent:0.##}%)");
            }

        }

        await LogAsync("");
        Console.WriteLine();
        await LogAsync($"Likely schema (SuccessRate => {threshold}%):");
        Console.WriteLine($"Likely schema (SuccessRate => {threshold}%):");
        
        foreach (var kvp in analyzer.PositionCounts.OrderBy(x => x.Key))
        {
            int totalAtPosition = kvp.Value.Sum();
            if (totalAtPosition == 0) continue;

            int maxCount = kvp.Value.Max();
            int maxIndex = Array.IndexOf(kvp.Value, maxCount);
            double percent = (double)maxCount / totalAtPosition * 100.0;

            if (percent >= threshold)
            {
                string log = $"   Position {kvp.Key} = ({(ItemEnum)maxIndex}, {percent:0.##}%)";
                await LogAsync(log);
                Console.WriteLine(log);
            }
        }

        await LogAsync("");
    }
    
    public void Dispose()
    {
        Writer.Flush();
        Writer.Close();
        Writer.Dispose();
    }
}