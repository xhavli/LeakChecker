using System.Text.Json;

namespace LeakChecker.Utilities;

public record EncodingDetector
{
    public required string ScriptPath { get; init; }
}

public record ContentDetector
{
    public required string ScriptPath { get; init; }
}

public record AppConfig
{
    public required string InputDirectory { get; init; }
    public required string OutputDirectory { get; init; }
    public required string TemporaryDirectory { get; init; }
    public required EncodingDetector EncodingDetector { get; init; }
    public required ContentDetector ContentDetector { get; init; }

    public static AppConfig ParseAppConfig()
    {
        var currentDir = Directory.GetCurrentDirectory();

        // Walk up two levels to get to project folder
        var projectDir = Directory.GetParent(currentDir)?.Parent?.Parent?.FullName;

        // Build path to AppConfig.json
        var jsonPath = Path.Combine(projectDir ?? string.Empty, "AppConfig.json");

        if (!File.Exists(jsonPath))
        {
            Logger.LogError($"AppConfig file not found in {jsonPath}. Program terminate with exit code 1");
            Environment.Exit(1);
        }

        var configJson = File.ReadAllText(jsonPath);
        var config = JsonSerializer.Deserialize<AppConfig>(configJson);

        if (config == null)
        {
            Logger.LogError("Configuration file is missing or failed to parse. Program terminate with exit code 1");
            Environment.Exit(1);
        }

        if (string.IsNullOrEmpty(config.InputDirectory) || !Directory.Exists(config.InputDirectory))
        {
            Logger.LogError($"Input directory '{config.InputDirectory}' is missing. Program terminate with exit code 1");
            Environment.Exit(1);
        }

        if (string.IsNullOrEmpty(config.OutputDirectory) || !Directory.Exists(config.OutputDirectory))
        {
            Logger.LogError($"Output directory '{config.OutputDirectory}' is missing. Program terminate with exit code 1");
            Environment.Exit(1);
        }
        
        if (string.IsNullOrEmpty(config.TemporaryDirectory) || !Directory.Exists(config.TemporaryDirectory))
        {
            Logger.LogError($"Temporary directory '{config.TemporaryDirectory}' is missing. " +
                            $"Program terminate with exit code 1");
            Environment.Exit(1);
        }

        if (string.IsNullOrEmpty(config.EncodingDetector.ScriptPath) ||
            !File.Exists(config.EncodingDetector.ScriptPath))
        {
            Logger.LogError("Encoding detector python script is missing or file on path " +
                            $"'{config.EncodingDetector.ScriptPath}' doesn't exist. Program terminate with exit code 1");
            Environment.Exit(1);
        }

        return config;
    }
}
