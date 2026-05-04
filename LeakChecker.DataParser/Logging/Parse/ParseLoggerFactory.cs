using LeakChecker.DataParser.Helpers.Settings;

namespace LeakChecker.DataParser.Logging.Parse;

public class ParseLoggerFactory(ISettings settings) : IParseLoggerFactory
{
    public IParseLogger CreateAsync(string filePath)
    {
        return ParseLogger.Create(filePath, settings);
    }
}