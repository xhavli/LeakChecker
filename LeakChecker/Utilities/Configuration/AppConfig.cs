namespace LeakChecker.Utilities.Configuration;

public record AppConfig
{
    public required string InputDirectory { get; init; }
    public required string LogDirectory { get; init; }
    public required string OutputDirectory { get; init; }
    public required string TmpDirectory { get; init; }
    public string Environment { get; init; } = "Production";
}