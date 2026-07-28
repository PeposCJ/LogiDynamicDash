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

No immediate LED, torque, force-feedback, or wheel-movement change was
reported during the stationary ten-second observation. Subsequent stationary
checking found that the shift LEDs no longer updated and the normal
force-feedback centering did not return the wheel to center. The wheel instead
moved slightly counterclockwise.

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

The capture also contains three matched operations on runtime `0x10`, public
feature `0x8123` Force Feedback, surrounding the exclusive lifecycle:

```text
start +0.000 s  function 1  parameters 00 00 00
start +0.010 s  function 8  parameters FF FF 00 ...
end   +9.070 s  function 1  parameters 00 00 00
```

Their timing and feature identity correlate with the observed loss of normal
iRacing LED/FFB behavior. The exact semantics of those operations have not
yet been established.

Leaving and re-entering the car did not recover the shift LEDs. Returning to
the iRacing main menu and loading the circuit again did recover them, which is
consistent with the simulator process rebuilding its Logitech output state.

## Interpretation

This remains the first end-to-end confirmation that iRacing telemetry can travel
through LogiDynamicDash, the guarded DirectInput bridge, Logitech's
Display Game Data feature `0x8130`, and the RS50 firmware-rendered Dynamic
OLED.

It is not an acceptance of the streaming design. Exclusive foreground
acquisition is required by the installed driver and disrupted the simulator's
LED and FFB state beyond the ten-second session. A full-lap or moving-car test
is prohibited until a transport or lifecycle design demonstrates safe
coexistence without taking persistent exclusive ownership from iRacing.
