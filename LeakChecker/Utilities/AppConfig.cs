using System.Text.Json;
using LeakChecker.Logging;

namespace LeakChecker.Utilities;

public record AppConfig
{
    public required string InputDirectory { get; init; }
    public required string LogDirectory { get; init; }
    public required string OutputDirectory { get; init; }
    public required string TmpDirectory { get; init; }

    public static AppConfig ParseAppConfig()
    {
        var currentDir = Directory.GetCurrentDirectory();

        // Walk up two levels to get to project folder
        var projectDir = Directory.GetParent(currentDir)?.Parent?.Parent?.FullName;

        // Build path to AppConfig.json
        var jsonPath = Path.Combine(projectDir ?? string.Empty, "appsettings.json");

        if (!File.Exists(jsonPath))
        {
            Console.Error.WriteLine($"appsettings file not found in {jsonPath}. Program terminate with exit code 1");
            Environment.Exit(1);
        }

        var configJson = File.ReadAllText(jsonPath);
        var config = JsonSerializer.Deserialize<AppConfig>(configJson);

        if (config == null)
        {
            Console.Error.WriteLine("Configuration file is missing or failed to parse. Program terminate with exit code 1");
            Environment.Exit(1);
        }

        if (string.IsNullOrEmpty(config.InputDirectory) || !Directory.Exists(config.InputDirectory))
        {
            Console.Error.WriteLine($"Input directory '{config.InputDirectory}' is missing. Program terminate with exit code 1");
            Environment.Exit(1);
        }
        
        if (string.IsNullOrEmpty(config.LogDirectory) || !Directory.Exists(config.LogDirectory))
        {
            Console.Error.WriteLine($"{nameof(config.LogDirectory)} is missing in appsettings.json or does not exists on current machine. " +
                                    $"Trying to create a new directory at '{config.LogDirectory}'.");
            try
            {
                Directory.CreateDirectory(config.LogDirectory);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"[EXCEPTION] Could not create {nameof(config.LogDirectory)}. {e.Message}. Program terminate with exit code 1");
                Environment.Exit(1);
            }
        }

        if (string.IsNullOrEmpty(config.OutputDirectory) || !Directory.Exists(config.OutputDirectory))
        {
            Console.Error.WriteLine($"Output directory '{config.OutputDirectory}' is missing. Program terminate with exit code 1");
            Environment.Exit(1);
        }
        
        return config;
    }
}
