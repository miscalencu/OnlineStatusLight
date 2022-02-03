namespace OnlineStatusLight.Core.Models
{
    public class SonoffConfigurationInfo
    {
        public int Seq { get; set; }
        public int Error { get; set; }
        public SonoffConfigurationData Data { get; set; }
    }

    public class SonoffConfigurationData
    {
        public string DeviceId { get; set; }
        public string Switch { get; set; }
        public string Startup { get; set; }
        public string Pulse { get; set; }
        public int PulseWidth { get; set; }
        public string SSID { get; set; }
        public string SignalStrength { get; set; }
        public bool OtaUnlock { get; set; }
        public string FwVersion { get; set; }
        public string BSSId { get; set; }
    }
}
