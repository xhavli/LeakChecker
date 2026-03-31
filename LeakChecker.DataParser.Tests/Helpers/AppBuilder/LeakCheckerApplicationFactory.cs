using LeakChecker.DataParser.Helpers.Settings;
using LeakChecker.DataParser.Logging.Parse;
using LeakChecker.DataParser.Tests.Helpers.Logging.Parse;
using LeakChecker.DataParser.Tests.Helpers.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LeakChecker.DataParser.Tests.Helpers.AppBuilder;

public static class LeakCheckerApplicationFactory
{
    public static IHost CreateHost(string[]? args = null, Action<IServiceCollection>? configureTestServices = null, IConfiguration? overrideConfig = null)
    {
        var builder = Program.CreateHostBuilder(args, overrideConfig);

        builder.ConfigureServices((context, services) =>
        {
            configureTestServices?.Invoke(services);
            JsonSettings jsonSettings = new();

            IConfiguration configSource = overrideConfig ?? context.Configuration;
            configSource.GetSection("DataParser").Bind(jsonSettings);

            ISettings settings = new NullSettings();
            settings.ApplyGlobalSettings();

            services.AddSingleton(settings);
            services.AddSingleton<IParseLoggerFactory, NullParseLoggerFactory>();
        });

        return builder.Build();
    }
}
