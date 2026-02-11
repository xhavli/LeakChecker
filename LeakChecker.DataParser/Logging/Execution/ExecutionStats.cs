namespace LeakChecker.Logging.Execution;

public class ExecutionStats(Guid executionId, DateTime startTime)
{
    public Guid ExecutionId { get; } = executionId;
    public List<Guid> FilesParsed { get; set; } = new();
    public long MalformedRecordsRead { get; set; }
    public long RecordsParsed { get; set; }
    public long LinesParsed { get; set; }
    public long BytesParsed { get; set; }
    public DateTime ExecutionStart { get; init; } = startTime;
    public DateTime ExecutionEnd { get; set; }
    public TimeSpan Duration => ExecutionEnd - ExecutionStart;
    public double LineSpeed => Duration.TotalSeconds > 0 ? LinesParsed / Duration.TotalSeconds : 0;
    public double ByteSpeed => Duration.TotalSeconds > 0 ? BytesParsed / Duration.TotalSeconds : 0;
    public double Accuracy => 
        RecordsParsed <= 0 ? 0 : Math.Max(0, (double)(RecordsParsed - MalformedRecordsRead) / RecordsParsed * 100);
}