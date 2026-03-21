namespace LeakChecker.DataParser.Utilities.Settings;

public class JsonSettings
{
    public string? InputDirectory { get; set; }
    public string? LogDirectory { get; set; }
    public string? OutputDirectory { get; set; }
    public string? TmpDirectory { get; set; }
    public string? PythonVenvPath { get; set; }
    public string? PythonScriptName { get; set; }
    public int CsharpPort { get; set; }
    public int PythonPort { get; set; }
    public int StartupTimeoutSeconds { get; set; }
    public int ThreadsCapacity { get; set; }
    public int ChannelCapacity { get; set; }
    public int SchemaThreshold { get; set; }
    public int CsvSamples { get; set; }
    public int SqlSamples { get; set; }
    public int ExcelSamples { get; set; }
    public string? Environment { get; set; }
    public bool Verbose { get; set; }

    public void Validate()
    {
        ValidateDirectory(InputDirectory, nameof(InputDirectory));
        ValidateDirectory(LogDirectory, nameof(LogDirectory));
        ValidateDirectory(OutputDirectory, nameof(OutputDirectory));
        ValidateDirectory(TmpDirectory, nameof(TmpDirectory));
        
        ValidateRange(CsharpPort, nameof(CsharpPort));
        ValidateRange(PythonPort, nameof(PythonPort));
        ValidateRange(StartupTimeoutSeconds, nameof(StartupTimeoutSeconds));
        ValidateRange(ThreadsCapacity, nameof(ThreadsCapacity));
        ValidateRange(ChannelCapacity, nameof(ChannelCapacity));
        ValidateRange(SchemaThreshold, nameof(SchemaThreshold));
    }

    private static void ValidateDirectory(string? path, string name)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException($"Config value '{name}' is missing or empty.");

        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException($"Directory for '{name}' does not exist: '{path}'.");
    }

    private static void ValidateRange(int value, string name)
    {
        try
        {
            _ = Convert.ToUInt16(value);
        }
        catch (Exception e)
        {
            throw new ArgumentOutOfRangeException(nameof(value), $"Value of {name} must be in ushort range 0-65535. {e.Message}");
        }
    }
}