# RS50 Layout J Capability Query Success

Date: 2026-07-27 local / 2026-07-28 UTC

## Scope

Build B executed exactly one guarded DirectInput Layout J capability query.
It used the standard `c_dfDIJoystick2` data format, exclusive foreground
acquisition, outer command `4`, inner command `12`, and a ten-byte output
buffer prefilled with sentinel `0xA5`. This build contains no display setter,
generic command entry point, raw HID write, or force-feedback effect.

G HUB was closed, the RS50 was awake, and Dynamic was showing the firmware
Test screen. The operator authorized the standard format, exclusive
acquisition, and one Layout J capability query.

## DirectInput Result

The bounded run started at `2026-07-28T04:38:21.830Z` and completed at
`2026-07-28T04:38:21.929Z`.

```text
Query: Layout J support
Acquired: 1
SetCooperativeLevel HRESULT: 0x00000000
SetDataFormat HRESULT:      0x00000000
Acquire HRESULT:            0x00000000
Escape HRESULT:             0x00000000
Unacquire HRESULT:          0x00000000
Input capacity:             0
Output capacity:            10
Inner command:              12
Output bytes:               01 A5 A5 A5 A5 A5 A5 A5 A5 A5
Supported:                  1
```

Only output byte zero changed. Bytes 1-9 retained the sentinel, so the result
matches the recovered ABI contract and reports Layout J as supported.

## USBPcap Result

Capture:
`2026-07-27_rs50_layout_j_support_success.pcapng`

Baseline:
`2026-07-27_rs50_layout_j_baseline.pcapng`

The device-scoped offline analysis identified the RS50 at USB address `3` and
reconstructed:

- 52 HID++ reports: 26 HOST and 26 DEVICE;
- 26 exact request/response matches and zero unmatched reports;
- Root discovery of public feature `0x8130` at runtime index `0x12`;
- exactly 11 operational requests to runtime `0x12`, matching the static
  prediction for an uncached first layout query.

The 11 display-feature exchanges consisted of one function-`0` layout-count
query followed by ten function-`1` descriptor queries for indices 0 through 9.
The device reported ten layouts:

| Index | Layout ID | Descriptor bytes after ID |
| ---: | ---: | --- |
| 0 | 1 (A) | `00 00 00 00` |
| 1 | 2 (B) | `00 00 00 00` |
| 2 | 3 (C) | `00 00 00 00` |
| 3 | 4 (D) | `0B 00 00 00` |
| 4 | 5 (E) | `07 03 00 00` |
| 5 | 6 (F) | `01 03 00 00` |
| 6 | 7 (G) | `01 03 00 00` |
| 7 | 8 (H) | `15 0A 00 00` |
| 8 | 9 (I) | `13 0A 13 0A` |
| 9 | 10 (J) | `13 0A 13 0A` |

Layout J therefore reports ID `10` and four text capacities
`19/10/19/10`, exactly matching the independently recovered driver and
firmware model.

## Physical Observation

After the capture was saved, the operator explicitly confirmed:

> no observe ningun cambio fisico

No change was observed in the OLED, LEDs, torque, or wheel position during or
after the single Layout J query. Dynamic continued to show the Test fallback.

## Interpretation and Safety Gate

The DirectInput result and USB transaction shape both match the static model.
This confirms that the installed Logitech driver can query the physical
RS50's Display Game Data layout catalog and that Layout J is supported.

Together with the non-mutating physical observation, this closes the Build B
gate. It permits compiling and auditing the separately guarded Build C fixed
setter. It does not authorize executing that setter without a new explicit
operator approval and a scoped USB capture, and it does not authorize live
telemetry.
