using System.Diagnostics;
using System.Net;
using LeakChecker.DataParser.Logging;
using LeakChecker.DataParser.Logging.Execution;

namespace LeakChecker.DataParser.Content.Detection.RecognitionService;

public class PythonNerService(ExecutionLogger logger)
{
    private Process? _process;

    public async Task Start(string pythonPath, string pythonArgs)
    {
        await logger.Log("PythonNerService: Starting...");
        
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = pythonPath,
                Arguments = "main.py",
                UseShellExecute = true,
                CreateNoWindow = false
            };
            
            _process = new Process();
            _process.StartInfo = psi;
            
            _process.Start();
        }
        catch (Exception e)
        {
            await logger.Log($"PythonNerService: Failure at startup. {e.Message}", LogLevel.Failure, LogContext.PythonNerService); 
            throw;
        }
        
        await logger.Log("PythonNerService: Started");
    }
    
    public async Task WaitForStart(int csharpPort, int pythonPort, int timeoutSeconds)
    {
        await logger.Log($"Sending status check. CsharpPort {csharpPort}, PythonPort {pythonPort}, " +
                         $"timeout {timeoutSeconds} seconds", LogLevel.Info, LogContext.PythonNerService);

        
        // Try to contact Python status endpoint if its already running
        try
        {
            using HttpClient client = new();
            string statusUrl = $"http://localhost:{pythonPort}/status";
            string status = await client.GetStringAsync(statusUrl);

            if (status.Trim().Equals("ready", StringComparison.OrdinalIgnoreCase))
            {
                await logger.Log("Received READY via status endpoint", LogLevel.Success, LogContext.PythonNerService);
                return;
            }
        }
        catch (TaskCanceledException e) when (!e.CancellationToken.IsCancellationRequested)
        {
            // ignored - Python not running yet
        }
        
        await logger.Log("Waiting for READY notification", LogLevel.Warning, LogContext.PythonNerService);

        // Wait for Python start and send ready
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        using var listener = new HttpListener();
        listener.Prefixes.Add($"http://localhost:{csharpPort}/");
        listener.Start();
        while (!cts.IsCancellationRequested)
        {
            try
            {
                var context = await listener.GetContextAsync();

                using var reader = new StreamReader(context.Request.InputStream);
                string body = await reader.ReadToEndAsync(cts.Token);

                if (body.Trim().Equals("ready", StringComparison.OrdinalIgnoreCase))
                {
                    await logger.Log("Received READY via startup notification", LogLevel.Success, LogContext.PythonNerService);
                    return; // exit immediately, no response required
                }

                // Ignore anything else and continue listening
            }
            catch (OperationCanceledException)
            {
                break; // timeout
            }
        }

        await logger.Log("Waiting for READY signal: Timed out", LogLevel.Failure, LogContext.PythonNerService);
        throw new TimeoutException("Waiting for READY signal: Timed out.");
    }

    public async Task Stop()
    {
        if(_process == null) return;
        
        await logger.Log("Terminating Python subprocess", LogLevel.Info, LogContext.PythonNerService);
        
        try
        {
            if (_process is { HasExited: false })
            {
                await logger.Log("Python subprocess: Sending kill", LogLevel.Info, LogContext.PythonNerService);
                _process.Kill(entireProcessTree: true);
                await _process.WaitForExitAsync();
                _process.Dispose();
                await logger.Log("Python subprocess: Stopped successfully", LogLevel.Info, LogContext.PythonNerService);
            }
        }
        catch (Exception e)
        {
            await logger.Log(e.ToString(), LogLevel.Failure, LogContext.PythonNerService);
        }
    }
}