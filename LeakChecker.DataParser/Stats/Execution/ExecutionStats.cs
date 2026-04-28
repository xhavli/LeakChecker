using LeakChecker.DataParser.Stats.Parse;
using MongoDB.Bson;

namespace LeakChecker.DataParser.Stats.Execution;

public class ExecutionStats(Guid executionId, DateTime startTime)
{
    public Guid ExecutionId { get; } = executionId;
    public List<Guid> FilesParsed { get; set; } = new();
    public long BytesRead { get; set; }
    public long LinesRead { get; set; }
    public long RecordsRead { get; set; }
    public long MalformedRead { get; set; }
    public DateTime ExecutionStart { get; init; } = startTime;
    public DateTime ExecutionEnd { get; set; }
    public TimeSpan Duration => ExecutionEnd - ExecutionStart;
    public double LineSpeed => Duration.TotalSeconds > 0 ? LinesRead / Duration.TotalSeconds : 0;
    public double ByteSpeed => Duration.TotalSeconds > 0 ? BytesRead / Duration.TotalSeconds : 0;
    public double Accuracy => 
        RecordsRead <= 0 ? 0 : Math.Max(0, (double)(RecordsRead - MalformedRead) / RecordsRead * 100);
    
    public void Update(ParseStats parseStats)
    {
        lock (this)
        {
            FilesParsed.Add(parseStats.ParseId);
            LinesRead += parseStats.LinesRead;
            BytesRead += parseStats.BytesRead;
            RecordsRead += parseStats.RecordsRead;
            MalformedRead += parseStats.MalformedRead;
        }
    }
    
    public BsonDocument ToBsonDocument()
    {
        return new BsonDocument
        {
            { nameof(ExecutionId), new BsonBinaryData(ExecutionId, GuidRepresentation.Standard) },
            {nameof(FilesParsed), new BsonArray(FilesParsed.Select(id => new BsonBinaryData(id, GuidRepresentation.Standard))) },
            { nameof(BytesRead), BytesRead },
            { nameof(ByteSpeed), ByteSpeed },
            { nameof(LinesRead), LinesRead },
            { nameof(LineSpeed), LineSpeed },
            { nameof(RecordsRead), RecordsRead },
            { nameof(MalformedRead), MalformedRead },
            { nameof(Accuracy), Accuracy },
            { nameof(ExecutionStart), ExecutionStart.ToUniversalTime() },
            { nameof(ExecutionEnd), ExecutionEnd.ToUniversalTime() },
        };
    }
}