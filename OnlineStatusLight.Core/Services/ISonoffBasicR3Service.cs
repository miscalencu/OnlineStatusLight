using OnlineStatusLight.Core.Models;

namespace OnlineStatusLight.Core.Services
{
    public interface ISonoffBasicR3Service
    {
        Task SwitchOn(SonoffLedType led, bool switchOffOthers = true);

        Task SwitchOff(SonoffLedType led, bool switchOffOthers = true);

        Task SwitchOffAll(SonoffLedType? ignore = null, bool force = false);

        Task<SonoffConfigurationInfo> GetConfiguration(SonoffLedType led);
    }
}