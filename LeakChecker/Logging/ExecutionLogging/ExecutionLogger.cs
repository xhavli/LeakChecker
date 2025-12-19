using System.Globalization;
using System.Text;
using LeakChecker.Utilities.Configuration;

namespace LeakChecker.Logging.ExecutionLogging;

public class ExecutionLogger : IDisposable
{
    private readonly AppConfig _config;
    private readonly DateTime _startTime;
    private readonly StreamWriter _writer;

    public ExecutionLogger(AppConfig config)
    {
        _config = config;
        _startTime = DateTime.Now;
        
        string fileTimeStamp = $"{_startTime:yyyy-M-dTHH-mm-ss}";
        string reportFileName = $"{fileTimeStamp}.txt";
        string reportFilePath = Path.Combine(_config.LogDirectory, reportFileName);

        _writer = new StreamWriter(reportFilePath, append: true, encoding: Encoding.UTF8);
        _writer.AutoFlush = true;
        
        CreateReportHeader();
    }
    
    private async Task LogLineAsync(string message = "")
    {
        Console.WriteLine(message);
        await _writer.WriteLineAsync(message);
    }
    
    public async Task Log(string message, LogLevel level = LogLevel.Info, LogContext? context = null )
    {
        ConsoleColor consoleColor = Console.ForegroundColor;
        switch (level)
        {
            case LogLevel.Info:
                consoleColor = ConsoleColor.DarkBlue;
                break;
            case LogLevel.Warning:
                consoleColor = ConsoleColor.Yellow;
                break;
            case LogLevel.Success:
                consoleColor = ConsoleColor.Green;
                break;
            case LogLevel.Exception:
                consoleColor = ConsoleColor.Red;
                break;
            default:
                Console.ResetColor();
                break;
        }

        string log = context is null ? $"[{DateTime.Now:T}] {level.GetString()} {message}"
                                     : $"[{DateTime.Now:T}] {level.GetString()} {context.Value.GetString()} {message}";
        
        Console.ForegroundColor = consoleColor;
        await LogLineAsync(log);
        Console.ResetColor();
    }

    private void CreateReportHeader()
    {
        _writer.WriteLine("-----------------------------------------------------------");
        _writer.WriteLine($"Log folder path: {_config.LogDirectory}");
        _writer.WriteLine($"Tmp folder path: {_config.TmpDirectory}");
        _writer.WriteLine($"Input folder path: {_config.InputDirectory}");
        _writer.WriteLine($"Output folder path: {_config.OutputDirectory}");
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

        await LogLineAsync($"Records parsed: {stats.RecordsParsed:N0}");
        await LogLineAsync($"Malformed records parsed: {stats.MalformedRecordsRead:N0}");
        await LogLineAsync($"Parse accuracy (correct vs malformed): {stats.Accuracy:N2} %");

        await LogLineAsync($"Lines parsed: {stats.LinesParsed:N0}");
        await LogLineAsync($"Line speed: {stats.LineSpeed:N2} lines/second");
        
        await LogLineAsync($"Bytes parsed: {stats.BytesParsed:N0}");
        await LogLineAsync($"Byte speed: {stats.ByteSpeed:N2} bytes/second");

        await LogLineAsync($"Execution start: {stats.ExecutionStart.ToString("F", CultureInfo.InvariantCulture)}");
        await LogLineAsync($"Execution end: {stats.ExecutionEnd.ToString("F", CultureInfo.InvariantCulture)}");
        await LogLineAsync($"Execution duration: {stats.Duration}");
    }

    public void Dispose()
    {
        _writer.Flush();
        _writer.Close();
        _writer.Dispose();
    }
}