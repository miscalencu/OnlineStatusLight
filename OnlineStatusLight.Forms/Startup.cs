using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using OnlineStatusLight.Application.Extensions;
using OnlineStatusLight.Application.Services;
using OnlineStatusLight.Core.Constants;
using OnlineStatusLight.Core.Enums;
using OnlineStatusLight.Core.Exceptions;
using OnlineStatusLight.Core.Extensions;
using app = System.Windows.Forms;

namespace OnlineStatusLight.Forms
{
    public class Startup
    {
        //public static IConfigurationRoot configuration;
        public static IHost AppHost;

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureServices((builder, services) =>
                {
                    // add configuration
                    var configuration = new ConfigurationBuilder()
                        .AddJsonFile(Path.GetFullPath("appsettings.json"), true, true)
                        .AddUserSecrets<Startup>(true)
                        .Build();

                    // configure source and light services
                    var sourceServiceType = GetSourceService(configuration);
                    var lightServiceType = GetLightService(configuration);

                    services.ConfigureSourceServices(
                        sourceServiceType,
                        options => configuration.GetSection(ConfigurationConstants.SourceServiceLogFile).Bind(options),
                        options => configuration.GetSection(ConfigurationConstants.SourceServiceAzure).Bind(options)
                    );
                    services.ConfigureLightServices(
                        lightServiceType,
                        options => configuration.GetSection(ConfigurationConstants.LightServiceSonOff).Bind(options),
                        options => configuration.GetSection(ConfigurationConstants.LightServiceRazer).Bind(options)
                        );

                    // configure services
                    services
                        .AddSingleton<SyncLightService>()
                        .AddHostedService(p => p.GetRequiredService<SyncLightService>());

                    // configure logger
                    services
                        .AddLogging(configure =>
                        {
                            configure.AddNLog("nlog.config");
                            configure.AddConsole();
                        })
                        .Configure<LoggerFilterOptions>(options => options.MinLevel = LogLevel.Trace);
                });
        }

        private static SourceServiceType GetSourceService(IConfiguration configuration)
        {
            var sourceService = configuration![ConfigurationConstants.SourceServiceType];
            var sourceServiceType = sourceService.GetEnumValue<SourceServiceType>();
            if (!sourceServiceType.HasValue)
                throw new ConfigurationException("Invalid source service type in appsettings.json");
            return sourceServiceType.Value;
        }

        private static LightServiceType GetLightService(IConfiguration configuration)
        {
            var lightService = configuration![ConfigurationConstants.LightServiceType];
            var lightServiceType = lightService.GetEnumValue<LightServiceType>();
            if (!lightServiceType.HasValue)
                throw new ConfigurationException("Invalid light service type in appsettings.json");
            return lightServiceType.Value;
        }

        public static void SetupErrorLogger()
        {
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionTrapper;
            AppDomain.CurrentDomain.ProcessExit += ProcessExit;
        }

        private static void ProcessExit(object? sender, EventArgs e)
        {
            var _sync = Startup.AppHost.Services.GetRequiredService<SyncLightService>();
            _sync.Dispose();
        }

        private static void UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs exception)
        {
            var _logger = Startup.AppHost.Services.GetRequiredService<ILogger>();
            var _sync = Startup.AppHost.Services.GetRequiredService<SyncLightService>();

            _sync.Dispose();
            _logger.LogError(exception.ExceptionObject as Exception, "An error has occured" + Environment.NewLine);
            _logger.LogWarning("Exiting because an error occured! Please check logs for error details.");

            MessageBox.Show("Exiting because an error occured! Please check logs for error details.");

            app.Application.Exit();
            Environment.Exit(1);
        }
    }
}