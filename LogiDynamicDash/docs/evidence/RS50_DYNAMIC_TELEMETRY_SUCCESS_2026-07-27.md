# RS50 Dynamic Telemetry Success

## Result

Build D completed its first authorized physical trial on 2026-07-27. With
iRacing running and the car stationary in the pits, LogiDynamicDash acquired
the RS50, displayed live telemetry for ten seconds, and released the device
automatically.

The operator observed:

```text
SPEED
0 KMH
GEAR
N
```

No LED, torque, force-feedback, or wheel-movement change was observed.

## Guarded Execution

The Release build required the four exact ordered arming arguments documented
in the operator checklist. Its console progressed through:

```text
IRACING / WAITING
SPEED / N/A / GEAR / ?
SPEED / 0 KMH / GEAR / N
```

The process exited successfully after its ten-second cancellation boundary.

## USBPcap Evidence

The local capture is:

```text
2026-07-27_rs50_build_d_live_telemetry_trial_1.pcapng
```

SHA-256:

```text
1E3F92EFAAAAE885023D012DE65C75E072C49EBF572935B8D1E7EEAB1688A61C
```

Raw captures remain local and are not committed.

Offline, device-scoped analysis at USB address `3` extracted 68,001 reports.
All 29 HOST HID++ requests had exact DEVICE response headers, with zero
unmatched requests and zero invalid records. Display Game Data runtime `0x12`
contained exactly three function-`3` setters:

1. `IRACING / WAITING / "" / ""`
2. `SPEED / N/A / GEAR / ?`
3. `SPEED / 0 KMH / GEAR / N`

All three setters had matched responses. Their payloads were distinct, the
rate was below the five-updates-per-second limit, and no HID++ error response
was present. The same capture reconstructed the ten-layout catalog and Layout
J capacities `19/10/19/10`.

## Interpretation

This is the first end-to-end confirmation that iRacing telemetry can travel
through LogiDynamicDash, the guarded DirectInput bridge, Logitech's
Display Game Data feature `0x8130`, and the RS50 firmware-rendered Dynamic
OLED.

Exclusive foreground acquisition remains required by the installed driver.
Before driving a full lap, a separate stationary input-continuity trial must
confirm that iRacing continues receiving steering and pedal inputs while the
OLED stream owns the DirectInput device.
