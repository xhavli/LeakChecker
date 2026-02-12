using System.Text;
using LeakChecker.DataParser.Logging.Parse;
using LeakChecker.Encodings;
using LeakChecker.Format;
using LeakChecker.Logging.Parse;

namespace LeakChecker.DataParser.Tests.Logging.Helpers.Parse;

public sealed class NullParseStats : ParseStats
{
    private NullParseStats()
    {
        ParseId = Guid.Empty;
        ExecutionId = Guid.Empty;
        FileName = string.Empty;
        FilePath = string.Empty;
        FileSize = 0;
        MalformedRecordsRead = 0;
        LinesRead = 0;
        BytesRead = 0;
        RecordsRead = 0;
        Encoding = Encoding.UTF8;
        EncodingSegments = new List<EncodingSegment>();
        Delimiters = new List<char>();
        Formats = new List<FormatEnum>();
        Context = new List<string>();
        ParseStart = DateTime.MaxValue;
        ParseEnd = DateTime.MaxValue;
    }

    public new TimeSpan Duration => TimeSpan.Zero;
    public new double ByteSpeed => 0;
    public new double LineSpeed => 0;
    public new double Accuracy => 0;

    public new static NullParseStats Create(Guid executionId, IParseLogger parseLogger, string filePath)
    {
        return new NullParseStats
        {
            ParseId = Guid.Empty,
            ExecutionId = Guid.Empty,
            ParseStart = DateTime.MaxValue,
            FileName = string.Empty,
            FilePath = string.Empty,
            FileSize = 0
        };
    }
}