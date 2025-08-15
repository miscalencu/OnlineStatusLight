using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLog;
using OnlineStatusLight.Application.Services;
using OnlineStatusLight.Core.Exceptions;
using app = System.Windows.Forms;

namespace OnlineStatusLight.Forms
{
    internal static class Program
    {
        [STAThread]
        private static async Task Main(string[] args)
        {
            using IHost host = Startup.CreateHostBuilder(args).Build();

            await host.StartAsync();

            Startup.AppHost = host;
            Startup.SetupErrorLogger();

            var service = Startup.AppHost.Services.GetRequiredService<SyncLightService>() ??
                throw new ConfigurationException("SyncLightService is null. Please check your configuration file (section lightservice)");

            app.Application.Run(new OnlineStatusLightContext(service));

            LogManager.Flush();
        }
    }
}