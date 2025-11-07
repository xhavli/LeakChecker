using System.Text;
using LeakChecker.Encodings;
using LeakChecker.Format;

namespace LeakChecker.Logging.FileLogging;

public class FileStats
{
    public Guid Id { get; } = Guid.NewGuid();
    public string? FileName { get; init; }
    public string? FilePath { get; init; }
    public long FileBytes { get; init; }
    public long LinesRead { get; set; }
    public long RecordsCount { get; set; }
    public Encoding? Encoding { get; set; }
    public List<EncodingSegment> EncodingSegments { get; set; } = new();
    public List<char> Delimiters { get; set; } = new();
    public List<FormatEnum> Formats { get; set; } = new();
    public DateTime ProcessingStart { get; init; }
    public DateTime ProcessingEnd { get; set; }
    public TimeSpan Duration => ProcessingEnd - ProcessingStart;
    
    public void PrintFileStats()
    {
        Console.WriteLine($"File {FileName} stats:");
        var origin = string.IsNullOrEmpty(Encoding?.WebName) ? "NULL" : Encoding.WebName;
        Console.WriteLine($"Origin encoding [{origin}]");
        Console.WriteLine($"Origin encoding segments [{EncodingSegments.Count}]");
        foreach (var format in Formats)
        {
            Console.WriteLine($"File format: {format.ToString()}");
        }
        Console.WriteLine($"Records processed: {RecordsCount}");
        Console.WriteLine($"Processing time: {Duration}");
    }
}

