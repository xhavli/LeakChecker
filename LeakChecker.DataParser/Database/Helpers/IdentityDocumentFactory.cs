using LeakChecker.Common.Enums;
using LeakChecker.DataParser.Content.Detection.ItemRecognition;
using LeakChecker.DataParser.Helpers.DataNormalization;
using LeakChecker.DataParser.Helpers.Extensions;
using MongoDB.Bson;

namespace LeakChecker.DataParser.Database.Helpers;

public class IdentityDocumentFactory(ObjectId parseId)
{
    private static readonly Dictionary<ItemEnum, string> EnumNames =
        Enum.GetValues<ItemEnum>().ToDictionary(e => e, e => e.ToString());

    public BsonDocument CreateIdentityDocument(Dictionary<ItemEnum, List<string>> record)
    {
        BsonArray hashes = new();
        BsonArray others = new();
        BsonArray domains = new();
        HashSet<string>? domainsSeen = null;
        BsonDocument document = new BsonDocument { { "ParseId", parseId } };

        foreach (var property in record)
        {
            List<string> rawValues = property.Value;
            int count = rawValues.Count;

            ItemEnum type = property.Key;
            var values = new BsonArray(count);

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
                    { "Type", EnumNames[type] },
                    { "Values", values }
                });
                continue;
            }
            
            if (type == ItemEnum.Name)
            {
                var lowercased = new BsonArray(count);
                for (int i = 0; i < count; i++)
                    lowercased.Add(new BsonString(rawValues[i].ToLowerInvariant()));

                AddOrMerge(document, nameof(ItemEnum.Name), values);
                AddOrMerge(document, nameof(ItemEnum.NameLowercase), lowercased);
                continue;
            }

            if (type == ItemEnum.Username)
            {
                var lowercased = new BsonArray(count);
                for (int i = 0; i < count; i++)
                    lowercased.Add(new BsonString(rawValues[i].ToLowerInvariant()));

                AddOrMerge(document, nameof(ItemEnum.Username), values);
                AddOrMerge(document, nameof(ItemEnum.UsernameLowercase), lowercased);
                continue;
            }

            if (type == ItemEnum.Email)
            {
                var emailLowercase = new BsonArray(count);
                for (int i = 0; i < count; i++)
                {
                    string email = rawValues[i];
                    string emailLower = email.ToLowerInvariant();
                    emailLowercase.Add(new BsonString(emailLower));

                    int atIndex = emailLower.IndexOf('@');

                    // Invalid email -> Other
                    if (atIndex < 0 || atIndex != emailLower.LastIndexOf('@'))
                    {
                        others.Add(new BsonString(email));
                        continue;
                    }

                    // Valid email — extract and reverse domain from already-lowercased string
                    string domainReversed = emailLower.AsSpan(atIndex + 1).ToString().ReverseString();

                    // Fast path: single email (count == 1), skip HashSet entirely
                    if (count == 1)
                    {
                        domains.Add(new BsonString(domainReversed));
                    }
                    else
                    {
                        domainsSeen ??= new HashSet<string>(StringComparer.Ordinal);
                        if (domainsSeen.Add(domainReversed))
                            domains.Add(new BsonString(domainReversed));
                    }
                }

                AddOrMerge(document, nameof(ItemEnum.Email), values);
                AddOrMerge(document, nameof(ItemEnum.EmailLowercase), emailLowercase);
                continue;
            }
            
            if (type == ItemEnum.Web)
            {
                for (int i = 0; i < count; i++)
                {
                    string? domainReversed = WebRecognizer.ExtractReversedDomain(rawValues[i]);
                    if (domainReversed == null) continue;

                    if (count == 1)
                    {
                        domains.Add(new BsonString(domainReversed));
                    }
                    else
                    {
                        domainsSeen ??= new HashSet<string>(StringComparer.Ordinal);
                        if (domainsSeen.Add(domainReversed))
                            domains.Add(new BsonString(domainReversed));
                    }
                }

                AddOrMerge(document, nameof(ItemEnum.Web), values);
                continue;
            }

            AddOrMerge(document, EnumNames[type], values);
        }

        if (hashes.Count > 0)
            document.Add(nameof(ItemEnum.Hash), hashes);

        if (others.Count > 0)
            document.Add(nameof(ItemEnum.Other), others);

        if (domains.Count > 0)
            document.Add(nameof(ItemEnum.DomainReversedLowercase), domains);

        return document;
    }

    private static void AddOrMerge(BsonDocument document, string key, BsonArray values)
    {
        if (values.Count == 0)
            return;

        if (!document.TryGetValue(key, out var existing))
        {
            document.Add(key, values);
            return;
        }

        var arr = (BsonArray)existing;
        foreach (var v in values)
            arr.Add(v);
    }
}