using LeakChecker.Common.Enums;
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
        BsonArray domains = new();
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
                continue;
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
                document.Add(nameof(ItemEnum.Username), new BsonArray(values));
                
                for (int i = 0; i < property.Value.Count; i++)
                {
                    property.Value[i] = property.Value[i].ToLowerInvariant();
                }
                
                document.Add(nameof(ItemEnum.Email), usernameElement);
                document.Add(nameof(ItemEnum.UsernameLowercase), new BsonArray(property.Value));
                continue;
            }

            if (type == ItemEnum.Email)
            {
                // BsonArray emailArray = new();

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
                    string domainReversed = parts[1].ToLowerInvariant().ReverseString();
                    
                    if (!domains.Contains(domainReversed))
                        domains.Add(BsonValue.Create(domainReversed));

                    // BsonDocument emailDocument = new BsonDocument
                    // {
                    //     { "Raw", email },
                    //     { "DomainReversed", domain.ReverseString() },
                    //     { "Lowercase", email.ToLowerInvariant() }
                    // };
                    //
                    // emailArray.Add(emailDocument);
                }

                string[] emailLowercase = new string[property.Value.Count];
                for (int i = 0; i < property.Value.Count; i++)
                {
                    emailLowercase[i] = property.Value[i].ToLowerInvariant();
                }
                
                document.Add(nameof(ItemEnum.Email), new BsonArray(values));
                document.Add(nameof(ItemEnum.EmailLowercase), new BsonArray(emailLowercase));
                
                // document.Add(nameof(ItemEnum.Email), emailElement);
                continue;
            }
            
            document.Add(type.ToString(), new BsonArray(values));
        }

        if (hashes.Count > 0)
            document.Add(nameof(ItemEnum.Hash), hashes);
        
        if (others.Count > 0)
            document.Add(nameof(ItemEnum.Other), others);

        if (domains.Count > 0)
            document.Add(nameof(ItemEnum.DomainReversedLowercase), domains);

        return document;
    }
}