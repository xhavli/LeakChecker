namespace LeakChecker.Logging.Parse;

public interface IParseLoggerFactory
{
    Task<IParseLogger> CreateAsync(Guid executionId, string filePath);
}