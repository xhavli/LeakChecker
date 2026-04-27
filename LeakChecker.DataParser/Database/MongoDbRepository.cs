using LeakChecker.Common.Enums;
using LeakChecker.DataParser.Stats.Parse;
using MongoDB.Bson;
using MongoDB.Driver;

namespace LeakChecker.DataParser.Database;

public static class MongoDbRepository
{
    private static readonly MongoClient Client = new("mongodb://localhost:27017");
    private static readonly IMongoDatabase Database = Client.GetDatabase("LeakCheckerDb");
    private static readonly IMongoCollection<BsonDocument> StatsCollection = Database.GetCollection<BsonDocument>(nameof(CollectionType.Stats));
    private static readonly IMongoCollection<BsonDocument> UsersCollection = Database.GetCollection<BsonDocument>(nameof(CollectionType.Users));
    private static readonly IMongoCollection<BsonDocument> ParseCollection = Database.GetCollection<BsonDocument>(nameof(CollectionType.Parsings));
    private static readonly IMongoCollection<BsonDocument> ExecutionCollection = Database.GetCollection<BsonDocument>(nameof(CollectionType.Executions));
    private static readonly InsertManyOptions UnorderedOptions = new() { IsOrdered = false };
    
    private static readonly FilterDefinition<BsonDocument> DashboardFilter = Builders<BsonDocument>.Filter.Eq("_id", "dashboard");

    public static async Task SaveUserDocument(BsonDocument document)
    {
        await UsersCollection.InsertOneAsync(document);
    }
    
    public static async Task SaveUserDocuments(List<BsonDocument> documents)
    {
        if (documents.Count == 0)
            return;
        
        await UsersCollection.InsertManyAsync(documents, UnorderedOptions);
    }
    
    public static async Task SaveParsingDocument(BsonDocument document)
    {
        await ParseCollection.InsertOneAsync(document);
    }

    public static async Task SaveExecutionDocument(BsonDocument document)
    {
        await ExecutionCollection.InsertOneAsync(document);
    }
    
    public static async Task UpsertDashboardFromParse(ParseStats stats)
    {
        var update = Builders<BsonDocument>.Update
            .Inc("TotalUsers",    stats.RecordsRead - stats.MalformedRead)
            .Inc("TotalParses",   1L)
            .Inc("TotalBytes",    stats.BytesRead)
            .Inc("TotalRecords",  stats.RecordsRead)
            .Inc("TotalMalformed", stats.MalformedRead)
            .SetOnInsert("_id", "dashboard");

        await StatsCollection.UpdateOneAsync(DashboardFilter, update, new UpdateOptions { IsUpsert = true });
    }

    public static async Task<BsonDocument?> GetDashboardStats()
    {
        return await StatsCollection
            .Find(DashboardFilter)
            .FirstOrDefaultAsync();
    }
}