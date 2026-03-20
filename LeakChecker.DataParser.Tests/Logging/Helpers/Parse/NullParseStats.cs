using System.Text;
using LeakChecker.DataParser.Content;
using LeakChecker.DataParser.Encodings;
using LeakChecker.DataParser.Format;
using LeakChecker.DataParser.Logging.Parse;

namespace LeakChecker.DataParser.Tests.Logging.Helpers.Parse;

public sealed class NullParseStats(string sourcePath = "") : IParseStats
{
    public Guid ParseId { get; init; } = Guid.Empty;
    public string SourcePath { get; init; } = sourcePath;
    public string ParsePath { get; set; } = string.Empty;
    public long MalformedRecordsRead { get; set; } = 0;
    public long LinesRead { get; set; } = 0;
    public long BytesRead { get; set; } = 0;
    public long RecordsRead { get; set; } = 0;
    public Encoding? Encoding { get; set; }
    public List<EncodingSegment> EncodingSegments { get; set; } = new();
    public List<char> Delimiters { get; init; } = new();
    public List<FormatEnum> Formats { get; init; } = new();
    public List<string> Context { get; init; } = new();
    public List<Dictionary<int, ItemEnum>> Schemas { get; init; } = new();
}