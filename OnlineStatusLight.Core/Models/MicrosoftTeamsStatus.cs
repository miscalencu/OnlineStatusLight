namespace OnlineStatusLight.Core.Models
{
    // https://docs.microsoft.com/en-us/microsoftteams/presence-admins
    public enum MicrosoftTeamsStatus
    {
        Available,
        Busy,        
        DoNotDisturb,
        Away,
        Offline,
        Unknown,
        OutOfOffice,
        InAMeeting
    }
}
