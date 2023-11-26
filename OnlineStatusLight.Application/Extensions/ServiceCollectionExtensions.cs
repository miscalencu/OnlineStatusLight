using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OnlineStatusLight.Application.Razer;
using OnlineStatusLight.Application.Services.LightServices;
using OnlineStatusLight.Application.Services.SourceServices;
using OnlineStatusLight.Core.Configuration;
using OnlineStatusLight.Core.Constants;
using OnlineStatusLight.Core.Enums;
using OnlineStatusLight.Core.Services;
using Polly;

namespace OnlineStatusLight.Application.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static void ConfigureSourceServices(
            this IServiceCollection services,
            SourceServiceType sourceServiceType,
            Action<SourceLogFileConfiguration> sourceLogFileSetup,
            Action<SourceAzureConfiguration> sourceAzureSetup)
        {
            switch (sourceServiceType)
            {
                case SourceServiceType.LogFile:
                    services.Configure(sourceLogFileSetup);
                    services.AddSingleton<IMicrosoftTeamsService, LogFileSourceService>();
                    break;

                case SourceServiceType.Azure:
                    services.Configure(sourceAzureSetup);
                    services.AddSingleton<IMicrosoftTeamsService, AzureSourceService>();
                    break;

                default:
                    throw new Exception($"Not implemented source service type: {sourceServiceType}");
            }
        }

        public static void ConfigureLightServices(
            this IServiceCollection services,
            LightServiceType lightServiceType,
            Action<LightSonoffConfiguration> lightSonoffConfiguration,
            Action<LightRazerConfiguration> lightRazerConfiguration)
        {
            switch (lightServiceType)
            {
                case LightServiceType.SonOff:
                    services.AddSingleton<ILightService, SonoffBasicR3Service>();
                    services.Configure(lightSonoffConfiguration);
                    services.AddSonOffHttpClients();
                    break;

                case LightServiceType.Razor:
                    services.Configure(lightRazerConfiguration);
                    services.AddSingleton<ILightService, RazerLightService>();
                    break;

                default:
                    throw new Exception($"Not implemented source service type: {lightServiceType}");
            }
        }

        public static void AddSonOffHttpClients(this IServiceCollection services)
        {
            var serviceProdiver = services.BuildServiceProvider();
            var configurationSerction = serviceProdiver.GetService<IOptions<LightSonoffConfiguration>>();

            if (configurationSerction == null)
                throw new Exception("Sonoff configuration not found");

            var configuration = configurationSerction.Value;

            services.AddHttpClient("sonoffRedLedApi", c =>
            {
                c!.BaseAddress = new Uri($"http://{configuration.Red.Ip}:8081/zeroconf/");
                c.DefaultRequestHeaders!.Add("User-Agent", GlobalConstants.AppName);
            }).AddTransientHttpErrorPolicy(p =>
                p.WaitAndRetryAsync(3, _ => TimeSpan.FromMilliseconds(500)
            ));

            services.AddHttpClient("sonoffGreenLedApi", c =>
            {
                c!.BaseAddress = new Uri($"http://{configuration.Green.Ip}:8081/zeroconf/");
                c.DefaultRequestHeaders!.Add("User-Agent", GlobalConstants.AppName);
            }).AddTransientHttpErrorPolicy(p =>
                p.WaitAndRetryAsync(3, _ => TimeSpan.FromMilliseconds(500)
            ));
        }
    }
}