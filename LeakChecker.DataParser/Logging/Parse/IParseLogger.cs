using LeakChecker.DataParser.Content;
using LeakChecker.DataParser.Encodings;
using LeakChecker.DataParser.Format.Detection;
using LeakChecker.DataParser.Format.Schema;

namespace LeakChecker.DataParser.Logging.Parse;

public interface IParseLogger : IDisposable
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
    Task LogEncodingConversion(string message);
    Task LogContentHeader();
    Task LogDelimiterHeuristic(DelimiterHeuristicResult result, int count);
    Task LogSchemaDetectionHeader();
    Task LogSample(string sample);
    Task LogSqlInsertHeader(SqlInsertDetector.SqlHeader header);
    Task LogHeuristicData(SchemaHeuristic analyzer);
    Task LogDominantSchema(SchemaHeuristic analyzer, double threshold);
    Task LogFinalSchema(Dictionary<int, ItemEnum> schema);
    Task LogFileStats(ParseStats stats);
}