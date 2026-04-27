using LeakChecker.Common.Enums;
using LeakChecker.DataParser.Stats.Execution;
using LeakChecker.DataParser.Stats.Parse;
using MongoDB.Bson;

namespace LeakChecker.DataParser.Database;

public class NullDatabase : IDatabase
{
    public Task SaveUserOne(Dictionary<ItemEnum, List<string>> record, Guid parseId) => Task.CompletedTask;
    public Task SaveUserMany(List<Dictionary<ItemEnum, List<string>>> records, Guid parseId) => Task.CompletedTask;
    public Task SaveParseOne(ParseStats stats) => Task.CompletedTask;
    public Task SaveExecutionOne(ExecutionStats stats) => Task.CompletedTask;
    public Task UpsertDashboardStats(ParseStats stats) => Task.CompletedTask;
    public Task<BsonDocument?> GetDashboardStats() => Task.FromResult<BsonDocument?>(null);
}
