using LeakChecker.Utilities.Configuration;

namespace LeakChecker.Logging.FileLogging;

public class FileLoggerFactory(AppConfig config) : IFileLoggerFactory
{
    public Task<IFileLogger> CreateAsync(Guid parsingId, Guid executionId, DateTime parseStart, string filePath)
    {
        return FileLogger.CreateAsync(config, parsingId, executionId, parseStart, filePath);
    }
}