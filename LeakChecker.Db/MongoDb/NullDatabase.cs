namespace LeakChecker.Db.MongoDb;

public class NullDatabase : IDatabase
{
    public Task SaveIdentityMany(List<Dictionary<ItemType, List<string>>> records, ObjectId parseId) => Task.CompletedTask;
    public Task SaveParseOne(ParseStats stats) => Task.CompletedTask;
    public Task SaveExecutionOne(ExecutionStats stats) => Task.CompletedTask;
    public Task UpsertDashboardStats(ParseStats stats) => Task.CompletedTask;
    public Task CreateIndexes() => Task.CompletedTask;
    public Task<BsonDocument?> GetDashboardStats() => Task.FromResult<BsonDocument?>(null);
}
