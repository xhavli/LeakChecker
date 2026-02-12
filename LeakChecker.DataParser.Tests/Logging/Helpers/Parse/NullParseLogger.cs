using LeakChecker.Content;
using LeakChecker.Encodings;
using LeakChecker.Format.Detection;
using LeakChecker.Format.Schema;
using LeakChecker.Logging;
using LeakChecker.Logging.Parse;

namespace LeakChecker.DataParser.Tests.Logging.Helpers.Parse;

public sealed class NullParseLogger(string path) : IParseLogger
{
    public Guid ParseId { get; } = Guid.Empty;
    public Guid ExecutionId { get; } = Guid.Empty;
    public DateTime ParseStart { get; } = DateTime.MaxValue;
    public string SubjectFileName { get; } = string.Empty;
    public string SubjectFilePath { get; } = path;
    public string SubjectTmpFilePath { get; } = string.Empty;

    public Task Log(string? message, LogLevel level = LogLevel.Info, LogContext? context = null) => Task.CompletedTask;
    public Task LogEncodingHeader() => Task.CompletedTask;
    public Task LogEncodingStats(List<EncodingSegment> segments) => Task.CompletedTask;
    public Task LogEncodingDetails(List<EncodingSegment> segments) => Task.CompletedTask;
    public Task LogEncodingConversion(string message) => Task.CompletedTask;
    public Task LogContentHeader() => Task.CompletedTask;
    public Task LogDelimiterHeuristic(DelimiterHeuristicResult result, int count) => Task.CompletedTask;
    public Task LogSchemaDetectionHeader() => Task.CompletedTask;
    public Task LogSample(string sample) => Task.CompletedTask;
    public Task LogSqlInsertHeader(string subject, IList<string> headers, string fullHeader) => Task.CompletedTask;
    public Task LogHeuristicData(SchemaHeuristic analyzer) => Task.CompletedTask;
    public Task LogDominantSchema(SchemaHeuristic analyzer, double threshold) => Task.CompletedTask;
    public Task LogFinalSchema(Dictionary<int, ItemEnum> schema) => Task.CompletedTask; 
    public Task LogFileStats(ParseStats stats) => Task.CompletedTask;
    public void Dispose() { }
}