using LeakChecker.Logging.Parse;

namespace LeakProcessor.Tests.Logging.Helpers.Parse;

/// <summary>
/// Factory used in test dependency injection to create NullFileLogger instances.
/// Ensures no file I/O or console output occurs during tests.
/// </summary>
public sealed class NullParseLoggerFactory : IParseLoggerFactory
{
    public Task<IParseLogger> CreateAsync(Guid executionId, string filePath)
        => Task.FromResult<IParseLogger>(new NullParseLogger(filePath));
}