using MongoDB.Bson;

namespace LeakChecker.UI.Models;

public class ParseDetailModel
{
    public ObjectId ParseId           { get; init; }
    public Guid     ExecutionId       { get; init; }
    public string   SourcePath        { get; init; } = string.Empty;
    public string   FileName          => Path.GetFileName(SourcePath);
    public long     FileSize          { get; init; }
    public long     BytesRead         { get; init; }
    public double   ByteSpeed         { get; init; }
    public long     LinesRead         { get; init; }
    public double   LineSpeed         { get; init; }
    public long     RecordsRead       { get; init; }
    public long     MalformedRead     { get; init; }
    public double   Acceptance        { get; init; }
    public TimeSpan Duration          { get; init; }
    public DateTime ParseStart        { get; init; }
    public DateTime ParseEnd          { get; init; }
    public string?  Encoding          { get; init; }
    public List<EncodingSegmentModel> EncodingSegments { get; init; } = [];
    public List<string>               Formats          { get; init; } = [];
    public List<char>                 Delimiters       { get; init; } = [];
    public List<string>               Context          { get; init; } = [];
    public List<Dictionary<string, string>> Schemas    { get; init; } = [];
}

public class EncodingSegmentModel
{
    public long    Start    { get; init; }
    public long    EndOffset => Start + Length;
    public long    Length   { get; init; }
    public string? Encoding { get; init; }
    public string  DisplayEncoding => Encoding ?? "[NULL]";
}