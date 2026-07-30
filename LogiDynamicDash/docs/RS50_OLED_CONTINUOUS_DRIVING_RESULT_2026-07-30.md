# RS50 OLED Continuous Driving Result — 2026-07-30

## First physical run (`7d1c10f`)

The manually stopped, no-speed-limit route ran for 48.19 seconds before its
then-current missing-acknowledgement policy faulted the session. Recorded
telemetry and OLED results were:

```text
telemetry renders: 220
maximum speed: 40.33 km/h
gears: N, 1
OLED frames: 222
acknowledged: 106
rate-limited: 2
unchanged: 114
close events: 1
```

The operator physically observed the display continuing to change with live
telemetry. The main bar was confirmed as RPM and the thin bar below it as
speed, correcting the earlier accelerator interpretation.

One later Layout E setter was visibly applied but its matching acknowledgement
did not appear within five unrelated reports and the bounded 500 ms response
window. The application closed safely under the conservative policy. The run
did not complete the intended driving session, so the continuous-driving gate
remains open.

## Missing-acknowledgement policy correction

The setter has no sequence or payload correlation in its acknowledgement.
Physical evidence shows that an OLED frame can be applied even when its ACK is
not observed by the host. Treating one missing setter ACK as a permanent
device failure makes continuous use unnecessarily fragile.

The corrected policy is:

- discovery still requires an exact response;
- a received setter response still receives exact protocol validation;
- real USB I/O, device, and protocol errors remain fatal;
- one missing setter ACK is recorded as `unacknowledged`;
- the unacknowledged frame is not automatically retried;
- identical-frame suppression and the 5 Hz change limit still apply;
- later changed telemetry continues through the same open session.

The correction is covered by deterministic tests but still requires a fresh
physical continuous-driving validation.

## Corrected continuous-driving run (`99ae6dd`)

One separately authorized run exercised the corrected policy until the
operator manually stopped it with `Ctrl+C`:

```text
duration: 306.77 seconds
telemetry renders: 1404
minimum speed: 0 km/h
maximum speed: 200.31 km/h
final speed: 0 km/h
gears: N, 1, 2, 3, 4, 5, 6
OLED frames: 1520
acknowledged: 1165
unacknowledged: 5
rate-limited: 116
unchanged: 234
fatal failures: 0
close events: 1
final state: Stopped
```

Every unacknowledged frame was followed by a valid acknowledgement
approximately 0.21–0.23 seconds later. The operator observed the OLED updating
throughout the complete run, with the main RPM gauge, thin speed indicator,
numeric speed, and gear behaving coherently.

FFB, LEDs, steering, pedals, buttons, and simulator connection remained
normal, with no apparent unexpected torque, resistance, movement, or
disconnect. The car was stopped before `Ctrl+C`, and cancellation produced a
clean application and physical-session close.

## Last-lap mode transition

When the car crossed the finish line, the physical OLED automatically showed
`LAST LAP` and the completed lap time. This validates the live control path
that detects a changed positive `LapLastLapTime`, selects the temporary
`LastLap` display mode, formats the configured last-lap Layout J page, and
returns to normal telemetry after its configured display window.

**Final result: the continuous-driving physical gate passed on the RS50.**

This confirms the tested RS50 path at real driving speed. Logitech PRO
compatibility, reconnection behavior, signed release readiness, and vendor
support remain separate claims.
