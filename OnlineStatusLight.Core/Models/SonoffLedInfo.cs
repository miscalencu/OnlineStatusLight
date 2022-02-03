namespace OnlineStatusLight.Core.Models
{
    public class SonoffLedInfo
    {
        public string DeviceId { get; set; }
        public SonoffLedType Type { get; set; }
        public SonoffLedStatus Status { get; set; }
        public HttpClient HttpAPIClient { get; set; }
    }

    public enum SonoffLedType
    {
        Red,
        Green
    }

    public enum SonoffLedStatus
    {
        On,
        Off
    }
}
