using LeakChecker.Common.Enums;
using MongoDB.Bson;
using MongoDB.Driver;

namespace LeakChecker.DataParser.Database;

public static class MongoDbRepository
{
    private static readonly MongoClient Client = new("mongodb://localhost:27017");
    private static readonly IMongoDatabase Database = Client.GetDatabase("LeakCheckerDb");
    private static readonly IMongoCollection<BsonDocument> UserCollection = Database.GetCollection<BsonDocument>(nameof(CollectionType.Users));
    private static readonly IMongoCollection<BsonDocument> ParseCollection = Database.GetCollection<BsonDocument>(nameof(CollectionType.Parses));
    private static readonly IMongoCollection<BsonDocument> ExecutionCollection = Database.GetCollection<BsonDocument>(nameof(CollectionType.Executions));
    private static readonly InsertManyOptions UnorderedOptions = new() { IsOrdered = false };

    public static async Task SaveUserDocument(BsonDocument document)
    {
        await UserCollection.InsertOneAsync(document);
    }
    
    public static async Task SaveUserDocuments(List<BsonDocument> documents)
    {
        if (documents.Count == 0)
            return;
        
        await UserCollection.InsertManyAsync(documents, UnorderedOptions);
    }
    
    public static async Task SaveParsingDocument(BsonDocument document)
    {
        await ParseCollection.InsertOneAsync(document);
    }
    
    public static async Task SaveParsingDocuments(List<BsonDocument> documents)
    {
        if (documents.Count == 0)
            return;
        
        await ParseCollection.InsertManyAsync(documents, UnorderedOptions);
    }
}