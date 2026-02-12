using System.Text;

namespace LeakChecker.DataParser.Encodings;

public class EncodingSegment
{
    public long StartOffset { get; init; }
    public long Length { get; set; }
    public long EndOffset => StartOffset + Length;
    public Encoding? Encoding { get; set; }
    public float Confidence { get; init; }

    public string ToMegabyteString()
    {
        string range = $"{StartOffset / 1024 / 1024,4} MB - {EndOffset / 1024 / 1024,4} MB";
        return $"{range} : {Encoding?.WebName,-20} confidence: {Confidence:F2}";
    }
    
    public string ToKilobyteString()
    {
        string range = $"{StartOffset / 1024:N0} KB - {EndOffset / 1024:N0} KB";
        return $"{range} : {Encoding?.WebName,-20} confidence: {Confidence:F2}";
    }
    
    public string ToByteString()
    {
        string range = $"{StartOffset:N0} B - {EndOffset:N0} B";
        return $"{range} : {Encoding?.WebName,-20} confidence: {Confidence:F2}";
    }
}