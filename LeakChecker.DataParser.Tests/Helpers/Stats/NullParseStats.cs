using System.Text;
using LeakChecker.Common.Enums;
using LeakChecker.DataParser.Encodings;
using LeakChecker.DataParser.Stats.Parse;
using MongoDB.Bson;

namespace LeakChecker.DataParser.Tests.Helpers.Stats;

public sealed class NullParseStats(string sourcePath = "") : IParseStats
{
    public ObjectId ParseId { get; init; } = ObjectId.Empty;
    public string SourcePath { get; init; } = sourcePath;
    public string ParsePath { get; set; } = string.Empty;
    public long LinesRead { get; set; } = 0;
    public long BytesRead { get; set; } = 0;
    public long RecordsRead { get; set; } = 0;
    public long MalformedRead { get; set; } = 0;
    public Encoding? Encoding { get; set; }
    public List<EncodingSegment> EncodingSegments { get; set; } = new();
    public List<char> Delimiters { get; init; } = new();
    public List<FormatEnum> Formats { get; init; } = new();
    public List<string> Context { get; init; } = new();
    public List<Dictionary<int, ItemEnum>> Schemas { get; init; } = new();
}