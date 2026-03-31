using LeakChecker.DataParser.Content;
using LeakChecker.DataParser.Content.Detection.ItemParsing;
using LeakChecker.DataParser.Helpers.DataNormalization;
using LeakChecker.DataParser.Stats.Parse;
using MongoDB.Bson;
using MongoDB.Driver;

namespace LeakChecker.DataParser.Database;

public static class DatabaseFacade
{
    
    private static readonly InsertManyOptions UnorderedOptions = new() { IsOrdered = false };
    
    public static async Task SaveUserOne(Dictionary<ItemEnum, List<string>> record, Guid parseId)
    {
        var document = CreateUserDocument(record, parseId);
        await UserCollection.InsertOneAsync(document);
    }
    
    public static async Task SaveUserMany(List<Dictionary<ItemEnum, List<string>>> records, Guid parseId)
    {
        if (records.Count == 0)
            return;

        var documents = new List<BsonDocument>();
        
        foreach (var record in records)
        {
            documents.Add(CreateUserDocument(record, parseId));
        }
        
        await UserCollection.InsertManyAsync(documents, UnorderedOptions);
    }

    private static BsonDocument CreateUserDocument(Dictionary<ItemEnum, List<string>> record, Guid parseId)
    {
        var document = new BsonDocument
        {
            { "ParseId", new BsonBinaryData(parseId, GuidRepresentation.Standard) }
        };

        foreach (var property in record)
        {
            ItemEnum type = property.Key;
            var values = new List<BsonValue>();
            
            foreach (var item in property.Value)
            {
                //Normalize only timestamp formats, others keep untouched
                NormalizedData normalized = DataNormalizer.NormalizeData(property.Key, item);

                type = normalized.Type;
                values.Add(BsonValue.Create(normalized.Value));
            }

            document.Add(type.ToString(), new BsonArray(values));
        }

        return document;
    }
    
    public static async Task SaveParseOne(ParseStats stats)
    {
        await ParseCollection.InsertOneAsync(stats.ToBsonDocument());
    }

    public static async Task SaveParseMany(List<ParseStats> stats)
    {
        if (stats.Count == 0)
            return;

        var documents = new List<BsonDocument>();
        
        foreach (var stat in stats)
        {
            documents.Add(stat.ToBsonDocument());
        }
        
        await ParseCollection.InsertManyAsync(documents, UnorderedOptions);
    }
}