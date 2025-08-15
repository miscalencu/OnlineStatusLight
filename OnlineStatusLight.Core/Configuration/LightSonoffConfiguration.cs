namespace OnlineStatusLight.Core.Configuration
{
    public class SonoffBasicLed
    {
        public string Ip { get; set; } = null!;
    }

    public class LightSonoffConfiguration
    {
        public float Version { get; set; }
        public int TimeoutInSeconds { get; set; }
        public SonoffBasicLed Red { get; set; } = null!;
        public SonoffBasicLed Green { get; set; } = null!;
    }
}