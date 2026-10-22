namespace LeakChecker.DataParser.Logging.Parse;

public interface IParseLoggerFactory
{
    IParseLogger Create(string filePath);
}