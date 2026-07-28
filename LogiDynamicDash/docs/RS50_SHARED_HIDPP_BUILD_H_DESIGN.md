# RS50 Shared HID++ Bounded Stream Build H

## Status

Build H compiles a separate bounded-stream executable but has not been run
against the physical RS50. No Build H device enumeration, stream open, request,
or report transmission has occurred.

`LogiDynamicDash` does not reference Build H, the physical transport, or
HidSharp. Building or running the dashboard cannot activate this route.

## Fixed Sequence

After seven exact ordered arming arguments, Build H:

1. opens only the validated Build F COL01/COL03 pair;
2. performs one Root discovery of `0x8130`;
3. sends exactly five fixed Layout J frames at 1 Hz;
4. validates every acknowledgement before continuing;
5. disposes both streams immediately after frame 5 or any failure.

Each frame contains:

```text
RS50 SHARED HIDPP
BUILD H
FRAME n OF 5
1 HZ
```

where `n` is fixed in source as 1 through 5. The implementation spells out all
five sends and four one-second delays. It has no execution loop, retry, timer,
task, simulator input, telemetry, caller text, duration, rate, count, feature,
function, layout, report ID, or raw-byte argument.

## Arming Contract

All arguments must appear exactly in this order:

```text
--arm-rs50-shared-hidpp-bounded-stream
--confirm-ghub-closed
--confirm-iracing-closed
--confirm-rs50-awake
--confirm-dynamic-selected
--confirm-usbpcap-running
--confirm-one-hz-five-fixed-frames
```

Run the source audit with:

```powershell
.\scripts\Test-Rs50SharedHidppBoundedStreamSurface.ps1
```

## Future Physical Stages

Compiling Build H does not authorize running it.

### H1: Repetition Baseline

The first physical execution requires G HUB and iRacing closed, RS50 awake,
Dynamic showing its fallback, and USBPcap already recording. Acceptance
requires:

- one discovery request/response;
- exactly five setters and five exact acknowledgements;
- setter spacing of at least 1 second;
- endpoint-0 HID output only;
- zero `0x8123`, `0x807A`, `0x807B`, or MI_02 host output;
- all five counters visibly rendered in order;
- no LED, FFB, torque, input, or wheel-position side effect.

### Later Simulator Coexistence

Build H explicitly confirms iRacing is closed and must not be reused for a
simulator test. Only after H1 passes should a separate, newly audited stage be
designed for a stationary car in the pits. Live telemetry, moving-car use, and
a full lap remain prohibited.
