using LeakChecker.DataParser.Content.Detection.RecognitionService;
using LeakChecker.DataParser.Helpers.ArchiveExtraction;
using LeakChecker.DataParser.Helpers.FileHelp;
using LeakChecker.DataParser.Helpers.Settings;
using LeakChecker.DataParser.Logging;
using LeakChecker.DataParser.Logging.Execution;
using LeakChecker.DataParser.Logging.Parse;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LeakChecker.DataParser;

public static class Program
{
    public static IHostBuilder CreateHostBuilder(string[]? args = null, IConfiguration? overrideConfig = null)
    {
        return Host.CreateDefaultBuilder(args ?? []).ConfigureServices((context, services) =>
            {
                JsonSettings jsonSettings = new();

                IConfiguration configSource = overrideConfig ?? context.Configuration;
                configSource.GetSection("DataParser").Bind(jsonSettings);

                ISettings settings = Settings.FromJson(jsonSettings);
                settings.ApplyGlobalSettings();

                // Settings
                services.AddSingleton(settings);

                // Loggers
                services.AddSingleton<ExecutionLogger>();
                services.AddSingleton<IParseLoggerFactory, ParseLoggerFactory>();
                
                // Helpers
                services.AddSingleton<FileHelper>();
                services.AddSingleton<ArchiveExtractor>();
                services.AddSingleton<PythonNerService>();
                
                // Orchestration
                services.AddSingleton<Orchestrator>();
            });
    }

    public static async Task<int> Main(string[] args)
    {
        try
        {
            using var host = CreateHostBuilder(args).Build();
            var orchestrator = host.Services.GetRequiredService<Orchestrator>();
            return await orchestrator.RunAsync();
        }
        catch (Exception e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            await Console.Error.WriteLineAsync($"[{DateTime.Now:T}] [{LogLevel.Failure}] [{LogContext.Program}] {e.Message}\n");
            await Console.Error.WriteLineAsync($"[{DateTime.Now:T}] [WHOLE_EXCEPTION] {e}");
            Console.WriteLine("\n[WARNING] Program will terminate with exit code 1.");
            Console.ResetColor();
            return 1;
        }
    }
}