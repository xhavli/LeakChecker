namespace LeakChecker.UI.Helpers;

public static class Formatter
{
    public static string Number(long n) => n.ToString("N0");
    
    public static string Percent(double value) => $"{value:F2}%";

    public static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max] + "…";

    public static string Duration(TimeSpan t) =>
        t.TotalHours   >= 1 ? $"{(int)t.TotalHours}h {t.Minutes}m"
        : t.TotalMinutes >= 1 ? $"{(int)t.TotalMinutes}m {t.Seconds}s"
        : $"{t.TotalSeconds:F1}s";

    public static string Bytes(long bytes)
    {
        if (bytes <= 0) return "0 B";
        string[] suffixes = ["B", "KB", "MB", "GB", "TB"];
        int i = Math.Min((int)Math.Floor(Math.Log(bytes, 1024)), suffixes.Length - 1);
        return $"{bytes / Math.Pow(1024, i):F2} {suffixes[i]}";
    }
}