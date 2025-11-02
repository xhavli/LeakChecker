using LeakChecker.Utilities.Configuration;

namespace LeakChecker.Logging.FileLogging;

public class FileLoggerFactory(AppConfig config) : IFileLoggerFactory
{
    public Task<IFileLogger> CreateAsync(string filePath)
    {
        bool enableConsole = ShouldEnableConsole();
        return FileLogger.CreateAsync(filePath, config, enableConsole);
    }

    private bool ShouldEnableConsole()
    {
        //TODO use environment variable
        return config.Environment.Equals("Development", StringComparison.OrdinalIgnoreCase);
    }
}