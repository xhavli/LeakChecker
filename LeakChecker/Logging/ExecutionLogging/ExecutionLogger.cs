using System.Globalization;
using System.Text;
using LeakChecker.Utilities;

namespace LeakChecker.Logging.ExecutionLogging;

public class ExecutionLogger
{
    private readonly DateTime _startTime;
    private readonly StreamWriter _writer;
    private readonly AppConfig _config;

    public ExecutionLogger(AppConfig config)
    {
        _startTime = DateTime.Now;
        _config = config;
        
        string nameTimeStamp = $"{_startTime:yyyy-M-dTHH-mm-ss}";
        string reportFileName = $"{nameTimeStamp}.txt";
        string reportFilePath = Path.Combine(_config.LogDirectory, reportFileName);

        _writer = new StreamWriter(reportFilePath, append: true, encoding: Encoding.UTF8);
        _writer.AutoFlush = true;
        
        CreateReportHeader();
    }
    
    private async Task LogAsync(string message)
    {
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
        await LogAsync(log);
        Console.ForegroundColor = consoleColor;
        Console.WriteLine(log);
        Console.ResetColor();
    }

    public async Task LogFileInfo(string filePath)
    {
        FileInfo fileInfo = new FileInfo(filePath);
        
        long sizeInBytes = fileInfo.Length;
        double sizeMB = sizeInBytes / (1024.0 * 1024);

        await LogAsync("----------------------------------------------------");
        await LogAsync($"{DateTime.Now:T}");
        await LogAsync($"File path: {fileInfo.FullName}");
        await LogAsync($"File size: {sizeMB:F2} MB / {sizeInBytes:N0} bytes ");
        await LogAsync("----------------------------------------------------");
        await LogAsync("");
    }
    
    private void CreateReportHeader()
    {
        string timeStamp = _startTime.ToString("F", CultureInfo.InvariantCulture);
        _writer.WriteLine($"Execution started at: {timeStamp}");
        _writer.WriteLine("-----------------------------------------------------------");
        _writer.WriteLine($"Log folder path: {_config.LogDirectory}");
        _writer.WriteLine($"Tmp folder path: {_config.TmpDirectory}");
        _writer.WriteLine($"Input folder path: {_config.InputDirectory}");
        _writer.WriteLine($"Output folder path: {_config.OutputDirectory}");
        _writer.WriteLine("-----------------------------------------------------------");
    }
}