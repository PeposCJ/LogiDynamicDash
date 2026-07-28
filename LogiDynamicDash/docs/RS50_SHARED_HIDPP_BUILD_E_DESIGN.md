# RS50 Shared HID++ Display Build E

## Status

Build E is an offline-only protocol and session implementation for the RS50
Dynamic OLED. It does not enumerate, open, read, or write a physical HID
collection and is not reachable from the application command line.

Its purpose is to replace the unsafe DirectInput lifecycle with a future
transport confined to the RS50's separate HID++ interface. The first
DirectInput telemetry trial proved OLED output but emitted Force Feedback
`RESET_ALL`, `SET_GLOBAL_GAINS(0xFFFF)`, and `RESET_ALL`, disrupting iRacing's
LED and FFB state. Build E contains none of those operations.

## Evidence Boundary

The successful USBPcap established these base-device transactions:

```text
Discover 0x8130:
HOST   10 FF 00 0B 81 30 00
DEVICE 12 FF 00 0B 12 00 00 ... zero padding

Set Layout J:
HOST   12 FF 12 3B 09 [19] [10] [19] [10] 00
DEVICE 12 FF 12 3B 00 ... zero body
```

Build E assigns software ID `0xA`, so its offline frames use `0x0A` and
`0x3A` instead of the captured driver's `0x0B` and `0x3B`. The feature runtime
index is never hardcoded: Root discovery must return public version-0 feature
`0x8130` before a Layout J transaction can be created.

## Closed Surface

Production code exposes exactly two transaction kinds:

1. Root `GetFeature(0x8130)`.
2. Feature-`0x8130` function-`3`, Layout J index `9`.

Callers cannot choose another:

- device index;
- feature ID or runtime index;
- function ID;
- software ID;
- layout index;
- report ID;
- arbitrary parameter buffer.

Every Layout J row first passes the existing canonical printable-ASCII and
`19/10/19/10` validation.

The exchange boundary accepts only the closed transaction object. The Build E
protocol and dashboard session provide no class that implements this boundary
against hardware. Build F now compiles such an adapter in a separate library
that the application does not reference. `Program` still has no shared-HID++
arming argument or construction path.

## Response Validation

Discovery and setter responses must:

- be exactly 64 bytes;
- use report ID `0x12`;
- address base device `0xFF`;
- echo the exact runtime/function/software header;
- not be a HID++ error response;
- preserve the expected public flags and version;
- contain zero in every reserved or acknowledgement byte.

Any exchange or validation failure permanently faults the session. No retry
or subsequent setter is allowed on that session.

## Stream Guards

The session:

- discovers the feature once per open;
- suppresses equal consecutive frames;
- limits changed frames to five per second;
- transmits no clear, idle, LED, FFB, profile, firmware, or unknown command;
- sends nothing when closed or disposed;
- performs no device operation during local close.

## Offline Validation

`Rs50HidppDisplayProtocolTests` and
`Rs50SharedHidppDisplaySessionTests` cover:

- exact discovery and captured Layout J framing;
- runtime/flags/version/header/body validation;
- HID++ Busy/Unsupported-style error rejection;
- invalid runtime rejection;
- lifecycle ordering;
- deduplication and the exact 200 ms boundary;
- fail-closed behavior;
- disposal.

Run the source-surface audit with:

```powershell
.\scripts\Test-Rs50SharedHidppSurface.ps1
```

It fails if the Build E source gains physical HID APIs, DirectInput,
Acquire/Unacquire, native imports, Force Feedback `0x8123`, RPM/LIGHTSYNC
features, other `0x813x` features, a production exchange implementation, a
public protocol type, or a CLI route.

## Gate Before Any Physical Use

Build F added one disconnected physical exchange implementation after:

1. read-only descriptor evidence identified COL01 and COL03;
2. VID `0x046D`, PID `0xC276`, interface, usage, report lengths, and exactly
   one matching collection became fail-closed gates;
3. a new surface audit proved the adapter cannot target any feature except
   Build E's closed `0x8130` transactions.

Windows control-transfer behavior remains unverified. G HUB must remain closed
to avoid two HID++ producers, and the user must separately authorize a
captured, stationary, one-frame test through a future one-shot route.

Acceptance for that future test requires USBPcap to show only feature
discovery and one matched Layout J setter, with zero `0x8123`, `0x807A`,
`0x807B`, interface-2, or endpoint-`0x03` host output. LEDs and FFB must remain
normal both during and after the test. A full lap remains prohibited until a
subsequent bounded stream and stationary input-continuity test also pass.

See [`RS50_SHARED_HIDPP_BUILD_F_DESIGN.md`](RS50_SHARED_HIDPP_BUILD_F_DESIGN.md).
