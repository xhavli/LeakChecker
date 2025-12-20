using System.Globalization;
using System.Text;
using LeakChecker.Content;
using LeakChecker.Encodings;
using LeakChecker.Format.Detection;
using LeakChecker.Format.Schema;
using LeakChecker.Utilities.Configuration;

namespace LeakChecker.Logging.FileLogging;

public class FileLogger : IFileLogger
{
    public Guid ParseId {get; init;}
    public Guid ExecutionId {get; init;}
    public DateTime ParseStart { get; }
    public string SubjectFileName { get; }
    public string SubjectFilePath { get; }
    public string SubjectTmpFilePath { get; }
    private bool EnableConsole { get; } = true;
    private readonly StreamWriter _writer;

    private FileLogger(
        Guid parseId,
        Guid executionId,
        DateTime parseStart,
        string subjectFilePath,
        string tmpFilePath,
        StreamWriter writer)
    {
        ParseId = parseId;
        ExecutionId = executionId;
        ParseStart = parseStart;
        SubjectFileName = Path.GetFileName(subjectFilePath);
        SubjectFilePath = subjectFilePath;
        SubjectTmpFilePath = tmpFilePath;
        _writer = writer;
    }

    public static async Task<IFileLogger> CreateAsync(
        AppConfig config,
        Guid parseId,
        Guid executionId,
        DateTime parseStart,
        string subjectFilePath)
    {
        string subjectFileName = Path.GetFileName(subjectFilePath);

        string fileTimeStamp = $"{parseStart:yyyy-M-dTHH-mm-ss}";
        string logFileName = $"{fileTimeStamp}_{subjectFileName}_{parseId}.txt";
        string logFilePath = Path.Combine(config.LogDirectory, logFileName);
        string tmpFilePath = Path.Combine(config.TmpDirectory, logFileName);

        var writer = new StreamWriter(
            logFilePath, append: true, encoding: new UTF8Encoding(encoderShouldEmitUTF8Identifier: false))
        {
            AutoFlush = true
        };

        var logger = new FileLogger(
            parseId, executionId, parseStart, subjectFilePath, tmpFilePath, writer);
        await logger.CreateLogHeaderAsync();

        return logger;
    }
    
    private async Task LogLineAsync(string message = "")
    {
        if (EnableConsole) Console.WriteLine(message);
        
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

        Console.ForegroundColor = consoleColor;
        await LogLineAsync(context is null ? $"[{DateTime.Now:T}] {level.GetString()} {message}"
                                           : $"[{DateTime.Now:T}] {level.GetString()} {context.Value.GetString()} {message}");
        Console.ResetColor();
    }

    private async Task CreateLogHeaderAsync()
    {
        string timeStamp = ParseStart.ToString("F", CultureInfo.InvariantCulture);
        FileInfo fileInfo = new FileInfo(SubjectFilePath);
        long fileSize = fileInfo.Length;
        double sizeMb = fileSize / (1024.0 * 1024);
        double sizeGb = fileSize / (1024.0 * 1024 * 1024);

        await LogLineAsync($"File parse start at: {timeStamp}");
        await LogLineAsync($"File path: {fileInfo.FullName}");
        await LogLineAsync($"File name: {fileInfo.Name}");
        await LogLineAsync($"File size: {sizeGb:F2} GB / {sizeMb:F2} MB / {fileSize:N0} bytes ");
    }
    
    public async Task LogEncodingHeader()
    {
        await LogLineAsync();
        await LogLineAsync("---------------------------------------------");
        await LogLineAsync("          [X] ENCODING DETECTION [X]");
        await LogLineAsync("---------------------------------------------");
        await Log("Encoding detection started");
    }
    
    public async Task LogEncodingStats(List<EncodingSegment> segments)
    {
        await LogLineAsync();
        await LogLineAsync("---------------------------------------------");
        await LogLineAsync("    [X] ENCODING DETECTION STATISTICS [X]");
        await LogLineAsync("---------------------------------------------");
        
        await LogLineAsync($"Encoding segments count: {segments.Count}");
        int distinctEncodingCount = segments
            .Select(s => s.Encoding?.WebName)
            .Distinct()
            .Count();
        await LogLineAsync($"Different encodings count: {distinctEncodingCount}");
        
        await LogLineAsync("--");
        await LogLineAsync("Encodings by segment count:");
        var encodingCounts = segments
            .GroupBy(s => s.Encoding?.WebName)
            .OrderByDescending(g => g.Count());
        foreach (var group in encodingCounts)
        {
            string encName = string.IsNullOrWhiteSpace(group.Key) ? "[NULL]" : group.Key;
            await LogLineAsync($"{encName,-15} : {group.Count()} segments");
        }
        
        await LogLineAsync("--");
        await LogLineAsync("Encodings by total size:");
        var encodingSizes = segments
            .GroupBy(s => s.Encoding?.WebName)
            .OrderByDescending(g => g.Sum(s => s.Length));
        foreach (var group in encodingSizes)
        {
            string encName = string.IsNullOrWhiteSpace(group.Key) ? "[NULL]" : group.Key;
            long totalBytes = group.Sum(s => s.Length);
            await LogLineAsync($"{encName,-15} : {totalBytes:N0} bytes");
        }
        
        await LogLineAsync("--");
        await LogLineAsync("Largest encoding segments:");
        var largestSegments = segments
            .OrderByDescending(s => s.Length)
            .Take(5)
            .ToList();
        foreach (var seg in largestSegments)
            await LogLineAsync($"{seg.ToByteString()}");
    }

    public async Task LogEncodingDetails(List<EncodingSegment> segments)
    {
        await LogLineAsync();
        await LogLineAsync("----------------------------------------------------------");
        await LogLineAsync("           [X] ENCODING DETECTION DETAILS [X]");
        await LogLineAsync("----------------------------------------------------------");
        
        foreach (var segment in segments)
        {
            await LogLineAsync(segment.ToByteString());
        }
    }
    
    public async Task LogContentHeader()
    {
        await LogLineAsync();
        await LogLineAsync("---------------------------------------------");
        await LogLineAsync("            [X] CONTENT PARSE [X]");
        await LogLineAsync("---------------------------------------------");
    }
    
    public async Task LogDelimiterHeuristic(DelimiterHeuristicResult result, int count = 5)
    {
        await LogLineAsync();
        await LogLineAsync("---------------------------------------------");
        await LogLineAsync("         [X] DELIMITER DETECTION [X]");
        await LogLineAsync("---------------------------------------------");
        await Log($"Best delimiter: [{result.BestDelimiter}]");
        await LogLineAsync($"Sampled {result.SampledLines:N0} lines (~{result.SampledBytes} chars) in {result.Duration} seconds");
        foreach (var candidate in result.Candidates.Take(count))
            await LogLineAsync(candidate.ToString());

        await LogLineAsync();
    }

    public async Task LogSqlInsertHeader(string subject, IList<string> headers, string fullHeader)
    {
        await Log($"SQL INSERT header: {fullHeader}");
        await LogLineAsync($"SQL INSERT subject: {subject}");

        for (int i = 0; i < headers.Count; i++)
            await LogLineAsync($"[{i}] {headers[i]}");
        
        await LogLineAsync();
    }

    public async Task LogSchemaDetectionHeader()
    {
        await LogLineAsync();
        await LogLineAsync("---------------------------------------------");
        await LogLineAsync("           [X] SCHEMA DETECTION [X]");
        await LogLineAsync("---------------------------------------------");
    }

    public async Task LogSample(string sample)
    {
        await LogLineAsync(sample);
    }
    
    public async Task LogHeuristicData(SchemaHeuristic analyzer)
    {
        await LogLineAsync();
        await LogLineAsync("Heuristic data:");

        foreach (var kvp in analyzer.AttributeCountsPerPosition.OrderBy(x => x.Key))
        {
            int position = kvp.Key;
            int[] counts = kvp.Value;
            int total = counts.Sum();

            await LogLineAsync($"   Position {position}: total {total}");

            var stats =
                counts.Select((count, index) => new { Attribute = (ItemEnum)index, Count = count })
                    .Where(x => x.Count > 0)
                    .OrderByDescending(x => x.Count);

            foreach (var rec in stats)
            {
                double pct = total == 0 ? 0 : (double)rec.Count / total * 100.0;
                await LogLineAsync($"      {rec.Attribute} = {rec.Count} ({pct:0.##}%)");
            }
        }
        await LogLineAsync("--");
    }
    
    public async Task LogDominantSchema(SchemaHeuristic analyzer, double threshold)
    {
        await LogLineAsync($"Dominant schema (SuccessRate >= {threshold}%):");

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
                await LogLineAsync($"   Position {pos} = ({attr})");
                continue;
            }

            // Use cached percentage if available
            double pct = dominant.TryGetValue(pos, out var tuple) ? tuple.percent : 0.0;

            await LogLineAsync($"   Position {pos} = {attr} - {pct:0.##}%");
        }
        await LogLineAsync("--");
    }
    
    public async Task LogFinalSchema(Dictionary<int, ItemEnum> schema)
    {
        await LogLineAsync("Final schema + assigned or guessed:");

        foreach (var kvp in schema.OrderBy(k => k.Key))
            await LogLineAsync($"   Position {kvp.Key} = {kvp.Value}");

        await LogLineAsync();
    }
    
    public async Task LogFileStats(FileStats stats)
    {
        await LogLineAsync();
        await LogLineAsync("------------------------------------------------");
        await LogLineAsync("             [X] FILE PARSE STATS [X]");
        await LogLineAsync("------------------------------------------------");

        await LogLineAsync($"File: {stats.FileName}");
        await LogLineAsync($"Parse ID: {stats.ParseId}");

        string originEnc = string.IsNullOrEmpty(stats.Encoding?.WebName) ? "NULL" : stats.Encoding.WebName;
        await LogLineAsync($"Origin encoding: [{originEnc}]");
        string? originEncCount = Convert.ToString(stats.EncodingSegments.Count == 0 ? "NULL" : stats.EncodingSegments.Count);
        await LogLineAsync($"Encoding segments: [{originEncCount}]");

        await LogLineAsync("Delimiters:");
        foreach (var delimiter in stats.Delimiters)
            await LogLineAsync($"   '{delimiter}'");

        await LogLineAsync("Formats:");
        foreach (var format in stats.Formats)
            await LogLineAsync($"   {format}");

        await LogLineAsync($"Correct records parsed: {stats.RecordsRead:N0}");
        await LogLineAsync($"Malformed records parsed: {stats.MalformedRecordsRead:N0}");
        await LogLineAsync($"Parse accuracy (correct vs malformed): {stats.Accuracy:F2} %");
        
        await LogLineAsync($"Lines read: {stats.LinesRead:N0}");
        await LogLineAsync($"Line speed: {stats.LineSpeed:F2} lines/sec");
        
        await LogLineAsync($"Bytes read: {stats.BytesRead:N0}");
        await LogLineAsync($"Byte speed: {stats.ByteSpeed:F2} bytes/sec");

        await LogLineAsync($"Parse start: {stats.ParseStart.ToString("F", CultureInfo.InvariantCulture)}");
        await LogLineAsync($"Parse end: {stats.ParseEnd.ToString("F", CultureInfo.InvariantCulture)}");
        await LogLineAsync($"Parse time: {stats.Duration}");
        await LogLineAsync();
    }

    public void Dispose()
    {
        _writer.Flush();
        _writer.Close();
        _writer.Dispose();
    }
}