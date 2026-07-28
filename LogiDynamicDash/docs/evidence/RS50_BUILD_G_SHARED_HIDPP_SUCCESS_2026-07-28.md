# RS50 Build G Shared HID++ Success

## Physical Result

- Date: 2026-07-28
- Capture:
  `2026-07-28_rs50_build_g_shared_hidpp_oneshot_attempt2.pcapng`
- RS50 USB address: `1.3`
- G HUB: closed
- iRacing: closed
- OLED mode: Dynamic
- Process exit: success

The RS50 displayed exactly:

```text
RS50 SHARED HIDPP
BUILD G
ONE SHOT ONLY
USBPCAP
```

The operator observed no unexpected OLED behavior, LED change, FFB or torque
change, or wheel movement.

## Exact USB Sequence

| Frame | Epoch | Direction | Endpoint | Payload |
|---|---:|---|---:|---|
| 14077 | 1785224168.473669 | host → RS50 | `0x00` | `10ff000a813000` |
| 14079 | 1785224168.476329 | RS50 → host | `0x82` | `12ff000a1200...` |
| 14081 | 1785224168.478073 | host → RS50 | `0x00` | fixed Layout J `12ff123a09...` |
| 14083 | 1785224168.480287 | RS50 → host | `0x82` | `12ff123a0000...` |

Both host outputs are `URB_FUNCTION_CLASS_INTERFACE`, endpoint-0
`URB_CONTROL`, HID `SET_REPORT`, interface `1`:

- discovery: output report ID `0x10`, length 7;
- Layout J: output report ID `0x12`, length 64.

The discovery response validates runtime feature index `0x12`, flags `0`, and
version `0`. The Layout J response is the exact zero-body acknowledgement.

## Timing

- discovery round trip: 2.660 ms;
- Layout J round trip: 2.214 ms;
- complete discovery-through-acknowledgement sequence: 6.618 ms.

## Forbidden-Traffic Check

The capture contains exactly two host reports carrying HID data: the discovery
and fixed Layout J setter above. It therefore contains:

- zero `0x8123` FFB operations;
- zero `0x807A` or `0x807B` LED/LIGHTSYNC operations;
- zero MI_02 / endpoint-`0x03` host output;
- no DirectInput Acquire/Unacquire lifecycle.

This validates the shared HID++ route as a physically functional alternative
to exclusive DirectInput for one RS50 OLED frame. It does not yet validate a
repeated stationary stream, simulator coexistence, moving-car use, or a full
lap.
