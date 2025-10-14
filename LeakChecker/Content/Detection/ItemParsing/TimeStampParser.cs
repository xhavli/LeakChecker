namespace LeakChecker.Content.Detection.ItemParsing;

public static class TimeStampParser
{
    // Set valid range
    private static readonly DateTime MinDate = new DateTime(2000, 1, 1);
    private static readonly DateTime MaxDate = DateTime.UtcNow.AddYears(10);
    
    public static bool TryParse(string token, out DateTime dateTime)
    {
        dateTime = default;
        
        if (!long.TryParse(token, out long raw) || raw < 0)
            return false;

        try
        {
            var dt = DateTimeOffset.FromUnixTimeSeconds(raw).UtcDateTime;
            if (IsInRange(dt)) { dateTime = dt; Console.WriteLine("UnixSeconds"); return true; }  // MinDate = 946684800
        }
        catch
        {
            // ignored
        }

        try
        {
            var dt = DateTimeOffset.FromUnixTimeMilliseconds(raw).UtcDateTime;
            if (IsInRange(dt)) { dateTime = dt; Console.WriteLine("UnixMilliseconds"); return true; }  // MinDate = 946684800000
        }
        catch
        {
            // ignored
        }

        try
        {
            // Try Windows FILETIME
            var dt = DateTime.FromFileTimeUtc(raw);
            if (IsInRange(dt)) { dateTime = dt; Console.WriteLine("FileTime"); return true; }  // MinDate = 125911584000000000
        }
        catch
        {
            // ignored
        }

        try
        {
            // Try .NET ticks
            var dt = new DateTime(raw, DateTimeKind.Utc);
            if (IsInRange(dt)) { dateTime = dt; Console.WriteLine(".Net ticks"); return true; }  // MinDate = 630822816000000000
        }
        catch
        {
            // ignored
        }

        return false; //TODO
        try
        {
            // Try Excel serial date (days since 1899-12-30)
            DateTime excelEpoch = new DateTime(1899, 12, 30);
            var dt = excelEpoch.AddDays(raw);
            if (IsInRange(dt)) { dateTime = dt; Console.WriteLine("Excel"); return true; }  // MinDate = 36526
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