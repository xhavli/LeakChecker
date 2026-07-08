using LeakChecker.Common.Enums;

namespace LeakChecker.DataParser.Helpers.DataNormalization;

public sealed class NormalizedData(ItemType type, object value)
{
    public ItemType Type { get; private set; } = type;
    public object Value { get; private set; } = value;
}