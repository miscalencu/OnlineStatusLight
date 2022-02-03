using Microsoft.Extensions.Hosting;
using NLog;
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

            app.Application.Run(new OnlineStatusLightContext());

            LogManager.Flush();
        }
    }
}