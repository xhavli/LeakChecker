using System.Text.RegularExpressions;
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
    
    public static async Task CreateUsersIndexes()
    {
        var indexModels = new List<CreateIndexModel<BsonDocument>>
        {
            // IDX_DomainReversedLowercase_Asc_Sparse
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending(nameof(ItemEnum.DomainReversedLowercase)),
                new CreateIndexOptions { Name = "IDX_DomainReversedLowercase_Asc_Sparse", Sparse = true }
            ),

            // IDX_EmailLowercase_Asc_Sparse
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending(nameof(ItemEnum.EmailLowercase)),
                new CreateIndexOptions { Name = "IDX_EmailLowercase_Asc_Sparse", Sparse = true }
            ),

            // IDX_PhoneNumber_Asc_Sparse
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending(nameof(ItemEnum.PhoneNumber)),
                new CreateIndexOptions { Name = "IDX_PhoneNumber_Asc_Sparse", Sparse = true }
            ),

            // IDX_UsernameLowercase_Asc_Sparse
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending(nameof(ItemEnum.UsernameLowercase)),
                new CreateIndexOptions { Name = "IDX_UsernameLowercase_Asc_Sparse", Sparse = true }
            ),

            // IDX_Timestamps_Asc_Sparse
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending(nameof(ItemEnum.Timestamp)),
                new CreateIndexOptions { Name = "IDX_Timestamps_Asc_Sparse", Sparse = true }
            ),
        };

        await UsersCollection.Indexes.CreateManyAsync(indexModels);
    }
    
    public static async Task<List<BsonDocument>> SearchUsers(
        string field,
        ConditionType condition,
        string value,
        ObjectId? afterId = null,
        int limit = 50)
    {
        FilterDefinition<BsonDocument> conditionFilter = condition switch
        {
            ConditionType.ExactMatch => Builders<BsonDocument>.Filter.AnyEq(field, value),
            ConditionType.StartsWith => Builders<BsonDocument>.Filter.AnyIn(field,
                [new BsonRegularExpression($"^{Regex.Escape(value)}")]),
            ConditionType.EndsWith   => Builders<BsonDocument>.Filter.AnyIn(field,
                [new BsonRegularExpression($"{Regex.Escape(value)}$")]),
            ConditionType.Contains   => Builders<BsonDocument>.Filter.AnyIn(field,
                [new BsonRegularExpression(Regex.Escape(value))]),
            _ => throw new ArgumentOutOfRangeException(nameof(condition))
        };

        // Cursor: only fetch docs after the last seen _id
        var cursorFilter = afterId.HasValue
            ? Builders<BsonDocument>.Filter.And(conditionFilter,
                Builders<BsonDocument>.Filter.Gt("_id", afterId.Value))
            : conditionFilter;

        return await UsersCollection
            .Find(cursorFilter)
            .Sort(Builders<BsonDocument>.Sort.Ascending("_id"))
            .Limit(limit)
            .ToListAsync();
    }
    
    public static async Task<long> CountUsers(string field, ConditionType condition, string value)
    {
        FilterDefinition<BsonDocument> filter = condition switch
        {
            ConditionType.ExactMatch => Builders<BsonDocument>.Filter.AnyEq(field, value),
            ConditionType.StartsWith => Builders<BsonDocument>.Filter.AnyIn(field,
                [new BsonRegularExpression($"^{Regex.Escape(value)}")]),
            ConditionType.EndsWith   => Builders<BsonDocument>.Filter.AnyIn(field,
                [new BsonRegularExpression($"{Regex.Escape(value)}$")]),
            ConditionType.Contains   => Builders<BsonDocument>.Filter.AnyIn(field,
                [new BsonRegularExpression(Regex.Escape(value))]),
            _ => throw new ArgumentOutOfRangeException(nameof(condition))
        };

        return await UsersCollection.CountDocumentsAsync(filter);
    }
}