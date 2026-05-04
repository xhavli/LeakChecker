using LeakChecker.Common.Enums;
using LeakChecker.DataParser.Encodings;
using LeakChecker.DataParser.Format.Detection;
using LeakChecker.DataParser.Format.Schema;
using LeakChecker.DataParser.Stats.Parse;
using MongoDB.Bson;

namespace LeakChecker.DataParser.Logging.Parse;

public interface IParseLogger : IDisposable
{
    ObjectId ParseId { get; }
    DateTime ParseStart { get; }
    string SubjectFilePath { get; }
    string SubjectTmpFilePath { get; }

    void Log(string? message, LogLevel level = LogLevel.Info, LogContext? context = null);
    void LogEncodingHeader();
    void LogEncodingStats(List<EncodingSegment> segments, int take = 10);
    void LogEncodingDetails(List<EncodingSegment> segments);
    void LogEncodingConversion();
    void LogContentHeader();
    void LogDelimiterHeuristic(DelimiterHeuristicResult result, int take = 10);
    void LogSchemaDetectionHeader();
    void LogSample(string sample);
    void LogSqlInsertHeader(SqlInsertHeader insertHeader);
    void LogHeuristicData(SchemaHeuristic analyzer);
    void LogDominantSchema(SchemaHeuristic analyzer, double threshold);
    void LogFinalSchema(Dictionary<int, ItemEnum> schema);
    void LogParseStats(ParseStats stats);
}