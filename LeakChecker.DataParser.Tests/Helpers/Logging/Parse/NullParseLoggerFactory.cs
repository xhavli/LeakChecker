using LeakChecker.DataParser.Logging.Parse;

namespace LeakChecker.DataParser.Tests.Helpers.Logging.Parse;

/// <summary>
/// Factory used in test dependency injection to create NullFileLogger instances.
/// Ensures no file I/O or console output occurs during tests.
/// </summary>
public sealed class NullParseLoggerFactory : IParseLoggerFactory
{
    public IParseLogger CreateAsync(string filePath)
    {
        return new NullParseLogger();
    }
}