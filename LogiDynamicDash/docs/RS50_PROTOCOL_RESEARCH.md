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

No public Dynamic OLED API has been identified. However, HID++ feature
`0x8130` is now a strongly identified internal **Display Game Data** transport
candidate.

Passive HID monitoring and G HUB USB captures did not reveal a
framebuffer, text stream, or documented OLED feature.

The Dynamic screen currently falls back to the Test screen because no
known application is supplying Dynamic display data.

This fallback matches Logitech's own PRO Racing Wheel setup guide. It describes
Dynamic as an extension point for potential future screen updates and states
that it defaults to Test. That official wording sharply narrows the producer
search: the firmware and current DirectInput driver contain the implementation,
but a public game-facing release was not guaranteed. It also makes a missing
legitimate producer an expected result rather than evidence that the recovered
feature is unrelated.

### Strongest Candidate: Feature 0x8130 Display Game Data

The complete G HUB startup capture showed the base device (`0xFF`) advertising
public feature `0x8130` at runtime index `0x12`, flags `0x00`, version 0. Static
inspection of the installed G HUB agent independently found the exact class
name `devio::Feature8130DisplayGameData` and its two associated interfaces.

Static analysis of the official RS50 base firmware then confirmed the feature
registry entry and all four handlers. Function `0` returns ten layouts;
function `1` returns one six-byte descriptor for layout A through J; function
`2` clears pending Dynamic data; and function `3` installs bounded byte/text
fields for the selected layout. G HUB's DirectInput/FFB manager independently
names layouts C through J.

Function `1` takes a zero-based layout index and returns six bytes:
`[zero-based index, one-based layout ID, four capability bytes]`. The driver's
one-time capability loader dispatches on the one-based ID, stores the
descriptor, and sets a separate validity flag. DirectInput's layout-support
boolean is that validity flag, not response byte `0`.

The largest layouts contain four text fields with maxima 19, 10, 19, and 10
bytes. Firmware uppercases printable ASCII, replaces invalid characters, and
NUL-terminates fields. This confirms a typed, firmware-rendered telemetry
layout protocol rather than a raw framebuffer.

The firmware draw routines consume the two numeric state bytes as normalized
8-bit values: each is converted to floating point and scaled by `118 / 255`
before being passed to a display primitive. Thus the wire bytes represent
0-255 gauge/progress extents across a 118-pixel span, not decimal text values.
Which game telemetry concepts occupy those gauges remains unassigned.

The render-dispatch table maps all ten layout indices to concrete firmware
routines. Layout J is especially useful for a first text-only dashboard: it
draws its four independent fields as centered rows at vertical positions 2,
12, 35, and 45. Layout I uses the same four stored fields but adds left-side
decoration and right-aligns fields two and four. This makes J a strong
engineering candidate for labels such as speed/unit and gear/value without
claiming that Logitech assigned those semantics. Physical output remains
unverified.

Normal G HUB startup enumerated `0x8130` but sent zero operational requests to
runtime index `0x12`. The field-to-screen meanings and activation/session rules
are therefore still unknown, and no function-`3` payload is yet approved for
physical transmission. Full sanitized evidence, exact layout bounds, firmware
hash, and inference boundaries are recorded in
[`evidence/RS50_FEATURE_8130_DISPLAY_GAME_DATA_2026-07-22.md`](evidence/RS50_FEATURE_8130_DISPLAY_GAME_DATA_2026-07-22.md).

A guarded DirectInput general-support query later reproduced that discovery
with G HUB closed. The RS50 returned support byte `1`, USBPcap assigned
`0x8130` to runtime `0x12`, and the OLED did not change. The capture contained
no operational request to runtime `0x12`, confirming that support discovery is
not itself a layout update. See
[`evidence/RS50_DIRECTINPUT_QUERY_BUILD_A3_SUCCESS_2026-07-27.md`](evidence/RS50_DIRECTINPUT_QUERY_BUILD_A3_SUCCESS_2026-07-27.md).

The subsequent single Layout J capability query also matched the recovered
model. DirectInput returned support byte `1` with an intact ten-byte sentinel
contract, while USBPcap captured one layout-count request plus ten descriptor
requests to runtime `0x12`. The physical RS50 reported Layout J ID `10` and
four string capacities `19/10/19/10`. Explicit operator confirmation of the
physical observation recorded no OLED, LED, torque, or wheel-position change.
This closes the query gate but does not itself demonstrate display output. See
[`evidence/RS50_LAYOUT_J_QUERY_SUCCESS_2026-07-27.md`](evidence/RS50_LAYOUT_J_QUERY_SUCCESS_2026-07-27.md).

Build C then confirmed display output. One DirectInput Layout J setter yielded
one matched feature-`0x8130` function-`3` request/response and visibly replaced
the Test fallback. The captured wire strings and photographed rows were
`RS50 / LOGIDYNAMI / TEST 1 / OLED LINK`. Comparing them with the four
game-facing inputs proves the driver's pairwise permutation `2/1/4/3`; the
ten-character second row also physically confirms the recovered capacity. No
torque, LED, or wheel-movement change was observed.
See
[`evidence/RS50_STATIC_LAYOUT_J_SUCCESS_2026-07-27.md`](evidence/RS50_STATIC_LAYOUT_J_SUCCESS_2026-07-27.md).

The feature name occurs in the installed G HUB depot only inside the agent and
DirectInput/FFB manager binaries, not the Electron front-end archive. The
manager does contain internal `Display::Message::SetLayout` commands, so the
transport sits below the visible G HUB UI. It remains outside the exported
legacy Wheel SDK and TRUEFORCE API surface inspected earlier.

The manager embeds separate 32-bit and 64-bit DirectInput force-feedback
drivers. Static extraction of the 64-bit `hidpp_forcefeedback` DLL confirmed
the complete game-to-device path: typed `EscapeCommands::Wheel::LayoutC`
through `LayoutJ` callbacks create internal messages 29 through 36, and the
driver's `Feature8130DisplayGameData` implementation serializes them as
function-`3` HID++ payloads. At the game-facing layer, C contains one 32-bit
floating value; D contains two 32-bit values plus one string; E contains two
32-bit values plus two strings; F/G/H contain two strings; and I/J contain
four strings. Before USB transmission, each numeric value is clamped to
`0.0..1.0`, multiplied by `255.0`, rounded, and reduced to the byte expected
by firmware. This is an internal DirectInput Escape surface, not an exported
function in the installed public Wheel SDK.

The Layout C callback at virtual address `0x18001BF30` forwards the incoming
32-bit value unchanged to the message constructor at `0x1800115D0`. That
constructor creates message ID `29` and copies the original float bits into
the message object at offset `0x14`. The message-29 dispatch case then calls
the Layout C adapter at `0x180015A20`, which implements
`round(clamp(value, 0, 1) * 255)`. The Layout D and E adapters apply the same
formula independently to both of their numeric values. Thus the official
game-facing range is normalized `0.0..1.0`, while the firmware wire range is
`0..255`.

`LogiDynamicExplorer.Protocol.Rs50DisplayGameDataPayloadEncoder` implements
that recovered conversion and the A-J field bounds as an offline-only
artifact. It returns only function-`3` parameter bytes. It deliberately does
not construct a HID++ report header, select a device, resolve a runtime feature
index, open a HID handle, or call a write API. Text is restricted to the
firmware's safe ASCII domain, lower-case ASCII is uppercased, unsupported
characters become `?`, embedded NUL is rejected, and oversized fields fail
instead of being silently truncated.

The exact DirectInput envelope is also statically recovered. The driver's
`IDirectInputEffectDriver::Escape` accepts outer `DIEFFESCAPE.dwCommand = 4`.
Its input buffer uses version `1` at offset 4 and an inner command byte at
offset 8. Inner commands 13/14 select data-free layouts A/B; commands 15-22
select layouts C-J. Their minimum input sizes are respectively 12, 12, 16,
52, 84, 76, 76, 76, 140, and 140 bytes. Text-bearing buffers contain MSVC
`std::string` objects, not a portable packed-wire format. This proves the
internal producer ABI, but it is not yet a safe or public integration surface.
The x86 driver independently reproduces the same outer mapping: its entry `4`
calls a version-1, 22-command display dispatcher, while entry `5` handles the
distinct subtype-and-double family. This eliminates architecture-specific
table interpretation as a source of the earlier off-by-one error.

The complete inner command family is coherent: command `1` is `SetIdle`,
command `2` queries general display support, commands `3` through `12` query
support for layouts A through J, and commands `13` through `22` set layouts A
through J. On the firmware side, function `3` immediately marks Dynamic data
pending, stores the selected layout, and reloads an expiry counter with
`240000` (`0x0003A980`). It does not require a preceding function-`0` or
function-`1` handshake. The decrement routine runs once per main-loop wakeup
from a flag set by the firmware's `SysTick` handler. That handler also calls
the matching STM32 HAL tick increment routine; ST documents its default time
base as 1 ms. The nominal Dynamic expiry is therefore `240000 ms`, or 240
seconds (four minutes). A physical elapsed-time observation can independently
confirm that static result.

The installed RS50 joystick registration selects force-feedback CLSID
`{62B43F0E-E7DB-4329-8C13-A966D84A289F}`. Its 64-bit COM server is
`Direct Input Force Feedback\1_1_13\hidpp_forcefeedback_x64.dll`; its hash is
identical to the DLL extracted from `di_ffb_manager.exe`. DirectInput can
therefore reach the recovered Escape dispatcher through the RS50's normal
`guidFFDriver`, without a separate RS50 kernel filter.

A later game/RPM capture provided useful negative evidence: G HUB sent live
updates only to runtime `0x0B`, which the startup catalog maps to feature
`0x807A` (RPM Indicator). Its changing value covered all 11 states from 0 to
10, while runtime `0x12` (`0x8130`) received zero host reports. Details are in
[`evidence/RS50_RPM_LIVE_FEED_2026-07-22.md`](evidence/RS50_RPM_LIVE_FEED_2026-07-22.md).

### Firmware-Rendered Settings UI

The current evidence is consistent with the OLED settings screens being
rendered by the wheel firmware. Under this hypothesis, G HUB sends individual
configuration values, and the firmware presents those values using its own
menus and graphics.

The official base firmware contains the `TORQUE`, `PROFILE`, `TEST`, and
`DYNAMIC` HomeScreen strings, a four-entry pointer table for them, and code
that selects and draws those entries. Together with the physical G HUB-open /
G HUB-closed observations, this confirms that the HomeScreen/settings UI is
firmware-side. The base may still relay drawing operations to the rim module;
the result does not imply that OLED pixels originate in G HUB.

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

The same capture also enumerated `0x8130` on the base device (`0xFF`) at
runtime index `0x12`. Later static analysis identified its G HUB class as
`Feature8130DisplayGameData`. G HUB made zero operational calls to that runtime
index during startup.

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

Logitech's downloadable Steering Wheel SDK 8.75.30 adds a useful historical
control. Its official standalone C++ sample implements live RPM LEDs by
constructing `DIEFFESCAPE` and calling `IDirectInputDevice8::Escape` directly.
The manual documents that this DInput helper can work without initializing the
SDK. This validates DirectInput Escape as an established Logitech producer
boundary, while the absence of OLED/layout functions confirms that the
Display Game Data extension is not part of that public 2018 API. Reproducible
details are in
[`evidence/RS50_OFFICIAL_WHEEL_SDK_DIRECTINPUT_2026-07-22.md`](evidence/RS50_OFFICIAL_WHEEL_SDK_DIRECTINPUT_2026-07-22.md).

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

Short (`0x10`) and generic long (`0x11`) HID++ reports are also decoded into
their device index, runtime feature index, function, software ID, and complete
parameter bytes. Unknown parameters remain raw hexadecimal evidence; the tool
does not assign OLED semantics without a controlled correlation.

Saved reports can be summarized in batches without HID access:

```text
LogiDynamicExplorer --analyze-reports reports.tsv
```

Each non-comment line may contain only hexadecimal bytes, or tab-separated
`HOST`/`DEVICE`, optional metadata columns, and hexadecimal bytes in the final
column. The summary groups HID++ reports by header, counts distinct parameter
signatures, reconstructs FeatureSet ordinal-to-feature mappings from matched
requests and responses (including long `0x12` responses), and reports the
number of later host requests to each runtime index. Request/response matches
require the same device, runtime feature, function, and software ID; they are
structural correlations only.

On Windows, a USBPcap/Wireshark capture can be exported and analyzed without
creating an intermediate report file. The physical USB address is mandatory
so unrelated devices are excluded:

```powershell
scripts\Export-Rs50HidReports.ps1 `
  -PcapPath C:\captures\rs50.pcapng `
  -DeviceAddress 4 |
  dotnet run --project LogiDynamicExplorer -- --analyze-reports -
```

The extractor reads an existing capture only. It does not open the RS50 or
transmit HID reports.

Safe validation steps include:

1. Confirm whether the settings UI continues to operate with G HUB closed.
2. Compare isolated configuration changes with sustained USB activity.
3. Inventory every RS50 HID report descriptor without sending reports.
4. Keep firmware-rendered settings traffic separate from any future Dynamic
   display evidence.

### iRacing Rev-Light Protocol Validation — 2026-07-27

Four controlled USB captures were recorded with iRacing to validate the RS50
rev-light protocol and improve the methodology used for the ongoing Dynamic
OLED investigation.

The sanitized findings are preserved in:

[`evidence/RS50_IRACING_REV_LIGHT_2026-07-27.md`](evidence/RS50_IRACING_REV_LIGHT_2026-07-27.md)

The captures confirmed that feature `0x807A` is used for the live RPM strip and
that the steady-state feed runs at approximately 60 Hz.

A startup capture taken before launching iRacing exposed the one-time arm
sequence:

```text
10ff0b0c000000   fn0
10ff0b1c000000   fn1
10ff0b2c000000   fn2
10ff0b0c000000   fn0
11ff0b6c...      then the fn2 + fn6 live stream
This first-party evidence showed that an extra fn3 command previously used by
an external Linux driver implementation was not part of the normal arm
sequence. That command was identified as SET_EFFECT and could overwrite the
user's active LIGHTSYNC effect. The incorrect command and its associated
lighting restore workaround were subsequently removed from that implementation.
A dedicated request/response capture also confirmed a 0x12 device response
for every tested host write in the rev-light stream.
The redline capture confirmed that no special flash command is used. Redline is
represented by the normal live level reaching:
LL = 10
The iRacing pit-limiter capture showed that the full-strip flash is implemented
using the same live level mechanism, alternating:
LL = 10
LL = 0
at approximately 1.2 Hz.
These findings do not identify the Dynamic OLED transport. Their main value to
LogiDynamicDash is methodological: they demonstrate a repeatable workflow for
proprietary RS50 feature research:
feature discovery
→ runtime feature index
→ one-time initialization
→ live command stream
→ device responses
→ controlled physical-state comparison
Future Dynamic OLED work should use the same structure while remaining
read-only until the relevant feature and command semantics are understood.



## Safety

The explorer must not send unknown HID output reports.

Only documented or experimentally verified reports may be transmitted.
