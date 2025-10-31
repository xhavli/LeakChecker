using System.Globalization;
using System.Text;
using LeakChecker.Content;
using LeakChecker.Encodings;
using LeakChecker.Format;
using LeakChecker.Utilities;

namespace LeakChecker.Logging.FileLogging;

public class FileLogger : IDisposable
{
    public string SubjectFileName { get; }
    public string SubjectFilePath { get; }
    public string SubjectTmpFilePath { get; }
    public long SubjectFileBytes { get; private set; }
    public readonly DateTime ProcessingStart = DateTime.Now;
    private readonly StreamWriter _writer;

    private FileLogger(string subjectFilePath, string tmpFilePath, StreamWriter writer)
    {
        SubjectFilePath = subjectFilePath;
        SubjectFileName = Path.GetFileName(subjectFilePath);
        SubjectTmpFilePath = tmpFilePath;
        _writer = writer;
    }

    public static async Task<FileLogger> CreateAsync(string subjectFilePath, AppConfig config)
    {
        string subjectFileName = Path.GetFileName(subjectFilePath);
        DateTime processingStart = DateTime.Now;

        string nameTimeStamp = $"{processingStart:yyyy-M-dTHH-mm-ss}";
        string logFileName = $"{nameTimeStamp}_{subjectFileName}.txt";
        string logFilePath = Path.Combine(config.LogDirectory, logFileName);
        string tmpFilePath = Path.Combine(config.TmpDirectory, logFileName);

        var writer = new StreamWriter(logFilePath, append: true,
            encoding: new UTF8Encoding(encoderShouldEmitUTF8Identifier: false))
        {
            AutoFlush = true
        };

        var logger = new FileLogger(subjectFilePath, tmpFilePath, writer);

        await logger.CreateHeaderAsync();

        return logger;
    }

    
    private async Task LogAsync(string message)
    {
        await _writer.WriteLineAsync(message);
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

    private async Task CreateHeaderAsync()
    {
        string timeStamp = ProcessingStart.ToString("F", CultureInfo.InvariantCulture);
        FileInfo fileInfo = new FileInfo(SubjectFilePath);
        SubjectFileBytes = fileInfo.Length;
        double sizeMb = SubjectFileBytes / (1024.0 * 1024);
        double sizeGb = SubjectFileBytes / (1024.0 * 1024 * 1024);

        await LogAsync($"File processing start at: {timeStamp}");
        await LogAsync($"File path: {fileInfo.FullName}");
        await LogAsync($"File name: {fileInfo.Name}");
        await LogAsync($"File size: {sizeGb:F2} GB / {sizeMb:F2} MB / {SubjectFileBytes:N0} bytes ");
    }
    
    public async Task LogEncodingHeader()
    {
        await LogAsync("");
        await LogAsync("---------------------------");
        await LogAsync("[X] ENCODING PROCESSING [X]");
        await LogAsync("---------------------------");
        await Log("Encoding processing started");
    }
    
    public async Task LogConsistEncDetection()
    {
        await Log("Trying to detect consistent encoding");
    }
    
    public async Task LogConcatEncDetection()
    {
        await Log("Trying to detect concatenated encoding");
    }

    public async Task LogEncodingStats(List<EncodingSegment> segments)
    {
        await LogAsync("");
        await LogAsync("--------------------------------------");
        await LogAsync("[X] ENCODING PROCESSING STATISTICS [X]");
        await LogAsync("--------------------------------------");
        
        await LogAsync($"Encoding segments count: {segments.Count}");
        int distinctEncodingCount = segments
            .Select(s => s.Encoding?.WebName)
            .Distinct()
            .Count();
        await LogAsync($"Different encodings count: {distinctEncodingCount}");
        
        var largestSegment = segments
            .MaxBy(s => s.Length);
        await LogAsync($"Largest encoding segment: {largestSegment!.ToByteString()}");
        
        await LogAsync("-");
        await LogAsync("Encodings by segment count:");
        var encodingCounts = segments
            .GroupBy(s => s.Encoding?.WebName)
            .OrderByDescending(g => g.Count());
        
        foreach (var group in encodingCounts)
        {
            await LogAsync($"{group.Key,-15} : {group.Count()} segments");
        }
        
        await LogAsync("-");
        await LogAsync("Encodings by total size:");
        var encodingSizes = segments
            .GroupBy(s => s.Encoding?.WebName)
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
            await LogAsync(segment.ToByteString());
        }
    }
    
    public async Task LogFormatHeader()
    {
        await LogAsync("");
        await LogAsync("---------------------------");
        await LogAsync("[X] FORMAT PROCESSING [X]");
        await LogAsync("---------------------------");
        await Log("Format processing started");
    }
    
    public async Task LogContentHeader()
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
        await Log($"SQL Insert subject: {subject}");
        var columnNames = columnList.Split(',');
        for (int i = 0; i < columnNames.Length; i++)
        {
            await LogAsync($"[{i}] {columnNames[i].Trim().Trim('`')}");
        }

        await LogAsync("");
    }
    
    public async Task LogContentHeuristic(SchemaHeuristic analyzer, double threshold = 50.0)
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
        _writer.Flush();
        _writer.Close();
        _writer.Dispose();
    }
}