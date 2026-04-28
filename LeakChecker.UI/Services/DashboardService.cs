using LeakChecker.DataParser.Database;
using LeakChecker.UI.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace LeakChecker.UI.Services;

public class DashboardService(IDatabase database, IMongoDatabase db) : IDashboardService
{
    private readonly IMongoCollection<BsonDocument> _parses =
        db.GetCollection<BsonDocument>("Parsings");

    public async Task<DashboardStats> GetStatsAsync()
    {
        var doc = await database.GetDashboardStats();

        if (doc is null)
            return new DashboardStats();

        return new DashboardStats
        {
            TotalBytes    = doc["TotalBytes"].AsInt64,
            TotalParses   = doc["TotalParses"].AsInt64,
            TotalRecords  = doc["TotalRecords"].AsInt64,
            TotalMalformed = doc["TotalMalformed"].AsInt64,
        };
    }

    public async Task<List<ParseRow>> GetRecentParsesAsync(int limit = 50)
    {
        var docs = await _parses
            .Find(FilterDefinition<BsonDocument>.Empty)
            .Sort(Builders<BsonDocument>.Sort.Descending("ParseEnd"))
            .Limit(limit)
            .ToListAsync();

        return docs.Select(Map).ToList();
    }

    private static ParseRow Map(BsonDocument p)
    {
        var records  = p.GetValue("RecordsRead",   0L).ToInt64();
        var malformed = p.GetValue("MalformedRead", 0L).ToInt64();
        var start    = ParseDate(p, "ParseStart");
        var end      = ParseDate(p, "ParseEnd");

        return new ParseRow
        {
            SourcePath  = p.GetValue("SourcePath", "?").AsString,
            RecordsRead = records,
            BytesRead   = p.GetValue("BytesRead", 0L).ToInt64(),
            Accuracy    = records <= 0 ? 0 : Math.Max(0, (double)(records - malformed) / records * 100),
            Duration    = end > start ? end - start : TimeSpan.Zero,
            ParseEnd    = end,
        };
    }

    private static DateTime ParseDate(BsonDocument doc, string field) =>
        doc.GetValue(field, BsonNull.Value) != BsonNull.Value
            ? doc[field].ToUniversalTime()
            : DateTime.MinValue;
}