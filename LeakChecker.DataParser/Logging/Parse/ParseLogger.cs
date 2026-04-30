using System.Globalization;
using LeakChecker.Common.Enums;
using LeakChecker.DataParser.Encodings;
using LeakChecker.DataParser.Format.Detection;
using LeakChecker.DataParser.Format.Schema;
using LeakChecker.DataParser.Helpers.Enums;
using LeakChecker.DataParser.Helpers.Settings;
using LeakChecker.DataParser.Stats.Parse;
using MongoDB.Bson;

namespace LeakChecker.DataParser.Logging.Parse;

public class ParseLogger : IParseLogger
{
    public ObjectId ParseId { get; }
    public DateTime ParseStart { get; }
    public string SubjectFilePath { get; }
    public string SubjectTmpFilePath { get; }
    private readonly bool _verbose;
    private readonly StreamWriter _writer;
    private const ConsoleColor InfoColor = ConsoleColor.DarkBlue;
    private const ConsoleColor WarningColor = ConsoleColor.DarkYellow;
    private const ConsoleColor SuccessColor = ConsoleColor.Green;
    private const ConsoleColor FailureColor = ConsoleColor.Red;

    private ParseLogger(
        ObjectId parseId,
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
        ObjectId parseId = ObjectId.GenerateNewId();
        DateTime parseStart = DateTime.Now;
        
        string fileName = Path.GetFileName(subjectFilePath);
        string parseTimeStamp = $"{parseStart:yyyy-MM-ddTHH-mm-ss}";
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
    
    private async Task LogInternal(string message = "")
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
            await LogInternal(log);
            return;
        }
        
        ConsoleColor logColor = level switch
        {
            LogLevel.Info => InfoColor,
            LogLevel.Warning => WarningColor,
            LogLevel.Success => SuccessColor,
            LogLevel.Failure => FailureColor,
            _ => Console.ForegroundColor
        };

        Console.ForegroundColor = logColor;
        await LogInternal(log);
        Console.ResetColor();
    }

    private async Task CreateLogHeaderAsync()
    {
        string timeStamp = ParseStart.ToString("F", CultureInfo.InvariantCulture);
        FileInfo fileInfo = new FileInfo(SubjectFilePath);
        long fileSize = fileInfo.Length;
        double sizeMb = (double) fileSize / SizeEnum.MegaByte;
        double sizeGb = (double) fileSize / SizeEnum.GigaByte;

        await LogInternal($"Parse start: {timeStamp}");
        await LogInternal($"File path: {fileInfo.FullName}");
        await LogInternal($"File name: {fileInfo.Name}");
        await LogInternal($"File size: {sizeGb:F2} GiB / {sizeMb:F2} MiB / {fileSize:N0} Bytes");
    }
    
    public async Task LogEncodingHeader()
    {
        await LogInternal();
        await LogInternal("---------------------------------------------");
        await LogInternal("          [X] ENCODING DETECTION [X]");
        await LogInternal("---------------------------------------------");
        await Log("Encoding detection started");
    }
    
    public async Task LogEncodingStats(List<EncodingSegment> segments, int take = 10)
    {
        await LogInternal();
        await LogInternal("---------------------------------------------");
        await LogInternal("    [X] ENCODING DETECTION STATISTICS [X]");
        await LogInternal("---------------------------------------------");
        
        await LogInternal($"Encoding segments count: {segments.Count}");
        int distinctEncodingCount = segments
            .Select(s => s.Encoding?.WebName)
            .Distinct()
            .Count();
        await LogInternal($"Different encodings count: {distinctEncodingCount}");
        
        await LogInternal();
        await LogInternal("Encodings by segment count:");
        var encodingCounts = segments
            .GroupBy(s => s.Encoding?.WebName)
            .OrderByDescending(g => g.Count());
        foreach (var group in encodingCounts)
        {
            string encName = string.IsNullOrWhiteSpace(group.Key) ? "[NULL]" : group.Key;
            await LogInternal($"   {encName,-15} : {group.Count()} segments");
        }
        
        await LogInternal();
        await LogInternal("Encodings by total size:");
        var encodingSizes = segments
            .GroupBy(s => s.Encoding?.WebName)
            .OrderByDescending(g => g.Sum(s => s.Length));
        foreach (var group in encodingSizes)
        {
            string encName = string.IsNullOrWhiteSpace(group.Key) ? "[NULL]" : group.Key;
            long totalBytes = group.Sum(s => s.Length);
            await LogInternal($"   {encName,-15} : {totalBytes:N0} bytes");
        }
        
        await LogInternal();
        var largestSegments = segments
            .OrderByDescending(s => s.Length)
            .Take(take)
            .ToList();

        int actualCount = largestSegments.Count;
        string messagePrefix = segments.Count <= take ? "All" : $"Top {actualCount}";

        await LogInternal($"{messagePrefix} largest encoding segments:");
        foreach (var seg in largestSegments)
            await LogInternal($"   {seg.ToByteString()}");
    }

    public async Task LogEncodingDetails(List<EncodingSegment> segments)
    {
        await LogInternal();
        await LogInternal("----------------------------------------------------------");
        await LogInternal("           [X] ENCODING DETECTION DETAILS [X]");
        await LogInternal("----------------------------------------------------------");
        
        foreach (var segment in segments)
        {
            await LogInternal(segment.ToByteString());
        }
    }

    public async Task LogEncodingConversion()
    {
        await LogInternal();
        await LogInternal("---------------------------------------------");
        await LogInternal("         [X] ENCODING CONVERSION [X]");
        await LogInternal("---------------------------------------------");
        await Log("Encoding conversion started");
    }
    public async Task LogContentHeader()
    {
        await LogInternal();
        await LogInternal("---------------------------------------------");
        await LogInternal("            [X] CONTENT PARSE [X]");
        await LogInternal("---------------------------------------------");
        await Log("Content parsing started");
    }
    
    public async Task LogDelimiterHeuristic(DelimiterHeuristicResult result, int take = 5)
    {
        
        await LogInternal();
        await LogInternal("---------------------------------------------");
        await LogInternal("         [X] DELIMITER DETECTION [X]");
        await LogInternal("---------------------------------------------");
        
        await Log($"Best delimiter: [{result.BestDelimiter}]");
        await LogInternal($"Sampled {result.SampledLines:N0} lines (~{result.SampledBytes} chars) in {result.Duration} seconds");
        
        var actualCount = Math.Min(take, result.Candidates.Count);
        string messagePrefix = actualCount == take ? "All" : $"Top {actualCount}";
        
        await LogInternal($"{messagePrefix} delimiter candidates detail:");
        foreach (var candidate in result.Candidates.Take(actualCount))
            await LogInternal($"    {candidate}");

        await LogInternal();
    }

    public async Task LogSqlInsertHeader(SqlInsertHeader insertHeader)
    {
        await Log($"SQL insert header: {insertHeader.FullHeader}");
        await LogInternal($"SQL insert subject: {insertHeader.Subject}");
        
        for (int i = 0; i < insertHeader.Headers.Count; i++)
            await LogInternal($"   [{i}] {insertHeader.Headers[i]}");
        
        await LogInternal();
    }

    public async Task LogSchemaDetectionHeader()
    {
        await LogInternal();
        await LogInternal("---------------------------------------------");
        await LogInternal("           [X] SCHEMA DETECTION [X]");
        await LogInternal("---------------------------------------------");
    }

    public async Task LogSample(string sample)
    {
        await LogInternal(sample);
    }
    
    public async Task LogHeuristicData(SchemaHeuristic analyzer)
    {
        await LogInternal();
        await LogInternal("Heuristic data:");

        foreach (var kvp in analyzer.AttributeCountsPerPosition.OrderBy(x => x.Key))
        {
            int position = kvp.Key;
            int[] counts = kvp.Value;
            int total = counts.Sum();

            await LogInternal($"   Position {position}: total {total}");

            var stats =
                counts.Select((count, index) => new { Attribute = (ItemEnum)index, Count = count })
                    .Where(x => x.Count > 0)
                    .OrderByDescending(x => x.Count);

            foreach (var rec in stats)
            {
                double pct = total == 0 ? 0 : (double)rec.Count / total * 100.0;
                await LogInternal($"      {rec.Attribute} = {rec.Count} ({pct:0.##}%)");
            }
        }
        await LogInternal();
    }
    
    public async Task LogDominantSchema(SchemaHeuristic analyzer, double threshold)
    {
        await LogInternal($"Dominant schema (SuccessRate >= {threshold}%):");

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
                await LogInternal($"   Position {position} = ({attribute})");
                continue;
            }

            // Use cached percentage if available
            double percent = dominantStats.TryGetValue(position, out var tuple) ? tuple.percent : 0.0;

            await LogInternal($"   Position {position} = {attribute} - {percent:0.##}%");
        }
        await LogInternal();
    }
    
    public async Task LogFinalSchema(Dictionary<int, ItemEnum> schema)
    {
        await LogInternal("Final schema = Dominant + (Assigned or Guessed):");

        foreach (var kvp in schema.OrderBy(k => k.Key))
            await LogInternal($"   Position {kvp.Key} = {kvp.Value}");

        await LogInternal();
    }
    
    public async Task LogParseStats(ParseStats stats)
    {
        stats.ParseEnd = DateTime.Now;
        
        await LogInternal();
        await LogInternal("------------------------------------------------");
        await LogInternal("             [X] FILE PARSE STATS [X]");
        await LogInternal("------------------------------------------------");

        await LogInternal($"File name: {stats.FileName}");
        await LogInternal($"Parse ID: {stats.ParseId}");

        string originalEnc = string.IsNullOrEmpty(stats.Encoding?.WebName) ? "[NULL]" : stats.Encoding.WebName;
        await LogInternal($"Original encoding: {originalEnc}");
        string? originEncCount = Convert.ToString(stats.EncodingSegments.Count == 0 ? "[NULL]" : stats.EncodingSegments.Count);
        await LogInternal($"Encoding segments: {originEncCount}");

        await LogInternal("Delimiters:");
        foreach (var delimiter in stats.Delimiters)
        {
            var display = delimiter == '\t' ? "\\t" : delimiter.ToString();
            await LogInternal($"   '{display}'");
        }

        await LogInternal("Formats:");
        foreach (var format in stats.Formats)
            await LogInternal($"   {format}");
        
        await LogInternal("Subjects:");
        foreach (var subject in stats.Context)
            await LogInternal($"   {subject}");

        await LogInternal($"Correct records parsed: {stats.RecordsRead:N0}");
        await LogInternal($"Malformed records read: {stats.MalformedRead:N0}");
        await LogInternal($"Parse accuracy (correct vs malformed): {stats.Accuracy:F2} %");
        
        await LogInternal($"Lines parsed: {stats.LinesRead:N0}");
        await LogInternal($"Line speed: {stats.LineSpeed:F2} lines/sec");
        
        await LogInternal($"Bytes parsed: {stats.BytesRead:N0}");
        await LogInternal($"Byte speed: {stats.ByteSpeed:F2} bytes/sec");

        await LogInternal($"Parse start: {stats.ParseStart.ToString("F", CultureInfo.InvariantCulture)}");
        await LogInternal($"Parse ended: {stats.ParseEnd.ToString("F", CultureInfo.InvariantCulture)}");
        await LogInternal($"Parse time: {stats.Duration}");
        await LogInternal();
    }

    public void Dispose()
    {
        _writer.Flush();
        _writer.Close();
        _writer.Dispose();
    }
}