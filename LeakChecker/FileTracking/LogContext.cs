namespace LeakChecker.FileTracking;

public enum LogContext
{
    Encoding,
    Delimiter,
    Content,
    ExternalService,
}

public static class LogContextStrings
{
    private static readonly Dictionary<LogContext, string> Map = new()
    {
        { LogContext.Encoding, "[ENCODING]" },
        { LogContext.Delimiter, "[DELIMITER]" },
        { LogContext.Content, "[CONTENT]" },
        { LogContext.ExternalService, "[EXTERNAL-SERVICE]" }
    };

    public static string GetString(this LogContext context) => Map[context];
}