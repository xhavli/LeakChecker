using LeakChecker.Common.Enums;
using LeakChecker.DataParser.Database;
using LeakChecker.UI.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace LeakChecker.UI.Services;

public class DashboardService(IDatabase database, IMongoDatabase db) : IDashboardService
{
    private readonly IMongoCollection<BsonDocument> _parses =
        db.GetCollection<BsonDocument>(nameof(CollectionType.Parsings));

    public async Task<DashboardStats> GetStatsAsync()
    {
        var doc = await database.GetDashboardStats();
        if (doc is null) return new DashboardStats();

        return new DashboardStats
        {
            TotalBytes    = doc["TotalBytes"].AsInt64,
            TotalParses   = doc["TotalParses"].AsInt64,
            TotalRecords  = doc["TotalRecords"].AsInt64,
            TotalMalformed = doc["TotalMalformed"].AsInt64,
        };
    }

    public async Task<List<ParseListModel>> GetRecentParsesAsync(int limit = 50)
    {
        var docs = await _parses
            .Find(FilterDefinition<BsonDocument>.Empty)
            .Sort(Builders<BsonDocument>.Sort.Descending("ParseEnd"))
            .Limit(limit)
            .ToListAsync();

        return docs.Select(MapList).ToList();
    }

    public async Task<ParseDetailModel?> GetParseByIdAsync(string mongoId)
    {
        var filter = Builders<BsonDocument>.Filter.Eq(
            "_id", ObjectId.Parse(mongoId));

        var doc = await _parses.Find(filter).FirstOrDefaultAsync();
        return doc is null ? null : MapDetail(doc);
    }

    private static ParseListModel MapList(BsonDocument p)
    {
        var records   = p.GetValue("RecordsRead",   0L).ToInt64();
        var malformed = p.GetValue("MalformedRead", 0L).ToInt64();
        var start     = ParseDate(p, "ParseStart");
        var end       = ParseDate(p, "ParseEnd");

        return new ParseListModel
        {
            ParseId     = p["_id"].AsObjectId,
            SourcePath  = p.GetValue("SourcePath", "?").AsString,
            RecordsRead = records,
            BytesRead   = p.GetValue("BytesRead", 0L).ToInt64(),
            Accuracy    = records <= 0 ? 0 : Math.Max(0, (double)(records - malformed) / records * 100),
            Duration    = end > start ? end - start : TimeSpan.Zero,
            ParseEnd    = end,
        };
    }

    private static ParseDetailModel MapDetail(BsonDocument p)
    {
        var start = ParseDate(p, "ParseStart");
        var end   = ParseDate(p, "ParseEnd");

        var schemas = new List<Dictionary<string, string>>();
        if (p.Contains("Schemas") && p["Schemas"] is BsonArray schemaArr)
            foreach (var s in schemaArr.OfType<BsonDocument>())
                schemas.Add(s.ToDictionary(e => e.Name, e => e.Value.ToString() ?? ""));

        var segments = new List<EncodingSegmentModel>();
        if (p.Contains("EncodingSegments") && p["EncodingSegments"] is BsonArray segArr)
            foreach (var s in segArr.OfType<BsonDocument>())
                segments.Add(new EncodingSegmentModel
                {
                    Start      = s.GetValue("Start",      0L).ToInt64(),
                    Length     = s.GetValue("Length",     0L).ToInt64(),
                    Encoding   = s.GetValue("Encoding", BsonNull.Value) != BsonNull.Value
                        ? s["Encoding"].AsString 
                        : null,
                });

        return new ParseDetailModel
        {
            ParseId      = p["_id"].AsObjectId,
            ExecutionId  = ReadGuid(p, "ExecutionId"),
            SourcePath   = p.GetValue("SourcePath",  "?").AsString,
            FileSize     = p.GetValue("FileSize",    0L).ToInt64(),
            BytesRead    = p.GetValue("BytesRead",   0L).ToInt64(),
            ByteSpeed    = p.GetValue("ByteSpeed",   0.0).ToDouble(),
            LinesRead    = p.GetValue("LinesRead",   0L).ToInt64(),
            LineSpeed    = p.GetValue("LineSpeed",   0.0).ToDouble(),
            RecordsRead  = p.GetValue("RecordsRead", 0L).ToInt64(),
            MalformedRead = p.GetValue("MalformedRead", 0L).ToInt64(),
            Accuracy     = p.GetValue("Accuracy",   0.0).ToDouble(),
            ParseStart   = start,
            ParseEnd     = end,
            Duration     = end > start ? end - start : TimeSpan.Zero,
            Encoding     = p.GetValue("Encoding", BsonNull.Value) != BsonNull.Value
                               ? p["Encoding"].AsString : null,
            EncodingSegments = segments,
            Formats      = p.Contains("Formats")    && p["Formats"]    is BsonArray fa
                               ? fa.Select(v => v.AsString).ToList() : [],
            Delimiters   = p.Contains("Delimiters") && p["Delimiters"] is BsonArray da
                               ? da.Select(v => v.AsString[0]).ToList() : [],
            Context      = p.Contains("Context")    && p["Context"]    is BsonArray ca
                               ? ca.Select(v => v.AsString).ToList() : [],
            Schemas      = schemas,
        };
    }

    private static DateTime ParseDate(BsonDocument doc, string field) =>
        doc.GetValue(field, BsonNull.Value) != BsonNull.Value
            ? doc[field].ToUniversalTime()
            : DateTime.MinValue;

    private static Guid ReadGuid(BsonDocument doc, string field)
    {
        if (!doc.Contains(field)) return Guid.Empty;
        var v = doc[field];
        return v.IsBsonBinaryData ? v.AsBsonBinaryData.ToGuid() : Guid.Empty;
    }
}