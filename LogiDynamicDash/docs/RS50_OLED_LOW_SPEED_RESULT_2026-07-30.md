# RS50 OLED Build L Low-Speed Result — 2026-07-30

## Scope

One separately authorized Build L execution used commit `d277ad0` with:

- one intended iRacing simulator session active;
- G HUB closed;
- the car starting stopped in a controlled pit-lane area;
- RS50 awake with Dynamic selected;
- the 15-second automatic bound and 20 km/h hard limit armed.

No retry, full lap, or unrestricted moving-car run was authorized.

## Recorded result

The process completed its 15-second window with exit code 0. The sanitized
application log records:

```text
application-20260730-074655-8459243.jsonl
telemetry renders: 69
minimum speed: 0 km/h
maximum speed: 14.02 km/h
gears: N, 1, 2
final state: Stopped
```

The sanitized OLED log records:

```text
rs50-oled-20260730-074655-9395556.jsonl
frames: 72
acknowledged: 55
rate-limited: 2
unchanged: 15
failures: 0
close events: 1
layouts: H, E
```

The maximum recorded speed remained below both the intended 15 km/h driving
target and the armed 20 km/h hard limit.

## Physical OLED evidence

The operator observed speed, `KMH`, gears, and the graphical bar updating.
The supplied photo visibly shows:

```text
14 KMH    2
```

Layout E receives two normalized gauges from this application:

- the main gauge is RPM divided by the configured 8000 RPM maximum;
- the small secondary gauge is speed divided by the configured 300 km/h
  gauge maximum.

The small gauge was initially described as an accelerator indicator because
it changed while the accelerator was pressed. The application does not
currently request or transmit iRacing `Throttle`; it sends a second normalized
byte derived from speed. At 14 km/h that byte represents approximately 4.7%,
consistent with the small filled segment in the photo.

The firmware unquestionably draws the visual bar. The current observation
alone cannot rule out the possibility that firmware also overlays a locally
known pedal value. This remains an explicit hypothesis, not a confirmed
throttle channel. A stationary neutral pedal-sweep test can distinguish it:
speed and the transmitted thin-indicator byte remain zero while accelerator
input and RPM change.

## Gate status

OLED telemetry coherence, bounded speed, acknowledgement, and clean shutdown
passed. The operator's post-check found FFB, LEDs, steering, pedals, buttons,
and simulator connection normal, with no apparent unexpected torque,
resistance, movement, or disconnect.

**Final result: the complete Build L low-speed safety gate passed.**

This result does not authorize a full lap or unrestricted moving-car mode.

## Stationary pedal-sweep follow-up

A later, separately authorized stationary run kept the car in neutral at
effectively zero speed while the accelerator was pressed and released. The
operator observed the main RPM bar moving and did not observe the small
indicator moving. This favors the implemented interpretation:

- the main gauge follows the normalized RPM byte;
- the thin indicator follows the normalized speed byte;
- there is no current evidence that firmware overlays an autonomous
  accelerator indicator.

The run stopped safely after approximately four seconds because one Layout E
write did not receive a matching acknowledgement within the configured
16-report scan. Multiple changing Layout E frames had been acknowledged before
that failure. The local diagnostic recorded the typed `IOException`, faulted
state, and clean close; the operator found FFB, LEDs, controls, and connection
normal afterward. No retry was performed.

After the host session closes, the OLED retains the last acknowledged frame.
The operator observed that the firmware returns to its Dynamic `Test` fallback
on its own several minutes later. The host currently sends no explicit clear
frame on shutdown.

## ACK-window correction and validation

The failed pedal-sweep run showed that 16 unrelated HID reports could arrive
in approximately 56 ms before the matching OLED acknowledgement. The adapter
was corrected to scan within two simultaneous bounds: at most 256 reports and
at most 500 ms. It still writes each request exactly once and fails closed if
no matching response arrives.

One attempted validation was safely rejected before mobile telemetry could be
sent because a different active simulator session reported second gear and
approximately 65.25 km/h. After closing that session, one separately
authorized stationary retry completed the full 10 seconds:

```text
telemetry renders: 45
maximum speed: 0.002 km/h
gear: N
OLED frames: 49
acknowledged: 38
rate-limited: 3
unchanged: 8
failures: 0
close events: 1
final state: Stopped
```

The operator again observed only the main RPM bar moving during accelerator
pulses; the thin indicator did not appear to move at zero speed. FFB and LEDs
remained normal. This validates the corrected multiplexed-ACK window on
physical hardware and provides repeat evidence against a firmware-generated
accelerator indicator.
