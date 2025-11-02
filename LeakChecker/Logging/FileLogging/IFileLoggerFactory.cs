namespace LeakChecker.Logging.FileLogging;

public interface IFileLoggerFactory
{
    Task<IFileLogger> CreateAsync(string filePath);
}