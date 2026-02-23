using System.Text.Json;

namespace LeakChecker.DataParser.Utilities.Configuration;

public static class AppConfigParser
{
    public static AppConfig LoadFromFile(string fileName)
    {
        // *LeakChecker.DataParser\bin\Debug\net0.0
        var currentDir = Directory.GetCurrentDirectory();

        // *LeakChecker.DataParser
        var projectDir = Directory.GetParent(currentDir)?.Parent?.Parent?.FullName 
                            ?? throw new DirectoryNotFoundException("Unable to determine project directory");

        var jsonPath = Path.Combine(projectDir, fileName);

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
        ValidateDirectory(config.InputDirectory, nameof(config.InputDirectory));
        ValidateDirectory(config.LogDirectory, nameof(config.LogDirectory));
        ValidateDirectory(config.OutputDirectory, nameof(config.OutputDirectory));
        ValidateDirectory(config.TmpDirectory, nameof(config.TmpDirectory));
        
        ValidateRange(config.CsharpPort, nameof(config.CsharpPort));
        ValidateRange(config.PythonPort, nameof(config.PythonPort));
        ValidateRange(config.StartupTimeoutSeconds, nameof(config.StartupTimeoutSeconds));
        ValidateRange(config.ThreadsCapacity, nameof(config.ThreadsCapacity));
        ValidateRange(config.ChannelCapacity, nameof(config.ChannelCapacity));
        ValidateRange(config.SchemaThreshold, nameof(config.SchemaThreshold));
    }

    private static void ValidateDirectory(string? path, string name)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException($"Config value '{name}' is missing or empty");

        if (!Directory.Exists(path))
        {
            try
            {
                Directory.CreateDirectory(path);
            }
            catch (Exception e)
            {
                throw new DirectoryNotFoundException($"Directory for '{name}' does not exist: '{path}'. {e.Message}");
            }
        }
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