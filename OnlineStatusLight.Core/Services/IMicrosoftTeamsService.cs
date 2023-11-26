using OnlineStatusLight.Core.Models;

namespace OnlineStatusLight.Core.Services
{
    public interface IMicrosoftTeamsService
    {
        // in seconds
        int PoolingInterval { get; set; }

        Task<MicrosoftTeamsStatus> GetCurrentStatus();
    }
}