namespace LeakChecker.DataParser.Logging.Parse;

public interface IParseLoggerFactory
{
    Task<IParseLogger> CreateAsync(string filePath);
}