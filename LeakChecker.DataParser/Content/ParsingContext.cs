using LeakChecker.Common.Enums;
using LeakChecker.DataParser.Helpers.Settings;
using LeakChecker.DataParser.Logging.Parse;
using LeakChecker.DataParser.Stats.Parse;

namespace LeakChecker.DataParser.Content;

public class ParsingContext
{
    public char Delimiter;
    public required StreamReader Reader;
    public Dictionary<int, ItemEnum> Schema = new();

    public required IParseStats Stats;
    public required IParseLogger Logger;

    public long StartLine;
    public long ParseLimit;
    public int MalformedLimit;
    
    public required ISettings Settings;
}