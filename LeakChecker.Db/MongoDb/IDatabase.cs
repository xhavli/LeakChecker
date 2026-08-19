namespace LeakChecker.Db.MongoDb;

public interface IDatabase
{
    Task SaveIdentityMany(List<Dictionary<ItemType, List<string>>> records, ObjectId parseId);
    Task SaveParseOne(ParseStats stats);
    Task SaveExecutionOne(ExecutionStats stats);
    Task UpsertDashboardStats(ParseStats stats);
    Task CreateIndexes();
    Task<BsonDocument?> GetDashboardStats();
}