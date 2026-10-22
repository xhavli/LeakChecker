namespace LeakChecker.DataParser.Helpers.Settings;

public record ExecutionSettings
{
    public string InputDirectory { get; init; } = string.Empty;
    public string TmpDirectory { get; init; } = string.Empty;

    public string SolutionDirectory { get; init; } = Directory.GetParent(Directory.GetCurrentDirectory())?.Parent?.Parent?.Parent?.FullName!;

    public int ThreadsCapacity { get; init; }
    public int ChannelCapacity { get; init; }

    public double? ParseSizeLimitGb { get; init; }
    public string? ResumeFromPath { get; init; }

    public string Environment { get; init; } = string.Empty;
}