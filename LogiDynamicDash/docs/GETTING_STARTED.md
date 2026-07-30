# Getting Started

## Requirements

- 64-bit Windows
- Logitech RS50 with the OLED home screen set to `Dynamic`
- iRacing
- G HUB closed while LogiDynamicDash owns the validated OLED collections
- .NET 10 Desktop Runtime for the framework-dependent package

## Start the dashboard

1. Open `LogiDynamicDash.Configurator.exe`.
2. Select KMH or MPH.
3. Keep automatic profiles enabled for the recommended first run.
4. Set how many seconds `LAST LAP` should remain visible.
5. Select **Start dashboard**.
6. Open or join an iRacing session.

The status panel reports the OLED connection, iRacing telemetry, detected car,
and official category. The runtime waits if either the wheel or iRacing is not
available. If the RS50 sleeps or disconnects, it retries the exact validated
OLED interface every two seconds. Select **Stop** before opening G HUB.

## Profiles

Automatic selection uses this order:

1. exact CarID profile;
2. category profile;
3. reviewed built-in category recommendation;
4. the visible fallback configuration.

Use **Save for category** to customize Sports Car, Formula Car, Oval, Dirt
Oval, or Dirt Road. Use **Save for this car** after a live CarID is detected
or after inspecting a schema 2 replay.

Profiles and active settings are stored under:

```text
%LOCALAPPDATA%\LogiDynamicDash
```

## Safe shutdown

Select **Stop** and wait for `Dashboard stopped`. Stopping disposes both OLED
HID streams. The wheel firmware may retain the last frame briefly before its
normal Dynamic/Test fallback returns.

LogiDynamicDash does not send force-feedback, steering, LED, feature-report,
firmware, or bootloader commands.
