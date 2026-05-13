using System.Text;
using System.Text.RegularExpressions;
using LeakChecker.Common.Enums;
using LeakChecker.DataParser.Database.Helpers;
using LeakChecker.DataParser.Stats.Parse;
using MongoDB.Bson;
using MongoDB.Driver;

namespace LeakChecker.DataParser.Database.Repository;

public static class MongoDbRepository
{
    private const string DbName = "LeakCheckerDb";
    private const string DashboardId = "dashboard";

    private static readonly MongoClient Client = new("mongodb://localhost:27017");
    private static readonly IMongoDatabase Database = Client.GetDatabase(DbName);
    private static readonly IMongoCollection<BsonDocument> ParseCollection = Database.GetCollection<BsonDocument>(nameof(CollectionType.Parsings));
    private static readonly IMongoCollection<BsonDocument> StatsCollection = Database.GetCollection<BsonDocument>(nameof(CollectionType.Dashboard));
    private static readonly IMongoCollection<BsonDocument> ExecutionCollection = Database.GetCollection<BsonDocument>(nameof(CollectionType.Executions));
    private static readonly IMongoCollection<BsonDocument> IdentitiesCollection = Database.GetCollection<BsonDocument>(nameof(CollectionType.Identities));
    private static readonly InsertManyOptions UnorderedOptions = new() { IsOrdered = false };
    
    private static readonly FilterDefinition<BsonDocument> DashboardFilter = Builders<BsonDocument>.Filter.Eq("_id", DashboardId);

    public static async Task SaveIdentityDocuments(List<BsonDocument> documents)
    {
        if (documents.Count == 0)
            return;

        try
        {
            await IdentitiesCollection.InsertManyAsync(documents, UnorderedOptions);
        }
        catch (EncoderFallbackException)
        {
            var safe = DocumentSanitizer.SanitizeDocumentsEncoding(documents);
            await IdentitiesCollection.InsertManyAsync(safe, UnorderedOptions);
        }
        catch (FormatException ex) when (ex.Message.Contains("larger than MaxDocumentSize"))
        {
            var safe = DocumentSanitizer.FilterOversizedDocuments(documents);
            if (safe.Count > 0)
                await IdentitiesCollection.InsertManyAsync(safe, UnorderedOptions);
        }
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
            .SetOnInsert("_id", DashboardId);

        await StatsCollection.UpdateOneAsync(DashboardFilter, update, new UpdateOptions { IsUpsert = true });
    }

    public static async Task<BsonDocument?> GetDashboardStats()
    {
        return await StatsCollection
            .Find(DashboardFilter)
            .FirstOrDefaultAsync();
    }
    
    public static async Task CreateIdentityIndexes()
    {
        var indexModels = new List<CreateIndexModel<BsonDocument>>
        {
            new(Builders<BsonDocument>.IndexKeys.Ascending(nameof(ItemEnum.NameLowercase)),
                new CreateIndexOptions { Name = $"IDX_{nameof(ItemEnum.NameLowercase)}_Asc_Sparse", Sparse = true }),
            
            new(Builders<BsonDocument>.IndexKeys.Ascending(nameof(ItemEnum.UsernameLowercase)),
                new CreateIndexOptions { Name = $"IDX_{nameof(ItemEnum.UsernameLowercase)}_Asc_Sparse", Sparse = true }),
            
            new(Builders<BsonDocument>.IndexKeys.Ascending(nameof(ItemEnum.EmailLowercase)),
                new CreateIndexOptions { Name = $"IDX_{nameof(ItemEnum.EmailLowercase)}_Asc_Sparse", Sparse = true }),
            
            new(Builders<BsonDocument>.IndexKeys.Ascending(nameof(ItemEnum.DomainReversedLowercase)),
                new CreateIndexOptions { Name = $"IDX_{ItemEnum.DomainReversedLowercase}_Asc_Sparse", Sparse = true }),
            
            //TODO organization
            new(Builders<BsonDocument>.IndexKeys.Ascending(nameof(ItemEnum.Organization)),
                new CreateIndexOptions { Name = $"IDX_{nameof(ItemEnum.Organization)}_Asc_Sparse", Sparse = true }),
            
            //TODO location
            new(Builders<BsonDocument>.IndexKeys.Ascending(nameof(ItemEnum.Location)),
                new CreateIndexOptions { Name = $"IDX_{nameof(ItemEnum.Location)}_Asc_Sparse", Sparse = true }),
            
            //TODO passswd
            new(Builders<BsonDocument>.IndexKeys.Ascending(nameof(ItemEnum.Password)),
                new CreateIndexOptions { Name = $"IDX_{ItemEnum.Password}_Asc_Sparse", Sparse = true }),
            
            new(Builders<BsonDocument>.IndexKeys.Ascending(nameof(ItemEnum.Gender)),
                new CreateIndexOptions { Name = $"IDX_{nameof(ItemEnum.Gender)}_Asc_Sparse", Sparse = true }),
            
            new(Builders<BsonDocument>.IndexKeys.Ascending(nameof(ItemEnum.Iban)),
                new CreateIndexOptions { Name = $"IDX_{nameof(ItemEnum.Iban)}_Asc_Sparse", Sparse = true }),
            
            new(Builders<BsonDocument>.IndexKeys.Ascending(nameof(ItemEnum.Id)),
                new CreateIndexOptions { Name = $"IDX_{nameof(ItemEnum.Id)}_Asc_Sparse", Sparse = true }),
            
            new(Builders<BsonDocument>.IndexKeys.Ascending(nameof(ItemEnum.Ipv4)),
                new CreateIndexOptions { Name = $"IDX_{nameof(ItemEnum.Ipv4)}_Asc_Sparse", Sparse = true }),
            
            new(Builders<BsonDocument>.IndexKeys.Ascending(nameof(ItemEnum.Ipv6)),
                new CreateIndexOptions { Name = $"IDX_{nameof(ItemEnum.Ipv6)}_Asc_Sparse", Sparse = true }),
            
            new(Builders<BsonDocument>.IndexKeys.Ascending(nameof(ItemEnum.MacAddress)),
                new CreateIndexOptions { Name = $"IDX_{nameof(ItemEnum.MacAddress)}_Asc_Sparse", Sparse = true }),
            
            new(Builders<BsonDocument>.IndexKeys.Ascending(nameof(ItemEnum.MaritalStatus)),
                new CreateIndexOptions { Name = $"IDX_{nameof(ItemEnum.MaritalStatus)}_Asc_Sparse", Sparse = true }),
            
            new(Builders<BsonDocument>.IndexKeys.Ascending(nameof(ItemEnum.PhoneNumber)),
                new CreateIndexOptions { Name = $"IDX_{nameof(ItemEnum.PhoneNumber)}_Asc_Sparse", Sparse = true }),
            
            new(Builders<BsonDocument>.IndexKeys.Ascending(nameof(ItemEnum.Timestamp)),
                new CreateIndexOptions { Name = $"IDX_{nameof(ItemEnum.Timestamp)}_Asc_Sparse", Sparse = true }),
            
            new(Builders<BsonDocument>.IndexKeys.Ascending(nameof(ItemEnum.Other)),
                new CreateIndexOptions { Name = $"IDX_{nameof(ItemEnum.Other)}_Asc_Sparse", Sparse = true }),
        };

        await IdentitiesCollection.Indexes.CreateManyAsync(indexModels);
    }

    public static async Task CreateParseIndexes()
    {
         var indexModels = new List<CreateIndexModel<BsonDocument>>
        {
            new(Builders<BsonDocument>.IndexKeys.Descending("ParseEnded"), 
                new CreateIndexOptions { Name = "IDX_ParseEnded_Desc"}),
        };

        await ParseCollection.Indexes.CreateManyAsync(indexModels);
    }
    
    public static async Task<List<BsonDocument>> SearchIdentity(
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

        return await IdentitiesCollection
            .Find(cursorFilter)
            .Sort(Builders<BsonDocument>.Sort.Ascending("_id"))
            .Limit(limit)
            .ToListAsync();
    }
    
    public static async Task<long> CountIdentities(string field, ConditionType condition, string value)
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

        return await IdentitiesCollection.CountDocumentsAsync(filter);
    }
    
    public static async Task<List<BsonDocument>> SearchIdentityByDateTime(
        string field,
        DateTime utcValue,
        ObjectId? afterId = null,
        int limit = 50)
    {
        var matchFilter = Builders<BsonDocument>.Filter.AnyEq(field, utcValue);

        var cursorFilter = afterId.HasValue
            ? Builders<BsonDocument>.Filter.And(matchFilter,
                Builders<BsonDocument>.Filter.Gt("_id", afterId.Value))
            : matchFilter;

        return await IdentitiesCollection
            .Find(cursorFilter)
            .Sort(Builders<BsonDocument>.Sort.Ascending("_id"))
            .Limit(limit)
            .ToListAsync();
    }

    public static async Task<long> CountIdentitiesByDateTime(string field, DateTime utcValue)
    {
        var filter = Builders<BsonDocument>.Filter.AnyEq(field, utcValue);
        return await IdentitiesCollection.CountDocumentsAsync(filter);
    }

    public static async Task<List<BsonDocument>> SearchIdentityByDateRange(
        string field,
        DateTime utcFrom,
        DateTime utcTo,
        ObjectId? afterId = null,
        int limit = 50)
    {
        var rangeFilter = Builders<BsonDocument>.Filter.ElemMatch(
            field,
            new BsonDocumentFilterDefinition<BsonValue>(new BsonDocument
            {
                { "$gte", utcFrom },
                { "$lt", utcTo }
            })
        );

        var cursorFilter = afterId.HasValue
            ? Builders<BsonDocument>.Filter.And(rangeFilter, 
                Builders<BsonDocument>.Filter.Gt("_id", afterId.Value))
            : rangeFilter;

        return await IdentitiesCollection
            .Find(cursorFilter)
            .Sort(Builders<BsonDocument>.Sort.Ascending("_id"))
            .Limit(limit)
            .ToListAsync();
    }

    public static async Task<long> CountIdentitiesByDateRange(string field, DateTime utcFrom, DateTime utcTo)
    {
        var filter = Builders<BsonDocument>.Filter.ElemMatch(
            field,
            new BsonDocumentFilterDefinition<BsonValue>(new BsonDocument
            {
                { "$gte", utcFrom },
                { "$lt", utcTo }
            })
        );
        
        return await IdentitiesCollection.CountDocumentsAsync(filter);
    }
}