using System.Globalization;
using LeakChecker.Common.Enums;
using LeakChecker.DataParser.Helpers.DataNormalization;

namespace LeakChecker.DataParser.Content.Detection.ItemParsing;

public static class TimestampParser
{
    // Set valid range      INFO: MinDate = new (2001, 9, 9, 1, 46, 40, DateTimeKind.Utc); MinUnixSeconds = 1 000 000 000
    private static readonly DateTime MinDate = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime MaxDate = DateTime.UtcNow.AddYears(10);

    // Precomputed boundaries
    private static readonly long MinUnixSeconds = new DateTimeOffset(MinDate).ToUnixTimeSeconds();
    private static readonly long MaxUnixSeconds = new DateTimeOffset(MaxDate).ToUnixTimeSeconds();

    private static readonly long MinUnixMilliseconds = new DateTimeOffset(MinDate).ToUnixTimeMilliseconds();
    private static readonly long MaxUnixMilliseconds = new DateTimeOffset(MaxDate).ToUnixTimeMilliseconds();

    private static readonly long MinFileTime = MinDate.ToFileTimeUtc();
    private static readonly long MaxFileTime = MaxDate.ToFileTimeUtc();

    private static readonly long MinTicks = MinDate.Ticks;
    private static readonly long MaxTicks = MaxDate.Ticks;

    private const DateTimeStyles Styles = 
        DateTimeStyles.AllowWhiteSpaces |
        DateTimeStyles.AssumeUniversal |
        DateTimeStyles.AdjustToUniversal;

    public static bool TryParse(string token, out ItemType itemType, out DateTime dateTime)
    {
        itemType = ItemType.Null;
        dateTime = default;

        if (!long.TryParse(token, out var raw) || raw < 0)
            return false;

        if (raw >= MinUnixSeconds && raw <= MaxUnixSeconds)
        {
            dateTime = DateTimeOffset.FromUnixTimeSeconds(raw).UtcDateTime;
            itemType = ItemType.UnixSeconds;
            return true;
        }

        if (raw >= MinUnixMilliseconds && raw <= MaxUnixMilliseconds)
        {
            dateTime = DateTimeOffset.FromUnixTimeMilliseconds(raw).UtcDateTime;
            itemType = ItemType.UnixMilliseconds;
            return true;
        }

        if (raw >= MinFileTime && raw <= MaxFileTime)
        {
            dateTime = DateTime.FromFileTimeUtc(raw);
            itemType = ItemType.FileTime;
            return true;
        }

        if (raw >= MinTicks && raw <= MaxTicks)
        {
            dateTime = new DateTime(raw, DateTimeKind.Utc);
            itemType = ItemType.NetTicks;
            return true;
        }

        return false;
    }
    
    /// <summary>
    /// Normalize ItemEnum: Timestamp, UnixSeconds, UnixMilliseconds, NetTicks, FileTime to
    /// ItemEnum.Timestamp and convert string value to C# DateTime.
    /// Others keep original type and original string value.
    /// </summary>
    /// <param name="type">ItemEnum</param>
    /// <param name="value">String</param>
    /// <returns>Normalized or original type and value</returns>
    public static NormalizedData NormalizeTimestamp(ItemType type, string value)
    {
        return type switch
        {
            ItemType.UnixSeconds when long.TryParse(value, out var us) =>
                new NormalizedData(ItemType.Timestamp, DateTimeOffset.FromUnixTimeSeconds(us).UtcDateTime),

            ItemType.UnixMilliseconds when long.TryParse(value, out var ums) =>
                new NormalizedData(ItemType.Timestamp, DateTimeOffset.FromUnixTimeMilliseconds(ums).UtcDateTime),

            ItemType.FileTime when long.TryParse(value, out var ft) =>
                new NormalizedData(ItemType.Timestamp, DateTime.FromFileTimeUtc(ft)),

            ItemType.NetTicks when long.TryParse(value, out var nt) =>
                new NormalizedData(ItemType.Timestamp, new DateTime(nt, DateTimeKind.Utc)),

            ItemType.Timestamp when DateTime.TryParse(value, CultureInfo.InvariantCulture, Styles, out var ts) =>
                new NormalizedData(ItemType.Timestamp, ts),

            _ => new NormalizedData(type, value)
        };
    }
}