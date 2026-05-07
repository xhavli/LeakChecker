using LeakChecker.Common.Enums;
using LeakChecker.DataParser.Helpers.DataNormalization;
using LeakChecker.DataParser.Helpers.Extensions;
using MongoDB.Bson;

namespace LeakChecker.DataParser.Database.Helpers;

public class IdentityDocumentFactory(ObjectId parseId)
{
    public BsonDocument CreateIdentityDocument(Dictionary<ItemEnum, List<string>> record)
    {
        BsonArray hashes = new();
        BsonArray others = new();
        BsonArray domains = new();
        HashSet<string> domainsSeen = new(StringComparer.Ordinal);
        BsonDocument document = new BsonDocument { { "ParseId", parseId } };

        foreach (var property in record)
        {
            List<string> rawValues = property.Value;
            int count = rawValues.Count;

            ItemEnum type = property.Key;
            var values = new List<BsonValue>(count);

            for (int i = 0; i < count; i++)
            {
                NormalizedData normalized = DataNormalizer.NormalizeData(property.Key, rawValues[i]);
                type = normalized.Type;
                values.Add(BsonValue.Create(normalized.Value));
            }

            if (type == ItemEnum.Other)
            {
                foreach (var v in values)
                    others.Add(v);
                
                continue;
            }

            if (type > ItemEnum.Other)
            {
                hashes.Add(new BsonDocument
                {
                    { "Type", type.ToString() },
                    { "Values", new BsonArray(values) }
                });
                continue;
            }
            
            if (type == ItemEnum.Name)
            {
                var lowercased = new BsonValue[count];
                for (int i = 0; i < count; i++)
                    lowercased[i] = BsonValue.Create(rawValues[i].ToLowerInvariant());

                document.Add(nameof(ItemEnum.Name), new BsonArray(values));
                document.Add(nameof(ItemEnum.NameLowercase), new BsonArray(lowercased));
                continue;
            }

            if (type == ItemEnum.Username)
            {
                var lowercased = new BsonValue[count];
                for (int i = 0; i < count; i++)
                    lowercased[i] = BsonValue.Create(rawValues[i].ToLowerInvariant());

                document.Add(nameof(ItemEnum.Username), new BsonArray(values));
                document.Add(nameof(ItemEnum.UsernameLowercase), new BsonArray(lowercased));
                continue;
            }

            if (type == ItemEnum.Email)
            {
                var emailLowercase = new BsonValue[count];
                for (int i = 0; i < count; i++)
                {
                    string email = rawValues[i];
                    string emailLower = email.ToLowerInvariant();
                    emailLowercase[i] = BsonValue.Create(emailLower);

                    int atIndex = emailLower.IndexOf('@');

                    // Invalid email -> Other
                    if (atIndex < 0 || atIndex != emailLower.LastIndexOf('@'))
                    {
                        others.Add(BsonValue.Create(email));
                        continue;
                    }

                    // Valid email — extract and reverse domain from already-lowercased string
                    string domainReversed = emailLower.AsSpan(atIndex + 1).ToString().ReverseString();
                    if (domainsSeen.Add(domainReversed))
                        domains.Add(BsonValue.Create(domainReversed));
                }

                document.Add(nameof(ItemEnum.Email), new BsonArray(values));
                document.Add(nameof(ItemEnum.EmailLowercase), new BsonArray(emailLowercase));
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