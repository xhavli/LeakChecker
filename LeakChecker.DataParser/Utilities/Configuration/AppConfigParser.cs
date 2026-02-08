using System.Text.Json;

namespace LeakChecker.Utilities.Configuration;

public static class AppConfigParser
{
    public static AppConfig LoadFromFile(string path)
    {
        // Get current working directory (bin/Debug/... or run location)
        var currentDir = Directory.GetCurrentDirectory();

        // Walk up 2 folders to get to project root (same as your old code)
        var projectDir = Directory.GetParent(currentDir)?.Parent?.Parent?.FullName
                         ?? throw new DirectoryNotFoundException("Unable to determine project directory");

        // Build full path to appsettings.json
        var jsonPath = Path.Combine(projectDir, path);

        if (!File.Exists(jsonPath))
            throw new FileNotFoundException($"AppConfig file not found in: {jsonPath}");

        var json = File.ReadAllText(jsonPath);
        var config = JsonSerializer.Deserialize<AppConfig>(json)
                     ?? throw new InvalidOperationException("AppConfig deserialized to null");

        Validate(config);
        return config;
    }

    private static void Validate(AppConfig config)
    {
        ValidateDirectory(config.InputDirectory, nameof(config.InputDirectory), mustExist: true);
        ValidateDirectory(config.LogDirectory, nameof(config.LogDirectory), mustExist: false);
        ValidateDirectory(config.OutputDirectory, nameof(config.OutputDirectory), mustExist: true);
        ValidateDirectory(config.TmpDirectory, nameof(config.TmpDirectory), mustExist: false);
        
        ValidateNotNegative(config.CsharpPort);
        ValidateNotNegative(config.PythonPort);
        ValidateNotNegative(config.ConnectionTimeout);
        ValidateNotNegative(config.ThreadsCapacity);
        ValidateNotNegative(config.ChannelCapacity);
        ValidateNotNegative(config.SchemaThreshold);
    }

    private static void ValidateDirectory(string? path, string name, bool mustExist)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException($"Config value '{name}' is missing or empty");

        if (!Directory.Exists(path))
        {
            if (mustExist)
                throw new DirectoryNotFoundException($"Directory for '{name}' does not exist: '{path}'");

            Directory.CreateDirectory(path); // for Log + Tmp paths
        }
    }

    private static void ValidateNotNegative(int value)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Value must be zero or greater.");
    }
}