using System.Text;
using LeakChecker.DataParser.Helpers.Enums;
using MongoDB.Bson;

namespace LeakChecker.DataParser.Database.Helpers;

public static class DocumentSanitizer
{
    private const int MaxDocumentSize = 16 * SizeEnum.MegaByte;
    private static readonly Encoding Utf8 = Encoding.GetEncoding(Encoding.UTF8.WebName, 
        encoderFallback: new EncoderReplacementFallback(string.Empty),  // Drop unencodable chars
        decoderFallback: new DecoderReplacementFallback(string.Empty)); // Drop invalid bytes
    
    public static List<BsonDocument> SanitizeDocumentsEncoding(List<BsonDocument> documents)
    {
        var safe = new List<BsonDocument>(documents.Count);
        for (int i = 0; i < documents.Count; i++)
            safe.Add(SanitizeDocumentEncoding(documents[i]));
        return safe;
    }

    private static BsonDocument SanitizeDocumentEncoding(BsonDocument doc)
    {
        var sanitized = new BsonDocument();
        foreach (var element in doc)
            sanitized.Add(element.Name, SanitizeValueEncoding(element.Value));
        return sanitized;
    }
    
    private static BsonValue SanitizeValueEncoding(BsonValue value)
    {
        if (value is BsonString s)
            return new BsonString(Utf8.GetString(Utf8.GetBytes(s.Value)));

        if (value is BsonArray arr)
        {
            var sanitized = new BsonArray(arr.Count);
            for (int i = 0; i < arr.Count; i++)
                sanitized.Add(SanitizeValueEncoding(arr[i]));
            return sanitized;
        }

        if (value is BsonDocument nested)
            return SanitizeDocumentEncoding(nested);

        return value;
    }
    
    public static List<BsonDocument> FilterOversizedDocuments(List<BsonDocument> documents)
    {
        var safe = new List<BsonDocument>(documents.Count);
        for (int i = 0; i < documents.Count; i++)
        {
            if (documents[i].ToBson().Length < MaxDocumentSize)
                safe.Add(documents[i]);
        }
        return safe;
    }
}