using LeakChecker.DataParser.Content;
using LeakChecker.DataParser.Encodings;
using LeakChecker.DataParser.Format.Detection;
using LeakChecker.DataParser.Format.Schema;
using LeakChecker.DataParser.Stats.Parse;

namespace LeakChecker.DataParser.Logging.Parse;

public interface IParseLogger : IDisposable
{
    Guid ParseId { get; }
    DateTime ParseStart { get; }
    string SubjectFilePath { get; }
    string SubjectTmpFilePath { get; }

    Task Log(string? message, LogLevel level = LogLevel.Info, LogContext? context = null);
    Task LogEncodingHeader();
    Task LogEncodingStats(List<EncodingSegment> segments, int take = 10);
    Task LogEncodingDetails(List<EncodingSegment> segments);
    Task LogEncodingConversion();
    Task LogContentHeader();
    Task LogDelimiterHeuristic(DelimiterHeuristicResult result, int take = 10);
    Task LogSchemaDetectionHeader();
    Task LogSample(string sample);
    Task LogSqlInsertHeader(SqlInsertHeader insertHeader);
    Task LogHeuristicData(SchemaHeuristic analyzer);
    Task LogDominantSchema(SchemaHeuristic analyzer, double threshold);
    Task LogFinalSchema(Dictionary<int, ItemEnum> schema);
    Task LogParseStats(ParseStats stats);
}