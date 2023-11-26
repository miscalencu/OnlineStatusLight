namespace OnlineStatusLight.Core.Configuration
{
    public class SonoffBasicLed
    {
        public string Ip { get; set; }
    }

    public class LightSonoffConfiguration
    {
        public float Version { get; set; }
        public SonoffBasicLed Red { get; set; }
        public SonoffBasicLed Green { get; set; }
    }
}