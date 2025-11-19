using System.Text;
using LeakChecker.Encodings;
using LeakChecker.Format;

namespace LeakChecker.Logging.FileLogging;

public class FileStats
{
    public Guid ParsingId { get; init; } = Guid.NewGuid();
    public string? FileName { get; init; }
    public string? FilePath { get; init; }
    public long FileBytes { get; init; }
    public long LinesRead { get; set; }
    public long BytesRead { get; set; }
    public long RecordsCount { get; set; }
    public Encoding? Encoding { get; set; }
    public List<EncodingSegment> EncodingSegments { get; set; } = new();
    public List<char> Delimiters { get; set; } = new();
    public List<FormatEnum> Formats { get; set; } = new();
    public DateTime ParsingStart { get; init; } = DateTime.Now;
    public DateTime ParsingEnd { get; set; }
    public TimeSpan Duration => ParsingEnd - ParsingStart;
    public double ByteSpeed => Duration.TotalSeconds > 0 ? BytesRead / Duration.TotalSeconds : 0;
    public double LineSpeed => Duration.TotalSeconds > 0 ? RecordsCount / Duration.TotalSeconds : 0;
}

