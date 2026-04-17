using System.Diagnostics;
using System.Net;
using LeakChecker.DataParser.Helpers.Settings;
using LeakChecker.DataParser.Logging;
using LeakChecker.DataParser.Logging.Execution;

namespace LeakChecker.DataParser.Content.Detection.RecognitionService;

public class PythonNerService(ISettings settings, ExecutionLogger logger)
{
    private Process? _process;

    public async Task Start()
    {
        if (await ServiceIsRunning())
            return;
        
        logger.Log("Start", LogLevel.Info, LogContext.PythonNerService);
        
        try
        {
            string pythonDir = Path.Combine(settings.ProjectDirectory, "LeakChecker.DataParser/Content/Detection/RecognitionService/Python");
            
            string pythonExe = Path.Combine(pythonDir, settings.PythonVenvPath);
            string scriptPath = Path.Combine(pythonDir, settings.PythonScriptName);

            if (!File.Exists(pythonExe))
                throw new FileNotFoundException($"Python .venv not found: {pythonExe}");

            if (!File.Exists(scriptPath))
                throw new FileNotFoundException($"Script not found: {scriptPath}");

            var psi = new ProcessStartInfo
            {
                FileName = pythonExe,
                Arguments = $"\"{scriptPath}\" --python-port {settings.PythonPort} --csharp-port {settings.CsharpPort}",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            _process = new Process { StartInfo = psi };
            _process.Start();
        }
        catch (Exception e)
        {
            logger.Log($"Failure at startup. {e.Message}", LogLevel.Failure, LogContext.PythonNerService);
            throw;
        }

        logger.Log("Started", LogLevel.Info, LogContext.PythonNerService);
    }
    
    public async Task WaitStart()
    {
        // Try to contact Python status endpoint from C#, if its already running
        if (await ServiceIsRunning())
            return;
        
        // Wait for Python start and send ready to C#
        await WaitForReady();

        if (!await ServiceIsRunning())
        {
            const int waitSec = 10;
            logger.Log($"Python service API still not reachable. Waiting extra {waitSec} seconds.",
                LogLevel.Warning, LogContext.PythonNerService);
            await Task.WhenAny(Task.Delay(TimeSpan.FromSeconds(waitSec)));
        }
    }

    public async Task Stop()
    {
        if(_process == null)
            return;
        
        logger.Log("Terminate", LogLevel.Info, LogContext.PythonNerService);
        
        try
        {
            if (_process is { HasExited: false })
            {
                logger.Log("Sending kill", LogLevel.Info, LogContext.PythonNerService);
                _process.Kill(entireProcessTree: true);
                await _process.WaitForExitAsync();
                _process.Dispose();
                logger.Log("Terminated", LogLevel.Info, LogContext.PythonNerService);
            }
        }
        catch (Exception e)
        {
            logger.Log(e.ToString(), LogLevel.Failure, LogContext.PythonNerService);
        }
    }

    private async Task<bool> ServiceIsRunning()
    {
        logger.Log($"CsharpPort {settings.CsharpPort} sending status check to PythonPort {settings.PythonPort}",
            LogLevel.Info, LogContext.PythonNerService);
        
        try
        {
            using HttpClient client = new();
            string statusUrl = $"http://localhost:{settings.PythonPort}/status";
            string status = await client.GetStringAsync(statusUrl);

            if (status.Trim().Equals("ready", StringComparison.OrdinalIgnoreCase))
            {
                logger.Log("Received READY via status endpoint.", LogLevel.Success, LogContext.PythonNerService);
                return true;
            }
        }
        catch (HttpRequestException)
        {
            // ignored - Python not running yet
        }
        
        return false;
    }

    private async Task WaitForReady()
    {
        logger.Log($"Waiting for READY notification with timeout {settings.StartupTimeoutSeconds} seconds.",
            LogLevel.Warning, LogContext.PythonNerService);
        
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(settings.StartupTimeoutSeconds));
        using var listener = new HttpListener();
        listener.Prefixes.Add($"http://localhost:{settings.CsharpPort}/");
        listener.Start();

        try
        {
            while (true)
            {
                var acceptTask = listener.GetContextAsync();
                var completed = await Task.WhenAny(acceptTask, Task.Delay(Timeout.Infinite, cts.Token));

                if (completed != acceptTask)
                    throw new TimeoutException("Waiting for READY signal timed out.");

                var context = await acceptTask;

                using var reader = new StreamReader(context.Request.InputStream);
                var body = await reader.ReadToEndAsync(cts.Token);

                if (body.Trim().Equals("ready", StringComparison.OrdinalIgnoreCase))
                {
                    logger.Log("Received READY via startup notification.", LogLevel.Success, LogContext.PythonNerService);
                    return;
                }
            }
        }
        finally
        {
            listener.Stop();
        }
    }
}