# RS50 Feature 0x8130 Display Game Data Evidence

## Result

HID++ feature `0x8130` is the strongest Dynamic OLED candidate found so far.
The conclusion is based on two independent local observations:

1. The physical RS50 advertised public feature `0x8130` during G HUB startup.
2. The installed G HUB implementation names the corresponding class
   `Feature8130DisplayGameData`.

This identifies a game-data feature, not yet a safe write protocol. No report
from this feature has been transmitted or replayed by the project.

## Physical FeatureSet Evidence

- Capture date: 2026-07-18
- Device: Logitech RS50, VID `0x046D`, PID `0xC276`
- Capture scope: physical RS50 USB address only
- Initial state: G HUB closed; then started normally
- Host request frame: 267, 25.830623 seconds
- Device response frame: 269, 25.833538 seconds

Sanitized request:

```text
10 FF 01 1B 12 00 00
```

Sanitized response prefix:

```text
12 FF 01 1B 81 30 00 00
```

Interpretation:

- device index: `0xFF` (base device)
- FeatureSet runtime index: `0x01`
- requested ordinal/runtime index: `0x12`
- returned feature ID: `0x8130`
- flags: `0x00` (public)
- version: 0
- request/response latency: approximately 2.915 ms

The updated offline batch analyzer reconstructed the complete catalog from
matched FeatureSet requests and responses. For this entry it reported:

```text
device 0xFF, runtime 0x12 -> feature 0x8130
(Display Game Data; G HUB static name), flags 0x00, version 0;
HOST operational requests 0
```

The full PCAP remains local and is not committed.

## Installed G HUB Static Evidence

The inspected executable was:

```text
C:\ProgramData\LGHUB\depots\794591\core\LGHUB\lghub_agent.exe
File version: 2026.4.919028
Size: 88,446,616 bytes
SHA-256: 77E3B6FF1ED78CBEBFCCC6F63EED297D14D6DF1454A8B4E50AD1263115B7BED6
```

Its MSVC run-time type information contains these exact class names:

```text
devio::IFeature8130DisplayGameData
devio::IIFeature8130DisplayGameData
devio::Feature8130DisplayGameData
devio::FeatureCreator<devio::Feature8130DisplayGameData>
```

The same feature implementation is linked into the installed
`di_ffb_manager.exe`. Deeper inspection found internal
`Logi::Display::Message::SetLayout` types there, but no corresponding export in
the installed public Wheel or TRUEFORCE SDK DLLs. This is static binary
evidence; the external entry path must still be confirmed at runtime.

An exact-string search across the complete installed G HUB depot found
`DisplayGameData` only in `lghub_agent.exe` and `di_ffb_manager.exe`. The
Electron front-end archive (`app.asar`) contains neither that class name nor an
`0x8130` route. The DirectInput/FFB manager does preserve internal layout
message types, while the public SDK-facing exports do not. This places the
known feature below G HUB's public UI and public SDK surface.

### Embedded DirectInput Driver

`di_ffb_manager.exe` is a resource container as well as a manager. Its PE
resources include four force-feedback drivers:

| Resource | Size | SHA-256 |
|---|---:|---|
| `HIDPP_FORCEFEEDBACK_X64.DLL` | 4,900,504 | `17AB8FBB23FD549CCCCDB72A502C3BDCD984F80B6C40E48027C25512D0405A7F` |
| `HIDPP_FORCEFEEDBACK_X86.DLL` | 4,277,400 | `ECB831F985CBE8F59CE2E2EC450F4B3B8AECBDC7B632264698AF1EC0EACD7E8D` |
| `JERRY_FORCEFEEDBACK_X64.DLL` | 378,008 | `B6B4F9952B7D05896BB73480725948F8CDE9E6DDE737009CE80152F25E295DEA` |
| `JERRY_FORCEFEEDBACK_X86.DLL` | 306,840 | `EBA7B29F5269D9C5DD7EB9E078187520284CC2B3D4FF8520CF1BA9CF095545F5` |

The extracted files were inspected as data only and were not loaded or
executed. They remain outside the repository.

The same HID++ DLLs are installed as COM servers at version `1.1.13`. The
physical RS50 joystick registry key selects this driver explicitly:

```text
Joystick OEM key: VID_046D&PID_C276
OEMForceFeedback CLSID: {62B43F0E-E7DB-4329-8C13-A966D84A289F}
x64 InProcServer32:
  C:\Program Files\Logitech\Direct Input Force Feedback\1_1_13\
  hidpp_forcefeedback_x64.dll
```

The installed x64/x86 hashes exactly match the corresponding embedded
resources above. The RS50 INF applies the standard HID installation to
`USB\VID_046D&PID_C276&MI_00` and registers this force-feedback COM driver; it
does not attach the legacy Logitech lower HID filter to the RS50 entry.

The installed x64 COM server also has a valid Authenticode signature from
`Logitech Inc`; the signing-certificate thumbprint observed on 2026-07-22 is
`BFAB6EF922D0DDE848AE747B2AD58506E17D9ED1`. The future bridge gate checks both
publisher validity and the audited binary hash, so a G HUB driver update stops
the probe until the new binary is reviewed.

The HID++ driver's RTTI exposes `std::function` callbacks for
`EscapeCommands::Wheel::Rpm`, `LedConfig`, `LedCaps`, and every display payload
from `LayoutC` through `LayoutJ`. This locates the display producer interface
inside Logitech's DirectInput force-feedback driver. It is an internal
DirectInput Escape surface rather than a public export; the DLL exports only
the four standard COM registration/class-factory functions.

The callbacks construct these consecutive internal display messages:

| Message ID | Payload type | Game-facing fields copied by the driver |
|---:|---|---|
| 29 | Layout C | one 32-bit floating value |
| 30 | Layout D | two 32-bit values, one string |
| 31 | Layout E | two 32-bit values, two strings |
| 32 | Layout F | two strings |
| 33 | Layout G | two strings |
| 34 | Layout H | two strings |
| 35 | Layout I | four strings |
| 36 | Layout J | four strings |

The DLL implements the standard COM `IDirectInputEffectDriver` interface. Its
vtable identifies the display entry point as `IDirectInputEffectDriver::Escape`.
Microsoft documents that this driver method is reached from an application's
`IDirectInputEffect::Escape` or `IDirectInputDevice::Escape` call, and that
`DIEFFESCAPE.dwCommand` is manufacturer-specific:
[driver interface](https://learn.microsoft.com/en-us/windows/win32/api/dinputd/nn-dinputd-idirectinputeffectdriver),
[escape structure](https://learn.microsoft.com/en-us/windows/win32/api/dinput/ns-dinput-dieffescape).
The recovered two-level dispatch envelope is:

```text
DIEFFESCAPE.dwCommand = 4
lpvInBuffer + 0x04: uint32 version = 1
lpvInBuffer + 0x08: inner command
lpvInBuffer + 0x0C: layout payload
```

The outer value is anchored by the driver's six-entry dispatch table, not by
the order of nearby type names. The dispatcher reads `dwCommand` at
`DIEFFESCAPE + 4`. Table entry `0` parses the exact 20-byte version-1 RPM
structure published in Logitech's official SDK sample. Entry `4` forwards the
escape object to the 22-command Display Game Data dispatcher. Entry `5`
instead parses a distinct 20-byte family with a subtype byte and a double;
it is not the OLED display envelope. This official-command-0 cross-check
corrects an earlier off-by-one interpretation of the outer selector.

The independently extracted 32-bit driver confirms the same mapping. Its
dispatcher at `0x102676E2` reads `[DIEFFESCAPE + 4]`, bounds the selector to
`0..5`, and uses the absolute jump table at `0x1026789C`. Entry `0` at
`0x102676F5` validates the same 20-byte RPM structure. Entry `4` at
`0x1026786B` calls `0x102678C0`, whose inner dispatcher checks version `1`,
subtracts one from the byte at input offset `8`, bounds it to `0..21`, and
therefore accepts inner commands `1..22`. Entry `5` at `0x102677F4` again
parses the separate subtype-and-double family. Both architectures thus agree
that the display outer command is `4`.

The display setters and minimum `cbInBuffer` values are:

| Inner command | Layout | Minimum bytes | Payload after the 12-byte header |
|---:|---|---:|---|
| 13 | A | 12 | none |
| 14 | B | 12 | none |
| 15 | C | 16 | one `float` |
| 16 | D | 52 | two `float` values, one MSVC `std::string` |
| 17 | E | 84 | two `float` values, two MSVC `std::string` objects |
| 18 | F | 76 | two MSVC `std::string` objects |
| 19 | G | 76 | two MSVC `std::string` objects |
| 20 | H | 76 | two MSVC `std::string` objects |
| 21 | I | 140 | four MSVC `std::string` objects |
| 22 | J | 140 | four MSVC `std::string` objects |

The commands preceding the setters complete the same display family:

| Inner command | Meaning |
|---:|---|
| 1 | `DisplaySetIdleMessage` / set Dynamic display idle |
| 2 | `DisplayIsSupportedMessage` / query general display support |
| 3-12 | query support/capabilities for layouts A-J, in order |

These meanings are corroborated by the dispatch order and the preserved
`DisplayIsLayoutA...JSupportedMessage` type names, rather than inferred from
the numeric sequence alone.

The x64 driver's command-4 dispatcher also reveals the exact output-buffer
contract for those support queries. Each successful callback writes its
boolean result to the first output byte, after checking these minimum output
capacities:

| Inner command | Query | Minimum output bytes |
|---:|---|---:|
| 2 | General display support | 1 |
| 3-5 | Layout A-C support | 1 |
| 6 | Layout D support | 4 |
| 7-10 | Layout E-H support | 6 |
| 11-12 | Layout I-J support | 10 |

The larger capacities are validation requirements, not additional recovered
fields. Every query branch calls its capability object and then converges on
the same `mov byte ptr [rdi], al` at `0x1802AA5DE`; no branch writes bytes
`1..N-1`. Only output byte `0` therefore has defined meaning. A physical test
should prefill the full output buffer with a sentinel and verify that all
remaining bytes stay unchanged rather than interpreting them as capabilities.

Inner command `3` (Layout A query) has an unsafe malformed-buffer path in the
x64 driver: a null or undersized output is normalized to null, but the common
return path can still store through it. This does not affect a correctly formed
one-byte output buffer, but it rules out negative or boundary probing. Every
query must provide the documented exact capacity; malformed buffers are not a
valid research test.

All commands 1-12 require the same 12-byte minimum input envelope and version
`1`. Setter commands 13-22 do not require an output buffer. These checks are
now represented by a transport-free contract type in the explorer; it cannot
open DirectInput or transmit a command.

The general support callback is not a local cached flag. Its call chain creates
an asynchronous request object, submits it through the driver's feature
transport, and waits with a 200 ms timeout before returning the boolean. A
physical command-2 validation should therefore expect a host/device exchange
even though it is a query and not a setter. It remains an explicitly approved
transmission, not part of passive observation.

Layout support queries use a separate lazy capability cache. Every A-J getter
first calls `0x1800F4F00`. On its first invocation, that routine sends feature
function `0` with a one-byte zero input, reads the returned layout count, then
loops from zero to count-minus-one and sends feature function `1` for each
layout index. It parses the six-byte descriptors into per-layout cached fields
and marks the cache initialized. With the RS50 firmware's count of ten, the
first layout-support query is therefore expected to produce eleven feature
transactions; subsequent A-J queries on the same feature-object lifetime use
the cache. This is another reason to isolate Build B from the one-request
general support probe.

The routine sets its one-time initialization flag before issuing those
requests and does not clear it on the observed failure paths. A timeout or
partial first load can therefore leave false/empty cached capabilities for the
rest of that feature-object lifetime. A physical retry must recreate the
DirectInput device/feature object rather than issuing another layout query on
the same handle.

This is an in-process C++ ABI, not a portable byte packet: a text-bearing
Escape buffer includes live `std::string` object state and possibly pointers.
Blindly reproducing the buffer from another language or process would be both
unsafe and incorrect. The firmware-side HID++ payload remains the actual
portable wire format, but it still awaits an explicitly approved physical
validation.

The recovered sizes also identify the x64 structure packing. A 12-byte header,
two four-byte floats, and one 32-byte MSVC `std::string` total 52 bytes only
with four-byte packing; the driver reads Layout D's values at offsets `0x0C`
and `0x10` and its string at `0x14`. The same model exactly produces E at 84,
F/G/H at 76, and I/J at 140 bytes. Managed layout models now test every size
and field offset while keeping the string representation opaque and explicitly
non-marshallable.

The proposed production boundary is therefore an x64 MSVC C++ bridge that owns
the live strings and DirectInput objects, with a small C ABI consumed by .NET.
The staged implementation and physical safety gates are documented in
[`../RS50_DIRECTINPUT_BRIDGE_DESIGN.md`](../RS50_DIRECTINPUT_BRIDGE_DESIGN.md).

The same DLL contains the complete `Feature8130DisplayGameData` implementation.
For Layout C its final setter allocates a two-byte function-`3` payload: the
firmware layout ID followed by the converted numeric byte. The D-J setters
similarly prepend their layout ID, copy bounded byte/text fields according to
the capability descriptor, and dispatch function `3`. This joins the formerly
separate DirectInput-message and firmware findings into one static path:

```text
game/SDK -> DirectInput Escape Layout C-J -> internal message 29-36
         -> Feature8130DisplayGameData -> HID++ function 3 -> firmware renderer
```

The Layout C Escape callback at `0x18001BF30` passes the incoming value's raw
32-bit representation to `0x1800115D0`. The latter allocates a 24-byte message,
sets message ID `29`, and stores those bits unchanged at object offset `0x14`.
This locates the float-to-byte conversion after internal message creation and
before the final Feature8130 setter. The 36-entry dispatcher jump table maps
message `29` to a wrapper that calls the Layout C adapter at `0x180015A20`.
That adapter clamps the float to `0.0..1.0`, multiplies it by the confirmed
constant `255.0`, calls the bundled rounding helper, and passes the resulting
byte to the Feature8130 setter. Layout D (`0x180015AA0`) and Layout E
(`0x180015C40`) repeat the same conversion independently for both numeric
fields. The resulting cross-layer formula is therefore:

```text
wire byte = round(clamp(game float, 0.0, 1.0) * 255.0)
OLED extent = wire byte * 118 / 255
```

The research explorer now contains an offline encoder for these exact A-J
parameter layouts. Its output starts with the firmware's zero-based layout
index and ends with the bounded layout fields. It intentionally omits all
transport metadata: no HID report ID, device ID, runtime feature index, or USB
write operation is present. Non-finite numeric inputs, embedded NUL, and
oversized text fields are rejected before a payload can be produced. This is a
reproducible protocol artifact, not a hardware sender.

The callable entry point has not been invoked at runtime, so this path is
evidence for protocol structure, not authorization to inject an Escape command
or HID report.

### Official SDK Transport Precedent

Logitech's official Steering Wheel SDK 8.75.30 provides an independent C++
sample that sends dynamic RPM values through `IDirectInputDevice8::Escape`.
The published sample uses outer command `0`, a versioned input structure, and
three float values; its manual says the DInput helper works without SDK
initialization. This independently confirms DirectInput Escape as an intended
Logitech game-to-wheel transport pattern.

The SDK predates the RS50/PRO display extension and exposes no OLED or layout
API. The installed driver extends the same boundary with outer command `4` and
the A-J inner command family. Full source hashes, manual verification, and the
public/private API boundary are recorded in
[`RS50_OFFICIAL_WHEEL_SDK_DIRECTINPUT_2026-07-22.md`](RS50_OFFICIAL_WHEEL_SDK_DIRECTINPUT_2026-07-22.md).

The neighboring public features advertised by the same base device also have
unambiguous G HUB class names:

| Feature | G HUB class meaning |
|---|---|
| `0x807A` | RPM Indicator |
| `0x807B` | RPM LED Pattern |
| `0x8120` | Gaming Attachments |
| `0x8123` | Force Feedback |
| `0x8127` | Dual Clutch |
| `0x8130` | **Display Game Data** |
| `0x8132` | Axis Mapping |
| `0x8133` | Global Damping |
| `0x8134` | Brake Force |
| `0x8136` | Torque Limit |
| `0x8137` | Configuration Profiles |
| `0x8138` | Operating Range |
| `0x8139` | TRUEFORCE |
| `0x8140` | FFB Filter |

This neighborhood rules out a simple confusion between `0x8130` and the RPM
LED or force-feedback features: G HUB implements and names them separately.

## Firmware Confirmation

The official base firmware already downloaded by G HUB was inspected without
executing or modifying it:

```text
File: rs50_main_v165_4_39.dfu
Embedded version: U165.04_B0039-gf6375b51
Size: 235,600 bytes
SHA-256: 62C152D68BA0873B7330401050A2DD96E099CFB6E648759F4329D59E382D3D9D
Image base: 0x08010000
DFU wrapper before image: 0x20 bytes
```

At file offset `0x2C64C`, the firmware feature registry contains feature ID
`0x8130`, function count `4`, and four Thumb handler pointers:

```text
function 0 -> 0x08033911
function 1 -> 0x08033919
function 2 -> 0x0803393D
function 3 -> 0x08033981
```

This independently confirms that `0x8130` is implemented by the RS50 base
firmware rather than invented solely by G HUB.

## Confirmed Feature Shape

The firmware and G HUB implementations agree on this protocol:

1. Function `0` returns exactly `10` layouts.
2. Function `1` accepts an index from `0` through `9` and returns six bytes:
   the index, layout ID `index + 1`, and a four-byte layout-capability record.
3. Function `2` clears the pending Dynamic-data state in RAM.
4. Function `3` accepts a layout index and its bounded data fields, sanitizes
   text, and schedules the selected layout for rendering.

Function `3` performs that scheduling directly: it marks the Dynamic-data
state pending, stores the selected layout, and reloads an expiry counter with
decimal `240000` (`0x0003A980`). A firmware service routine decrements that
counter and clears pending state when it reaches zero. No function-`0` or
function-`1` handshake is required before the setter.

The counter's timing chain is also statically recoverable:

1. `0x08033440` decrements the counter once and clears pending at zero.
2. Its only direct caller is `0x0803481C`, inside the main display task.
3. That task waits on RAM flag `0x20001510` before each iteration.
4. `0x0801959C` sets that flag and calls the routine at `0x08011540`.
5. The vector-table entry at image offset `0x3C` points to `0x0801959D`,
   identifying it as the Cortex-M `SysTick` handler.
6. `0x08011540` has the STM32 `HAL_IncTick` operation
   `uwTick += uwTickFreq`.

ST's official HAL source documents the default SysTick time base as 1 ms and
shows that exact increment operation in `HAL_IncTick`:
[STM32G4 HAL time-base implementation](https://github.com/STMicroelectronics/stm32g4xx-hal-driver/blob/master/Src/stm32g4xx_hal.c).
Consequently the nominal expiry is `240000 ms = 240 s = 4 min`. This duration
should still be confirmed with an elapsed-time physical observation after a
legitimate producer stops.

The ten function-1 descriptors are:

```text
request parameter: zero-based layout index 0..9
response byte 0: zero-based layout index 0..9
response byte 1: one-based layout ID 1..10
response bytes 2..5: four capability bytes
```

The firmware handler at `0x08033918` writes the requested index to byte `0`,
`index + 1` to byte `1`, and the four-byte descriptor to bytes `2..5`. The
driver dispatches on byte `1`, stores the descriptor, and sets a separate
per-layout validity flag. DirectInput returns that validity flag as its
layout-support boolean; it does not reinterpret response byte `0` as support.

| Index | Layout | Capability bytes | Function-3 data after index |
|---:|---|---|---|
| 0 | A | `00 00 00 00` | none |
| 1 | B | `00 00 00 00` | none |
| 2 | C | `00 00 00 00` | one byte |
| 3 | D | `0B 00 00 00` | two bytes, then text up to 11 bytes |
| 4 | E | `07 03 00 00` | two bytes, text up to 3, text up to 7 |
| 5 | F | `01 03 00 00` | text up to 1, text up to 3 |
| 6 | G | `01 03 00 00` | text up to 1, text up to 3 |
| 7 | H | `15 0A 00 00` | text up to 21, text up to 10 |
| 8 | I | `13 0A 13 0A` | text up to 19, 10, 19, and 10 |
| 9 | J | `13 0A 13 0A` | text up to 19, 10, 19, and 10 |

The display dispatcher at `0x080338BC` selects the renderer through a
ten-entry table rooted at `0x0804607C`. The recovered targets are:

| Index | Layout | Renderer |
|---:|---|---:|
| 0 | A | `0x08033380` |
| 1 | B | `0x0803390C` |
| 2 | C | `0x080333A0` |
| 3 | D | `0x08032E3C` |
| 4 | E | `0x08032F1C` |
| 5 | F | `0x08033028` |
| 6 | G | `0x080330BC` |
| 7 | H | `0x08033150` |
| 8 | I | `0x0803322C` |
| 9 | J | `0x08032D48` |

Layout J renders in five phases: clear, then one phase for each text field.
The firmware measures and horizontally centers all four fields. Their vertical
positions are respectively 2, 12, 35, and 45. Layout I uses the same four RAM
fields, centers fields one and three, right-aligns fields two and four against
an x boundary of 116, and draws additional left-side glyphs or decoration on
the second and fourth rows. Consequently J is the least semantically
prescriptive candidate for an initial text dashboard, while I is a decorated
variant. The suggested use of J for speed/unit and gear/value is a project
mapping, not a recovered Logitech field name.

The text copier stops at NUL, converts lowercase ASCII to uppercase, accepts
printable bytes `0x20` through `0x7F`, replaces other bytes with `?`, and adds
a terminator. The largest I/J payload is 59 bytes including the layout index,
which fits a 64-byte HID++ report with its four-byte header and padding.

Separate firmware draw routines load the numeric state bytes at offsets 2 and
3, convert them to floating point, and calculate `value * 118 / 255` before a
display draw call. This establishes their wire domain as normalized `0..255`
gauge/progress values mapped onto a 118-pixel span. It does not by itself name
either gauge as RPM, speed, fuel, or another telemetry concept. The
game-facing conversion is the normalized formula documented above, but the
semantic assignment of each gauge still requires a legitimate producer
capture or an explicitly approved physical test.

G HUB's embedded DirectInput/FFB driver provides a second independent naming layer.
Its preserved C++ type information contains
`Logi::Display::Message::SetLayout` specializations for
`EscapeCommands::Wheel::LayoutC` through `LayoutJ`, with consecutive message
IDs 29 through 36. Layouts A and B carry no function-3 data, which explains why
no payload structure type is preserved for them.

This establishes a typed, firmware-rendered layout protocol rather than a raw
pixel framebuffer. The semantic assignment of each text/byte field to visible
gear, speed, units, or labels still requires one legitimate runtime capture.

## Important Negative Evidence

### Official Product Position

The official Logitech PRO Racing Wheel setup guide describes Dynamic as
support for "potential future updates" to screen functionality and says it
defaults to Test. The same page describes Test, Profile, and Torque as active
features:
[official guide, printed page 16](https://www.logitech.com/assets/70071/3/pro_racing_wheel_for_ps_%26_pc_samr.pdf).

The PDF page was rendered and visually checked, not inferred only from search
text. This is strong evidence that Logitech shipped the Dynamic selector as a
reserved extension point rather than promising a currently supported game
dashboard. It does not prove that no later private integration exists, but it
explains the observed Test fallback and the lack of an OLED method in the
public Wheel SDK.

During the complete 51.09-second G HUB startup capture:

- G HUB enumerated runtime index `0x12` as `0x8130`.
- It sent zero operational requests to device `0xFF`, runtime index `0x12`.
- No 64-byte host report or sustained display-rate stream appeared.

Therefore normal G HUB startup discovers the feature but does not activate a
Dynamic data session. A legitimate game-side producer or another activation
condition is still required to capture real payloads.

## Safe Next Evidence

The next useful capture must observe a legitimate producer while the wheel is
already in Dynamic mode. The decisive signature is host traffic to device
`0xFF`, runtime index `0x12`, especially function `3`; functions `0` and `1`
may appear for discovery but are not a required setter handshake.

Until such traffic is captured, the project must not transmit function-`3`
payloads from the static analysis. The layout bounds and numeric conversion
are now known, but field meanings, session requirements, update cadence, and
rejection behavior remain unverified on physical hardware.

This was the original safety gate before the DirectInput route could be
validated. It is now superseded only for the separately guarded, explicitly
approved Build C experiment: physical general and Layout J queries matched the
static model, and Build C exposes one fixed Layout J payload through the
audited Logitech driver. Arbitrary function-`3` traffic, raw HID transmission,
live telemetry, and repeated setters remain prohibited until that single
static experiment succeeds.
