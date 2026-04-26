using LeakChecker.DataParser.Content;
using LeakChecker.DataParser.Helpers.DataNormalization;
using LeakChecker.DataParser.Helpers.Extensions;
using MongoDB.Bson;

namespace LeakChecker.DataParser.Database;

public static class UserDocumentFactory
{
    public static BsonDocument CreateUserDocument(Dictionary<ItemEnum, List<string>> record, Guid parseId)
    {
        BsonArray hashes = new();
        BsonArray others = new();
        BsonDocument document = new BsonDocument { { "ParseId", new BsonBinaryData(parseId, GuidRepresentation.Standard) } };

        foreach (var property in record)
        {
            ItemEnum type = property.Key;
            var values = new List<BsonValue>();
            
            foreach (var item in property.Value)
            {
                NormalizedData normalized = DataNormalizer.NormalizeData(property.Key, item);
                type = normalized.Type;
                values.Add(BsonValue.Create(normalized.Value));
            }

            if (type == ItemEnum.Other)
            {
                others.Add(new BsonArray(values));
            }

            if (type >= ItemEnum.Hash)
            {
                hashes.Add(new BsonDocument
                {
                    { "Type", type.ToString() },
                    { "Values", new BsonArray(values) }
                });
                
                continue;
            }
            
            if (type == ItemEnum.Username)
            { 
                var usernameElement = new BsonArray();

                foreach (var username in property.Value)
                {
                    usernameElement.Add(new BsonDocument
                    {
                        { "Raw", username },
                        { "Lowercase", username.ToLowerInvariant() }
                    });
                }
                
                document.Add(nameof(ItemEnum.Email), usernameElement);
                continue;
            }

            if (type == ItemEnum.Email)
            {
                var emailElement = new BsonArray();

                foreach (var email in property.Value)
                {
                    var parts = email.Split('@');

                    // Invalid Email -> Other
                    if (parts.Length != 2)
                    {
                        others.Add(BsonValue.Create(email));
                        continue;
                    }

                    // Valid Email
                    var domain = parts[1].ToLowerInvariant();

                    emailElement.Add(new BsonDocument
                    {
                        { "Raw", email },
                        { "DomainReversed", domain.ReverseString() },
                        { "Lowercase", email.ToLowerInvariant() }
                    });
                }
                
                document.Add(nameof(ItemEnum.Email), emailElement);
                continue;
            }
            
            document.Add(type.ToString(), new BsonArray(values));
        }

        if (hashes.Count > 0)
            document.Add(nameof(ItemEnum.Hash), hashes);
        
        if (others.Count > 0)
            document.Add(nameof(ItemEnum.Other), others);

        return document;
    }
}