# RS50 DirectInput Support Query Build A2 Result

## Scope

Build A2 preserved the sole documented display-support request from Build A
and added the DirectInput condition proven by
`DIERR_NOTEXCLUSIVEACQUIRED`:

- visible process-owned foreground window;
- `DISCL_EXCLUSIVE | DISCL_FOREGROUND`;
- one `Acquire` attempt;
- one Escape only if acquisition succeeded;
- immediate `Unacquire` after Escape;
- no layout query, setter, effect, raw HID call, or retry.

## Exact result

```text
Query call started UTC: 2026-07-28T04:18:02.323Z
Query call completed UTC: 2026-07-28T04:18:02.323Z
Product: Logitech G HUB RS50 (USB)
VID: 0x46d PID: 0xc276
Acquired: 0
Cooperative HRESULT: 0x0
Acquire HRESULT: 0x80070057
Escape HRESULT: 0x8000ffff
Unacquire HRESULT: 0x8000ffff
Output capacity: 1 -> 1
Output byte: 0xa5
Supported: 0
Query failed: Exclusive foreground device acquisition failed
```

`0x80070057` is `E_INVALIDARG`, returned by DirectInput as
`DIERR_INVALIDPARAM`. Microsoft documents that a data format must be set with
`SetDataFormat` or `SetActionMap` before `Acquire`, even when the application
does not intend to read input state:
[`IDirectInputDevice8::Acquire`](https://learn.microsoft.com/en-us/previous-versions/windows/desktop/ee417818%28v%3Dvs.85%29).

The `Escape` and `Unacquire` fields remain their initialized
`E_UNEXPECTED` values because acquisition never succeeded. The unchanged
sentinel is not a negative device-support answer.

## Capture analysis

The RS50 remained USB device address `3`. The saved query capture spans
`04:17:34.830862Z` through `04:18:02.320021Z`; its final packet precedes the
failed Acquire by approximately 3 ms. The offline analysis found:

```text
Candidate HID++ reports: 0
Candidate invalid lines: 0
Candidate non-HID++ reports ignored: 0
Positive exact-report count deltas:
  (none)
```

Therefore Build A2 produced no host transport request.

## Decision

Build A2 is retired and must not be repeated. Build A3 adds only
`SetDataFormat(&c_dfDIJoystick2)` before the exclusive acquisition. It records
the data-format HRESULT separately and preserves the one-query, immediate
release, no-setter boundary.
