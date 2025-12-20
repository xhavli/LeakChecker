using LeakChecker.Content;
using LeakChecker.Encodings;
using LeakChecker.Format.Detection;
using LeakChecker.Format.Schema;

namespace LeakChecker.Logging.FileLogging;

public interface IFileLogger : IDisposable
{
    Guid ParseId { get; }
    Guid ExecutionId { get; }
    DateTime ParseStart { get; }
    string SubjectFileName { get; }
    string SubjectFilePath { get; }
    string SubjectTmpFilePath { get; }

    Task Log(string? message, LogLevel level = LogLevel.Info, LogContext? context = null);
    Task LogEncodingHeader();
    Task LogEncodingStats(List<EncodingSegment> segments);
    Task LogEncodingDetails(List<EncodingSegment> segments);
    Task LogContentHeader();
    Task LogDelimiterHeuristic(DelimiterHeuristicResult result, int count);
    Task LogSchemaDetectionHeader();
    Task LogSample(string sample);
    Task LogSqlInsertHeader(string subject, IList<string> headers, string fullHeader);
    Task LogHeuristicData(SchemaHeuristic analyzer);
    Task LogDominantSchema(SchemaHeuristic analyzer, double threshold);
    Task LogFinalSchema(Dictionary<int, ItemEnum> schema);
    Task LogFileStats(FileStats stats);
}