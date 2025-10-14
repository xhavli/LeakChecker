using System.Diagnostics;
using LeakChecker.Logging;
using LeakChecker.Logging.ExecutionLogging;

namespace LeakChecker.Content.Detection.RecognitionService;

public class PythonNerService(ExecutionLogger logger)
{
    private Process? _process = new();

    public async Task Start()
    {
        await logger.Log("PythonNerService: Starting...");
        
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = "main.py",  //TODO relative path
                UseShellExecute = true,
                CreateNoWindow = false
            };
            
            _process = new Process();
            _process.StartInfo = psi;
            
            _process.Start();
        }
        catch (Exception e)
        {
            await logger.Log($"PythonNerService: Failure at startup. {e.Message}", LogLevel.Exception, LogContext.PythonNerService); 
            throw;
        }
        
        await logger.Log("PythonNerService: Started");
    }

    public async Task WaitForStart(int attempts = 10, int timeout = 5_000)
    {
        using HttpClient client = new();
        while (attempts > 0)
        {
            try
            {
                    string statusUri = "http://localhost:8000/status";
                    string status = await client.GetStringAsync(statusUri);
                    if (status.Equals("ready", StringComparison.OrdinalIgnoreCase)) break;
                        
            }
            catch (Exception e)
            {
                    await logger.Log($"PythonNerService: Waiting. Attempts remaining {attempts}, timeout {timeout} milliseconds", LogLevel.Warning);
                    await Task.Delay(timeout);
                    attempts--;
            }
        }

        if (attempts == 0)
        {
            string exMessage = "PythonNerService: Failed on startup";
            await logger.Log(exMessage, LogLevel.Exception, LogContext.PythonNerService);
            throw new Exception(exMessage);
        }
        
        await logger.Log("PythonNerService: Ready", LogLevel.Success, LogContext.PythonNerService);
    }
    
    public async Task Stop()
    {
        try
        {
            if (_process is { HasExited: false })
            {
                await logger.Log("PythonNerService: sending kill");
                _process.Kill(entireProcessTree: true);
                await _process.WaitForExitAsync();
                _process.Dispose();
                await logger.Log("PythonNerService: stopped");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}