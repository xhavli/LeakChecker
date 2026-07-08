using LeakChecker.Common.Enums;
using LeakChecker.DataParser.Content.Detection.ItemParsing;
using LeakChecker.DataParser.Helpers.Extensions;

namespace LeakChecker.DataParser.Helpers.DataNormalization;

public static class DataNormalizer
{
    public static NormalizedData NormalizeData(ItemType type, string value)
    {
        switch (type)
        {
            case ItemType.FileTime:
            case ItemType.NetTicks:
            case ItemType.Timestamp:
            case ItemType.UnixSeconds:
            case ItemType.UnixMilliseconds:
                return TimestampParser.NormalizeTimestamp(type, value);
            
            case ItemType.Iban:
            case ItemType.PhoneNumber:
                return KeepTypeRemoveWhitespaces(type, value);
            
            default:
                return new NormalizedData(type, value);
        }
    }
    
    private static NormalizedData KeepTypeRemoveWhitespaces(ItemType type, string value)
    {
        return new NormalizedData(type, value.RemoveWhitespaces());
    }
}