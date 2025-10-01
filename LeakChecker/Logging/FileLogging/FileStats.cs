using LeakChecker.EncodingDetection;
using LeakChecker.FormatDetection;

namespace LeakChecker.Logging.FileLogging;

public class FileStats
{
    public Guid Id { get; } = Guid.NewGuid();
    public long FileBytes { get; init; }
    public string FileName { get; init; }
    public string FilePath { get; init; }
    public long RecordsCount { get; set; }
    public List<EncodingSegment> EncodingSegments { get; set; } = new();
    public List<FormatEnum> Formats { get; set; } = new();
    public DateTime ProcessingStart { get; init; }
    public DateTime ProcessingEnd { get; set; }
    public TimeSpan Duration => ProcessingEnd - ProcessingStart;
    
    public void PrintFileStats()
    {
        Console.WriteLine($"File {FileName} stats:");
        foreach (var format in Formats)
        {
            Console.WriteLine($"File format: {format.ToString()}");
        }
        Console.WriteLine($"Records processed: {RecordsCount}");
        Console.WriteLine($"Processing time: {Duration}");
    }
}

