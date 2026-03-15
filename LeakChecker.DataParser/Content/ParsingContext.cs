using LeakChecker.DataParser.Logging.Parse;

namespace LeakChecker.DataParser.Content;

public class ParsingContext
{
    public char Delimiter;
    public required StreamReader Reader;
    public Dictionary<int, ItemEnum> Schema = new();

    public required ParseStats Stats;
    public required IParseLogger Logger;

    public long StartLine;
    public int Threshold;
    public long ParseLimit;
    public int SamplesLimit;
    public int MalformedLimit;
}