namespace LeakChecker.DataParser.Utilities.Configuration;

public record AppConfig
{
    public required string InputDirectory { get; set; }
    public required string LogDirectory { get; set; }
    public required string OutputDirectory { get; set; }
    public required string TmpDirectory { get; set; }
    public required string PythonVenvPath { get; init; }
    public required string PythonScriptName { get; init; }
    public required int CsharpPort { get; init; }
    public required int PythonPort { get; init; }
    public required int StartupTimeoutSeconds { get; init; }
    public required int ThreadsCapacity { get; init; }
    public required int ChannelCapacity { get; init; }
    public required int SchemaThreshold { get; init; }
    public required string Environment { get; init; }
    public required bool Verbose { get; init; }
}