namespace LeakChecker.Logging;

public enum LogContext
{
    Main,
    Encoding,
    Delimiter,
    Content,
    PythonService,
    ExternalService,
}

public static class LogContextStrings
{
    private static readonly Dictionary<LogContext, string> Map = new()
    {
        { LogContext.Main, "[MAIN]" },
        { LogContext.Encoding, "[ENCODING]" },
        { LogContext.Delimiter, "[DELIMITER]" },
        { LogContext.Content, "[CONTENT]" },
        { LogContext.PythonService, "[PYTHON-SERVICE]" },
        { LogContext.ExternalService, "[EXTERNAL-SERVICE]" },
    };

    public static string GetString(this LogContext context) => Map[context];
}