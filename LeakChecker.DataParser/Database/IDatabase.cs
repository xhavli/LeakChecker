using LeakChecker.Common.Enums;
using LeakChecker.DataParser.Stats.Execution;
using LeakChecker.DataParser.Stats.Parse;
using MongoDB.Bson;

namespace LeakChecker.DataParser.Database;

public interface IDatabase
{
    Task SaveUserOne(Dictionary<ItemEnum, List<string>> record, Guid parseId);
    Task SaveUserMany(List<Dictionary<ItemEnum, List<string>>> records, Guid parseId);
    Task SaveParseOne(ParseStats stats);
    Task SaveExecutionOne(ExecutionStats stats);
    Task UpsertDashboardStats(ParseStats stats);
    Task<BsonDocument?> GetDashboardStats();
}