using Microsoft.Extensions.Logging;
using OnlineStatusLight.Application.Extensions;
using OnlineStatusLight.Core.Models;
using OnlineStatusLight.Core.Services;

namespace OnlineStatusLight.Application
{
    public class SonoffBasicR3Service : ISonoffBasicR3Service, ILightService
    {
        private readonly ILogger<SonoffBasicR3Service> _logger;

        public Dictionary<SonoffLedType, SonoffLedInfo> Leds { get; set; } = new Dictionary<SonoffLedType, SonoffLedInfo>();

        // load statuses and device ids for all leds
        public SonoffBasicR3Service(IHttpClientFactory httpClientFactory, ILogger<SonoffBasicR3Service> logger)
        {
            // add green led
            Leds.Add(SonoffLedType.Green, new SonoffLedInfo()
            {
                HttpAPIClient = httpClientFactory.CreateClient("sonoffGreenLedApi"),
                Status = SonoffLedStatus.Off,
                Type = SonoffLedType.Green
            });

            // add red led
            Leds.Add(SonoffLedType.Red, new SonoffLedInfo()
            {
                HttpAPIClient = httpClientFactory.CreateClient("sonoffRedLedApi"),
                Status = SonoffLedStatus.Off,
                Type = SonoffLedType.Red
            });

            _logger = logger;
        }

        public async Task SwitchOff(SonoffLedType led, bool switchOffOthers = true)
        {
            await EnsureDeviceIdIsLoaded(led);
            if (switchOffOthers)
            {
                await SwitchOffAll(led);
            }

            if (this.Leds[led].Status == SonoffLedStatus.Off)
            {
                return;
            }

            _logger.LogInformation($"Switching {led} to OFF.");

            var response = await this.Leds[led].HttpAPIClient.PostJsonAsync<SonoffConfigurationInfo>("switch", new
            {
                deviceid = this.Leds[led].DeviceId,
                data = new
                {
                    Switch = "off"
                }
            });
            response.HttpResponse.EnsureSuccessStatusCode();

            this.Leds[led].Status = SonoffLedStatus.Off;
        }

        public async Task SwitchOn(SonoffLedType led, bool switchOffOthers = true)
        {
            await EnsureDeviceIdIsLoaded(led);
            if (switchOffOthers)
            {
                await SwitchOffAll(led);
            }

            if (this.Leds[led].Status == SonoffLedStatus.On) {
                return;
            }

            _logger.LogInformation($"Switching {led} to ON.");

            var response = await this.Leds[led].HttpAPIClient.PostJsonAsync<SonoffConfigurationInfo>("switch", new 
            {
                deviceid = this.Leds[led].DeviceId,
                data = new
                {
                    Switch = "on",
                    Pulse = "off"
                }
            });
            response.HttpResponse.EnsureSuccessStatusCode();

            this.Leds[led].Status = SonoffLedStatus.On;
        }

        public async Task SwitchOffAll(SonoffLedType? ignore = null)
        {
            foreach (var led in this.Leds
                .Where(l => !ignore.HasValue || l.Key != ignore.Value)
                .Where(l => l.Value.Status == SonoffLedStatus.On))
            {
                await SwitchOff(led.Key, false);
            }
        }

        public async Task<SonoffConfigurationInfo> GetConfiguration(SonoffLedType led)
        {
            var response = await this.Leds[led].HttpAPIClient.PostJsonAsync<SonoffConfigurationInfo>("info", new
            {
                deviceid = "",
                data = new { }
            });

            response.HttpResponse.EnsureSuccessStatusCode();

            return response.Data;
        }

        private async Task EnsureDeviceIdIsLoaded(SonoffLedType led)
        {
            if (Leds[led].DeviceId == null)
            {
                var conf = await GetConfiguration(led);
                Leds[led].DeviceId = conf.Data.DeviceId;
                // Leds[led].Status = conf.Data.Switch == "on" ? SonoffLedStatus.On: SonoffLedStatus.Off;
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
        }

        public void End()
        {
             _logger.LogInformation($"Ending SonoffBasicR3Service");
            SwitchOffAll().GetAwaiter().GetResult();
        }
    }
}
