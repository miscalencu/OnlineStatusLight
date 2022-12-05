using Microsoft.Extensions.Hosting;
using OnlineStatusLight.Core.Models;
using OnlineStatusLight.Core.Services;

namespace OnlineStatusLight.Application
{
    public class SyncLightService : IHostedService, IDisposable
    {
        private readonly IMicrosoftTeamsService _microsoftTeamsService;
        private readonly ILightService _lightService;

        private Timer _timer;

        public SyncLightService(
            IMicrosoftTeamsService microsoftTeamsService, 
            ILightService lightService)
        {
            _microsoftTeamsService = microsoftTeamsService;
            _lightService = lightService;
        }

        public async Task Sync()
        {
            var status = _microsoftTeamsService.GetCurrentStatus();
            await _lightService.SetState(status);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _lightService.Start();
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
            _lightService.End();
        }
    }
}
