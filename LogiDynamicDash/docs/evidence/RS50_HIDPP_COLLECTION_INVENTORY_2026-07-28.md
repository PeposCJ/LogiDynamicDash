# RS50 HID++ Collection Inventory

## Session

- Date: 2026-07-28
- Device: Logitech RS50, VID `0x046D`, PID `0xC276`
- Operation: descriptor inventory only
- Streams opened: none
- Output/feature reports sent: none

The existing Release explorer enumerated descriptors through HidSharp without
calling `TryOpen`, `Write`, or `SetFeature`.

## Observed Collections

| Collection | Usage | Input | Output | Classification |
|---|---|---|---|---|
| MI_00 | `0001:0004` | no-ID, 31 bytes | none | joystick inputs |
| MI_01 COL01 | `FF43:0701` | `0x10`, 7 bytes | `0x10`, 7 bytes | HID++ short |
| MI_01 COL02 | `FF43:0702` | `0x11`, 20 bytes | `0x11`, 20 bytes | HID++ long |
| MI_01 COL03 | `FF43:0704` | `0x12`, 64 bytes | `0x12`, 64 bytes | HID++ very long |
| MI_02 | `FFFD:FD01` | `0x01`, 64 bytes | `0x01`, 64 bytes | real-time FFB/TRUEFORCE |

The inventory exactly matches the report IDs and lengths observed in saved
USBPcap traffic.

## Build F Consequence

Feature discovery for `0x8130` sends a short `0x10` request and receives a
very-long `0x12` response. Layout J sends and receives `0x12`. A shared HID++
adapter therefore needs:

1. MI_01 COL01 for the discovery write;
2. MI_01 COL03 for Layout J writes and all matching responses.

It does not need MI_00, MI_01 COL02, or MI_02. Build F rejects those
collections as targets and requires COL01/COL03 to share the same normalized
physical interface path.

This inventory does not prove how Windows/HidSharp will implement an output
write on these collections. A future captured stationary trial must verify
that the host produces the expected endpoint-0 HID `SET_REPORT` control
transfer and no interface-2/endpoint-`0x03` output.

Microsoft documents `WriteFile` as the user-mode path for continuously sending
HID output reports, but does not guarantee the resulting USB transfer type:
[Sending HID Reports](https://learn.microsoft.com/en-us/windows-hardware/drivers/hid/sending-hid-reports).
