# RS50 OLED Continuous Driving Trial Checklist

## Scope

This is a manually stopped RS50 OLED driving trial with no application speed
ceiling and no automatic duration. It may be used for a complete lap after
the stationary and Build L gates have passed.

The application only reads iRacing telemetry and writes OLED frames. It does
not command steering, pedals, FFB, LEDs, torque, or vehicle controls.

No speed or elapsed-time condition stops this route. These protections remain:

- invalid or non-finite on-track speed fails closed;
- USB, HID++, protocol, or acknowledgement failure fails closed;
- `Ctrl+C` performs normal cancellation and hardware-session disposal;
- every OLED request is written once; there is no automatic write retry.

## Preparation

1. Use the reviewed continuous-driving commit and its Release binary.
2. Close G HUB normally.
3. Close every other simulator or replay session.
4. Open the intended iRacing session and start stopped in a safe location.
5. Select `Settings -> Home Screen -> Dynamic` on the RS50.
6. Confirm normal baseline FFB, LEDs, steering, pedals, buttons, simulator
   connection, and no unexpected torque or movement.
7. Start a video showing the OLED and, when practical, simulator telemetry.
8. Use a visible PowerShell console so `Ctrl+C` remains available.
9. Wireshark and USBPcap are not required; sanitized application and OLED logs
   remain active.

## Launch

From the repository root:

```powershell
.\LogiDynamicDash\bin\Release\net10.0\LogiDynamicDash.exe `
  --enable-rs50-oled-driving-trial `
  --confirm-ghub-closed `
  --confirm-iracing-running `
  --confirm-controlled-driving-session `
  --confirm-rs50-dynamic-selected `
  --acknowledge-no-speed-limit `
  --acknowledge-manual-stop-required `
  --config .\.tmp\stationary-test.json `
  --confirm-settings
```

## Driving and shutdown

1. Begin driving normally only after live speed and gear appear.
2. Observe speed, gear, RPM gauge, and thin speed indicator during the lap.
3. If any OLED, FFB, LED, control, connection, torque, or resistance behavior
   becomes abnormal, stop the car safely and press `Ctrl+C`.
4. At the end of the intended run, stop the car in a safe location.
5. With the car stopped, focus the visible PowerShell console and press
   `Ctrl+C` once.
6. Wait for the process to exit before reopening G HUB.
7. Preserve the newest `application-*.jsonl` and `rs50-oled-*.jsonl` logs.

## Acceptance

Accept only if:

- displayed speed and gear remain coherent throughout the run;
- RPM and speed gauges update without persistent stale frames;
- the application log contains continuous connected telemetry;
- OLED results are acknowledged, unchanged, or rate-limited, with no failure;
- manual `Ctrl+C` produces a clean stopped state and close event;
- FFB, LEDs, steering, pedals, buttons, and simulator connection remain
  normal;
- there is no unexpected movement, torque impulse, resistance change, or
  disconnect.

Passing this trial validates continuous physical use for the tested RS50. It
does not establish Logitech PRO compatibility, automatic reconnection, signed
release readiness, or vendor support.
