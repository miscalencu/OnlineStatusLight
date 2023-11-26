namespace OnlineStatusLight.Core.Configuration
{
    public class SourceAzureConfiguration
    {
        public int Interval { get; set; }
        public string ClientId { get; set; } = null!;
        public string Authority { get; set; } = null!;
        public string TenantId { get; set; } = null!;
        public string ClientSecret { get; set; } = null!;
        public string RedirectUri { get; set; } = null!;
    }
}