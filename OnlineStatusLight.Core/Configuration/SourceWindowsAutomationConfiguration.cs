namespace OnlineStatusLight.Core.Configuration
{
    public class SourceWindowsAutomationConfiguration
    {
        public int Interval { get; set; }
        public string? WindowName { get; set; }
        public string? ProcessName { get; set; }
        public bool RestartProcess { get; set; } = true;
        public string? RestartArgument { get; set; }
        public string? StatusPattern { get; set; }
    }
}