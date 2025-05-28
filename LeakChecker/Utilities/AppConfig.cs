using System.Text.Json;

namespace LeakChecker.Utilities;

public record EncodingDetectorConfig
{
    public required int AccuracyPercent { get; set; }
    public required string ScriptPath { get; init; }
}

public record ContentDetectorConfig
{
    public required string ScriptPath { get; init; }
}

public record AppConfig
{
    public required string InputDirectory { get; init; }
    public required string OutputDirectory { get; init; }
    public required EncodingDetectorConfig EncodingDetector { get; init; }
    public required ContentDetectorConfig ContentDetector { get; init; }

    public static AppConfig ParseAppConfiguration()
    {
        var currentDir = Directory.GetCurrentDirectory();

        // Walk up two levels to get to project folder
        var projectDir = Directory.GetParent(currentDir)?.Parent?.Parent?.FullName;

        // Build path to AppConfig.json
        var jsonPath = Path.Combine(projectDir ?? string.Empty, "AppConfig.json");

        if (!File.Exists(jsonPath))
        {
            NicePrint.PrintError($"AppConfig file not found in {jsonPath}. " +
                                 "Program terminate with exit code 1");
            Environment.Exit(1);
        }

        var configJson = File.ReadAllText(jsonPath);
        var config = JsonSerializer.Deserialize<AppConfig>(configJson);

        if (config == null)
        {
            NicePrint.PrintError("Configuration file is missing or failed to parse. " +
                                 "Program terminate with exit code 1");
            Environment.Exit(1);
        }

        if (string.IsNullOrEmpty(config.InputDirectory) || !Directory.Exists(config.InputDirectory))
        {
            NicePrint.PrintError("Input directory is missing. Program terminate with exit code 1");
            Environment.Exit(1);
        }

        if (string.IsNullOrEmpty(config.OutputDirectory) || !Directory.Exists(config.OutputDirectory))
        {
            NicePrint.PrintError("Output directory is missing. Program terminate with exit code 1");
            Environment.Exit(1);
        }

        switch (config.EncodingDetector.AccuracyPercent)
        {
            case <= 0:
                NicePrint.PrintWarning("Accuracy percent is 0% or negative. " +
                                       "Program terminate without doing anything with exit code 0");
                Environment.Exit(0);
                break;
            case >= 100:
                NicePrint.PrintWarning("Accuracy percent is 100% or higher. It may take a long time " +
                                       "and cause performance issues when parsing large files");
                config.EncodingDetector.AccuracyPercent = 100;
                break;
        }

        if (string.IsNullOrEmpty(config.EncodingDetector.ScriptPath) ||
            !File.Exists(config.EncodingDetector.ScriptPath))
        {
            NicePrint.PrintError("Encoding detector python script is missing or file doesn't exist. " +
                                 "Program terminate with exit code 1");
            Environment.Exit(1);
        }

        return config;
    }
}
