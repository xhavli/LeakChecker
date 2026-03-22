using LeakChecker.DataParser.Content;
using LeakChecker.DataParser.Encodings;
using LeakChecker.DataParser.Format.Detection;
using LeakChecker.DataParser.Format.Schema;
using LeakChecker.DataParser.Logging;
using LeakChecker.DataParser.Logging.Parse;
using LeakChecker.DataParser.Stats.Parse;

namespace LeakChecker.DataParser.Tests.Helpers.Logging.Parse;

public sealed class NullParseLogger : IParseLogger
{
    public Guid ParseId => Guid.Empty;
    public DateTime ParseStart => DateTime.MaxValue;
    public string SubjectFilePath => string.Empty;
    public string SubjectTmpFilePath => string.Empty;

    public Task Log(string? message, LogLevel level = LogLevel.Info, LogContext? context = null) => Task.CompletedTask;
    public Task LogEncodingHeader() => Task.CompletedTask;
    public Task LogEncodingStats(List<EncodingSegment> segments) => Task.CompletedTask;
    public Task LogEncodingDetails(List<EncodingSegment> segments) => Task.CompletedTask;
    public Task LogEncodingConversion(string message) => Task.CompletedTask;
    public Task LogContentHeader() => Task.CompletedTask;
    public Task LogDelimiterHeuristic(DelimiterHeuristicResult result, int count) => Task.CompletedTask;
    public Task LogSchemaDetectionHeader() => Task.CompletedTask;
    public Task LogSample(string sample) => Task.CompletedTask;
    public Task LogSqlInsertHeader(SqlInsertHeader insertHeader) => Task.CompletedTask;
    public Task LogHeuristicData(SchemaHeuristic analyzer) => Task.CompletedTask;
    public Task LogDominantSchema(SchemaHeuristic analyzer, double threshold) => Task.CompletedTask;
    public Task LogFinalSchema(Dictionary<int, ItemEnum> schema) => Task.CompletedTask; 
    public Task LogParseStats(ParseStats stats) => Task.CompletedTask;
    public void Dispose() { }
}