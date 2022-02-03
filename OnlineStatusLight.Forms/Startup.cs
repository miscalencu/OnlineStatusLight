using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using OnlineStatusLight.Application;
using OnlineStatusLight.Core.Services;
using Polly;
using app = System.Windows.Forms;

namespace OnlineStatusLight.Forms
{
    public class Startup
    {
        public static IConfigurationRoot ConfigurationRoot;
        public static IHost AppHost;

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureServices((_, services) =>
                {
                    // add configuration
                    ConfigurationRoot = new ConfigurationBuilder()
                        .AddJsonFile(Path.GetFullPath("appsettings.json"), true, true)
                        .Build();

                    // configure services
                    services
                        .AddSingleton<IMicrosoftTeamsService, MicrosoftTeamsService>()
                        .AddSingleton<ISonoffBasicR3Service, SonoffBasicR3Service>()
                        .AddSingleton<SyncLightService>()
                        .AddHostedService<SyncLightService>();

                    // configure logger
                    services
                        .AddLogging(configure =>
                        {
                            configure.AddNLog("nlog.config");
                            // configure.AddConsole();
                        })
                        .Configure<LoggerFilterOptions>(options => options.MinLevel = LogLevel.Trace);

                    // add http clients
                    services = ConfigureHttpClients(services, ConfigurationRoot);
                });
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

        public static IServiceCollection ConfigureHttpClients(IServiceCollection services, IConfiguration configuration)
        {
            services.AddHttpClient("sonoffRedLedApi", c =>
            {
                c!.BaseAddress = new Uri($"http://{configuration!["sonoff:red:ip"]}:8081/zeroconf/");
                c.DefaultRequestHeaders!.Add("User-Agent", "OnlineStatusLight");
            }).AddTransientHttpErrorPolicy(p =>
                p.WaitAndRetryAsync(3, _ => TimeSpan.FromMilliseconds(500)
            ));

            services.AddHttpClient("sonoffGreenLedApi", c =>
            {
                c!.BaseAddress = new Uri($"http://{configuration!["sonoff:green:ip"]}:8081/zeroconf/");
                c.DefaultRequestHeaders!.Add("User-Agent", "OnlineStatusLight");
            }).AddTransientHttpErrorPolicy(p =>
                p.WaitAndRetryAsync(3, _ => TimeSpan.FromMilliseconds(500)
            ));

            return services;
        }

        static void UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs exception)
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
