using System.Runtime.InteropServices.JavaScript;

namespace LeakChecker.Logging.ExecutionLogging;

public class ExecutionStats
{
    public Guid ExecutionId { get; init; } = Guid.NewGuid();
    public List<Guid> FilesParsed { get; set; } = new();
    public long BytesParsed { get; set; }
    public long LinesParsed { get; set; }
    public DateTime ParsingStart { get; init; } = DateTime.Now;
    public DateTime ParsingEnd { get; set; }
    public TimeSpan Duration => ParsingEnd - ParsingStart;
    public double ByteSpeed => Duration.TotalSeconds > 0 ? BytesParsed / Duration.TotalSeconds : 0;
    public double LineSpeed => Duration.TotalSeconds > 0 ? LinesParsed / Duration.TotalSeconds : 0;
    
    public void PrintExecutionStats()
    {
        Console.WriteLine("Execution stats:");
        Console.WriteLine($"Execution ID: {ExecutionId}");
        Console.WriteLine($"Files parsed: {FilesParsed.Count}");
        foreach (var guid in FilesParsed)
        {
            Console.WriteLine($"    Parsing ID: {guid}");
        }
        Console.WriteLine($"Bytes parsed: {BytesParsed}");
        Console.WriteLine($"Byte speed: {ByteSpeed} bytes/second");
        Console.WriteLine($"Lines parsed: {LinesParsed}");
        Console.WriteLine($"Lines speed: {LineSpeed} lines/second");
        Console.WriteLine($"Execution start: {ParsingStart}");
        Console.WriteLine($"Execution end: {ParsingEnd}");
        Console.WriteLine($"Execution time: {Duration}");
    }
}