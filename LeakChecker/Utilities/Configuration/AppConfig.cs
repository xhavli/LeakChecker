namespace LeakChecker.Utilities.Configuration;

public record AppConfig
{
    public required string InputDirectory { get; init; }
    public required string LogDirectory { get; init; }
    public required string OutputDirectory { get; init; }
    public required string TmpDirectory { get; init; }
    public required string PythonNerService { get; init; }
    public required string PythonNerServArgs { get; init; }
    public int CsharpPort { get; init; }
    public int PythonPort { get; init; }
    public int ConnectionTimeout { get; init; }
    public int ThreadsCapacity { get; init; }
    public int ChannelCapacity { get; init; }
    public int SchemaThreshold { get; init; }
    public string Environment { get; init; } = "Production";
}