using LeakChecker.DataParser.Utilities.Settings;

namespace LeakChecker.DataParser.Logging.Parse;

public class ParseLoggerFactory(ISettings settings) : IParseLoggerFactory
{
    public Task<IParseLogger> CreateAsync(Guid executionId, string filePath)
    {
        return ParseLogger.CreateAsync(settings, filePath);
    }
}