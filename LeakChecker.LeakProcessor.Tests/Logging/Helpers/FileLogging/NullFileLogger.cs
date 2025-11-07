using LeakChecker.Encodings;
using LeakChecker.Format;
using LeakChecker.Logging;
using LeakChecker.Logging.FileLogging;

namespace LeakProcessor.Tests.Logging.Helpers.FileLogging;

public sealed class NullFileLogger(string path) : IFileLogger
{
    public string SubjectFileName { get; } = Path.GetFileName(path);
    public string SubjectFilePath { get; } = path;
    public long SubjectFileBytes { get; } = new FileInfo(path).Length;
    public DateTime ProcessingStart { get; } = DateTime.UtcNow;

    public Task Log(string? message, LogLevel level = LogLevel.Info, LogContext? context = null) => Task.CompletedTask;
    public Task LogEncodingHeader() => Task.CompletedTask;
    public Task LogConsistEncDetection() => Task.CompletedTask;
    public Task LogConcatEncDetection() => Task.CompletedTask;
    public Task LogEncodingStats(List<EncodingSegment> segments) => Task.CompletedTask;
    public Task LogEncodingDetails(List<EncodingSegment> segments) => Task.CompletedTask;
    public Task LogContentHeader() => Task.CompletedTask;
    public Task LogSqlInsertHeader(string subject, string columnList, string fullHeader) => Task.CompletedTask;
    public Task LogContentHeuristic(SchemaHeuristic analyzer, double threshold = 50) => Task.CompletedTask;

    public void Dispose() { }
}