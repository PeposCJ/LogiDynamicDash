# RS50 DirectInput Support Query Build A Result

## Scope

Build A attempted exactly one documented general-display-support query:

- DirectInput device VID `046D`, PID `C276`;
- outer `DIEFFESCAPE.dwCommand = 4`;
- inner command `2`;
- version `1`;
- 12-byte input and one-byte output initialized to sentinel `0xA5`;
- nonexclusive/background cooperative level;
- no acquisition and no display setter.

The RS50 had entered power-saving sleep before the call, so this run is not a
valid physical OLED-support observation. It remains valid evidence about the
DirectInput precondition because the runtime rejected the call before a
boolean output was produced.

## Exact result

```text
Query call started UTC: 2026-07-28T03:57:11.771Z
Query call completed UTC: 2026-07-28T03:57:11.779Z
Product: Logitech G HUB RS50 (USB)
VID: 0x46d PID: 0xc276
Acquired: 0
Cooperative HRESULT: 0x0
Escape HRESULT: 0x80040205
Output capacity: 1 -> 1
Output byte: 0xa5
Supported: 0
Query failed: Documented display support query failed
```

Windows SDK `dinput.h` defines `0x80040205` as
`DIERR_NOTEXCLUSIVEACQUIRED`: the operation cannot be performed unless the
device is acquired in `DISCL_EXCLUSIVE` mode.

`Supported: 0` is not a device answer. The unchanged sentinel proves that no
valid boolean response was returned.

## Decision

Build A is retired and must not be repeated. Build A2 remains query-only but
uses a visible process-owned foreground window, requests
`DISCL_EXCLUSIVE | DISCL_FOREGROUND`, acquires immediately before the single
Escape call, and unacquires immediately afterward. It records cooperative,
Acquire, Escape, and Unacquire HRESULTs.

Build A2 still contains no layout query, display setter, force-feedback
effect, raw HID call, or retry loop. Its physical execution requires a new
baseline, an active query capture, an awake RS50 showing Dynamic/Test, and a
new explicit authorization.

## Capture analysis

The saved capture identified the RS50 at USB device address `3`. Restricting
the capture to the exact CLI interval
`03:57:11.771Z`-`03:57:11.779Z` produced:

```text
Parsed reports: 8
Directions: HOST 0, DEVICE 8, UNKNOWN 0
Report IDs: DEVICE 0x08=8
HOST HID++ requests without an exact header match: 0
```

The offline baseline/query comparator found zero HID++ reports and zero
positive exact-report deltas. The eight device-originated `0x08` reports are
ordinary non-HID++ input and were ignored by the differential analyzer.

This capture independently supports the HRESULT interpretation: Build A did
not put a Logitech HID++ request on the USB transport during the attempted
Escape call.
