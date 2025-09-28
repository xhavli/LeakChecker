namespace LeakChecker.Logging;

public enum LogContext
{
    Main,
    Encoding,
    Format,
    Content,
    Parsing,
    PythonNerService,
    ExternalService,
}

public static class LogContextStrings
{
    private static readonly Dictionary<LogContext, string> Map = new()
    {
        { LogContext.Main, "[MAIN]" },
        { LogContext.Encoding, "[ENCODING]" },
        { LogContext.Format, "[FORMAT]" },
        { LogContext.Content, "[CONTENT]" },
        { LogContext.Parsing, "[PARSING]" },
        { LogContext.PythonNerService, "[PYTHON-NER-SERVICE]" },
        { LogContext.ExternalService, "[EXTERNAL-SERVICE]" },
    };

    public static string GetString(this LogContext context) => Map[context];
}