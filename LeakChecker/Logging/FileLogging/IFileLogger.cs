using LeakChecker.Content;
using LeakChecker.Encodings;
using LeakChecker.Format;

namespace LeakChecker.Logging.FileLogging;

public interface IFileLogger : IDisposable
{
    string SubjectFileName { get; }
    string SubjectFilePath { get; }
    long SubjectFileBytes { get; }
    DateTime ProcessingStart { get; }

    Task Log(string? message, LogLevel level = LogLevel.Info, LogContext? context = null);
    Task LogEncodingHeader();
    Task LogConsistEncDetection();
    Task LogConcatEncDetection();
    Task LogEncodingStats(List<EncodingSegment> segments);
    Task LogEncodingDetails(List<EncodingSegment> segments);
    Task LogContentHeader();
    Task LogSqlInsertHeader(string subject, IList<string> headers, string fullHeader);
    Task LogHeuristicData(SchemaHeuristic analyzer, double threshold);
    Task LogSchema(Dictionary<int, ItemEnum> schema);
}