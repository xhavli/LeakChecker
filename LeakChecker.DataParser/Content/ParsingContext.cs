using LeakChecker.DataParser.Logging.Parse;

namespace LeakChecker.DataParser.Content;

public class ParsingContext
{
    public long StartLine;
    public Dictionary<int, ItemEnum> Schema = new();
    public Dictionary<int, Dictionary<int, ItemEnum>> Schemas = new();

    public char Delimiter;
    public required StreamReader Reader;

    public required ParseStats Stats;
    public required IParseLogger Logger;

    public int Threshold;
    public long ParseLimit;
    public int SamplesLimit;
    public int MalformedLimit;
}