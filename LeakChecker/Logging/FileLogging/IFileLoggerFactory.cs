namespace LeakChecker.Logging.FileLogging;

public interface IFileLoggerFactory
{
    Task<IFileLogger> CreateAsync(Guid parsingId, Guid executionId, DateTime parseStart, string filePath);
}