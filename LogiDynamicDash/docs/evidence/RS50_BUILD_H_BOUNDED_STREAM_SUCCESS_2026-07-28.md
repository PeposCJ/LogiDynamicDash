# RS50 Build H Bounded Stream Success

## Physical Result

- Date: 2026-07-28
- Capture: `2026-07-28_rs50_build_h_1hz_five_frames.pcapng`
- RS50 USB address: `1.3`
- G HUB: closed
- iRacing: closed
- OLED mode: Dynamic
- Process exit: success

The OLED visibly advanced in order from `FRAME 1 OF 5` through
`FRAME 5 OF 5`. The operator observed no LED, FFB, torque, or wheel-position
change.

## Exact Traffic

The capture contains exactly six host reports carrying HID data:

| Frame | Epoch | Operation |
|---:|---:|---|
| 7 | 1785225318.036998 | Root discovery of `0x8130` |
| 11 | 1785225318.046539 | Layout J `FRAME 1 OF 5` |
| 15 | 1785225319.051380 | Layout J `FRAME 2 OF 5` |
| 19 | 1785225320.054977 | Layout J `FRAME 3 OF 5` |
| 23 | 1785225321.057969 | Layout J `FRAME 4 OF 5` |
| 27 | 1785225322.061663 | Layout J `FRAME 5 OF 5` |

Every host output used:

- `URB_FUNCTION_CLASS_INTERFACE`;
- endpoint `0x00`, direction OUT;
- `URB_CONTROL`;
- HID `SET_REPORT`;
- interface `1`.

The discovery used output report `0x10`, length 7. Every Layout J setter used
output report `0x12`, length 64.

COL03 returned exactly six matching reports:

- frame 9: public feature `0x8130`, runtime index `0x12`, version 0;
- frames 13, 17, 21, 25, and 28: exact zero-body Layout J acknowledgements.

## Rate

The measured intervals between consecutive setters were:

- 1.004841 seconds;
- 1.003597 seconds;
- 1.002992 seconds;
- 1.003694 seconds.

Every interval was at least one second. The bounded stream therefore remained
at or below its authorized 1 Hz rate.

## Forbidden-Traffic Check

Because the six expected operations are the only host reports carrying HID
data, the capture contains:

- zero `0x8123` FFB operations;
- zero `0x807A` or `0x807B` LED/LIGHTSYNC operations;
- zero MI_02 / endpoint-`0x03` host output;
- no DirectInput Acquire/Unacquire lifecycle.

This validates repeated shared-HID++ OLED writes with the simulator closed. It
does not validate simulator coexistence, telemetry input, moving-car use, or a
full lap.
