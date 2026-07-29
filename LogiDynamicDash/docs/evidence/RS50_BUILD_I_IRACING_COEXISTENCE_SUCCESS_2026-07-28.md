# RS50 Build I iRacing Coexistence Success

## Physical Result

- Date: 2026-07-28
- Accepted capture:
  `2026-07-28_rs50_build_i_iracing_stationary_coexistence_attempt2.pcapng`
- RS50 USB address: `1.3`
- Capture method: USBPcapCMD, device filter `3`, 128 MB buffer
- G HUB: closed
- iRacing: running
- Car: stationary in pits, neutral, brake held
- Process exit: success

The OLED visibly advanced from `FRAME 1 OF 5` through `FRAME 5 OF 5`.
Before and after Build I, the operator confirmed:

- normal centering/FFB after a small steering displacement;
- normal shift-LED response during a brief neutral rev.

There was no unexpected movement, torque impulse, input loss, LED failure, FFB
change, or simulator disconnect.

## Capture Quality

The first Wireshark-managed capture recorded only frames 2 and 3 because dense
TRUEFORCE traffic produced multi-second capture gaps. Its physical outcome was
positive, but it was rejected as protocol evidence.

Attempt 2 used direct USBPcapCMD capture with an address-3 filter and 128 MB
buffer. The resulting pcapng contains 820,863 packets over 137.419 seconds
without gaps in the Build I/FFB validation window.

## Exact Display Sequence

| Frame | Epoch | Operation |
|---:|---:|---|
| 468390 | 1785226923.923343 | Root discovery of `0x8130` |
| 468426 | 1785226923.929201 | Layout J `FRAME 1 OF 5` |
| 474475 | 1785226924.942312 | Layout J `FRAME 2 OF 5` |
| 480509 | 1785226925.952897 | Layout J `FRAME 3 OF 5` |
| 486527 | 1785226926.957416 | Layout J `FRAME 4 OF 5` |
| 492566 | 1785226927.964961 | Layout J `FRAME 5 OF 5` |

COL03 returned:

- frame 468408: public `0x8130`, runtime index `0x12`, version 0;
- frames 468502, 474534, 480526, 486543, and 492583: exact zero-body
  acknowledgements.

Every display output used interface 1, endpoint 0, `URB_CONTROL`, and HID
`SET_REPORT`.

## Timing

Setter intervals:

- 1.013111 seconds;
- 1.010585 seconds;
- 1.004519 seconds;
- 1.007545 seconds.

Request-to-response times:

- discovery: 2.954 ms;
- frame 1: 12.096 ms;
- frame 2: 9.143 ms;
- frame 3: 2.719 ms;
- frame 4: 2.358 ms;
- frame 5: 2.973 ms.

## FFB and LED Coexistence

The complete capture has zero endpoint-0 control reports addressed to runtime
index `0x10`, previously mapped to feature `0x8123` Force Feedback. Therefore
there is no `RESET_ALL`, `SET_GLOBAL_GAINS`, or DirectInput exclusive-lifecycle
sequence.

Host submissions to real-time TRUEFORCE endpoint `0x03` remained continuous in
every one-second bucket across a 25-second Build I window:

- minimum: 990 transfers/s;
- maximum: 1000 transfers/s;
- total: 24,920 transfers.

Runtime-`0x0B` rev-light operations were captured during both manual checks:

- pre-check: epochs 1785226890–1785226892;
- post-check: epochs 1785226937–1785226938.

During the Build I sequence itself, the only endpoint-0 control reports were
the one display discovery and five display setters.

## Conclusion

Repeated shared-HID++ OLED writes coexist with iRacing's stationary FFB and
rev-light ownership without the destructive DirectInput lifecycle. This
validates designing a separately bounded stationary telemetry stage. It does
not yet authorize moving-car use or a full lap.
