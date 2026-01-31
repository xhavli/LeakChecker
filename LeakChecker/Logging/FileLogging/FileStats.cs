using System.Text;
using LeakChecker.Encodings;
using LeakChecker.Format;

namespace LeakChecker.Logging.FileLogging;

public class FileStats
{
    public Guid ParseId { get; init; }
    public Guid ExecutionId { get; init; }
    public string? FileName { get; init; }
    public string? FilePath { get; init; }
    public long FileSize { get; init; }
    public long MalformedRecordsRead { get; set; }
    public long LinesRead { get; set; }
    public long BytesRead { get; set; }
    public long RecordsRead { get; set; }
    public Encoding? Encoding { get; set; }
    public List<EncodingSegment> EncodingSegments { get; set; } = new();
    public List<char> Delimiters { get; set; } = new();
    public List<FormatEnum> Formats { get; set; } = new();
    public List<string> Context { get; set; } = new();
    public DateTime ParseStart { get; init; } = DateTime.Now;
    public DateTime ParseEnd { get; set; }
    public TimeSpan Duration => ParseEnd - ParseStart;
    public double ByteSpeed => Duration.TotalSeconds > 0 ? BytesRead / Duration.TotalSeconds : 0;
    public double LineSpeed => Duration.TotalSeconds > 0 ? LinesRead / Duration.TotalSeconds : 0;
    public double Accuracy =>
        RecordsRead <= 0 ? 0 : Math.Max(0, (double)(RecordsRead - MalformedRecordsRead) / RecordsRead * 100);
}

