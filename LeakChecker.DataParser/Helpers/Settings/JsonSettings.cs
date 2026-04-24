namespace LeakChecker.DataParser.Helpers.Settings;

public class JsonSettings
{
    public string? InputDirectory { get; set; }
    public string? LogDirectory { get; set; }
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
    public string? DbProvider { get; set; }

    public void Validate()
    {
        ValidateDirectory(InputDirectory, nameof(InputDirectory));
        ValidateDirectory(LogDirectory, nameof(LogDirectory));
        ValidateDirectory(TmpDirectory, nameof(TmpDirectory));
        
        ValidateCollision(InputDirectory!,TmpDirectory!);
        
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
            if (value < 1)
                throw new Exception();
        }
        catch (Exception e)
        {
            throw new ArgumentOutOfRangeException(nameof(value), $"Value of {name} must be in ushort range 1-65535. {e.Message}");
        }
    }

    private static void ValidateCollision(string inputDir, string tempDir)
    {
        // Normalize to full paths
        string fullInputPath = Path.GetFullPath(inputDir);
        string fullTempPath = Path.GetFullPath(tempDir);

        // Ensure temp path ends with separator for proper prefix matching
        if (!fullTempPath.EndsWith(Path.DirectorySeparatorChar))
            fullTempPath += Path.DirectorySeparatorChar;

        // Compare (case-insensitive on Windows)
        if (fullInputPath.StartsWith(fullTempPath, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("InputDirectory cannot be inside the TemporaryDirectory because all data will be deleted.");
    }
}