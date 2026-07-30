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
