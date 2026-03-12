using System.Globalization;
using System.Text;
using LeakChecker.DataParser.Content;
using LeakChecker.DataParser.Encodings;
using LeakChecker.DataParser.Format.Detection;
using LeakChecker.DataParser.Format.Schema;
using LeakChecker.DataParser.Utilities;
using LeakChecker.DataParser.Utilities.Configuration;

namespace LeakChecker.DataParser.Logging.Parse;

public class ParseLogger : IParseLogger
{
    public Guid ParseId { get; }
    public Guid ExecutionId { get; }
    public DateTime ParseStart { get; }
    public string SubjectFileName { get; }
    public string SubjectFilePath { get; }
    public string SubjectTmpFilePath { get; }
    private bool Verbose { get; }
    private readonly StreamWriter _writer;
    private const ConsoleColor InfoColor = ConsoleColor.DarkBlue;
    private const ConsoleColor WarningColor = ConsoleColor.DarkYellow;
    private const ConsoleColor SuccessColor = ConsoleColor.Green;
    private const ConsoleColor ExceptionColor = ConsoleColor.Red;

    private ParseLogger(
        Guid parseId,
        Guid executionId,
        DateTime parseStart,
        string subjectFilePath,
        string tmpFilePath,
        StreamWriter writer,
        bool verbose)
    {
        ParseId = parseId;
        ExecutionId = executionId;
        ParseStart = parseStart;
        SubjectFileName = Path.GetFileName(subjectFilePath);
        SubjectFilePath = subjectFilePath;
        SubjectTmpFilePath = tmpFilePath;
        _writer = writer;
        Verbose = verbose;
    }

    public static async Task<IParseLogger> CreateAsync(
        AppConfig config,
        Guid executionId,
        string subjectFilePath)
    {
        Guid parseId = Guid.NewGuid();
        DateTime parseStart = DateTime.Now;
        
        string subjectFileName = Path.GetFileName(subjectFilePath);
        string fileTimeStamp = $"{parseStart:yyyy-M-dTHH-mm-ss}";
        string logFileName = $"{fileTimeStamp}_{subjectFileName}_{parseId}.txt";
        string logFilePath = Path.Combine(config.LogDirectory, logFileName);
        string tmpFilePath = Path.Combine(config.TmpDirectory, logFileName);

        bool isDevelopment = string.Equals(config.Environment.Trim(), "Development", StringComparison.OrdinalIgnoreCase);
        var writer = new StreamWriter(logFilePath, append: true, encoding: new UTF8Encoding(encoderShouldEmitUTF8Identifier: false))
        {
            AutoFlush = isDevelopment
        };
        
        var logger = new ParseLogger(parseId, executionId, parseStart, subjectFilePath, tmpFilePath, writer, config.Verbose);
        
        await logger.CreateLogHeaderAsync();

        return logger;
    }
    
    private async Task LogLineAsync(string message = "")
    {
        if (Verbose) Console.WriteLine(message);
        await _writer.WriteLineAsync(message);
    }
    
    public async Task Log(string? message, LogLevel level = LogLevel.Info, LogContext? context = null )
    {
        string log = context is null ? $"[{DateTime.Now:T}] {level.GetString()} {message}"
                                     : $"[{DateTime.Now:T}] {level.GetString()} {context.Value.GetString()} {message}";
        
        if (!Verbose)   // Log without colored console print
        {
            await LogLineAsync(log);
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
        await LogLineAsync(log);
        Console.ResetColor();
    }

    private async Task CreateLogHeaderAsync()
    {
        string timeStamp = ParseStart.ToString("F", CultureInfo.InvariantCulture);
        FileInfo fileInfo = new FileInfo(SubjectFilePath);
        long fileSize = fileInfo.Length;
        double sizeMb = (double) fileSize / SizeEnum.MegaByte;
        double sizeGb = (double) fileSize / SizeEnum.GigaByte;

        await LogLineAsync($"Parse start: {timeStamp}");
        await LogLineAsync($"File path: {fileInfo.FullName}");
        await LogLineAsync($"File name: {fileInfo.Name}");
        await LogLineAsync($"File size: {sizeGb:F2} GiB / {sizeMb:F2} MiB / {fileSize:N0} Bytes");
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
        
        await LogLineAsync();
        await LogLineAsync("Encodings by segment count:");
        var encodingCounts = segments
            .GroupBy(s => s.Encoding?.WebName)
            .OrderByDescending(g => g.Count());
        foreach (var group in encodingCounts)
        {
            string encName = string.IsNullOrWhiteSpace(group.Key) ? "[NULL]" : group.Key;
            await LogLineAsync($"   {encName,-15} : {group.Count()} segments");
        }
        
        await LogLineAsync();
        await LogLineAsync("Encodings by total size:");
        var encodingSizes = segments
            .GroupBy(s => s.Encoding?.WebName)
            .OrderByDescending(g => g.Sum(s => s.Length));
        foreach (var group in encodingSizes)
        {
            string encName = string.IsNullOrWhiteSpace(group.Key) ? "[NULL]" : group.Key;
            long totalBytes = group.Sum(s => s.Length);
            await LogLineAsync($"   {encName,-15} : {totalBytes:N0} bytes");
        }
        
        await LogLineAsync();
        int count = 5;
        await LogLineAsync($"Top {count} largest encoding segments:");
        var largestSegments = segments
            .OrderByDescending(s => s.Length)
            .Take(count)
            .ToList();
        foreach (var seg in largestSegments)
            await LogLineAsync($"   {seg.ToByteString()}");
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

    public async Task LogEncodingConversion(string message)
    {
        await LogLineAsync();
        await LogLineAsync("---------------------------------------------");
        await LogLineAsync("         [X] ENCODING CONVERSION [X]");
        await LogLineAsync("---------------------------------------------");
        await Log(message);
    }
    public async Task LogContentHeader()
    {
        await LogLineAsync();
        await LogLineAsync("---------------------------------------------");
        await LogLineAsync("            [X] CONTENT PARSE [X]");
        await LogLineAsync("---------------------------------------------");
        await Log("Content parsing started");
    }
    
    public async Task LogDelimiterHeuristic(DelimiterHeuristicResult result, int count = 5)
    {
        await LogLineAsync();
        await LogLineAsync("---------------------------------------------");
        await LogLineAsync("         [X] DELIMITER DETECTION [X]");
        await LogLineAsync("---------------------------------------------");
        await Log($"Best delimiter: [{result.BestDelimiter}]");
        await LogLineAsync($"Sampled {result.SampledLines:N0} lines (~{result.SampledBytes} chars) in {result.Duration} seconds");
        await LogLineAsync($"Top {count} delimiter candidates detail:");
        foreach (var candidate in result.Candidates.Take(count))
            await LogLineAsync($"    {candidate}");

        await LogLineAsync();
    }

    public async Task LogSqlInsertHeader(SqlInsertDetector.SqlHeader header)
    {
        await Log($"SQL insert header: {header.FullHeader}");
        await LogLineAsync($"SQL insert subject: {header.Subject}");
        
        for (int i = 0; i < header.Headers.Count; i++)
            await LogLineAsync($"   [{i}] {header.Headers[i]}");
        
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
        await LogLineAsync();
    }
    
    public async Task LogDominantSchema(SchemaHeuristic analyzer, double threshold)
    {
        await LogLineAsync($"Dominant schema (SuccessRate >= {threshold}%):");

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
                await LogLineAsync($"   Position {position} = ({attribute})");
                continue;
            }

            // Use cached percentage if available
            double percent = dominantStats.TryGetValue(position, out var tuple) ? tuple.percent : 0.0;

            await LogLineAsync($"   Position {position} = {attribute} - {percent:0.##}%");
        }
        await LogLineAsync();
    }
    
    public async Task LogFinalSchema(Dictionary<int, ItemEnum> schema)
    {
        await LogLineAsync("Final schema = Dominant + (Assigned or Guessed):");

        foreach (var kvp in schema.OrderBy(k => k.Key))
            await LogLineAsync($"   Position {kvp.Key} = {kvp.Value}");

        await LogLineAsync();
    }
    
    public async Task LogFileStats(ParseStats stats)
    {
        stats.ParseEnd = DateTime.Now;
        
        await LogLineAsync();
        await LogLineAsync("------------------------------------------------");
        await LogLineAsync("             [X] FILE PARSE STATS [X]");
        await LogLineAsync("------------------------------------------------");

        await LogLineAsync($"File name: {stats.FileName}");
        await LogLineAsync($"Parse ID: {stats.ParseId}");

        string originEnc = string.IsNullOrEmpty(stats.Encoding?.WebName) ? "[NULL]" : stats.Encoding.WebName;
        await LogLineAsync($"Origin encoding: {originEnc}");
        string? originEncCount = Convert.ToString(stats.EncodingSegments.Count == 0 ? "[NULL]" : stats.EncodingSegments.Count);
        await LogLineAsync($"Encoding segments: {originEncCount}");

        await LogLineAsync("Delimiters:");
        foreach (var delimiter in stats.Delimiters)
        {
            var display = delimiter == '\t' ? "\\t" : delimiter.ToString();
            await LogLineAsync($"   '{display}'");
        }

        await LogLineAsync("Formats:");
        foreach (var format in stats.Formats)
            await LogLineAsync($"   {format}");
        
        await LogLineAsync("Subjects:");
        foreach (var subject in stats.Context)
            await LogLineAsync($"   {subject}");

        await LogLineAsync($"Correct records parsed: {stats.RecordsRead:N0}");
        await LogLineAsync($"Malformed records parsed: {stats.MalformedRecordsRead:N0}");
        await LogLineAsync($"Parse accuracy (correct vs malformed): {stats.Accuracy:F2} %");
        
        await LogLineAsync($"Lines parsed: {stats.LinesRead:N0}");
        await LogLineAsync($"Line speed: {stats.LineSpeed:F2} lines/sec");
        
        await LogLineAsync($"Bytes parsed: {stats.BytesRead:N0}");
        await LogLineAsync($"Byte speed: {stats.ByteSpeed:F2} bytes/sec");

        await LogLineAsync($"Parse start: {stats.ParseStart.ToString("F", CultureInfo.InvariantCulture)}");
        await LogLineAsync($"Parse ended: {stats.ParseEnd.ToString("F", CultureInfo.InvariantCulture)}");
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