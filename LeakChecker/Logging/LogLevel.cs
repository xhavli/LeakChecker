namespace LeakChecker.Logging;

public enum LogLevel
{
    Info,
    Success,
    Warning,
    Exception
}

public static class LogLevelStrings
{
    private static readonly Dictionary<LogLevel, string> Map = new()
    {
        { LogLevel.Info, "[INFO]" },
        { LogLevel.Success, "[SUCCESS]" },
        { LogLevel.Warning, "[WARNING]" },
        { LogLevel.Exception, "[EXCEPTION]" }
    };

    public static string GetString(this LogLevel level) => Map[level];
}
