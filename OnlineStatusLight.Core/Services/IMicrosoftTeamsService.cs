using OnlineStatusLight.Core.Models;
using System.Threading;

namespace OnlineStatusLight.Core.Services
{
    public interface IMicrosoftTeamsService
    {
        // in seconds
        int PoolingInterval { get; set; }

        Task<MicrosoftTeamsStatus> GetCurrentStatus(CancellationToken cancellationToken);
    }
}