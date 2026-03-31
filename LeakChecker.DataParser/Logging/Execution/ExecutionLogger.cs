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
    private readonly SemaphoreSlim _lock = new(1, 1);
    private const ConsoleColor InfoColor = ConsoleColor.DarkBlue;
    private const ConsoleColor WarningColor = ConsoleColor.DarkYellow;
    private const ConsoleColor SuccessColor = ConsoleColor.Green;
    private const ConsoleColor ExceptionColor = ConsoleColor.Red;

    public ExecutionLogger(ISettings settings)
    {
        ExecutionStart = DateTime.Now;
        
        string fileTimeStamp = $"{ExecutionStart:yyyy-M-dTHH-mm-ss}";
        string reportFileName = $"{fileTimeStamp}.txt";
        string reportFilePath = Path.Combine(settings.LogDirectory, reportFileName);

        _writer = new StreamWriter(reportFilePath, append: true, encoding: new UTF8Encoding(encoderShouldEmitUTF8Identifier: false))
        {
            AutoFlush = true
        };
        
        CreateReportHeader(settings);
    }
    
    private async Task WriteLineAsync(string message = "")
    {
        Console.WriteLine(message);

        await _lock.WaitAsync();
        try
        {
            await _writer.WriteLineAsync(message);
        }
        finally
        {
            _lock.Release();
        }
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
        await WriteLineAsync(context is null ? $"[{DateTime.Now:T}] {level.GetString()} {message}"
                                           : $"[{DateTime.Now:T}] {level.GetString()} {context.Value.GetString()} {message}");
        Console.ResetColor();
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
    
    public async Task LogExecutionStats(ExecutionStats stats)
    {
        await WriteLineAsync();

        await WriteLineAsync($"Execution ID: {stats.ExecutionId}");
        await WriteLineAsync($"Files parsed: {stats.FilesParsed.Count}");
        foreach (var id in stats.FilesParsed)
        {
            await WriteLineAsync($"    Parsing ID: {id}");
        }

        await WriteLineAsync($"Correct records parsed: {stats.RecordsParsed:N0}");
        await WriteLineAsync($"Malformed records parsed: {stats.MalformedRecordsRead:N0}");
        await WriteLineAsync($"Parse accuracy (correct vs malformed): {stats.Accuracy:N2} %");

        await WriteLineAsync($"Lines parsed: {stats.LinesParsed:N0}");
        await WriteLineAsync($"Line speed: {stats.LineSpeed:N2} lines/second");
        
        double parsedMb = (double) stats.BytesParsed / SizeEnum.MegaByte;
        double parsedGb = (double) stats.BytesParsed / SizeEnum.GigaByte;
        await WriteLineAsync($"Bytes parsed: {stats.BytesParsed:N0} B / {parsedMb:F2} MiB / {parsedGb:F2} GiB");
        await WriteLineAsync($"Byte speed: {stats.ByteSpeed:N2} bytes/second");

        await WriteLineAsync($"Execution start: {ExecutionStart.ToString("F", CultureInfo.InvariantCulture)}");
        await WriteLineAsync($"Execution ended: {stats.ExecutionEnd.ToString("F", CultureInfo.InvariantCulture)}");
        await WriteLineAsync($"Execution time: {stats.Duration}");
    }

    public void Dispose()
    {
        _writer.Flush();
        _writer.Close();
        _writer.Dispose();
    }
}