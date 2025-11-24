using LeakChecker.Content;
using LeakChecker.Encodings;
using LeakChecker.Format;
using LeakChecker.Logging;
using LeakChecker.Logging.FileLogging;

namespace LeakProcessor.Tests.Logging.Helpers.FileLogging;

public sealed class NullFileLogger(string path) : IFileLogger
{
    public Guid ParseId { get; } = Guid.Empty;
    public Guid ExecutionId { get; } = Guid.Empty;
    public DateTime ParseStart { get; } = DateTime.UtcNow;
    public string SubjectFileName { get; } = Path.GetFileName(path);
    public string SubjectFilePath { get; } = path;
    public string SubjectTmpFilePath { get; } = string.Empty;

    public Task Log(string? message, LogLevel level = LogLevel.Info, LogContext? context = null) => Task.CompletedTask;
    public Task LogEncodingHeader() => Task.CompletedTask;
    public Task LogEncodingStats(List<EncodingSegment> segments) => Task.CompletedTask;
    public Task LogEncodingDetails(List<EncodingSegment> segments) => Task.CompletedTask;
    public Task LogContentHeader() => Task.CompletedTask;
    public Task LogDelimiterHeuristic(DelimiterHeuristicResult result, int count) => Task.CompletedTask;
    public Task LogSchemaDetectionHeader() => Task.CompletedTask;
    public Task LogSample(string sample) => Task.CompletedTask;
    public Task LogSqlInsertHeader(string subject, IList<string> headers, string fullHeader) => Task.CompletedTask;
    public Task LogHeuristicData(SchemaHeuristic analyzer) => Task.CompletedTask;
    public Task LogDominantSchema(SchemaHeuristic analyzer, double threshold) => Task.CompletedTask;
    public Task LogFinalSchema(Dictionary<int, ItemEnum> schema) => Task.CompletedTask; 
    public Task LogFileStats(FileStats stats) => Task.CompletedTask;
    public void Dispose() { }
}