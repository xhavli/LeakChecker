using LeakChecker.DataParser.Content;
using LeakChecker.DataParser.Stats.Parse;
using MongoDB.Bson;

namespace LeakChecker.DataParser.Database;

public class MongoDbDatabaseFacade : IDatabase
{
    public async Task SaveUserOne(Dictionary<ItemEnum, List<string>> record, Guid parseId)
    {
        var document = UserDocumentFactory.CreateUserDocument(record, parseId);
        await MongoDbRepository.SaveUserDocument(document);
    }
    
    public async Task SaveUserMany(List<Dictionary<ItemEnum, List<string>>> records, Guid parseId)
    {
        if (records.Count == 0)
            return;

        List<BsonDocument> documents = new();
        
        foreach (var record in records)
        {
            documents.Add(UserDocumentFactory.CreateUserDocument(record, parseId));
        }
        
        await MongoDbRepository.SaveUserDocuments(documents);
    }
    
    public async Task SaveParseOne(ParseStats stats)
    {
        await MongoDbRepository.SaveParsingDocument(stats.ToBsonDocument());
    }

    public async Task SaveParseMany(List<ParseStats> stats)
    {
        if (stats.Count == 0)
            return;

        var documents = new List<BsonDocument>();
        
        foreach (var stat in stats)
        {
            documents.Add(stat.ToBsonDocument());
        }
        
        await MongoDbRepository.SaveParsingDocuments(documents);
    }
}