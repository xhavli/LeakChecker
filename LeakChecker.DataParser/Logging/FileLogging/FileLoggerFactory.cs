using LeakChecker.Utilities.Configuration;

namespace LeakChecker.Logging.FileLogging;

public class FileLoggerFactory(AppConfig config) : IFileLoggerFactory
{
    public Task<IFileLogger> CreateAsync(Guid executionId, string filePath)
    {
        return FileLogger.CreateAsync(config, executionId, filePath);
    }
}