using LeakChecker.DataParser.Content;
using LeakChecker.DataParser.Content.Detection.ItemParsing;

namespace LeakChecker.DataParser.Helpers.DataNormalization;

public static class DataNormalizer
{
    public static NormalizedData NormalizeData(ItemEnum type, string value)
    {
        switch (type)
        {
            case ItemEnum.FileTime:
            case ItemEnum.NetTicks:
            case ItemEnum.Timestamp:
            case ItemEnum.UnixSeconds:
            case ItemEnum.UnixMilliseconds:
                return TimestampParser.NormalizeTimestamp(type, value);
            
            default:
                return new NormalizedData(type, value);
        }
    }
}