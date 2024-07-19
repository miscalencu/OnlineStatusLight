using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OnlineStatusLight.Core.Models;
using OnlineStatusLight.Core.Services;

namespace OnlineStatusLight.Application.Services
{
    public class SyncLightService : IHostedService, IDisposable
    {
        private readonly IMicrosoftTeamsService _microsoftTeamsService;
        private readonly ILightService _lightService;
        private readonly ILogger<SyncLightService> _logger;
        private Timer _timer;

        public event EventHandler<MicrosoftTeamsStatus> StateChanged;

        public SyncLightService(
            IMicrosoftTeamsService microsoftTeamsService,
            ILightService lightService,
            ILogger<SyncLightService> logger)
        {
            _microsoftTeamsService = microsoftTeamsService;
            _lightService = lightService;
            _logger = logger;
        }

        public async Task Sync()
        {
            var status = await _microsoftTeamsService.GetCurrentStatus();
            StateChanged?.Invoke(this, status);
            try
            {
                await _lightService.SetState(status);
            }
            catch (Exception)
            {
                _logger.LogError("Error setting light service state.");
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _lightService.Start();
            }
            catch (Exception)
            {
                _logger.LogError("Error starting light service.");
            }

            _timer = new Timer(ExecuteSync, null, TimeSpan.Zero, TimeSpan.FromSeconds(_microsoftTeamsService.PoolingInterval));
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