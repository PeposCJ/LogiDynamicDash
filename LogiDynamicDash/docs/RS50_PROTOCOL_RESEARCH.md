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

### Working Hypothesis: Firmware-Rendered Settings UI

The current evidence is consistent with the OLED settings screens being
rendered by the wheel firmware. Under this hypothesis, G HUB sends individual
configuration values, and the firmware presents those values using its own
menus and graphics.

This hypothesis is not yet confirmed. It is supported by the configuration
reports observed on MI_01 COL03 and by the absence of an observed framebuffer,
text stream, or sustained display update stream during passive monitoring.

The Dynamic display mode may use a separate producer or protocol. No game or
public application is currently known to supply live Dynamic OLED data.

On 2026-07-17, the complete USB HID descriptor inventory was compared with
G HUB open and closed on physical RS50 hardware. The same five collections,
usages, report IDs, and report lengths were present in both states. This
confirms that G HUB does not change the exposed HID descriptor structure, but
it does not identify which HID++ feature may carry Dynamic display data.

During the same physical session, the complete on-wheel settings UI continued
to render normally after G HUB was closed. The display selector remained
available, and selecting Dynamic showed the documented `Test` fallback screen.
These observations strongly support firmware-rendered settings screens. The
rendering mechanism remains classified as likely rather than confirmed because
Logitech background services were not stopped and no causal timing measurement
was performed.

With G HUB closed and no physical controls changed, a time-limited passive read
of MI_01 COL03 received zero input reports during a 10-second baseline. This
confirms that the collection did not emit unsolicited configuration traffic
during that idle observation.

In a controlled follow-up with G HUB closed, RPM Brightness was changed once
using the wheel controls, from 99 to 98. MI_01 COL03 emitted exactly one 64-byte
input report, and the direct big-endian value decoded as 98 percent. This
confirms that the firmware applies the local settings interaction and notifies
the host of the resulting value. It does not identify the Dynamic OLED
transport.

```text
12 FF 0A 00 00 62 00 00 00 00 00 00 00 00 00 00
00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
```

### Passive Sub-Device Feature Enumeration

With G HUB initially closed, MI_01 COL02 was opened for passive input reads and
G HUB was then started normally. The session received 167 reports, including
51 report-ID `0x11` responses from device index `0x01`. No reports were sent by
LogiDynamicExplorer.

The sanitized FeatureSet responses independently confirmed `0x8091`, `0x18A2`,
and `0x9315`. They also revealed public feature `0x8093`, which was not
highlighted in the earlier project correspondence. Both `0x18A2` and `0x8093`
advertised flags `0x00`; `0x9315` advertised hidden and engineering flags
`0x70`.

The complete sanitized FeatureSet evidence and raw reports are preserved in
[`evidence/RS50_DEV01_FEATURESET_2026-07-17.md`](evidence/RS50_DEV01_FEATURESET_2026-07-17.md).
Device identity responses and the complete local capture are intentionally not
included.

LogiDynamicExplorer can decode a sanitized saved report without enumerating or
opening HID hardware:

```text
LogiDynamicExplorer --decode-report "11 01 01 1D 80 93 00 00"
```

The offline decoder separates the device index, feature index, function,
software ID, FeatureSet feature ID, flags, and version. It does not infer OLED
control from a public feature flag, and the explorer continues to print the
complete raw report alongside the decoded description.

Safe validation steps include:

1. Confirm whether the settings UI continues to operate with G HUB closed.
2. Compare isolated configuration changes with sustained USB activity.
3. Inventory every RS50 HID report descriptor without sending reports.
4. Keep firmware-rendered settings traffic separate from any future Dynamic
   display evidence.

## Safety

The explorer must not send unknown HID output reports.

Only documented or experimentally verified reports may be transmitted.
