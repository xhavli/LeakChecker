using LeakChecker.DataParser.Content.Detection.RecognitionService;
using LeakChecker.DataParser.Logging.Execution;
using LeakChecker.DataParser.Logging.Parse;
using LeakChecker.DataParser.Utilities;
using LeakChecker.DataParser.Utilities.Settings;
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

                services.AddSingleton(settings);

                services.AddSingleton<IParseLoggerFactory, ParseLoggerFactory>();
                services.AddSingleton<ExecutionLogger>();
                services.AddSingleton<FileHelper>();
                services.AddSingleton<PythonNerService>();
                services.AddSingleton<ParserRunner>();
            });
    }

    public static async Task<int> Main(string[] args)
    {
        try
        {
            using var host = CreateHostBuilder(args).Build();
            var runner = host.Services.GetRequiredService<ParserRunner>();
            return await runner.RunAsync();
        }
        catch (Exception e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            await Console.Error.WriteLineAsync($"[EXCEPTION] [MAIN] {e.Message}");
            await Console.Error.WriteLineAsync($"[WHOLE EXCEPTION] {e}");
            Console.WriteLine("[WARNING] Program will terminate with exit code 1.");
            Console.ResetColor();
            return 1;
        }
    }
}