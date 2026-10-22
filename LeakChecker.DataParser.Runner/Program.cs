using System.Text;
using LeakChecker.Common.Enums;
using LeakChecker.Db;
using LeakChecker.Db.Facade;
using LeakChecker.DataParser.Content.Detection.RecognitionService;
using LeakChecker.DataParser.Helpers.ArchiveExtraction;
using LeakChecker.DataParser.Helpers.FileHelp;
using LeakChecker.DataParser.Helpers.Settings;
using LeakChecker.DataParser.Interfaces;
using LeakChecker.DataParser.Logging.Execution;
using LeakChecker.DataParser.Logging.Parse;
using LeakChecker.DataParser.Orchestration.Execution;
using LeakChecker.DataParser.Orchestration.Parsing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LeakChecker.DataParser.Runner;

public static class Program
{
    public static IHostBuilder CreateHostBuilder(string[]? args = null, IConfiguration? overrideConfig = null)
    {
        return Host.CreateDefaultBuilder(args ?? []).ConfigureServices((context, services) =>
            {
                JsonSettings jsonSettings = new();

                IConfiguration configSrc = overrideConfig ?? context.Configuration;
                configSrc.GetSection("DataParser").Bind(jsonSettings);  //todo will be replaced soon

                ISettings settings = Settings.FromJson(jsonSettings, services); //todo will be replaced soon
                settings.ApplyGlobalSettings(); //todo will be replaced soon

                // Settings
                services.AddSingleton(settings);    //todo will be replaced soon

                LoadSettings(configSrc, services);
                ApplyGlobalEncodingSettings();
                
                ResolveDatabase(jsonSettings.DbProvider, services);

                // Loggers
                services.AddSingleton<ExecutionLogger>();
                services.AddSingleton<IParseLoggerFactory, ParseLoggerFactory>();
                
                // Helpers
                services.AddSingleton<FileHelper>();
                services.AddSingleton<ArchiveExtractor>();
                services.AddSingleton<PythonNerService>();
                
                // Orchestration
                services.AddSingleton<IParsingOrchestratorFactory, ParsingOrchestratorFactory>();
                services.AddSingleton<ExecutionOrchestrator>();
            });
    }

    public static async Task<int> Main(string[] args)
    {
        try
        {
            using var host = CreateHostBuilder(args).Build();
            
            var nerService = host.Services.GetRequiredService<PythonNerService>();
            var orchestrator = host.Services.GetRequiredService<ExecutionOrchestrator>();
            
            try
            {
                await RunNerSubprocess(nerService);
                return await orchestrator.RunAsync();
            }
            finally
            {
                await nerService.Stop();
            }
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
    
    private static async Task RunNerSubprocess(PythonNerService pythonNerService)
    {
        await pythonNerService.Start();
        await pythonNerService.WaitStart();
    }
    
    private static void ApplyGlobalEncodingSettings()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        Encoding defaultUtf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        Console.InputEncoding = defaultUtf8;
        Console.OutputEncoding = defaultUtf8;
    }

    private static void LoadSettings(IConfiguration config, IServiceCollection services)
    {
        LoggingSettings loggingSettings = new();
        config.GetSection("Logging").Bind(loggingSettings);
        services.AddSingleton(loggingSettings);

        ExecutionSettings executionSettings = new();
        config.GetSection("Execution").Bind(executionSettings);
        services.AddSingleton(executionSettings);
        
        NerSubprocessSettings nerSubprocessSettings = new();
        config.GetSection("NerSubprocess").Bind(nerSubprocessSettings);
        services.AddSingleton(nerSubprocessSettings);
        
        SamplingSettings samplingSettings = new();
        config.GetSection("Sampling").Bind(samplingSettings);
        services.AddSingleton(samplingSettings);
        
        DbSettings dbSettings = new();
        config.GetSection("Database").Bind(dbSettings);
        services.AddSingleton(dbSettings);
    }
    
    private static void ResolveDatabase(string? dbProvider, IServiceCollection services)
    {
        if (string.IsNullOrWhiteSpace(dbProvider))
        {
            services.AddTransient<IDatabase, NullDatabase>();
            return;
        }

        switch (dbProvider.Trim().ToLowerInvariant())
        {
            case "mongodb":
                services.AddTransient<IDatabase, MongoDbFacade>();
                break;
            default:
                throw new NotSupportedException($"Unsupported database provider '{dbProvider}'.");
        }
    }
}