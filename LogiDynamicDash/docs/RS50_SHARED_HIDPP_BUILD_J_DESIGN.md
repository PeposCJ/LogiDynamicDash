# RS50 Shared HID++ Stationary Telemetry Build J

## Status

Build J compiles as a separate, offline-validated executable for the first
shared-HID++ telemetry trial. J1 ran exactly once on 2026-07-29. Its native
process and physical/video observations passed, but its PCAP did not retain
the Layout J setter/ACK pair. J2 repeated the same bounded stationary trial
exactly once using the reviewed direct-capture method. Its local transcript
and direct PCAP contain byte-identical discovery and Layout J request/response
pairs, closing J1's evidence gap.

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
exchange, Build J gained a local write-through JSONL transcript. J2 exercised
this instrumentation on hardware and independently matched every recorded
request and response to the direct USB capture.

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

## Executed Physical J2 Evidence Trial

J2 repeated the same stationary ten-second Build J execution only to close
the evidence gap. It did not expand the allowed telemetry, duration, rate,
layout, or physical state.

J1 used a Wireshark-managed pcapng capture. This matches the previously
observed first Build I attempt, where dense TRUEFORCE traffic left
multi-second capture gaps. Build I's accepted attempt used USBPcapCMD
directly, a device-address filter, a 128 MiB buffer, and full snap length.
J2 must reuse that independently successful method.

As long as the wheel has not been reconnected, the current RS50 USB device
address is `7` on `\\.\USBPcap1`. If it has been reconnected or Windows has
renumbered it, stop and rediscover the address before capturing.

From a separate elevated PowerShell window at the repository root, start the
direct capture:

```powershell
& "C:\Program Files\USBPcap\USBPcapCMD.exe" `
  -d "\\.\USBPcap1" `
  --devices 7 `
  --inject-descriptors `
  -b 134217728 `
  -s 65535 `
  -o ".\2026-07-29_rs50_build_j2_stationary_telemetry_direct.pcap"
```

Do not route the capture through the Wireshark GUI. Leave the USBPcapCMD
window running, record video, and collect at least ten seconds of stationary
baseline. Then obtain a new explicit authorization and run the exact
nine-argument Build J command once.

The process must print the relative local transcript path under:

```text
.tmp/rs50-build-j-transcripts/
```

Continue the direct capture and video through the post-check, then stop
USBPcapCMD with `Ctrl+C`. Do not open or resave the raw `.pcap` in Wireshark
before offline analysis.

J2 acceptance additionally requires:

- transcript request/response pairs for discovery and Layout J;
- transcript response headers marked as exact matches;
- the direct PCAP contains the same request and response bytes;
- the direct PCAP contains no display transaction absent from the transcript;
- video again shows Test changing to `SPEED / 0 KMH / GEAR / N`;
- normal centering, RPM LEDs, inputs, and simulator connection afterward;
- no `0x8123` reset/gain/reset lifecycle or adverse physical effect.

J2 met every additional acceptance condition:

- the transcript contains exactly discovery and Layout J request/response
  pairs, both marked as exact header matches;
- the direct PCAP contains the same request and response bytes;
- no display transaction is absent from the transcript;
- the OLED displayed and retained `SPEED / 0 KMH / GEAR / N`;
- the operator reported normal LEDs and FFB with no physical adverse effect;
- the PCAP contains no `RESET_ALL -> SET_GLOBAL_GAINS -> RESET_ALL`
  lifecycle.

One matched `0x8123 RESET_ALL` from SW-ID `0xE` occurred 212.265 seconds
after Build J's SW-ID-`0xA` setter. It was not accompanied by
`SET_GLOBAL_GAINS` or a second reset and is not attributable to Build J.

The accepted evidence, hashes, exact frames, timing, and capture-stop note are
recorded in
[`evidence/RS50_BUILD_J2_STATIONARY_TELEMETRY_SUCCESS_2026-07-29.md`](evidence/RS50_BUILD_J2_STATIONARY_TELEMETRY_SUCCESS_2026-07-29.md).
The stationary telemetry gate is complete. A separately designed and
authorized bounded moving-car stage is still required before a full lap.
