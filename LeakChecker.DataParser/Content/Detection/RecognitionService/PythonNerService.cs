using System.Diagnostics;
using System.Net;
using LeakChecker.DataParser.Logging;
using LeakChecker.DataParser.Logging.Execution;
using LeakChecker.DataParser.Utilities.Configuration;

namespace LeakChecker.DataParser.Content.Detection.RecognitionService;

public class PythonNerService(AppConfig config, ExecutionLogger logger)
{
    private Process? _process;

    public async Task Start()
    {
        await logger.Log("Start", LogLevel.Info, LogContext.PythonNerService);

        try
        {
            string projectDir = Directory.GetParent(Directory.GetCurrentDirectory())?.Parent?.Parent?.Parent?.FullName!;
            string pythonDir = Path.Combine(projectDir, "LeakChecker.DataParser/Content/Detection/RecognitionService/Python");
            
            string pythonExe = Path.Combine(pythonDir, config.PythonVenvPath);
            string scriptPath = Path.Combine(pythonDir, config.PythonScriptName);

            if (!File.Exists(pythonExe))
                throw new FileNotFoundException($"Python .venv not found: {pythonExe}");

            if (!File.Exists(scriptPath))
                throw new FileNotFoundException($"Script not found: {scriptPath}");

            var psi = new ProcessStartInfo
            {
                FileName = pythonExe,
                Arguments = $"\"{scriptPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            _process = new Process { StartInfo = psi };
            _process.Start();
        }
        catch (Exception e)
        {
            await logger.Log($"Failure at startup. {e.Message}", LogLevel.Failure, LogContext.PythonNerService);
            throw;
        }

        await logger.Log("Started", LogLevel.Info, LogContext.PythonNerService);
    }
    
    public async Task WaitStart()
    {
        await logger.Log($"CsharpPort {config.CsharpPort} sending status check to PythonPort {config.PythonPort} with " +
                         $"timeout {config.StartupTimeoutSeconds} seconds.", LogLevel.Info, LogContext.PythonNerService);
        
        // Try to contact Python status endpoint if its already running
        try
        {
            using HttpClient client = new();
            string statusUrl = $"http://localhost:{config.PythonPort}/status";
            string status = await client.GetStringAsync(statusUrl);

            if (status.Trim().Equals("ready", StringComparison.OrdinalIgnoreCase))
            {
                await logger.Log("Received READY via status endpoint.", LogLevel.Success, LogContext.PythonNerService);
                return;
            }
        }
        catch (HttpRequestException)
        {
            // ignored - Python not running yet
        }
        
        // Wait for Python start and send ready to C#
        await logger.Log("Waiting for READY notification.", LogLevel.Warning, LogContext.PythonNerService);
        
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(config.StartupTimeoutSeconds));
        using var listener = new HttpListener();
        listener.Prefixes.Add($"http://localhost:{config.CsharpPort}/");
        listener.Start();

        try
        {
            while (true)
            {
                var acceptTask = listener.GetContextAsync();
                var completed = await Task.WhenAny(acceptTask, Task.Delay(Timeout.Infinite, cts.Token));

                if (completed != acceptTask)
                    throw new TimeoutException("Waiting for READY signal: Timed out.");

                var context = await acceptTask;

                using var reader = new StreamReader(context.Request.InputStream);
                var body = await reader.ReadToEndAsync(cts.Token);

                if (body.Trim().Equals("ready", StringComparison.OrdinalIgnoreCase))
                {
                    await logger.Log("Received READY via startup notification.", LogLevel.Success, LogContext.PythonNerService);
                    return;
                }
            }
        }
        finally
        {
            listener.Stop();
        }
    }

    public async Task Stop()
    {
        if(_process == null) return;
        
        await logger.Log("Terminate", LogLevel.Info, LogContext.PythonNerService);
        
        try
        {
            if (_process is { HasExited: false })
            {
                await logger.Log("Sending kill", LogLevel.Info, LogContext.PythonNerService);
                _process.Kill(entireProcessTree: true);
                await _process.WaitForExitAsync();
                _process.Dispose();
                await logger.Log("Terminated", LogLevel.Info, LogContext.PythonNerService);
            }
        }
        catch (Exception e)
        {
            await logger.Log(e.ToString(), LogLevel.Failure, LogContext.PythonNerService);
        }
    }
}