namespace LeakChecker.FileTracking;

public enum LogLevel
{
    Info,
    Warning,
    Success,
    Exception
}

public static class LogLevelStrings
{
    private static readonly Dictionary<LogLevel, string> Map = new()
    {
        { LogLevel.Info, "[INFO]" },
        { LogLevel.Warning, "[WARNING]" },
        { LogLevel.Success, "[SUCCESS]" },
        { LogLevel.Exception, "[EXCEPTION]" }
    };

    public static string GetString(this LogLevel level) => Map[level];
}
