using LeakChecker.Common.Enums;
using LeakChecker.DataParser.Encodings;
using LeakChecker.DataParser.Format.Detection;
using LeakChecker.DataParser.Format.Schema;
using LeakChecker.DataParser.Logging;
using LeakChecker.DataParser.Logging.Parse;
using LeakChecker.DataParser.Stats.Parse;
using MongoDB.Bson;

namespace LeakChecker.DataParser.Tests.Helpers.Logging.Parse;

public sealed class NullParseLogger : IParseLogger
{
    public ObjectId ParseId => ObjectId.Empty;
    public DateTime ParseStart => DateTime.MaxValue;
    public string SubjectFilePath => string.Empty;
    public string SubjectTmpFilePath => string.Empty;

    public void Log(string? message, LogLevel level = LogLevel.Info, LogContext? context = null) {}
    public void LogEncodingHeader() {}
    public void LogEncodingStats(List<EncodingSegment> segments, int take = 10) {}
    public void LogEncodingDetails(List<EncodingSegment> segments) {}
    public void LogEncodingConversion() {}
    public void LogContentHeader() {}
    public void LogDelimiterHeuristic(DelimiterHeuristicResult result, int take = 10) {}
    public void LogSchemaDetectionHeader() {}
    public void LogSample(string sample) {}
    public void LogSqlInsertHeader(SqlInsertHeader insertHeader) {}
    public void LogHeuristicData(SchemaHeuristic analyzer) {}
    public void LogDominantSchema(SchemaHeuristic analyzer, double threshold) {}
    public void LogFinalSchema(Dictionary<int, ItemType> schema) {} 
    public void LogParseStats(ParseStats stats) {}
    public void Dispose() { }
}