# RS50 Protocol Research

## Device

- Vendor ID: `0x046D`
- Product ID: `0xC276`
- Device: Logitech RS50

## USB Interfaces

| Interface | Purpose |
|---|---|
| MI_00 | Steering, pedals and buttons |
| MI_01 | HID++ configuration and device state |
| MI_02 | Force Feedback and TRUEFORCE stream |

## Confirmed HID++ Parameters

| Feature index | Meaning | Encoding |
|---|---|---|
| `0x0A` | RPM Brightness | Unsigned integer, 0–100 |
| `0x0B` | RPM Mode | Little-endian enumerated value; `0x0002` is confirmed as Outside In, while `0x0001` is likely Inside Out |
| `0x14` | Dampener | Normalized unsigned 16-bit |
| `0x15` | Brake Pressure | Normalized unsigned 16-bit |
| `0x16` | Strength | Normalized unsigned 16-bit, 100% = 8.0 Nm |
| `0x18` | Wheel Angle | Unsigned 16-bit degrees |
| `0x19` | TRUEFORCE Audio | Normalized unsigned 16-bit |
| `0x1A` | FFB Filter | Mode and filter value |

## Unknown or Partially Understood Parameters

The following parameter identifiers have been observed, but there is not enough
evidence to assign them names or complete semantics:

- `0x09`
- `0x0E`
- `0x17`

## Dynamic OLED Status

No public Dynamic OLED API has been identified.

Passive HID monitoring and G HUB USB captures did not reveal a
framebuffer, text stream, or documented OLED feature.

The Dynamic screen currently falls back to the Test screen because no
known application is supplying Dynamic display data.

## Safety

The explorer must not send unknown HID output reports.

Only documented or experimentally verified reports may be transmitted.
