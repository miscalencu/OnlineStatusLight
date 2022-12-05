using OnlineStatusLight.Core.Models;

namespace OnlineStatusLight.Core.Services
{
    public interface ILightService
    {
        void Start();
        void End();
        Task SetState(MicrosoftTeamsStatus status);
    }
}