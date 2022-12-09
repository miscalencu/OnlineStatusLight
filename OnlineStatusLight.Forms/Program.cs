using Microsoft.Extensions.Hosting;
using NLog;
using OnlineStatusLight.Application;
using app = System.Windows.Forms;

namespace OnlineStatusLight.Forms
{
    internal static class Program
    {
        [STAThread]
        static async Task Main(string[] args)
        {
            using IHost host = Startup.CreateHostBuilder(args).Build();
            await host.StartAsync();

            Startup.AppHost = host;
            Startup.SetupErrorLogger();
            var service = Startup.AppHost.Services.GetService(typeof(SyncLightService)) as SyncLightService;
            app.Application.Run(new OnlineStatusLightContext(service));

            LogManager.Flush();
        }
    }
}