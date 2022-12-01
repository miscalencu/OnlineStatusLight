﻿using Colore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OnlineStatusLight.Core.Models;
using OnlineStatusLight.Core.Services;

namespace OnlineStatusLight.Application.Razer
{
    public class RazerLightService : ILightService
    {
        private ILogger<RazerLightService> _logger;
        private bool _headSetOnly;
        private IChroma? _chroma;

        public RazerLightService(IConfiguration configuration, ILogger<RazerLightService> logger)
        {
            _logger = logger;
            _headSetOnly = bool.TryParse(configuration["razer:headsetonly"], out var headSetOnly) ? headSetOnly : false;
        }
        public async void End()
        {
            _logger.LogInformation("End RazerLightService");
            if(_chroma != null) {
                await _chroma.UninitializeAsync();
            }
        }

        public async Task SetState(MicrosoftTeamsStatus status)
        {
            switch (status)
            {
                case MicrosoftTeamsStatus.Available:
                    await SetTargetedDeviceColor(Colore.Data.Color.Green);
                    break;
                case MicrosoftTeamsStatus.Away:
                    await SetTargetedDeviceColor(Colore.Data.Color.Yellow);
                    break;
                case MicrosoftTeamsStatus.OutOfOffice:
                    await SetTargetedDeviceColor(Colore.Data.Color.Purple);
                    break;
                case MicrosoftTeamsStatus.Busy:
                case MicrosoftTeamsStatus.DoNotDisturb:
                    await SetTargetedDeviceColor(Colore.Data.Color.Red);
                    break;
                default:
                    _logger.LogInformation($"Received: {status}. Setting color to off...");
                    await SetTargetedDeviceColor(Colore.Data.Color.Black);
                    break;
            }
        }

        private async Task SetTargetedDeviceColor(Colore.Data.Color color) 
        {
            if(_chroma != null) 
            {
                if(_headSetOnly) {
                    await _chroma.Headset.SetStaticAsync(color);
                } else {
                    await _chroma.SetAllAsync(color);
                }
            }
        }

        public async void Start()
        {
            _logger.LogInformation("Start RazerLightService");
            _chroma = await ColoreProvider.CreateNativeAsync();
        }
    }
}