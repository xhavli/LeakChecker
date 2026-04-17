namespace LeakChecker.DataParser.Logging;

public enum LogContext
{
    Program,
    Execution,
    Encoding,
    Delimiter,
    Content,
    Parsing,
    ExternalService,
    PythonNerService,
}

public static class LogContextStrings
{
    private static readonly Dictionary<LogContext, string> Map = new()
    {
        { LogContext.Program, "[PROGRAM]" },
        { LogContext.Execution, "[EXECUTION]" },
        { LogContext.Encoding, "[ENCODING]" },
        { LogContext.Delimiter, "[DELIMITER]" },
        { LogContext.Content, "[CONTENT]" },
        { LogContext.Parsing, "[PARSING]" },
        { LogContext.ExternalService, "[EXTERNAL_SERVICE]" },
        { LogContext.PythonNerService, "[PYTHON_NER_SERVICE]" },
    };

    public static string GetString(this LogContext context) => Map[context];
}