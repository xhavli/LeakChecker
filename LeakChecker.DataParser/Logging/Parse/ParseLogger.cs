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

    public static IParseLogger Create(string subjectFilePath, ISettings settings)
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
        
        logger.CreateLogHeader();

        return logger;
    }
    
    private void LogInternal(string message = "")
    {
        if (_verbose)
            Console.WriteLine(message);
        
        _writer.WriteLine(message);
    }
    
    public void Log(string? message, LogLevel level = LogLevel.Info, LogContext? context = null )
    {
        string log = context is null ? $"[{DateTime.Now:T}] {level.GetString()} {message}"
                                     : $"[{DateTime.Now:T}] {level.GetString()} {context.Value.GetString()} {message}";
        
        if (!_verbose)   // Log without colored console print
        {
            LogInternal(log);
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
        LogInternal(log);
        Console.ResetColor();
    }

    private void CreateLogHeader()
    {
        string timeStamp = ParseStart.ToString("F", CultureInfo.InvariantCulture);
        FileInfo fileInfo = new FileInfo(SubjectFilePath);
        long fileSize = fileInfo.Length;
        double sizeMb = (double) fileSize / SizeEnum.MegaByte;
        double sizeGb = (double) fileSize / SizeEnum.GigaByte;

        LogInternal($"Parse start: {timeStamp}");
        LogInternal($"File path: {fileInfo.FullName}");
        LogInternal($"File name: {fileInfo.Name}");
        LogInternal($"File size: {sizeGb:F2} GiB / {sizeMb:F2} MiB / {fileSize:N0} Bytes");
    }
    
    public void LogEncodingHeader()
    {
        LogInternal();
        LogInternal("---------------------------------------------");
        LogInternal("          [X] ENCODING DETECTION [X]");
        LogInternal("---------------------------------------------");
        Log("Encoding detection started");
    }
    
    public void LogEncodingStats(List<EncodingSegment> segments, int take = 10)
    {
        LogInternal();
        LogInternal("---------------------------------------------");
        LogInternal("    [X] ENCODING DETECTION STATISTICS [X]");
        LogInternal("---------------------------------------------");
        
        LogInternal($"Encoding segments count: {segments.Count}");
        int distinctEncodingCount = segments
            .Select(s => s.Encoding?.WebName)
            .Distinct()
            .Count();
        LogInternal($"Different encodings count: {distinctEncodingCount}");
        
        LogInternal();
        LogInternal("Encodings by segment count:");
        var encodingCounts = segments
            .GroupBy(s => s.Encoding?.WebName)
            .OrderByDescending(g => g.Count());
        foreach (var group in encodingCounts)
        {
            string encName = string.IsNullOrWhiteSpace(group.Key) ? "[NULL]" : group.Key;
            LogInternal($"   {encName,-15} : {group.Count()} segments");
        }
        
        LogInternal();
        LogInternal("Encodings by total size:");
        var encodingSizes = segments
            .GroupBy(s => s.Encoding?.WebName)
            .OrderByDescending(g => g.Sum(s => s.Length));
        foreach (var group in encodingSizes)
        {
            string encName = string.IsNullOrWhiteSpace(group.Key) ? "[NULL]" : group.Key;
            long totalBytes = group.Sum(s => s.Length);
            LogInternal($"   {encName,-15} : {totalBytes:N0} bytes");
        }
        
        LogInternal();
        var largestSegments = segments
            .OrderByDescending(s => s.Length)
            .Take(take)
            .ToList();

        int actualCount = largestSegments.Count;
        string messagePrefix = segments.Count <= take ? "All" : $"Top {actualCount}";

        LogInternal($"{messagePrefix} largest encoding segments:");
        foreach (var seg in largestSegments)
            LogInternal($"   {seg.ToByteString()}");
    }

    public void LogEncodingDetails(List<EncodingSegment> segments)
    {
        LogInternal();
        LogInternal("----------------------------------------------------------");
        LogInternal("           [X] ENCODING DETECTION DETAILS [X]");
        LogInternal("----------------------------------------------------------");
        
        foreach (var segment in segments)
        {
            LogInternal(segment.ToByteString());
        }
    }

    public void LogEncodingConversion()
    {
        LogInternal();
        LogInternal("---------------------------------------------");
        LogInternal("         [X] ENCODING CONVERSION [X]");
        LogInternal("---------------------------------------------");
        Log("Encoding conversion started");
    }
    public void LogContentHeader()
    {
        LogInternal();
        LogInternal("---------------------------------------------");
        LogInternal("            [X] CONTENT PARSE [X]");
        LogInternal("---------------------------------------------");
        Log("Content parsing started");
    }
    
    public void LogDelimiterHeuristic(DelimiterHeuristicResult result, int take = 5)
    {
        
        LogInternal();
        LogInternal("---------------------------------------------");
        LogInternal("         [X] DELIMITER DETECTION [X]");
        LogInternal("---------------------------------------------");
        
        Log($"Best delimiter: [{result.BestDelimiter}]");
        LogInternal($"Sampled {result.SampledLines:N0} lines (~{result.SampledBytes} chars) in {result.Duration} seconds");
        
        var actualCount = Math.Min(take, result.Candidates.Count);
        string messagePrefix = actualCount == take ? "All" : $"Top {actualCount}";
        
        LogInternal($"{messagePrefix} delimiter candidates detail:");
        foreach (var candidate in result.Candidates.Take(actualCount))
            LogInternal($"    {candidate}");

        LogInternal();
    }

    public void LogSqlInsertHeader(SqlInsertHeader insertHeader)
    {
        Log($"SQL insert header: {insertHeader.FullHeader}");
        LogInternal($"SQL insert subject: {insertHeader.Subject}");
        
        for (int i = 0; i < insertHeader.Headers.Count; i++)
            LogInternal($"   [{i}] {insertHeader.Headers[i]}");
        
        LogInternal();
    }

    public void LogSchemaDetectionHeader()
    {
        LogInternal();
        LogInternal("---------------------------------------------");
        LogInternal("           [X] SCHEMA DETECTION [X]");
        LogInternal("---------------------------------------------");
    }

    public void LogSample(string sample)
    {
        LogInternal(sample);
    }
    
    public void LogHeuristicData(SchemaHeuristic analyzer)
    {
        LogInternal();
        LogInternal("Heuristic data:");

        foreach (var kvp in analyzer.AttributeCountsPerPosition.OrderBy(x => x.Key))
        {
            int position = kvp.Key;
            int[] counts = kvp.Value;
            int total = counts.Sum();

            LogInternal($"   Position {position}: total {total}");

            var stats =
                counts.Select((count, index) => new { Attribute = (ItemEnum)index, Count = count })
                    .Where(x => x.Count > 0)
                    .OrderByDescending(x => x.Count);

            foreach (var rec in stats)
            {
                double pct = total == 0 ? 0 : (double)rec.Count / total * 100.0;
                LogInternal($"      {rec.Attribute} = {rec.Count} ({pct:0.##}%)");
            }
        }
        LogInternal();
    }
    
    public void LogDominantSchema(SchemaHeuristic analyzer, double threshold)
    {
        LogInternal($"Dominant schema (SuccessRate >= {threshold}%):");

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
                LogInternal($"   Position {position} = ({attribute})");
                continue;
            }

            // Use cached percentage if available
            double percent = dominantStats.TryGetValue(position, out var tuple) ? tuple.percent : 0.0;

            LogInternal($"   Position {position} = {attribute} - {percent:0.##}%");
        }
        LogInternal();
    }
    
    public void LogFinalSchema(Dictionary<int, ItemEnum> schema)
    {
        LogInternal("Final schema = Dominant + (Assigned or Guessed):");

        foreach (var kvp in schema.OrderBy(k => k.Key))
            LogInternal($"   Position {kvp.Key} = {kvp.Value}");

        LogInternal();
    }
    
    public void LogParseStats(ParseStats stats)
    {
        stats.ParseEnd = DateTime.Now;
        
        LogInternal();
        LogInternal("------------------------------------------------");
        LogInternal("             [X] FILE PARSE STATS [X]");
        LogInternal("------------------------------------------------");

        LogInternal($"File name: {stats.FileName}");
        LogInternal($"Parse ID: {stats.ParseId}");

        string originalEnc = string.IsNullOrEmpty(stats.Encoding?.WebName) ? "[NULL]" : stats.Encoding.WebName;
        LogInternal($"Original encoding: {originalEnc}");
        string? originEncCount = Convert.ToString(stats.EncodingSegments.Count == 0 ? "[NULL]" : stats.EncodingSegments.Count);
        LogInternal($"Encoding segments: {originEncCount}");

        LogInternal("Delimiters:");
        foreach (var delimiter in stats.Delimiters)
        {
            var display = delimiter == '\t' ? "\\t" : delimiter.ToString();
            LogInternal($"   '{display}'");
        }

        LogInternal("Formats:");
        foreach (var format in stats.Formats)
            LogInternal($"   {format}");
        
        LogInternal("Subjects:");
        foreach (var subject in stats.Context)
            LogInternal($"   {subject}");

        LogInternal($"Correct records parsed: {stats.RecordsRead:N0}");
        LogInternal($"Malformed records read: {stats.MalformedRead:N0}");
        LogInternal($"Parse accuracy (correct vs malformed): {stats.Accuracy:F2} %");
        
        LogInternal($"Lines parsed: {stats.LinesRead:N0}");
        LogInternal($"Line speed: {stats.LineSpeed:F2} lines/sec");
        
        LogInternal($"Bytes parsed: {stats.BytesRead:N0}");
        LogInternal($"Byte speed: {stats.ByteSpeed:F2} bytes/sec");

        LogInternal($"Parse start: {stats.ParseStart.ToString("F", CultureInfo.InvariantCulture)}");
        LogInternal($"Parse ended: {stats.ParseEnd.ToString("F", CultureInfo.InvariantCulture)}");
        LogInternal($"Parse time: {stats.Duration}");
        LogInternal();
    }

    public void Dispose()
    {
        _writer.Flush();
        _writer.Close();
        _writer.Dispose();
    }
}