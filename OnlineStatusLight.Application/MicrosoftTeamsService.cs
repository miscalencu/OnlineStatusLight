using OnlineStatusLight.Core.Models;
using OnlineStatusLight.Core.Services;

namespace OnlineStatusLight.Application
{
    public class MicrosoftTeamsService : IMicrosoftTeamsService
    {
        public int PoolingInterval { get; set; }

        public MicrosoftTeamsService()
        {
            this.PoolingInterval = 5;
        }

        public MicrosoftTeamsStatus GetCurrentStatus()
        {
            // TO DO
            return MicrosoftTeamsStatus.Available;
        }
    }
}