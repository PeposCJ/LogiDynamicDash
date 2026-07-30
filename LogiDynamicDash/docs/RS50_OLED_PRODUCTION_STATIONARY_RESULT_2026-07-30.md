# RS50 Production Stationary Result — 2026-07-30

## Scope

One separately authorized execution of commit `26c263d` was attempted with:

- G HUB closed normally;
- iRacing running;
- the car stopped in the pits, neutral, brake held, and zero speed;
- RS50 awake with Dynamic showing the firmware Test fallback;
- video and direct USBPcap capture active;
- FFB, LEDs, steering, pedals, buttons, and connection normal at baseline.

No repeat, moving-car test, or Build L was authorized.

## Outcome

The process exited safely after approximately 1.6 seconds with the localized
Windows error `Controlador no válido`. The OLED remained on Test. Post-checks
confirmed normal FFB, RPM LEDs, steering, pedals, buttons, and simulator
connection, with no unexpected motion, torque, resistance, or disconnect.

The stationary production gate did not pass because no OLED transaction was
sent.

## Evidence

Local capture:

```text
2026-07-30_rs50_production_stationary_direct.pcap
bytes: 189673676
sha256: AA0A7DD9C8AE7B9528D1DE526CAF1E837D69717DEE944F457F38B9F8D5595E41
```

The capture remains local and ignored. It contains the expected RS50 USB
identity and normal simulator traffic but no production discovery request
`10 FF 00 0A 81 30 00`. No sanitized OLED log was created.

## Root Cause

The physical display is the second member of a composite display. The console
dashboard initializes first. The automated process host redirects standard
output and has no interactive Windows console handle, so cursor/title
operations failed before the physical session factory was called.

This explains all observations:

- no OLED change;
- no `0x8130` discovery request in USBPcap;
- no local OLED diagnostic, which was created only after physical exchange
  construction;
- normal post-check behavior.

## Correction and Next Gate

`ConsoleDashboard` now detects redirected output and becomes a silent
secondary display. A disappearing interactive console also no longer masks
safe OLED shutdown. The correction is covered offline.

Any physical retry requires a fresh capture, baseline, and explicit
authorization. The prior authorization was consumed. The PR remains draft,
and moving-car testing remains prohibited.

## Corrected-build retry (`ed96f3a`)

A second, separately authorized stationary execution completed its bounded
10-second window with exit code 0. The RS50 displayed:

```text
IRACING
WAITING
```

The local sanitized OLED log records a successful discovery/open, one
acknowledged Layout H frame, and a clean close:

```text
C:\Users\Dr. Con\AppData\Local\LogiDynamicDash\logs\
rs50-oled-20260730-065303-0149395.jsonl
```

The physical post-check confirmed normal FFB, RPM LEDs, steering, pedals,
buttons, and connection, with no unexpected motion, torque, resistance, or
disconnect. This passes the production transport, acknowledgement, shutdown,
and coexistence portions of the stationary gate.

The Wireshark GUI capture was saved locally and remains ignored:

```text
2026-07-30_rs50_production_stationary_retry.pcapng
bytes: 141302412
sha256: 32B09261A4C25EEA717C93E41A2ED5E757CAA69A3B27B66663FE33936252DC7B
```

The full telemetry-rendering portion remains open because the OLED did not
advance from `WAITING` to stationary speed and gear. A subsequent
hardware-free recording proved that the iRacing SDK can connect and identify
an active session. The operator later confirmed that a separate dirt-oval
iRacing window was open at the same time; that session produced the recorded
moving-car telemetry and was not the intended stationary scenario. The
recording is therefore neither stationary-gate evidence nor evidence of an
SDK-reader defect.

The next bounded build adds a separate sanitized application log that records
each accepted render trigger, state, mode, speed, and gear so one run can
distinguish missing telemetry callbacks from a display scheduling or
formatting fault. Only the intended stationary simulator session may remain
open for that run.

## Single-session diagnostic retry (`612f36b`)

With only the intended iRacing simulator session active, one separately
authorized 10-second stationary execution completed with exit code 0.

The sanitized application log records:

- connection established approximately 139 ms after the initial render;
- an on-track session identity;
- neutral gear (`0`);
- speed between approximately `0.0003` and `0.0019 m/s`, safely inside the
  stationary limit;
- continuous normal-mode telemetry renders for the full bounded window;
- clean application shutdown.

The OLED diagnostic records an acknowledged initial Layout H connection
frame, followed by acknowledged Layout E telemetry frames and clean close.
Physical photo evidence confirms that the RS50 left `IRACING / WAITING` and
displayed:

```text
0 KMH    N
[graphical bar]
```

This passes the stationary end-to-end telemetry path from iRacing through
formatting, HID++ transport, acknowledgement, and physical OLED rendering.
The operator's post-check found FFB, LEDs, direction, pedals, buttons, and
connection normal, with no apparent unexpected torque or movement.

**Final result: the complete stationary production safety gate passed.**
Moving-car validation remains a separate, explicitly bounded milestone and
is not authorized by this result.
