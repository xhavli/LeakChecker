namespace LeakChecker.DataParser.Content.Detection.ItemParsing;

public static class TimestampParser
{
    // Set valid range
    private static readonly DateTime MinDate = new DateTime(2000, 1, 1);
    // private static readonly DateTime MinDate = new DateTime(2001, 9, 9, 1, 46, 40, DateTimeKind.Utc); // Min UnixSeconds: 1 000 000 000
    private static readonly DateTime MaxDate = DateTime.UtcNow.AddYears(10);
    
    public static bool TryParse(string token, out ItemEnum itemEnum, out DateTime dateTime)
    {
        dateTime = default;
        itemEnum = ItemEnum.Null;
        
        if (!long.TryParse(token, out long raw) || raw < 0)
            return false;

        try
        {
            var dt = DateTimeOffset.FromUnixTimeSeconds(raw).UtcDateTime;
            if (IsInRange(dt)) 
            { 
                dateTime = dt;
                itemEnum = ItemEnum.UnixSeconds;
                return true; 
            }  // MinDate = 946 684 800
        }
        catch
        {
            // ignored
        }

        try
        {
            var dt = DateTimeOffset.FromUnixTimeMilliseconds(raw).UtcDateTime;
            if (IsInRange(dt))
            {
                dateTime = dt; 
                itemEnum = ItemEnum.UnixMilliseconds;
                return true;
            }  // MinDate = 946 684 800 000
        }
        catch
        {
            // ignored
        }

        try
        {
            // Try Windows FILETIME
            var dt = DateTime.FromFileTimeUtc(raw);
            if (IsInRange(dt))
            {
                dateTime = dt;
                itemEnum = ItemEnum.FileTime;
                return true;
            }  // MinDate = 125 911 584 000 000 000
        }
        catch
        {
            // ignored
        }

        try
        {
            // Try .NET ticks
            var dt = new DateTime(raw, DateTimeKind.Utc);
            if (IsInRange(dt))
            {
                dateTime = dt;
                itemEnum = ItemEnum.NetTicks;
                return true;
            }  // MinDate = 630 822 816 000 000 000
        }
        catch
        {
            // ignored
        }

        return false;
    }
    
    private static bool IsInRange(DateTime dt) =>
        dt >= MinDate && dt <= MaxDate;
}