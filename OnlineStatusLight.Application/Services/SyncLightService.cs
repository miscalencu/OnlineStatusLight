using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OnlineStatusLight.Core.Models;
using OnlineStatusLight.Core.Services;

namespace OnlineStatusLight.Application.Services
{
    public class SyncLightService : IHostedService
    {
        private readonly IMicrosoftTeamsService _microsoftTeamsService;
        private readonly ILightService _lightService;
        private readonly ILogger<SyncLightService> _logger;
        private Timer _timer = null!;

        public event EventHandler<MicrosoftTeamsStatus>? StateChanged;

        public SyncLightService(
            IMicrosoftTeamsService microsoftTeamsService,
            ILightService lightService,
            ILogger<SyncLightService> logger)
        {
            _microsoftTeamsService = microsoftTeamsService;
            _lightService = lightService;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _lightService.Start();
            }
            catch (Exception)
            {
                _logger.LogError("Error starting light service.");
            }

            _timer = new Timer(async _ => await Sync(cancellationToken), null, TimeSpan.Zero, TimeSpan.FromSeconds(_microsoftTeamsService.PoolingInterval));
        }

        public async Task Sync(CancellationToken cancellationToken)
        {
            var status = await _microsoftTeamsService.GetCurrentStatus(cancellationToken);
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

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Dispose();
            await _lightService.End();
        }
    }
}