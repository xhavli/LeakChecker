namespace LeakChecker.Logging.FileLogging;

public interface IFileLoggerFactory
{
    Task<IFileLogger> CreateAsync(Guid executionId, string filePath);
}