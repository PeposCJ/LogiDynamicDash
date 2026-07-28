# RS50 Shared HID++ One-Shot Build G

## Status

Build G creates a separate physical one-shot executable. Its corrected physical
trial succeeded on 2026-07-28. The RS50 rendered all four fixed lines exactly,
the device acknowledged both transactions, and the operator observed no OLED
side effect beyond the requested frame, LED change, FFB/torque change, or wheel
movement.

`LogiDynamicDash` does not reference Build G, the physical transport, or
HidSharp. Building or running the dashboard cannot activate this route.

## Closed Operation

After six exact ordered arming arguments, Build G:

1. opens only the Build F validated COL01 and COL03 streams;
2. sends one Root discovery request for feature `0x8130`;
3. validates the exact public version-0 discovery response;
4. sends one fixed Layout J frame;
5. validates its exact acknowledgement;
6. disposes both streams immediately.

The fixed OLED text is:

```text
RS50 SHARED HIDPP
BUILD G
ONE SHOT ONLY
USBPCAP
```

The executable accepts no user text, runtime index, feature, function, layout,
report ID, raw bytes, repeat count, interval, simulator input, or telemetry.
It contains no loop, timer, task, retry, DirectInput, FFB, LED, or raw HID API.
Any missing, extra, misspelled, or reordered argument exits before the physical
exchange factory is called.

## Arming Contract

All arguments must appear exactly in this order:

```text
--arm-rs50-shared-hidpp
--confirm-ghub-closed
--confirm-rs50-awake
--confirm-dynamic-selected
--confirm-usbpcap-running
--confirm-one-fixed-frame
```

Run the source audit with:

```powershell
.\scripts\Test-Rs50SharedHidppOneShotSurface.ps1
```

## Physical Trial Contract

Each physical execution requires a separate explicit authorization after
confirming:

- G HUB is fully closed;
- iRacing and other wheel-using applications are closed;
- the RS50 is awake;
- the OLED is in Dynamic and shows the Test fallback;
- USBPcap is already capturing the RS50;
- the user is ready to observe OLED, LEDs, wheel position, and FFB behavior.

Suggested capture filename:

```text
2026-07-28_rs50_build_g_shared_hidpp_oneshot.pcapng
```

Only after those conditions are confirmed may the Release executable be run
with all six arguments. The expected visible result is the fixed four-line
message above. The process exits immediately after the acknowledgement.

The successful attempt-2 capture met every offline acceptance condition:

- exactly one Root discovery request/response;
- exactly one Layout J setter/acknowledgement;
- expected endpoint-0 HID output semantics;
- zero `0x8123`, `0x807A`, or `0x807B` operations;
- zero MI_02 / endpoint-`0x03` host output;
- no LED, FFB, torque, input, or wheel-position side effect.

If any side effect appears, no second physical run is authorized. Preserve the
capture, return the wheel to a known safe state, and analyze the trace offline.
Live telemetry and a full lap remain prohibited until a separately designed,
bounded stationary coexistence stage proves repeated shared writes do not
interfere with simulator ownership.

## First Preflight Evidence

Capture:

```text
2026-07-28_rs50_build_g_shared_hidpp_oneshot.pcapng
```

Offline extraction found device address `1.3`, 10,441 device-to-host reports,
and zero host-to-device reports carrying HID data. Therefore the failed
preflight contained no discovery, Layout J, `0x8123`, `0x807A`, or `0x807B`
operation.

The failure established that Windows top-level HID collections have distinct
device-path instance segments. Build F now accepts those distinct paths while
still requiring exactly one collection with every expected VID/PID, MI_01
marker, usage, and report-length property. Connecting multiple RS50 devices
would create duplicate matches and fail closed.

## Successful Attempt-2 Evidence

Capture:

```text
2026-07-28_rs50_build_g_shared_hidpp_oneshot_attempt2.pcapng
```

USBPcap decoded exactly:

| Frame | Direction | Transfer | Operation |
|---|---|---|---|
| 14077 | host → endpoint 0 | HID control `SET_REPORT` `0x10` | Root discovery of `0x8130` |
| 14079 | COL03 → host | interrupt input `0x12` | runtime index `0x12`, public version 0 |
| 14081 | host → endpoint 0 | HID control `SET_REPORT` `0x12` | fixed Layout J setter |
| 14083 | COL03 → host | interrupt input `0x12` | exact zero-body acknowledgement |

The discovery response arrived in 2.660 ms, the setter acknowledgement in
2.214 ms, and the complete request/response sequence took 6.618 ms.

Those were the only two host reports carrying HID data. Consequently the
capture contains zero `0x8123`, `0x807A`, or `0x807B` operations and zero
MI_02 / endpoint-`0x03` host output.
