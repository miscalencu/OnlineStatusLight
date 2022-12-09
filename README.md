# OnlineStatusLight
.NET app that controls a lighting service to show online status of Microsoft Teams

=> Current implementation for MS Teams which checks the Status by reading the local logs file.
```
"msteams": {
    "interval": 5,
    "logfile": "%appdata%\\Microsoft\\Teams\\logs.txt"
}
```
Currently, there are two services supported: [Sonoff](#sonoff) and [Razer](#razer).

## Sonoff

Details of the hardware implementation here: https://www.linkedin.com/feed/update/urn:li:activity:6895151178066579456/.

- Red light turns on for Busy.
- Green light turns on for Available.
- Both lights turn on for Do Not Disturb (Presenting). Initial idea was to blink the red light but the switch makes a noise every time it is activated so I had to give up that idea.

Use this service by stating in appsettings.json or by omitting it completely:
```
"lightservice": "OnlineStatusLight.Application.SonoffBasicR3Service, OnlineStatusLight.Application"
```
Additional config:
```
"sonoff": {
    "red": {
      "ip": "192.168.0.62"
    },
    "green": {
      "ip": "192.168.0.61"
    }
}
```

## Razer
- Red light turns on for Busy, In a meeting and Do Not Disturb (Presenting).
- Green light turns on for Available.
- Yellow light turns on for Away
- Purple light turns on for OutOfOffice

Use this service by stating in appsettings.json:
```
"lightservice": "OnlineStatusLight.Application.Razer.RazerLightService, OnlineStatusLight.Application.Razer"
```
Additional config:
```
"razer": {
    // true = will color the HeadSet device only. false = will color ALL Razer devices
    "headsetonly": true
}
```