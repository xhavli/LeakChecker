using LeakChecker.DataParser.Content;

namespace LeakChecker.DataParser.Helpers.DataNormalization;

public sealed class NormalizedData(ItemEnum type, object value)
{
    public ItemEnum Type { get; private set; } = type;
    public object Value { get; private set; } = value;
}