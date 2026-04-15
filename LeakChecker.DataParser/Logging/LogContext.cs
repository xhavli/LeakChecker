namespace LeakChecker.DataParser.Logging;

public enum LogContext
{
    Program,
    Orchestrator,
    Encoding,
    Delimiter,
    Content,
    Parsing,
    PythonNerService,
    ExternalService,
}

public static class LogContextStrings
{
    private static readonly Dictionary<LogContext, string> Map = new()
    {
        { LogContext.Program, "[PROGRAM]" },
        { LogContext.Orchestrator, "[ORCHESTRATOR]" },
        { LogContext.Encoding, "[ENCODING]" },
        { LogContext.Delimiter, "[DELIMITER]" },
        { LogContext.Content, "[CONTENT]" },
        { LogContext.Parsing, "[PARSING]" },
        { LogContext.PythonNerService, "[PYTHON_NER_SERVICE]" },
        { LogContext.ExternalService, "[EXTERNAL_SERVICE]" },
    };

    public static string GetString(this LogContext context) => Map[context];
}