namespace LeakChecker.DataParser.Utilities.Configuration;

public record AppConfig
{
    public required string InputDirectory { get; init; }
    public required string LogDirectory { get; init; }
    public required string OutputDirectory { get; init; }
    public required string TmpDirectory { get; init; }
    public required string PythonNerService { get; init; }
    public required string PythonNerServArgs { get; init; }
    public required int CsharpPort { get; init; }
    public required int PythonPort { get; init; }
    public required int ConnectionTimeout { get; init; }
    public required int ThreadsCapacity { get; init; }
    public required int ChannelCapacity { get; init; }
    public required int SchemaThreshold { get; init; }
    public required string Environment { get; init; }
    public required bool Verbose { get; init; }
}