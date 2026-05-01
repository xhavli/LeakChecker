using LeakChecker.Common.Enums;
using LeakChecker.DataParser.Stats.Execution;
using LeakChecker.DataParser.Stats.Parse;
using MongoDB.Bson;

namespace LeakChecker.DataParser.Database;

public class MongoDbFacade : IDatabase
{
    public async Task SaveIdentityOne(Dictionary<ItemEnum, List<string>> record, ObjectId parseId)
    {
        IdentityDocumentFactory identityDocFactory = new IdentityDocumentFactory(parseId);
        var document = identityDocFactory.CreateIdentityDocument(record);
        await MongoDbRepository.SaveIdentityDocument(document);
    }
    
    public async Task SaveIdentityMany(List<Dictionary<ItemEnum, List<string>>> records, ObjectId parseId)
    {
        if (records.Count == 0)
            return;

        List<BsonDocument> documents = new();
        IdentityDocumentFactory identityDocFactory = new IdentityDocumentFactory(parseId);
        
        foreach (var record in records)
        {
            documents.Add(identityDocFactory.CreateIdentityDocument(record));
        }
        
        await MongoDbRepository.SaveIdentityDocuments(documents);
    }
    
    public async Task SaveParseOne(ParseStats stats)
    {
        await MongoDbRepository.SaveParsingDocument(stats.ToBsonDocument());
    }
    
    public async Task SaveExecutionOne(ExecutionStats stats)
    {
        await MongoDbRepository.SaveExecutionDocument(stats.ToBsonDocument());
    }
    
    public async Task UpsertDashboardStats(ParseStats stats)
    {
        await MongoDbRepository.UpsertDashboardFromParse(stats);
    }

    public async Task<BsonDocument?> GetDashboardStats()
    {
        return await MongoDbRepository.GetDashboardStats();
    }

    public async Task CreateIndexes()
    {
        await MongoDbRepository.CreateIndexes();
    }
}