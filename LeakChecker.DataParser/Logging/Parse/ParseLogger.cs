using System.Globalization;
using LeakChecker.DataParser.Content;
using LeakChecker.DataParser.Encodings;
using LeakChecker.DataParser.Format.Detection;
using LeakChecker.DataParser.Format.Schema;
using LeakChecker.DataParser.Stats.Parse;
using LeakChecker.DataParser.Utilities;
using LeakChecker.DataParser.Utilities.Settings;

namespace LeakChecker.DataParser.Logging.Parse;

public class ParseLogger : IParseLogger
{
    public Guid ParseId { get; }
    public DateTime ParseStart { get; }
    public string SubjectFilePath { get; }
    public string SubjectTmpFilePath { get; }
    private readonly bool _verbose;
    private readonly StreamWriter _writer;
    private const ConsoleColor InfoColor = ConsoleColor.DarkBlue;
    private const ConsoleColor WarningColor = ConsoleColor.DarkYellow;
    private const ConsoleColor SuccessColor = ConsoleColor.Green;
    private const ConsoleColor ExceptionColor = ConsoleColor.Red;

    private ParseLogger(
        Guid parseId,
        DateTime parseStart,
        string subjectFilePath,
        string tmpFilePath,
        StreamWriter writer,
        bool verbose)
    {
        ParseId = parseId;
        ParseStart = parseStart;
        SubjectFilePath = subjectFilePath;
        SubjectTmpFilePath = tmpFilePath;
        _writer = writer;
        _verbose = verbose;
    }

    public static async Task<IParseLogger> CreateAsync(string subjectFilePath, ISettings settings)
    {
        Guid parseId = Guid.NewGuid();
        DateTime parseStart = DateTime.Now;
        
        string fileName = Path.GetFileName(subjectFilePath);
        string parseTimeStamp = $"{parseStart:yyyy-M-dTHH-mm-ss}";
        string logFileName = $"{parseTimeStamp}_[{parseId}]_{fileName}.txt";
        string logFilePath = Path.Combine(settings.LogDirectory, logFileName);
        string tmpFileName = $"TMP_{logFileName}";
        string tmpFilePath = Path.Combine(settings.TmpDirectory, tmpFileName);

        bool isDevelopment = string.Equals(settings.Environment.Trim(), "Development", StringComparison.OrdinalIgnoreCase);
        var writer = new StreamWriter(logFilePath, append: true, encoding: settings.DefaultUtf8)
        {
            AutoFlush = isDevelopment
        };
        
        var logger = new ParseLogger(parseId, parseStart, subjectFilePath, tmpFilePath, writer, settings.Verbose);
        
        await logger.CreateLogHeaderAsync();

        return logger;
    }
    
    private async Task WriteLineAsync(string message = "")
    {
        if (_verbose)
            Console.WriteLine(message);
        
        await _writer.WriteLineAsync(message);
    }
    
    public async Task Log(string? message, LogLevel level = LogLevel.Info, LogContext? context = null )
    {
        string log = context is null ? $"[{DateTime.Now:T}] {level.GetString()} {message}"
                                     : $"[{DateTime.Now:T}] {level.GetString()} {context.Value.GetString()} {message}";
        
        if (!_verbose)   // Log without colored console print
        {
            await WriteLineAsync(log);
            return;
        }
        
        ConsoleColor consoleColor = Console.ForegroundColor;
        switch (level)
        {
            case LogLevel.Info:
                consoleColor = InfoColor;
                break;
            case LogLevel.Warning:
                consoleColor = WarningColor;
                break;
            case LogLevel.Success:
                consoleColor = SuccessColor;
                break;
            case LogLevel.Failure:
                consoleColor = ExceptionColor;
                break;
            default:
                Console.ResetColor();
                break;
        }

        Console.ForegroundColor = consoleColor;
        await WriteLineAsync(log);
        Console.ResetColor();
    }

    private async Task CreateLogHeaderAsync()
    {
        string timeStamp = ParseStart.ToString("F", CultureInfo.InvariantCulture);
        FileInfo fileInfo = new FileInfo(SubjectFilePath);
        long fileSize = fileInfo.Length;
        double sizeMb = (double) fileSize / SizeEnum.MegaByte;
        double sizeGb = (double) fileSize / SizeEnum.GigaByte;

        await WriteLineAsync($"Parse start: {timeStamp}");
        await WriteLineAsync($"File path: {fileInfo.FullName}");
        await WriteLineAsync($"File name: {fileInfo.Name}");
        await WriteLineAsync($"File size: {sizeGb:F2} GiB / {sizeMb:F2} MiB / {fileSize:N0} Bytes");
    }
    
    public async Task LogEncodingHeader()
    {
        await WriteLineAsync();
        await WriteLineAsync("---------------------------------------------");
        await WriteLineAsync("          [X] ENCODING DETECTION [X]");
        await WriteLineAsync("---------------------------------------------");
        await Log("Encoding detection started");
    }
    
    public async Task LogEncodingStats(List<EncodingSegment> segments, int take = 10)
    {
        await WriteLineAsync();
        await WriteLineAsync("---------------------------------------------");
        await WriteLineAsync("    [X] ENCODING DETECTION STATISTICS [X]");
        await WriteLineAsync("---------------------------------------------");
        
        await WriteLineAsync($"Encoding segments count: {segments.Count}");
        int distinctEncodingCount = segments
            .Select(s => s.Encoding?.WebName)
            .Distinct()
            .Count();
        await WriteLineAsync($"Different encodings count: {distinctEncodingCount}");
        
        await WriteLineAsync();
        await WriteLineAsync("Encodings by segment count:");
        var encodingCounts = segments
            .GroupBy(s => s.Encoding?.WebName)
            .OrderByDescending(g => g.Count());
        foreach (var group in encodingCounts)
        {
            string encName = string.IsNullOrWhiteSpace(group.Key) ? "[NULL]" : group.Key;
            await WriteLineAsync($"   {encName,-15} : {group.Count()} segments");
        }
        
        await WriteLineAsync();
        await WriteLineAsync("Encodings by total size:");
        var encodingSizes = segments
            .GroupBy(s => s.Encoding?.WebName)
            .OrderByDescending(g => g.Sum(s => s.Length));
        foreach (var group in encodingSizes)
        {
            string encName = string.IsNullOrWhiteSpace(group.Key) ? "[NULL]" : group.Key;
            long totalBytes = group.Sum(s => s.Length);
            await WriteLineAsync($"   {encName,-15} : {totalBytes:N0} bytes");
        }
        
        await WriteLineAsync();
        var largestSegments = segments
            .OrderByDescending(s => s.Length)
            .Take(take)
            .ToList();

        int actualCount = largestSegments.Count;
        string messagePrefix = segments.Count <= take ? "All" : $"Top {actualCount}";

        await WriteLineAsync($"{messagePrefix} largest encoding segments:");
        foreach (var seg in largestSegments)
            await WriteLineAsync($"   {seg.ToByteString()}");
    }

    public async Task LogEncodingDetails(List<EncodingSegment> segments)
    {
        await WriteLineAsync();
        await WriteLineAsync("----------------------------------------------------------");
        await WriteLineAsync("           [X] ENCODING DETECTION DETAILS [X]");
        await WriteLineAsync("----------------------------------------------------------");
        
        foreach (var segment in segments)
        {
            await WriteLineAsync(segment.ToByteString());
        }
    }

    public async Task LogEncodingConversion()
    {
        await WriteLineAsync();
        await WriteLineAsync("---------------------------------------------");
        await WriteLineAsync("         [X] ENCODING CONVERSION [X]");
        await WriteLineAsync("---------------------------------------------");
        await Log("Encoding conversion started");
    }
    public async Task LogContentHeader()
    {
        await WriteLineAsync();
        await WriteLineAsync("---------------------------------------------");
        await WriteLineAsync("            [X] CONTENT PARSE [X]");
        await WriteLineAsync("---------------------------------------------");
        await Log("Content parsing started");
    }
    
    public async Task LogDelimiterHeuristic(DelimiterHeuristicResult result, int take = 5)
    {
        
        await WriteLineAsync();
        await WriteLineAsync("---------------------------------------------");
        await WriteLineAsync("         [X] DELIMITER DETECTION [X]");
        await WriteLineAsync("---------------------------------------------");
        
        await Log($"Best delimiter: [{result.BestDelimiter}]");
        await WriteLineAsync($"Sampled {result.SampledLines:N0} lines (~{result.SampledBytes} chars) in {result.Duration} seconds");
        
        var actualCount = Math.Min(take, result.Candidates.Count);
        string messagePrefix = actualCount == take ? "All" : $"Top {actualCount}";
        
        await WriteLineAsync($"{messagePrefix} delimiter candidates detail:");
        foreach (var candidate in result.Candidates.Take(actualCount))
            await WriteLineAsync($"    {candidate}");

        await WriteLineAsync();
    }

    public async Task LogSqlInsertHeader(SqlInsertHeader insertHeader)
    {
        await Log($"SQL insert header: {insertHeader.FullHeader}");
        await WriteLineAsync($"SQL insert subject: {insertHeader.Subject}");
        
        for (int i = 0; i < insertHeader.Headers.Count; i++)
            await WriteLineAsync($"   [{i}] {insertHeader.Headers[i]}");
        
        await WriteLineAsync();
    }

    public async Task LogSchemaDetectionHeader()
    {
        await WriteLineAsync();
        await WriteLineAsync("---------------------------------------------");
        await WriteLineAsync("           [X] SCHEMA DETECTION [X]");
        await WriteLineAsync("---------------------------------------------");
    }

    public async Task LogSample(string sample)
    {
        await WriteLineAsync(sample);
    }
    
    public async Task LogHeuristicData(SchemaHeuristic analyzer)
    {
        await WriteLineAsync();
        await WriteLineAsync("Heuristic data:");

        foreach (var kvp in analyzer.AttributeCountsPerPosition.OrderBy(x => x.Key))
        {
            int position = kvp.Key;
            int[] counts = kvp.Value;
            int total = counts.Sum();

            await WriteLineAsync($"   Position {position}: total {total}");

            var stats =
                counts.Select((count, index) => new { Attribute = (ItemEnum)index, Count = count })
                    .Where(x => x.Count > 0)
                    .OrderByDescending(x => x.Count);

            foreach (var rec in stats)
            {
                double pct = total == 0 ? 0 : (double)rec.Count / total * 100.0;
                await WriteLineAsync($"      {rec.Attribute} = {rec.Count} ({pct:0.##}%)");
            }
        }
        await WriteLineAsync();
    }
    
    public async Task LogDominantSchema(SchemaHeuristic analyzer, double threshold)
    {
        await WriteLineAsync($"Dominant schema (SuccessRate >= {threshold}%):");

        // Expanded schema contains Previous entries
        var dominantSchema = analyzer.GetDominantSchema(threshold);

        // Cached % for the leading positions (no recompute)
        var dominantStats = analyzer.GetDominantStats(threshold);

        foreach (var kvp in dominantSchema.OrderBy(x => x.Key))
        {
            int position = kvp.Key;
            var attribute = kvp.Value;

            if (attribute == ItemEnum.Previous)
            {
                await WriteLineAsync($"   Position {position} = ({attribute})");
                continue;
            }

            // Use cached percentage if available
            double percent = dominantStats.TryGetValue(position, out var tuple) ? tuple.percent : 0.0;

            await WriteLineAsync($"   Position {position} = {attribute} - {percent:0.##}%");
        }
        await WriteLineAsync();
    }
    
    public async Task LogFinalSchema(Dictionary<int, ItemEnum> schema)
    {
        await WriteLineAsync("Final schema = Dominant + (Assigned or Guessed):");

        foreach (var kvp in schema.OrderBy(k => k.Key))
            await WriteLineAsync($"   Position {kvp.Key} = {kvp.Value}");

        await WriteLineAsync();
    }
    
    public async Task LogParseStats(ParseStats stats)
    {
        stats.ParseEnd = DateTime.Now;
        
        await WriteLineAsync();
        await WriteLineAsync("------------------------------------------------");
        await WriteLineAsync("             [X] FILE PARSE STATS [X]");
        await WriteLineAsync("------------------------------------------------");

        await WriteLineAsync($"File name: {stats.FileName}");
        await WriteLineAsync($"Parse ID: {stats.ParseId}");

        string originalEnc = string.IsNullOrEmpty(stats.Encoding?.WebName) ? "[NULL]" : stats.Encoding.WebName;
        await WriteLineAsync($"Original encoding: {originalEnc}");
        string? originEncCount = Convert.ToString(stats.EncodingSegments.Count == 0 ? "[NULL]" : stats.EncodingSegments.Count);
        await WriteLineAsync($"Encoding segments: {originEncCount}");

        await WriteLineAsync("Delimiters:");
        foreach (var delimiter in stats.Delimiters)
        {
            var display = delimiter == '\t' ? "\\t" : delimiter.ToString();
            await WriteLineAsync($"   '{display}'");
        }

        await WriteLineAsync("Formats:");
        foreach (var format in stats.Formats)
            await WriteLineAsync($"   {format}");
        
        await WriteLineAsync("Subjects:");
        foreach (var subject in stats.Context)
            await WriteLineAsync($"   {subject}");

        await WriteLineAsync($"Correct records parsed: {stats.RecordsRead:N0}");
        await WriteLineAsync($"Malformed records parsed: {stats.MalformedRecordsRead:N0}");
        await WriteLineAsync($"Parse accuracy (correct vs malformed): {stats.Accuracy:F2} %");
        
        await WriteLineAsync($"Lines parsed: {stats.LinesRead:N0}");
        await WriteLineAsync($"Line speed: {stats.LineSpeed:F2} lines/sec");
        
        await WriteLineAsync($"Bytes parsed: {stats.BytesRead:N0}");
        await WriteLineAsync($"Byte speed: {stats.ByteSpeed:F2} bytes/sec");

        await WriteLineAsync($"Parse start: {stats.ParseStart.ToString("F", CultureInfo.InvariantCulture)}");
        await WriteLineAsync($"Parse ended: {stats.ParseEnd.ToString("F", CultureInfo.InvariantCulture)}");
        await WriteLineAsync($"Parse time: {stats.Duration}");
        await WriteLineAsync();
    }

    public void Dispose()
    {
        _writer.Flush();
        _writer.Close();
        _writer.Dispose();
    }
}