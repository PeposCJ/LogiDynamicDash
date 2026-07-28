# RS50 Bounded Dynamic Telemetry Checklist

This checklist governed the first Build D physical trial. It sends formatted
iRacing telemetry for at most ten seconds and then releases DirectInput
acquisition automatically. It is not an endurance test or authorization for
an unlimited session.

The first trial completed successfully on 2026-07-27. See
[`evidence/RS50_DYNAMIC_TELEMETRY_SUCCESS_2026-07-27.md`](evidence/RS50_DYNAMIC_TELEMETRY_SUCCESS_2026-07-27.md).

## 1. Preconditions

1. Obtain separate explicit authorization for one ten-second Build D trial.
2. Keep G HUB and its updater/agent processes closed.
3. Wake the RS50 and set `Settings -> HomeScreen -> Dynamic`.
4. Confirm the OLED shows either Test or the previous static Build C text.
5. Start iRacing in a replay or safe stationary session with changing speed
   and gear telemetry.
6. Keep hands clear of the wheel and make the stop/power control accessible.

## 2. Baseline

Capture the current RS50 USB controller for five seconds without running
LogiDynamicDash. Save:

```text
2026-07-27_rs50_dynamic_telemetry_baseline.pcapng
```

## 3. Ten-Second Trial

Start a fresh USBPcap capture on the same controller. From the repository
root, run exactly:

```powershell
dotnet run `
  --project .\LogiDynamicDash\LogiDynamicDash.csproj `
  --configuration Release `
  -- `
  --enable-rs50-oled `
  --confirm-exclusive-layout-j-stream `
  --confirm-telemetry-transmission `
  --confirm-10-second-trial
```

All four application arguments and their order are mandatory. Missing,
partial, extra, or reordered arguments exit before creating a native window
or opening DirectInput.

The trial must stop itself after ten seconds. Do not repeat it, start G HUB,
or send SetIdle afterward.

## 4. Observe and Save

Record:

- whether speed and gear change on the OLED;
- whether brake bias and last-lap temporary screens appear if their telemetry
  changes;
- any display flicker, partial row, stale value, or unexpected fallback;
- any torque, LED, or wheel-movement change;
- the console exit result.

Save the trial capture as:

```text
2026-07-27_rs50_dynamic_telemetry_attempt_1.pcapng
```

## 5. Offline Acceptance

Do not authorize another run until offline analysis confirms:

- all feature-`0x8130` function-`3` requests have matched responses;
- no HID++ error response occurred;
- the observed rate never exceeds five function-`3` requests per second;
- consecutive identical payloads are absent;
- the first wire payload uses Layout J and matches the console frame;
- acquisition and release completed successfully.

Any failed Escape, unmatched request, physical side effect, excessive update
rate, or malformed display is a stop condition. Only after this bounded trial
passes may an unlimited user-facing session be designed.

The first trial passed the USB protocol checks but failed physical coexistence
acceptance: afterward, iRacing's shift LEDs stopped updating and normal FFB
centering was absent until the simulator returned to its main menu and loaded
the circuit again. No moving-car, input-continuity, endurance, or full-lap
trial is authorized. The required exclusive lifecycle must be redesigned or
replaced before further physical streaming.
