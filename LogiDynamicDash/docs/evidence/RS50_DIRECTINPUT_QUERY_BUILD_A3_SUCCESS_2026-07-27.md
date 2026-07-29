# RS50 DirectInput Support Query Build A3 Success

## Result

Build A3 completed the guarded general-display-support query successfully:

```text
Query call started UTC: 2026-07-28T04:23:44.922Z
Query call completed UTC: 2026-07-28T04:23:45.039Z
Product: Logitech G HUB RS50 (USB)
VID: 0x46d PID: 0xc276
Acquired: 1
Cooperative HRESULT: 0x0
Data format HRESULT: 0x0
Acquire HRESULT: 0x0
Escape HRESULT: 0x0
Unacquire HRESULT: 0x0
Output capacity: 1 -> 1
Output byte: 0x1
Supported: 1
```

The operator observed no change to the OLED, torque, LEDs, or wheel position.
Dynamic continued to show its normal Test fallback.

The query contained no layout request, display setter, force-feedback effect,
raw HID call, input-state read, or retry.

## USB evidence

The RS50 remained USB device address `3`. The saved query capture contains 30
HID++ reports:

```text
Directions: HOST 15, DEVICE 15, UNKNOWN 0
Exact HID++ response headers: 15
HOST HID++ requests without an exact header match: 0
```

Matched Root feature discovery produced:

```text
runtime 0x0B -> feature 0x807A RPM Indicator
runtime 0x10 -> feature 0x8123 Force Feedback
runtime 0x12 -> feature 0x8130 Display Game Data
```

The `0x8130` discovery request and response were:

```text
HOST   10 ff 00 0e 81 30 00
DEVICE 12 ff 00 0e 12 00 00 ...
```

The response assigns runtime index `0x12`, public flags `0x00`, version `0`.
No operational request targeted runtime `0x12`.

The capture also contains three HOST operations on runtime `0x10`, public
feature `0x8123` Force Feedback. They surround the exclusive
Acquire/Unacquire lifecycle. This association is an inference from timing and
feature identity; it is not display traffic.

## Interpretation

The general support command returns true after discovering the public
`Display Game Data` feature. It does not render or transmit a layout. This
explains why the OLED did not change and provides a safe gate for the next
non-setter stage.

The result proves:

- the installed Logitech DirectInput driver accepts outer command `4`;
- the RS50 reports general Dynamic Display support;
- public feature `0x8130` is present at runtime `0x12` in this device session;
- the query and bounded exclusive lifecycle were physically non-mutating.

It does not yet prove:

- Layout J capability or its returned descriptor;
- that a Layout J setter activates Dynamic;
- live telemetry rendering.

The next physical stage may query only Layout J capability command `12` after
separate code review, capture preparation, and explicit authorization. No
setter is authorized by this result.
