using LeakChecker.DataParser.Content;
using MongoDB.Bson;
using MongoDB.Driver;

namespace LeakChecker.DataParser.Database;

public static class DatabaseFacade
{
    
    private static readonly InsertManyOptions UnorderedOptions = new() { IsOrdered = false };
    
    public static async Task SaveOne(Dictionary<ItemEnum, List<string>> record, Guid parseId)
    {
        var document = CreateDocument(record, parseId);
        await Collection.InsertOneAsync(document);
    }

    public static async Task SaveMany(List<BsonDocument> records)
    {
        if (records.Count == 0)
            return;
        
        await Collection.InsertManyAsync(records, UnorderedOptions);
    }
    
    public static async Task SaveMany(List<Dictionary<ItemEnum, List<string>>> records, Guid parseId)
    {
        if (records.Count == 0)
            return;

        var documents = new List<BsonDocument>();
        
        foreach (var record in records)
        {
            documents.Add(CreateDocument(record, parseId));
        }
        
        await Collection.InsertManyAsync(documents, UnorderedOptions);
    }

    public static BsonDocument CreateDocument(Dictionary<ItemEnum, List<string>> record, Guid parseId )
    {
        BsonDocument document = new BsonDocument();
        document.Add("ParseId", new BsonBinaryData(parseId, GuidRepresentation.Standard));

        foreach (var item in record)
        {
            //TODO depends on enum, convert it to correct data type - specially DateTime
            document.Add(item.Key.ToString(), new BsonArray(item.Value));
        }

        return document;
    }
}