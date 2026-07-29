# RS50 Shared HID++ Bounded Stream Build H

## Status

Build H compiles a separate bounded-stream executable. Its H1 physical
repetition baseline succeeded on 2026-07-28: all five counters rendered in
order, every request was acknowledged, and the operator observed no LED, FFB,
torque, or wheel-position change.

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

## Physical Stages

### H1: Repetition Baseline — Passed

H1 ran with G HUB and iRacing closed, RS50 awake, Dynamic selected, and
USBPcap recording. It met every acceptance condition:

- one discovery request/response;
- exactly five setters and five exact acknowledgements;
- setter spacing of at least 1 second;
- endpoint-0 HID output only;
- zero `0x8123`, `0x807A`, `0x807B`, or MI_02 host output;
- all five counters visibly rendered in order;
- no LED, FFB, torque, input, or wheel-position side effect.

Capture:

```text
2026-07-28_rs50_build_h_1hz_five_frames.pcapng
```

The four setter intervals were 1.004841, 1.003597, 1.002992, and 1.003694
seconds. USBPcap found exactly six host reports carrying HID data: one
discovery and five setters. Each used interface 1, endpoint 0,
`URB_FUNCTION_CLASS_INTERFACE`, `URB_CONTROL`, and HID `SET_REPORT`.

COL03 returned one exact discovery response and five exact zero-body
acknowledgements. There were no other host reports, proving zero `0x8123`,
`0x807A`, `0x807B`, or MI_02 host operations.

### Later Simulator Coexistence

Build H explicitly confirms iRacing is closed and must not be reused for a
simulator test. H1 now permits designing a separate, newly audited Build I for
a stationary car in the pits. Live telemetry, moving-car use, and a full lap
remain prohibited until that new stage is implemented and validated.
