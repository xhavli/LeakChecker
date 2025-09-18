using System.Diagnostics;
using LeakChecker.Logging;
using LeakChecker.Logging.ExecutionLogging;

namespace LeakChecker.ContentDetection.RecognitionService;

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

    public async Task WaitForStart()
    {
        using HttpClient client = new();
        try
        {
            while (true)
            {

                string statusUri = "http://localhost:8000/status";
                string status = await client.GetStringAsync(statusUri);
                if (status.Equals("ready", StringComparison.OrdinalIgnoreCase)) break;
                    
                await logger.Log($"PythonNerService: Waiting. Status: {status}", LogLevel.Warning);
                await Task.Delay(5_000);    //5 sec
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
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