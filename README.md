# OnlineStatusLight
This is a .NET Code desktop app that controls a lighting service to show online status of Microsoft Teams

## Source Services 

There are currently three implementation for getting the MS Teams status:
- [WindowsAutomation](#windowsautomationsourceservice) - by using the Windows Automation (UIAutomation) to read the Teams status (Windows only).
- [Azure](#azuresourceservice) - by querying the MSGraph for the Teams Presence.
- [LogFile](#logfilesourceservice) - by reading the MS Teams local logs file

### WindowsAutomationSourceService

Implementation for MS Teams which reads the current status by using the Windows Automation (UIAutomation) to read the Teams status.

```
{
  "sourceService": {
	"type": "windowsAutomation",
	"windowsAutomation": {
	  "interval": 5,
      "windowName": "Microsoft Teams",
      "statusPattern": "Your profile, status @status"
	}
  }
}
```

This implementation will only work under Windows machines.

### AzureSourceService

Implementation for MS Teams which reads the current status using the [Presence API](https://learn.microsoft.com/en-us/graph/api/presence-get?view=graph-rest-1.0&tabs=http).

```
{
  "sourceService": {
    "type": "azure",
    "azure": {
      "interval": 5,
      "authority": "https://login.microsoftonline.com",
      "tenantId": "xxx",
      "clientId": "xxx",
      "clientSecret": "xxx",
      "redirectUri": "xxx"
    }
  },
}
```
This implementation required that you have an application setup in your current AzureAD, application that has granted the `Presence.Read` permission.

### LogFileSourceService

Implementation for MS Teams which checks the Status by reading the local logs file.
```
"sourceService": {
  "type": "logFile",
  "msteams": {
      "interval": 5,
      "logfile": "%appdata%\\Microsoft\\Teams\\logs.txt"
  }
}
```

⚠️ This implementation will only work for [Classic Teams app version](https://learn.microsoft.com/en-us/officeupdates/teams-app-versioning#classic-teams-app-version), not for the [Windows Teams version](https://learn.microsoft.com/en-us/officeupdates/teams-app-versioning#windows-version-history).

## Light Services

Currently, there are two services supported: 
- [Sonoff](#sonoff) - to control SonOff BasicR3 WiFi smart switches.
- [Razer](#razer) - to control Razer devices.

## Sonoff

Details of the hardware implementation here: https://www.linkedin.com/feed/update/urn:li:activity:6895151178066579456/.

- Red light turns on for Busy.
- Green light turns on for Available.
- Both lights turn on for Do Not Disturb (Presenting). Initial idea was to blink the red light but the switch makes a noise every time it is activated so I had to give up that idea.

Required configuration for this implementation:
```
"lightService": {
  "type": "sonOff",
  "sonoff": {
      "red": {
        "ip": "192.168.0.62"
      },
      "green": {
        "ip": "192.168.0.61"
      }
  }
}
```

## Razer
- Red light turns on for Busy, In a meeting and Do Not Disturb (Presenting).
- Green light turns on for Available.
- Yellow light turns on for Away
- Purple light turns on for OutOfOffice

Required configuration for this implementation:
```
"lightService": {
  "type": "razer",
  "razer": {
      // true = will color the HeadSet device only. false = will color ALL Razer devices
      "headsetonly": true
  }
}
```