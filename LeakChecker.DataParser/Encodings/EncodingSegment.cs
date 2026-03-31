using System.Text;
using LeakChecker.DataParser.Helpers.Enums;

namespace LeakChecker.DataParser.Encodings;

public class EncodingSegment
{
    public long StartOffset { get; init; }
    public long Length { get; init; }
    private long EndOffset => StartOffset + Length;
    public Encoding? Encoding { get; set; }
    public float Confidence { get; init; }

    private string GetEncodingName() =>
        string.IsNullOrWhiteSpace(Encoding?.WebName)
            ? "[NULL]"
            : Encoding.WebName;

    public string ToMegabyteString()
    {
        string range = $"{StartOffset / SizeEnum.MegaByte,4} MB - {EndOffset / SizeEnum.MegaByte,4} MB";
        return $"{range} : {GetEncodingName(),-20} confidence: {Confidence:F2}";
    }

    public string ToKilobyteString()
    {
        string range = $"{StartOffset / SizeEnum.KiloByte:N0} KB - {EndOffset / SizeEnum.KiloByte:N0} KB";
        return $"{range} : {GetEncodingName(),-20} confidence: {Confidence:F2}";
    }
    
    public string ToByteString()
    {
        string range = $"{StartOffset:N0} B - {EndOffset:N0} B";
        return $"{range} : {GetEncodingName(),-20} confidence: {Confidence:F2}";
    }
}