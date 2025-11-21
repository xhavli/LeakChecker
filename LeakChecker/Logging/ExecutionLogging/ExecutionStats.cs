using System.Runtime.InteropServices.JavaScript;

namespace LeakChecker.Logging.ExecutionLogging;

public class ExecutionStats(Guid executionId)
{
    public Guid ExecutionId { get; } = executionId;
    public List<Guid> FilesParsed { get; set; } = new();
    public long BytesParsed { get; set; }
    public long LinesParsed { get; set; }
    public DateTime ParsingStart { get; init; } = DateTime.Now;
    public DateTime ParsingEnd { get; set; }
    public TimeSpan Duration => ParsingEnd - ParsingStart;
    public double ByteSpeed => Duration.TotalSeconds > 0 ? BytesParsed / Duration.TotalSeconds : 0;
    public double LineSpeed => Duration.TotalSeconds > 0 ? LinesParsed / Duration.TotalSeconds : 0;
}