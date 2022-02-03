# OnlineStatusLight
.Net app that controls two (green and red) Sonoff switch lights to show online status

- Current implementation for MS Teams check the Status by reading the local logs file.
- Red light turns on for Busy.
- Green light turns of for Available.
- Both lights turn on for Do Not Disturb (Presenting). Initial idea was to blink the red light but the switch makes a noise every time is activated, I had to give up that idea.