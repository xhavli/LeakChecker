using LeakChecker.DataParser.Utilities.Configuration;

namespace LeakChecker.DataParser.Logging.Parse;

public class ParseLoggerFactory(AppConfig config) : IParseLoggerFactory
{
    public Task<IParseLogger> CreateAsync(Guid executionId, string filePath)
    {
        return ParseLogger.CreateAsync(config, executionId, filePath);
    }
}