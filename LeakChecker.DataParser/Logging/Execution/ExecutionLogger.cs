using System.Globalization;
using System.Text;
using LeakChecker.DataParser.Helpers.Enums;
using LeakChecker.DataParser.Helpers.Settings;
using LeakChecker.DataParser.Stats.Execution;

namespace LeakChecker.DataParser.Logging.Execution;

public class ExecutionLogger : IDisposable
{
    public readonly DateTime ExecutionStart;
    
    private readonly StreamWriter _writer;
    private readonly object _logLock = new();
    private const ConsoleColor InfoColor = ConsoleColor.DarkBlue;
    private const ConsoleColor WarningColor = ConsoleColor.DarkYellow;
    private const ConsoleColor SuccessColor = ConsoleColor.Green;
    private const ConsoleColor FailureColor = ConsoleColor.Red;

    public ExecutionLogger(ISettings settings)
    {
        ExecutionStart = DateTime.Now;
        
        string fileTimeStamp = $"{ExecutionStart:yyyy-MM-ddTHH-mm-ss}";
        string reportFileName = $"{fileTimeStamp}.txt";
        string reportFilePath = Path.Combine(settings.LogDirectory, reportFileName);

        _writer = new StreamWriter(reportFilePath, append: true, encoding: new UTF8Encoding(encoderShouldEmitUTF8Identifier: false))
        {
            AutoFlush = true
        };
        
        CreateReportHeader(settings);
    }
    
    private void LogInternal(string message = "")
    {
        lock (_logLock)
        {
            Console.WriteLine(message);
            _writer.WriteLine(message);
        }
    }

    private void LogInternal(string message, ConsoleColor color)
    {
        lock (_logLock)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();

            _writer.WriteLine(message);
        }
    }
    
    public void Log(string message, LogLevel level = LogLevel.Info, LogContext? context = null )
    {
        ConsoleColor logColor = level switch
        {
            LogLevel.Info => InfoColor,
            LogLevel.Warning => WarningColor,
            LogLevel.Success => SuccessColor,
            LogLevel.Failure => FailureColor,
            _ => Console.ForegroundColor
        };
        
        string formattedMessage = context is null
            ? $"[{DateTime.Now:T}] {level.GetString()} {message}"
            : $"[{DateTime.Now:T}] {level.GetString()} {context.Value.GetString()} {message}";

        LogInternal(formattedMessage, logColor);
    }

    private void CreateReportHeader(ISettings settings)
    {
        _writer.WriteLine($"Execution start: {ExecutionStart.ToString("F", CultureInfo.InvariantCulture)}");
        _writer.WriteLine("-----------------------------------------------------------");
        _writer.WriteLine($"Log folder path: {settings.LogDirectory}");
        _writer.WriteLine($"Tmp folder path: {settings.TmpDirectory}");
        _writer.WriteLine($"Input folder path: {settings.InputDirectory}");
        _writer.WriteLine();
        _writer.WriteLine($"Csharp port: {settings.CsharpPort}");
        _writer.WriteLine($"Python port: {settings.PythonPort}");
        _writer.WriteLine();
        _writer.WriteLine($"Connection timeout: {settings.StartupTimeoutSeconds}");
        _writer.WriteLine();
        _writer.WriteLine($"Threads capacity: {settings.ThreadsCapacity}");
        _writer.WriteLine($"Channel capacity: {settings.ChannelCapacity}");
        _writer.WriteLine();
        _writer.WriteLine($"Schema accuracy: {settings.SchemaThreshold}");
        _writer.WriteLine();
        _writer.WriteLine(string.Equals(settings.Environment.Trim(), "Development", StringComparison.OrdinalIgnoreCase)
                          ? $"Environment: {settings.Environment} - AutoFlush = ON"
                          : $"Environment: {settings.Environment} - AutoFlush = OFF");
        _writer.WriteLine($"Verbose: {settings.Verbose}");
        _writer.WriteLine("-----------------------------------------------------------");
    }
    
    public void LogExecutionStats(ExecutionStats stats)
    {
        LogInternal();

        LogInternal($"Execution ID: {stats.ExecutionId}");
        LogInternal($"Files parsed: {stats.FilesParsed.Count}");
        foreach (var id in stats.FilesParsed)
        {
            LogInternal($"    Parse ID: {id}");
        }

        LogInternal($"Correct records parsed: {stats.RecordsParsed:N0}");
        LogInternal($"Malformed records parsed: {stats.MalformedRecordsRead:N0}");
        LogInternal($"Parse accuracy (correct vs malformed): {stats.Accuracy:N2} %");

        LogInternal($"Lines parsed: {stats.LinesParsed:N0}");
        LogInternal($"Line speed: {stats.LineSpeed:N2} lines/second");
        
        double parsedMb = (double) stats.BytesParsed / SizeEnum.MegaByte;
        double parsedGb = (double) stats.BytesParsed / SizeEnum.GigaByte;
        LogInternal($"Bytes parsed: {stats.BytesParsed:N0} B / {parsedMb:F2} MiB / {parsedGb:F2} GiB");
        LogInternal($"Byte speed: {stats.ByteSpeed:N2} bytes/second");

        LogInternal($"Execution start: {ExecutionStart.ToString("F", CultureInfo.InvariantCulture)}");
        LogInternal($"Execution ended: {stats.ExecutionEnd.ToString("F", CultureInfo.InvariantCulture)}");
        LogInternal($"Execution time: {stats.Duration}");
    }

    public void Dispose()
    {
        _writer.Flush();
        _writer.Close();
        _writer.Dispose();
    }
}