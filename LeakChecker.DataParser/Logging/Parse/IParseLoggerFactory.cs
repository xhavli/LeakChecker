namespace LeakChecker.DataParser.Logging.Parse;

public interface IParseLoggerFactory
{
    IParseLogger CreateAsync(string filePath);
}