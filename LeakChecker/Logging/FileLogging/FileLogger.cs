using System.Globalization;
using System.Text;
using LeakChecker.Content;
using LeakChecker.Encodings;
using LeakChecker.Format;
using LeakChecker.Utilities;
using LeakChecker.Utilities.Configuration;

namespace LeakChecker.Logging.FileLogging;

public class FileLogger : IFileLogger
{
    public string SubjectFileName { get; }
    public string SubjectFilePath { get; }
    public string SubjectTmpFilePath { get; }
    public long SubjectFileBytes { get; private set; }
    private bool EnableConsole { get; }
    public DateTime ProcessingStart { get; } = DateTime.Now;
    private readonly StreamWriter _writer;

    private FileLogger(string subjectFilePath, string tmpFilePath, StreamWriter writer, bool enableConsole)
    {
        SubjectFilePath = subjectFilePath;
        SubjectFileName = Path.GetFileName(subjectFilePath);
        SubjectTmpFilePath = tmpFilePath;
        EnableConsole = enableConsole;
        _writer = writer;
    }

    public static async Task<IFileLogger> CreateAsync(string subjectFilePath, AppConfig config, bool enableConsole)
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

        var logger = new FileLogger(subjectFilePath, tmpFilePath, writer, enableConsole);

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
        if (EnableConsole)
        {
            Console.ForegroundColor = consoleColor;
            Console.WriteLine($"'{SubjectFilePath}' {log}");
            Console.ResetColor();
        }
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

    public async Task LogSqlInsertHeader(string subject, IList<string> headers, string fullHeader)
    {
        await Log(fullHeader);
        await Log($"SQL Insert subject: {subject}");

        for (int i = 0; i < headers.Count; i++)
            await LogAsync($"[{i}] {headers[i]}");

        await LogAsync("");
    }

    
    public async Task LogHeuristicData(SchemaHeuristic analyzer, double threshold)
    {
        await LogAsync("Heuristic input data:");
        Console.WriteLine("Heuristic input data:");

        foreach (var kvp in analyzer.AttributeCountsPerPosition.OrderBy(x => x.Key))
        {
            int total = kvp.Value.Sum();
            await LogAsync($"   Position {kvp.Key}: (total {total})");
            Console.WriteLine($"   Position {kvp.Key}: (total {total})");

            var stats = Enumerable.Range(0, analyzer.AttributeCount)
                .Where(i => kvp.Value[i] > 0)
                .Select(i => new { Attribute = (ItemEnum)i, Count = kvp.Value[i] })
                .OrderByDescending(r => r.Count);

            foreach (var rec in stats)
            {
                double pct = (double)rec.Count / total * 100.0;
                string log = $"      {rec.Attribute} = {rec.Count} ({pct:0.##}%)";
                await LogAsync(log);
                Console.WriteLine(log);
            }
        }

        await LogAsync("");
        Console.WriteLine();

        await LogAsync($"Dominant schema (SuccessRate >= {threshold}%):");
        Console.WriteLine($"Dominant schema (SuccessRate >= {threshold}%):");

        // Expanded schema contains Previous entries
        var fullSchema = analyzer.GetDominantSchema(threshold);

        // Cached % for the leading positions (no recompute)
        var dominant = analyzer.GetDominantStats(threshold);

        foreach (var kvp in fullSchema.OrderBy(x => x.Key))
        {
            int pos = kvp.Key;
            var attr = kvp.Value;

            if (attr == ItemEnum.Previous)
            {
                string log = $"   Position {pos} = ({attr})";
                await LogAsync(log);
                Console.WriteLine(log);
                continue;
            }

            // Use cached percentage if available
            double pct = dominant.TryGetValue(pos, out var tuple) ? tuple.percent : 0.0;

            string line = $"   Position {pos} = {attr} - {pct:0.##}%";
            await LogAsync(line);
            Console.WriteLine(line);
        }

        await LogAsync("");

    }

    public async Task LogSchema(Dictionary<int, ItemEnum> schema)
    {
        await Log("Final schema detected + guessed or assigned:");
        Console.WriteLine("Final schema:");
        foreach (var col in schema)
        {
            await Log($"[{col.Key}] = {col.Value}");
            Console.WriteLine($"[{col.Key}] = {col.Value}");
        }
    }
    
    public void Dispose()
    {
        _writer.Flush();
        _writer.Close();
        _writer.Dispose();
    }
}