using System.Text;
using LeakChecker.Common.Enums;
using LeakChecker.DataParser.Encodings;
using LeakChecker.DataParser.Logging.Parse;
using MongoDB.Bson;

namespace LeakChecker.DataParser.Stats.Parse;

public class ParseStats(Guid executionId, IParseLogger parseLogger, string sourcePath) : IParseStats
{
    public ObjectId ParseId { get; init; } = parseLogger.ParseId;
    public Guid ExecutionId { get; init; } = executionId;
    public string ParsePath { get; set; } = string.Empty;
    public string SourcePath { get; init; } = sourcePath;
    public string FileName => Path.GetFileName(SourcePath);
    public long FileSize { get; init; } = new FileInfo(sourcePath).Length;
    public long LinesRead { get; set; }
    public long BytesRead { get; set; }
    public long RecordsRead { get; set; }
    public long MalformedRead { get; set; }
    public Encoding? Encoding { get; set; }
    public List<EncodingSegment> EncodingSegments { get; set; } = new();
    public List<char> Delimiters { get; init; } = new();
    public List<FormatEnum> Formats { get; init; } = new();
    public List<string> Context { get; init; } = new();
    public List<Dictionary<int, ItemEnum>> Schemas { get; init; } = new();
    public DateTime ParseStart { get; init; } = DateTime.Now;
    public DateTime ParseEnd { get; set; }
    
    public TimeSpan Duration => ParseEnd - ParseStart;
    public double ByteSpeed => Duration.TotalSeconds > 0 ? BytesRead / Duration.TotalSeconds : 0;
    public double LineSpeed => Duration.TotalSeconds > 0 ? LinesRead / Duration.TotalSeconds : 0;
    public double Accuracy => RecordsRead <= 0 ? 0 : Math.Max(0, (double)(RecordsRead - MalformedRead) / RecordsRead * 100);

    public BsonDocument ToBsonDocument()
    {
        return new BsonDocument
        {
            { "_id", ParseId },
            { nameof(ExecutionId), new BsonBinaryData(ExecutionId, GuidRepresentation.Standard) },
            { nameof(SourcePath), SourcePath },
            { nameof(FileName), FileName },
            { nameof(FileSize), FileSize },
            { nameof(BytesRead), BytesRead },
            { nameof(ByteSpeed), ByteSpeed },
            { nameof(LinesRead), LinesRead },
            { nameof(LineSpeed), LineSpeed },
            { nameof(RecordsRead), RecordsRead },
            { nameof(MalformedRead), MalformedRead },
            { nameof(Accuracy), Accuracy },
            { nameof(Encoding), Encoding?.WebName is null ? BsonNull.Value : Encoding.WebName },
            { nameof(EncodingSegments), new BsonArray(EncodingSegments.Select(segment => new BsonDocument
                {
                    { "Start", segment.StartOffset },
                    { "Length", segment.Length },
                    { "Encoding", segment.Encoding?.WebName is null ? BsonNull.Value : segment.Encoding.WebName }
                }))
            },
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
    }
}

