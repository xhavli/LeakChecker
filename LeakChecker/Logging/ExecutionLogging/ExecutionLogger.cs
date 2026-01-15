using System.Globalization;
using System.Text;
using LeakChecker.Utilities;
using LeakChecker.Utilities.Configuration;

namespace LeakChecker.Logging.ExecutionLogging;

public class ExecutionLogger : IDisposable
{
    public readonly DateTime ExecutionStart;
    private bool Verbose { get; }
    private readonly StreamWriter _writer;
    private const ConsoleColor InfoColor = ConsoleColor.DarkBlue;
    private const ConsoleColor WarningColor = ConsoleColor.DarkYellow;
    private const ConsoleColor SuccessColor = ConsoleColor.Green;
    private const ConsoleColor ExceptionColor = ConsoleColor.Red;

    public ExecutionLogger(AppConfig config)
    {
        ExecutionStart = DateTime.Now;
        
        string fileTimeStamp = $"{ExecutionStart:yyyy-M-dTHH-mm-ss}";
        string reportFileName = $"{fileTimeStamp}.txt";
        string reportFilePath = Path.Combine(config.LogDirectory, reportFileName);

        _writer = new StreamWriter(reportFilePath, append: true, encoding: Encoding.UTF8);
        _writer.AutoFlush = true;
        
        Verbose = config.Verbose;
        
        CreateReportHeader(config);
    }
    
    private async Task LogLineAsync(string message = "")
    {
        if (Verbose) Console.WriteLine(message);
        await _writer.WriteLineAsync(message);
    }
    
    public async Task Log(string message, LogLevel level = LogLevel.Info, LogContext? context = null )
    {
        ConsoleColor consoleColor = Console.ForegroundColor;
        switch (level)
        {
            case LogLevel.Info:
                consoleColor = InfoColor;
                break;
            case LogLevel.Warning:
                consoleColor = WarningColor;
                break;
            case LogLevel.Success:
                consoleColor = SuccessColor;
                break;
            case LogLevel.Failure:
                consoleColor = ExceptionColor;
                break;
            default:
                Console.ResetColor();
                break;
        }

        
        Console.ForegroundColor = consoleColor;
        await LogLineAsync(context is null ? $"[{DateTime.Now:T}] {level.GetString()} {message}"
                                           : $"[{DateTime.Now:T}] {level.GetString()} {context.Value.GetString()} {message}");
        Console.ResetColor();
    }

    private void CreateReportHeader(AppConfig config)
    {
        _writer.WriteLine($"Execution start: {ExecutionStart.ToString("F", CultureInfo.InvariantCulture)}");
        _writer.WriteLine("-----------------------------------------------------------");
        _writer.WriteLine($"Log folder path: {config.LogDirectory}");
        _writer.WriteLine($"Tmp folder path: {config.TmpDirectory}");
        _writer.WriteLine($"Input folder path: {config.InputDirectory}");
        _writer.WriteLine($"Output folder path: {config.OutputDirectory}");
        _writer.WriteLine();
        _writer.WriteLine($"Csharp port: {config.CsharpPort}");
        _writer.WriteLine($"Python port: {config.PythonPort}");
        _writer.WriteLine();
        _writer.WriteLine($"Connection timeout: {config.ConnectionTimeout}");
        _writer.WriteLine();
        _writer.WriteLine($"Threads capacity: {config.ThreadsCapacity}");
        _writer.WriteLine($"Channel capacity: {config.ChannelCapacity}");
        _writer.WriteLine();
        _writer.WriteLine($"Schema accuracy: {config.SchemaThreshold}");
        _writer.WriteLine();
        _writer.WriteLine(string.Equals(config.Environment?.Trim(), "Development", StringComparison.OrdinalIgnoreCase)
                          ? $"Environment: {config.Environment} - AutoFlush = ON"
                          : $"Environment: {config.Environment} - AutoFlush = OFF");
        _writer.WriteLine($"Verbose: {config.Verbose}");
        _writer.WriteLine("-----------------------------------------------------------");
    }
    
    public async Task LogExecutionStats(ExecutionStats stats)
    {
        await LogLineAsync();

        await LogLineAsync($"Execution ID: {stats.ExecutionId}");
        await LogLineAsync($"Files parsed: {stats.FilesParsed.Count}");
        foreach (var id in stats.FilesParsed)
        {
            await LogLineAsync($"    Parsing ID: {id}");
        }

        await LogLineAsync($"Correct records parsed: {stats.RecordsParsed:N0}");
        await LogLineAsync($"Malformed records parsed: {stats.MalformedRecordsRead:N0}");
        await LogLineAsync($"Parse accuracy (correct vs malformed): {stats.Accuracy:N2} %");

        await LogLineAsync($"Lines parsed: {stats.LinesParsed:N0}");
        await LogLineAsync($"Line speed: {stats.LineSpeed:N2} lines/second");
        
        double parsedMb = (double) stats.BytesParsed / SizeEnum.MegaByte;
        double parsedGb = (double) stats.BytesParsed / SizeEnum.GigaByte;
        await LogLineAsync($"Bytes parsed: {stats.BytesParsed:N0} B / {parsedMb:F2} MiB / {parsedGb:F2} GiB");
        await LogLineAsync($"Byte speed: {stats.ByteSpeed:N2} bytes/second");

        await LogLineAsync($"Execution start: {ExecutionStart.ToString("F", CultureInfo.InvariantCulture)}");
        await LogLineAsync($"Execution ended: {stats.ExecutionEnd.ToString("F", CultureInfo.InvariantCulture)}");
        await LogLineAsync($"Execution time: {stats.Duration}");
    }

    public void Dispose()
    {
        _writer.Flush();
        _writer.Close();
        _writer.Dispose();
    }
}