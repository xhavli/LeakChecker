namespace LeakChecker.EncodingDetection;

public class EncodingSegment
{
    public long StartOffset { get; init; }
    public long Length { get; set; }
    public long EndOffset => StartOffset + Length;
    public string EncodingName { get; init; } = string.Empty;
    public float Confidence { get; init; }

    public string ShowMegaByte()
    {
        string range = $"{StartOffset / 1024 / 1024,4} MB - {EndOffset / 1024 / 1024,4} MB";
        return $"{range} : {EncodingName,-20} confidence: {Confidence:F2}";
    }
    
    public string ShowKiloByte()
    {
        string range = $"{StartOffset / 1024:N0} KB - {EndOffset / 1024:N0} KB";
        return $"{range} : {EncodingName,-20} confidence: {Confidence:F2}";
    }
    
    public string ShowByte()
    {
        string range = $"{StartOffset:N0} B - {EndOffset:N0} B";
        return $"{range} : {EncodingName,-20} confidence: {Confidence:F2}";
    }
}