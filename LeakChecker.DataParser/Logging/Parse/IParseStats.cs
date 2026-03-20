using System.Text;
using LeakChecker.DataParser.Content;
using LeakChecker.DataParser.Encodings;
using LeakChecker.DataParser.Format;

namespace LeakChecker.DataParser.Logging.Parse;

public interface IParseStats
{
    public Guid ParseId { get; init; }
    public string SourcePath { get; init; }
    public string ParsePath { get; set; }
    public long MalformedRecordsRead { get; set; }
    public long LinesRead { get; set; }
    public long BytesRead { get; set; }
    public long RecordsRead { get; set; }
    public Encoding? Encoding { get; set; }
    public List<EncodingSegment> EncodingSegments { get; set; }
    public List<char> Delimiters { get; init; }
    public List<FormatEnum> Formats { get; init; }
    public List<string> Context { get; init; }
    public List<Dictionary<int, ItemEnum>> Schemas { get; init; }
}