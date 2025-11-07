using LeakChecker.Logging.FileLogging;

namespace LeakProcessor.Tests.Logging.Helpers.FileLogging;

/// <summary>
/// Factory used in test dependency injection to create NullFileLogger instances.
/// Ensures no file I/O or console output occurs during tests.
/// </summary>
public sealed class NullFileLoggerFactory : IFileLoggerFactory
{
    public Task<IFileLogger> CreateAsync(string filePath)
        => Task.FromResult<IFileLogger>(new NullFileLogger(filePath));
}