using Microsoft.Extensions.Hosting;
using OnlineStatusLight.Core.Models;
using OnlineStatusLight.Core.Services;

namespace OnlineStatusLight.Application
{
    public class SyncLightService : IHostedService, IDisposable
    {
        private readonly IMicrosoftTeamsService _microsoftTeamsService;
        private readonly ISonoffBasicR3Service _sonoffBasicR3Service;

        private Timer _timer;

        public SyncLightService(
            IMicrosoftTeamsService microsoftTeamsService, 
            ISonoffBasicR3Service sonoffBasicR3Service)
        {
            _microsoftTeamsService = microsoftTeamsService;
            _sonoffBasicR3Service = sonoffBasicR3Service;
        }

        public async Task Sync()
        {
            var status = _microsoftTeamsService.GetCurrentStatus();
            switch (status)
            {
                case MicrosoftTeamsStatus.Available:
                    await _sonoffBasicR3Service.SwitchOn(SonoffLedType.Green);
                    break;
                case MicrosoftTeamsStatus.Busy:
                    await _sonoffBasicR3Service.SwitchOn(SonoffLedType.Red);
                    break;
                case MicrosoftTeamsStatus.DoNotDisturb:
                    await _sonoffBasicR3Service.BlinkOn(SonoffLedType.Red);
                    break;
                case MicrosoftTeamsStatus.Away:
                    await _sonoffBasicR3Service.BlinkOn(SonoffLedType.Green);
                    break;
                default:
                    await _sonoffBasicR3Service.SwitchOffAll();
                    break;
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(this.ExecuteSync, null, TimeSpan.Zero, TimeSpan.FromSeconds(_microsoftTeamsService.PoolingInterval));
            return Task.CompletedTask;
        }

        private void ExecuteSync(object state)
        {
            Sync().GetAwaiter().GetResult();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
            }
            _sonoffBasicR3Service.SwitchOffAll().GetAwaiter().GetResult();
        }
    }
}
