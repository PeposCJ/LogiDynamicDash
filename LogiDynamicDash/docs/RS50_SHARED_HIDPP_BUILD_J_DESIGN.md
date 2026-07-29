# RS50 Shared HID++ Stationary Telemetry Build J

## Status

Build J compiles as a separate, offline-validated executable for the first
shared-HID++ telemetry trial. J1 ran exactly once on 2026-07-29. The native
process and physical/video observations passed, but the PCAP did not retain
the Layout J setter/ACK pair, so its strict USB acceptance remains
inconclusive.

The trial reads only iRacing `IsOnTrackCar`, `Gear`, and `Speed`. While iRacing
reports the car on track and stationary, it renders:

```text
SPEED
0 KMH
GEAR
N
```

The numeric rows come only from live telemetry. Identical frames are
suppressed, changed frames are limited to 5 Hz, and execution ends after ten
seconds. Missing telemetry, leaving the car, invalid gear, negative/non-finite
speed, speed above 0.5 m/s, protocol failure, or an early telemetry shutdown
fails closed and disposes the HID++ exchange.
After the first connected telemetry update, a simulator telemetry disconnect
also fails closed immediately.

This is not authorization for moving-car use or a full lap.

## Isolation

Build J references only:

- `SVappsLAB.iRacingTelemetrySDK` 2.1.0;
- the typed shared-HID++ protocol;
- the validated shared-HID++ transport.

It does not reference the `LogiDynamicDash` application or native DirectInput
bridge. It contains no DirectInput acquisition, FFB, LED, raw HID, retry, or
caller-controlled display operation. The main dashboard does not reference
Build J or copy its physical transport.

Run the source audit with:

```powershell
.\scripts\Test-Rs50SharedHidppTelemetryTrialSurface.ps1
```

## Arming Contract

All arguments must appear exactly in this order:

```text
--arm-rs50-shared-hidpp-telemetry
--confirm-ghub-closed
--confirm-iracing-running
--confirm-car-stationary-in-pits
--confirm-rs50-awake
--confirm-dynamic-selected
--confirm-usbpcap-running
--confirm-video-recording
--confirm-10-second-telemetry-trial
```

No physical command should be run until the operator supplies a fresh,
explicit authorization after completing every preparation below.

## Offline Validation

The fake-only verifier has no direct transport reference and cannot open the
RS50. It covers:

- rejection before device or telemetry access unless all arguments match;
- exact speed and gear formatting;
- disconnected-frame suppression;
- identical-frame suppression and the 200 ms minimum interval;
- immediate stop before a setter when speed exceeds 0.5 m/s;
- immediate failure after a connected telemetry stream disconnects;
- failure on an early telemetry-source return;
- failure and disposal on an invalid acknowledgement.

Run it with:

```powershell
dotnet run --project `
  .\Rs50SharedHidppTelemetryTrial.Verification\Rs50SharedHidppTelemetryTrial.Verification.csproj `
  -c Release --no-restore
```

The standard xUnit tests also compile. Local execution may be blocked by
Windows Code Integrity policy
`{0283ac0f-fff1-49ae-ada1-8a933130cad6}`; that policy must not be disabled or
modified.

## Local Transaction Transcript

After J1's USBPcap failed to retain the physically successful setter/ACK
exchange, Build J gained a local write-through JSONL transcript. This
instrumentation has not yet been run against hardware.

For a physically armed run, it creates an automatically named file under:

```text
.tmp/rs50-build-j-transcripts/
```

Each exchange writes and flushes a request event before transmission, then
writes either the exact response and elapsed microseconds or only the
exception type. It never records an exception message, device path, general
telemetry stream, FFB, LED traffic, or unrelated HID reports. The path cannot
be selected by a caller and the file is ignored by Git.

The transcript is bounded to 52 transactions: one discovery plus the
mathematical maximum of 51 telemetry frames in a ten-second trial whose first
frame may be immediate and whose subsequent frames are limited to 5 Hz.
Reaching the bound fails closed before another request is transmitted.
Logging adds no device operation and does not replace independent USBPcap and
video evidence.

## Executed Physical J1 Trial

Use a fresh capture:

```text
2026-07-29_rs50_build_j_iracing_stationary_telemetry_10s.pcapng
```

Preparation:

1. close G HUB and its updater;
2. connect and wake the RS50;
3. start iRacing, enter the car, and remain stopped in the pits;
4. select neutral, hold the brake, and leave the engine idling;
5. confirm Dynamic currently shows the firmware Test screen;
6. confirm normal centering with one small, gentle steering displacement;
7. briefly rev in neutral and confirm the shift LEDs respond;
8. return to idle and reconfirm `Speed 0 km/h`, `Gear N`;
9. start USBPcapCMD filtered to the RS50 device address with a 128 MB buffer;
10. start a video recording that clearly shows the OLED;
11. capture at least ten seconds of untouched baseline traffic.

After a separate authorization, run Build J exactly once. Do not move the car,
change gear, rev the engine, steer, or touch the OLED settings during its ten
seconds.

Run the already validated Release executable with this exact command:

```powershell
& .\Rs50SharedHidppTelemetryTrial\bin\Release\net10.0\Rs50SharedHidppTelemetryTrial.exe `
  --arm-rs50-shared-hidpp-telemetry `
  --confirm-ghub-closed `
  --confirm-iracing-running `
  --confirm-car-stationary-in-pits `
  --confirm-rs50-awake `
  --confirm-dynamic-selected `
  --confirm-usbpcap-running `
  --confirm-video-recording `
  --confirm-10-second-telemetry-trial
```

Continue capturing after the process closes:

1. verify the OLED displayed `SPEED / 0 KMH / GEAR / N`;
2. repeat the same small centering check;
3. briefly rev in neutral and confirm the shift LEDs still respond;
4. confirm inputs and simulator connection remain normal;
5. capture at least ten more seconds;
6. stop and save USBPcap without driving;
7. stop and save the video.

## Acceptance Conditions

Accept J1 only if all conditions pass:

- the process exits successfully after the bounded trial;
- at least one stationary telemetry frame is acknowledged;
- OLED text matches the live stopped state;
- display traffic uses only the shared endpoint-0 HID++ route;
- no `0x8123 RESET_ALL -> SET_GLOBAL_GAINS -> RESET_ALL` lifecycle appears;
- legitimate TRUEFORCE and rev-light traffic remains continuous/equivalent;
- centering, rev LEDs, inputs, and simulator connection work afterward;
- there is no movement, torque impulse, unexpected resistance, or disconnect;
- the high-buffer capture contains the complete discovery/setter/ACK evidence.

Any failure keeps moving-car telemetry and full-lap testing prohibited.

J1 met the native, OLED, centering, LED, input, and physical-safety
conditions. It did not meet the complete-capture condition: offline analysis
found the `0x8130` discovery but no setter/ACK pair. Complete evidence and the
decision boundary are recorded in
[`evidence/RS50_BUILD_J_STATIONARY_TELEMETRY_RESULT_2026-07-29.md`](evidence/RS50_BUILD_J_STATIONARY_TELEMETRY_RESULT_2026-07-29.md).
Do not repeat J1 or advance to moving-car testing without a new reviewed
capture plan and fresh authorization.
