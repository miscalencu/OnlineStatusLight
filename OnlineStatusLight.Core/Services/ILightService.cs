using OnlineStatusLight.Core.Models;

namespace OnlineStatusLight.Core.Services
{
    public interface ILightService
    {
        Task Start();

        Task End();

        Task SetState(MicrosoftTeamsStatus status);
    }
}