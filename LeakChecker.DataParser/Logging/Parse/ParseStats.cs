using System.Text;
using LeakChecker.DataParser.Content;
using LeakChecker.DataParser.Encodings;
using LeakChecker.DataParser.Format;
using MongoDB.Bson;

namespace LeakChecker.DataParser.Logging.Parse;

public class ParseStats
{
    public Guid ParseId { get; init; }
    public Guid ExecutionId { get; init; }
    public string? FileName { get; init; }
    public string? FilePath { get; init; }
    public long FileSize { get; init; }
    public long MalformedRecordsRead { get; set; }
    public long LinesRead { get; set; }
    public long BytesRead { get; set; }
    public long RecordsRead { get; set; }
    public Encoding? Encoding { get; set; }
    public List<EncodingSegment> EncodingSegments { get; set; } = new();
    public List<char> Delimiters { get; set; } = new();
    public List<FormatEnum> Formats { get; set; } = new();
    public List<string> Context { get; set; } = new();
    public List<Dictionary<int, ItemEnum>> Schemas { get; set; } = new();
    public DateTime ParseStart { get; init; } = DateTime.Now;
    public DateTime ParseEnd { get; set; }
    
    public TimeSpan Duration => ParseEnd - ParseStart;
    public double ByteSpeed => Duration.TotalSeconds > 0 ? BytesRead / Duration.TotalSeconds : 0;
    public double LineSpeed => Duration.TotalSeconds > 0 ? LinesRead / Duration.TotalSeconds : 0;
    public double Accuracy =>
        RecordsRead <= 0 ? 0 : Math.Max(0, (double)(RecordsRead - MalformedRecordsRead) / RecordsRead * 100);
    
    public static ParseStats Create(Guid executionId, IParseLogger parseLogger, string filePath) 
    { 
        return new ParseStats 
        { 
            ParseId = parseLogger.ParseId, 
            ExecutionId = executionId, 
            ParseStart = parseLogger.ParseStart, 
            FileName = Path.GetFileName(filePath), 
            FilePath = filePath, 
            FileSize = new FileInfo(filePath).Length 
        }; 
    }

    public BsonDocument ToBsonDocument()
    {
        var document = new BsonDocument
        {
            { nameof(ParseId), new BsonBinaryData(ParseId, GuidRepresentation.Standard) },
            { nameof(ExecutionId), new BsonBinaryData(ExecutionId, GuidRepresentation.Standard) },
            { nameof(FileName), FileName is null ? BsonNull.Value : FileName },
            { nameof(FilePath), FilePath is null ? BsonNull.Value : FilePath },
            { nameof(FileSize), FileSize },
            { nameof(MalformedRecordsRead), MalformedRecordsRead },
            { nameof(LinesRead), LinesRead },
            { nameof(BytesRead), BytesRead },
            { nameof(RecordsRead), RecordsRead },
            { nameof(Encoding), Encoding?.WebName is null ? BsonNull.Value : Encoding.WebName },
            // { nameof(EncodingSegments), new BsonArray(EncodingSegments.Select(segment => new BsonDocument
            //     {
            //         { "Start", segment.StartOffset },
            //         { "Length", segment.Length },
            //         { "Encoding", segment.Encoding?.WebName is null ? BsonNull.Value : segment.Encoding.WebName }
            //     }))
            // },
            { nameof(Delimiters), new BsonArray(Delimiters.Select(d => d.ToString())) },
            { nameof(Formats), new BsonArray(Formats.Select(f => f.ToString())) },
            { nameof(Context), new BsonArray(Context) },
            { nameof(Schemas), new BsonArray(Schemas.Select(schema =>
                new BsonDocument(schema.Select(kvp =>
                    new BsonElement(kvp.Key.ToString(), kvp.Value.ToString())))))
            },
            { nameof(ParseStart), ParseStart.ToUniversalTime() },
            { nameof(ParseEnd), ParseEnd.ToUniversalTime() },
        };

        return document;
    }
}

