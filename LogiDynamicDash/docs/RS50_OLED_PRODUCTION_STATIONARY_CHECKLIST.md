# RS50 OLED Production Stationary Checklist

## Status

Prepared offline only. Do not execute this checklist until the operator has
the RS50 connected, the checklist has been reviewed again, and fresh explicit
authorization has been given.

This is one ten-second stationary smoke test of the production branch. It is
not Build L, moving-car authorization, a full lap, or permission to test
reconnection by unplugging the wheel.

## Build and Offline Preconditions

1. Check out the reviewed `codex/rs50-oled-production` commit.
2. Confirm the worktree contains no staged raw capture, video, firmware, or
   user-specific file.
3. Run:

   ```powershell
   dotnet test .\LogiDynamicDash.slnx -c Release
   dotnet build .\LogiDynamicDash.slnx -c Release -warnaserror
   .\scripts\Test-Rs50OledProductionSurface.ps1
   ```

4. Copy `logidynamicdash.example.json` to a local test configuration.
5. Run both hardware-free checks:

   ```powershell
   .\LogiDynamicDash\bin\Release\net10.0\LogiDynamicDash.exe `
     --preview-all --config .\stationary-test.json

   .\LogiDynamicDash\bin\Release\net10.0\LogiDynamicDash.exe `
     --simulate-all --config .\stationary-test.json
   ```

6. Stop if either command fails or if the selected layout values are not the
   intended values.

## Physical Preparation

1. Close G HUB normally. Do not stop drivers or services.
2. Start iRacing and enter the car.
3. Remain stopped in the pits, select neutral, and hold the brake.
4. Confirm iRacing shows zero speed.
5. Wake the RS50 and select Dynamic; confirm the firmware Test fallback.
6. Confirm steering centering, inputs, FFB, and RPM LEDs are normal.
7. Start a video that clearly shows the OLED.
8. Discover the current RS50 USB address. Do not reuse an old address after a
   reconnect or reboot.
9. Start direct USBPcapCMD capture for only that address, with full snap
   length and a 128 MiB buffer.
10. Record at least ten seconds of untouched baseline.

After replacing both placeholders with the freshly discovered values, the
capture command shape is:

```powershell
& "C:\Program Files\USBPcap\USBPcapCMD.exe" `
  -d "<USBPcap filter>" `
  --devices <RS50 address> `
  --inject-descriptors `
  -b 134217728 `
  -s 65535 `
  -o ".\YYYY-MM-DD_rs50_production_stationary_direct.pcap"
```

Do not execute the command while either placeholder remains.

Suggested local names:

```text
YYYY-MM-DD_rs50_production_stationary_direct.pcap
YYYY-MM-DD_rs50_production_stationary_video.mov
```

Both artifacts remain local and ignored. Phone video may contain device and
location metadata and must not be uploaded.

## Separate Authorization

After preparation, the operator must provide a new explicit statement that:

- G HUB is closed;
- iRacing is running;
- the car is stationary in the pits;
- Dynamic shows Test;
- video and direct USBPcapCMD are active;
- baseline is complete;
- one ten-second production run is authorized.

Do not infer authorization from completion of preparation.

## Single Production Execution

Run exactly once:

```powershell
.\LogiDynamicDash\bin\Release\net10.0\LogiDynamicDash.exe `
  --enable-rs50-oled-stationary-trial `
  --confirm-ghub-closed `
  --confirm-iracing-running `
  --confirm-car-stationary-in-pits `
  --confirm-rs50-dynamic-selected `
  --confirm-10-second-limit `
  --acknowledge-no-moving-car-use `
  --config .\stationary-test.json `
  --confirm-settings
```

The process must stop automatically after ten seconds. Do not steer, rev,
change gear, move the car, touch OLED settings, open G HUB, or run it again.
If iRacing reports an on-track speed above 0.5 m/s, the application must stop
before the next OLED frame.

## Post-Check

1. Record the exact OLED content observed.
2. Gently displace the wheel and confirm normal centering/FFB.
3. Briefly rev in neutral and confirm RPM LEDs respond.
4. Confirm steering, pedals, buttons, and simulator connection remain normal.
5. Capture at least ten additional seconds.
6. Stop USBPcapCMD with `Ctrl+C`.
7. If the capture process does not stop, identify its exact process tree
   before terminating only that tree. Never use a broad process-name kill.
8. Stop video and record artifact sizes plus SHA-256 hashes.
9. Preserve the sanitized JSONL log from the local application-data
   `LogiDynamicDash/logs` directory.

## Acceptance

Accept only if:

- the process exits successfully after the ten-second bound;
- the selected layout shows values consistent with stationary telemetry;
- every sanitized host result is acknowledged, unchanged, or rate-limited;
- USBPcap contains the expected typed discovery and display setters with
  exact ACKs, with timing/counts consistent with the sanitized log;
- there is no DirectInput or `0x8123` lifecycle attributable to the process;
- FFB, LEDs, inputs, and simulator connection remain normal;
- there is no movement, torque impulse, resistance change, or disconnect.

Any failure leaves the PR in draft and moving-car testing prohibited. A
successful result closes only the production stationary gate. Build L remains
separately postponed.
