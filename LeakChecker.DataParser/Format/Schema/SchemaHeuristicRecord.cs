using LeakChecker.DataParser.Content;

namespace LeakChecker.DataParser.Format.Schema;

public class SchemaHeuristicRecord
{
    public ItemEnum Attribute { get; init; }
    public int Position { get; init; }
    public int DelimitersInside { get; init; }
}