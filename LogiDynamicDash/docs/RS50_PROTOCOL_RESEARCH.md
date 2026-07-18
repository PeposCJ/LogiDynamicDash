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

In a controlled G HUB-open observation, the wheel began in Dynamic mode, was
changed to Test, and was then returned to Dynamic. The OLED immediately showed
the Test fallback after returning to Dynamic. A simultaneous passive read of
MI_01 COL02 received 12 report-ID `0x11` inputs from device index `0x02` and no
inputs from device index `0x01`. Two identical six-report groups coincided with
the two selector transitions. This confirms the fallback behavior and a
correlated input notification, but it does not reveal the host-to-display
transport or assign semantics to the observed `0`, `2`, and `1` payload values.
The sanitized evidence is preserved in
[`evidence/RS50_DYNAMIC_SELECTOR_2026-07-17.md`](evidence/RS50_DYNAMIC_SELECTOR_2026-07-17.md).

A controlled five-press observation on MI_01 COL03 isolated the physical
Settings button beside the OLED. With no selector navigation, successive
presses emitted parameter `0x17` values `01 00`, `01 01`, `01 00`, `01 01`,
and `01 00`. The final `01 00` state visibly left Settings open. This confirms
`0x0100` as Settings open and `0x0101` as Settings closed with the configured
HomeScreen restored. Each close was followed by a settings snapshot.

The HomeScreen choice is reached through Settings, then HomeScreen. Controlled
Profile, Torque, Test, and Dynamic selections produced
the same open/close values on COL03, so parameter `0x17` describes Settings
visibility rather than the selected HomeScreen. No distinct screen identifier
appeared on COL03, and none of this traffic was OLED pixel transport.

The same four-option sequence was then monitored on MI_01 COL02. Each option
produced the same six-report group from device index `0x02`, feature index
`0x0E`, containing the response payload sequence `0`, `2`, `1`. Four choices
produced four identical groups, so these values are also Settings interaction
traffic rather than screen identifiers. Neither COL02 nor COL03 exposed which
of the four screens was selected.

A button-only control then pressed the Settings button five times without
navigating. COL02 emitted exactly two `0`, `2`, `1` groups, aligned with the
second and fourth presses that closed Settings and restored the HomeScreen.
The first, third, and fifth presses opened Settings and produced no COL02
input. This confirms that the COL02 group is a Settings-close notification,
not a direct button event or selected-screen identifier.

With G HUB closed, a three-press COL03 control emitted exactly `0x0100`,
`0x0101`, and `0x0100`, matching the visible open, closed, and open Settings
states. No configuration snapshot followed the close. This confirms that the
`0x17` state reports originate from the wheel independently of G HUB, while the
additional snapshots observed with G HUB open were likely responses to host
queries triggered by G HUB after Settings closed.

The equivalent three-press control on MI_01 COL02 received zero reports with
G HUB closed. With G HUB open, Settings closes had produced the repeated
`0`, `2`, `1` response groups on that collection. The A/B comparison confirms
that those COL02 groups are not spontaneous firmware notifications: they are
responses caused by G HUB activity after the Settings-close event. This also
strengthens the classification of COL02 and the extra COL03 snapshot as
configuration-query traffic rather than Dynamic OLED content.

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

### Controlled G HUB Startup USB Capture

A full USB capture was then limited to the physical RS50 address while G HUB
started from a closed state. Unlike the passive HID reader, this observation
included host-to-device transfers across every RS50 interface.

G HUB enumerated `0x18A2`, `0x8091`, and `0x8093` on device index `0x01`,
assigning runtime indices `0x09`, `0x0E`, and `0x0F`. It did not send any
operational request to those runtime indices after enumeration. Subsequent
device `0x01` requests used only runtime indices `0x02`, `0x03`, and `0x05`.

The startup traffic contained 378 short 7-byte host HID++ requests and three
isolated 20-byte host HID++ requests. It contained no 64-byte host output and
no sustained large-payload stream consistent with a framebuffer update.

This distinguishes feature discovery from feature use: the candidates are
present, but normal G HUB startup does not exercise them or send an observed
Dynamic display frame. A game integration or separate telemetry producer may
still be required to activate another path.

The sanitized evidence is preserved in
[`evidence/RS50_GHUB_STARTUP_USB_2026-07-18.md`](evidence/RS50_GHUB_STARTUP_USB_2026-07-18.md).
The complete PCAP remains local and must not be committed.

### Installed Wheel SDK Boundary

An offline inspection of the G HUB depot found a separately installed
`wheel_sdk` package at version `9.1.1.0`. Its manager installs the legacy
32-bit and 64-bit steering-wheel SDK DLLs. The available exports cover wheel
state, force-feedback effects, operating range, preferred controller
properties, and direct RPM LED control. No OLED, screen, text, image, gear, or
speed display entry point was present.

The installed TRUEFORCE manager uses a local named-pipe protocol and includes
packets for playing LEDs, querying RPM LED capabilities, and setting RPM LEDs.
Its static packet names likewise did not expose a Dynamic OLED or general
display operation.

This is static implementation evidence, not a runtime protocol capture. It
shows that the locally installed public wheel and TRUEFORCE SDK surfaces can
drive force and RPM LEDs, but they do not expose an identifiable RS50 Dynamic
OLED API. The Dynamic producer boundary therefore remains outside the known
legacy wheel SDK surface.

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
