using LeakChecker.Common.Enums;
using LeakChecker.DataParser.Stats.Execution;
using LeakChecker.DataParser.Stats.Parse;
using MongoDB.Bson;

namespace LeakChecker.DataParser.Database;

public interface IDatabase
{
    Task SaveIdentityOne(Dictionary<ItemEnum, List<string>> record, ObjectId parseId);
    Task SaveIdentityMany(List<Dictionary<ItemEnum, List<string>>> records, ObjectId parseId);
    Task SaveParseOne(ParseStats stats);
    Task SaveExecutionOne(ExecutionStats stats);
    Task UpsertDashboardStats(ParseStats stats);
    Task CreateIndexes();
    Task<BsonDocument?> GetDashboardStats();
}