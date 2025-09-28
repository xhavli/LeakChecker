namespace LeakChecker.ContentDetection.ItemParsing;

public static class TimeStampParser
{
    public static bool TryParse(string token, out DateTime dateTime)
    {
        dateTime = default;

        if (!long.TryParse(token, out long raw) || raw < 0)
            return false;

        // Set valid range
        DateTime minDate = new DateTime(2000, 1, 1);
        DateTime maxDate = DateTime.UtcNow;

        bool IsInRange(DateTime dt) =>
            dt >= minDate && dt <= maxDate;

        try
        {
            var dt = DateTimeOffset.FromUnixTimeSeconds(raw).UtcDateTime;
            if (IsInRange(dt)) { dateTime = dt; return true; }
        }
        catch
        {
            // ignored
        }

        try
        {
            var dt = DateTimeOffset.FromUnixTimeMilliseconds(raw).UtcDateTime;
            if (IsInRange(dt)) { dateTime = dt; return true; }
        }
        catch
        {
            // ignored
        }

        try
        {
            // Try Windows FILETIME
            var dt = DateTime.FromFileTimeUtc(raw);
            if (IsInRange(dt)) { dateTime = dt; return true; }
        }
        catch
        {
            // ignored
        }

        try
        {
            // Try .NET ticks
            var dt = new DateTime(raw, DateTimeKind.Utc);
            if (IsInRange(dt)) { dateTime = dt; return true; }
        }
        catch
        {
            // ignored
        }

        try
        {
            // Try Excel serial date (days since 1899-12-30)
            DateTime excelEpoch = new DateTime(1899, 12, 30);
            var dt = excelEpoch.AddDays(raw);
            if (IsInRange(dt)) { dateTime = dt; return true; }
        }
        catch
        {
            // ignored
        }

        return false;
    }
}