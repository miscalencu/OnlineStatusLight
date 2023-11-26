using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OnlineStatusLight.Application.Extensions;
using OnlineStatusLight.Core.Configuration;
using OnlineStatusLight.Core.Constants;
using OnlineStatusLight.Core.Exceptions;
using OnlineStatusLight.Core.Models;
using OnlineStatusLight.Core.Services;

namespace OnlineStatusLight.Application.Services.LightServices
{
    public class SonoffBasicR3Service : ISonoffBasicR3Service, ILightService
    {
        private readonly ILogger<SonoffBasicR3Service> _logger;
        private readonly LightSonoffConfiguration _sonoffConfiguration;

        public Dictionary<SonoffLedType, SonoffLedInfo> Leds { get; set; } = new Dictionary<SonoffLedType, SonoffLedInfo>();

        // load statuses and device ids for all leds
        public SonoffBasicR3Service(
            IHttpClientFactory httpClientFactory,
            ILogger<SonoffBasicR3Service> logger,
            IOptions<LightSonoffConfiguration> sonoffConfiguration)
        {
            // add green led
            Leds.Add(SonoffLedType.Green, new SonoffLedInfo()
            {
                HttpAPIClient = httpClientFactory.CreateClient(SonoffConstants.GreenLedApi),
                Status = SonoffLedStatus.Off,
                Type = SonoffLedType.Green
            });

            // add red led
            Leds.Add(SonoffLedType.Red, new SonoffLedInfo()
            {
                HttpAPIClient = httpClientFactory.CreateClient(SonoffConstants.RedLedApi),
                Status = SonoffLedStatus.Off,
                Type = SonoffLedType.Red
            });

            _logger = logger;

            if (sonoffConfiguration == null)
                throw new ConfigurationException("Configuration not found for light service Sonoff.");

            _sonoffConfiguration = sonoffConfiguration.Value;
        }

        public async Task SwitchOff(SonoffLedType led, bool switchOffOthers = true)
        {
            await EnsureDeviceIdIsLoaded(led);
            if (switchOffOthers)
            {
                await SwitchOffAll(led);
            }

            _logger.LogInformation($"Switching {led} to OFF.");

            var response = await Leds[led].HttpAPIClient.PostJsonAsync<SonoffConfigurationInfo>("switch", new
            {
                deviceid = Leds[led].DeviceId,
                data = new
                {
                    Switch = "off"
                }
            });
            response.HttpResponse.EnsureSuccessStatusCode();

            Leds[led].Status = SonoffLedStatus.Off;
        }

        public async Task SwitchOn(SonoffLedType led, bool switchOffOthers = true)
        {
            await EnsureDeviceIdIsLoaded(led);
            if (switchOffOthers)
            {
                await SwitchOffAll(led);
            }

            if (Leds[led].Status == SonoffLedStatus.On)
            {
                return;
            }

            _logger.LogInformation($"Switching {led} to ON.");

            var response = await Leds[led].HttpAPIClient.PostJsonAsync<SonoffConfigurationInfo>("switch", new
            {
                deviceid = Leds[led].DeviceId,
                data = new
                {
                    Switch = "on",
                    Pulse = "off"
                }
            });
            response.HttpResponse.EnsureSuccessStatusCode();

            Leds[led].Status = SonoffLedStatus.On;
        }

        public async Task SwitchOffAll(SonoffLedType? ignore = null, bool force = false)
        {
            foreach (var led in Leds
                .Where(l => !ignore.HasValue || l.Key != ignore.Value)
                .Where(l => force == true || l.Value.Status == SonoffLedStatus.On))
            {
                await SwitchOff(led.Key, false);
            }
        }

        public async Task<SonoffConfigurationInfo> GetConfiguration(SonoffLedType led)
        {
            dynamic payload = new
            {
                deviceid = "",
                data = new { }
            };

            if (_sonoffConfiguration.Version > 3.5)
            {
                payload = new
                {
                    data = new
                    {
                        deviceid = ""
                    }
                };
            }

            var response = await Leds[led].HttpAPIClient.PostJsonAsync<SonoffConfigurationInfo>("info", (object)payload);

            response.HttpResponse.EnsureSuccessStatusCode();

            return response.Data;
        }

        private async Task EnsureDeviceIdIsLoaded(SonoffLedType led)
        {
            if (Leds[led].DeviceId == null)
            {
                var conf = await GetConfiguration(led);
                Leds[led].DeviceId = conf.Data.DeviceId;
            }
        }

        public async Task SetState(MicrosoftTeamsStatus status)
        {
            switch (status)
            {
                case MicrosoftTeamsStatus.Available:
                    await SwitchOn(SonoffLedType.Green);
                    break;

                case MicrosoftTeamsStatus.Busy:
                    await SwitchOn(SonoffLedType.Red);
                    break;

                case MicrosoftTeamsStatus.DoNotDisturb:
                    await SwitchOn(SonoffLedType.Red, false);
                    await SwitchOn(SonoffLedType.Green, false);
                    break;

                default:
                    await SwitchOffAll();
                    break;
            }
        }

        public void Start()
        {
            _logger.LogInformation($"Starting SonoffBasicR3Service");
            SwitchOffAll(null, true).GetAwaiter().GetResult();
        }

        public void End()
        {
            _logger.LogInformation($"Ending SonoffBasicR3Service");
            SwitchOffAll(null, true).GetAwaiter().GetResult();
        }
    }
}