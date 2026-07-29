# Official Wheel SDK DirectInput Escape Evidence

## Result

Logitech's official Steering Wheel SDK establishes `IDirectInputDevice8::Escape`
as a supported game-to-wheel transport for dynamic RPM data. The SDK's
independent sample constructs `DIEFFESCAPE` directly and calls
`deviceHandle->Escape`, without requiring the Logitech SDK to be initialized.

This is important lineage for the RS50 OLED investigation: the installed
RS50 force-feedback driver exposes its Display Game Data family through the
same DirectInput Escape entry point. It does **not** make the OLED extension a
public SDK API. The 2018 SDK publishes RPM LEDs only; no OLED, layout, text,
gear, or speed-display function appears in its headers or manual.

No executable from the downloaded SDK was installed or run. The archive,
manual, headers, and samples were inspected as data only.

## Official Source

The archive was downloaded on 2026-07-22 from the Logitech G Partner Developer
Lab's `DOWNLOAD FOR WINDOWS` link:

- Developer page: <https://www.logitechg.com/en-in/programs/partner-developer-lab>
- Archive: <https://www.logitechg.com/sdk/LogitechSteeringWheelSDK_8.75.30.zip>
- Page publication label: `07/02/2018`
- Archive size: 3,175,771 bytes
- Archive SHA-256:
  `D33EFE079085C15A92AD921EEEF4A77D4898A46D3ACD0EA7AEC86404615DAD66`

The developer page describes the Steering Wheel SDK as wrapping DirectInput
controls. It also states that G HUB is needed for the SDKs, although the
specific DInput helper documented below is explicitly usable without SDK
initialization.

## Published Manual Evidence

Inspected file:

```text
Doc/LogitechGamingSteeringWheelSDK.pdf
Size: 971,866 bytes
SHA-256: 75B004FA8B99585BD1606CAF1644554F6A5BF5385A7225B2F35052F8D0FDFEDD
```

Pages 21-22 document these related APIs:

```cpp
bool LogiPlayLeds(
    const int index,
    const float currentRPM,
    const float rpmFirstLedTurnsOn,
    const float rpmRedLine);

bool LogiPlayLedsDInput(
    const LPDIRECTINPUTDEVICE8 deviceHandle,
    const float currentRPM,
    const float rpmFirstLedTurnsOn,
    const float rpmRedLine);
```

The manual says `LogiPlayLedsDInput` plays the controller LEDs and can be used
without `LogiSteeringInitialize`. Its three game-facing RPM values are floats.

The relevant PDF pages were rendered to PNG and visually checked after text
extraction. Function names, parameter types, and the initialization note were
legible and consistent with the extracted text.

## Published Source Evidence

The archive contains a standalone C++ sample that reimplements the RPM helper
using only DirectInput. Its header defines:

```cpp
CONST DWORD ESCAPE_COMMAND_LEDS = 0;
CONST DWORD LEDS_VERSION_NUMBER = 0x00000001;

struct LedsRpmData
{
    FLOAT currentRPM;
    FLOAT rpmFirstLedTurnsOn;
    FLOAT rpmRedLine;
};

struct WheelData
{
    DWORD size;
    DWORD versionNbr;
    LedsRpmData rpmData;
};
```

The implementation zeroes both structures, fills `size`, version `1`, and the
three floats, then performs:

```cpp
data_.dwSize = sizeof(DIEFFESCAPE);
data_.dwCommand = ESCAPE_COMMAND_LEDS;
data_.lpvInBuffer = &wheelData_;
data_.cbInBuffer = sizeof(wheelData_);
hr = deviceHandle->Escape(&data_);
```

Reproducibility hashes:

| SDK path | SHA-256 |
|---|---|
| `Samples/.../LogiIndependant.h` | `5061E7A180E91AC03BC325AB3AB71B1E62B0B836C7AC0D733C94EE5793535216` |
| `Samples/.../LogiIndependant.cpp` | `BC3F18FE5D238FB27E1F47A1B0CFD58B27B54EB6B97778CF09C3186C951BF384` |

## Relationship To RS50 Display Game Data

The public 2018 RPM and internal current display paths share these structural
properties:

| Layer | Official 2018 RPM API | Installed RS50 display extension |
|---|---|---|
| Application handle | `LPDIRECTINPUTDEVICE8` | `LPDIRECTINPUTDEVICE8` |
| Entry point | `IDirectInputDevice8::Escape` | `IDirectInputDevice8::Escape` |
| Outer command | `0` | `4` |
| Version field | `1` | `1` at input offset 4 |
| Payload | three RPM floats | inner command plus typed layout fields |
| Public header/API | yes | no identified export or header |

This comparison strengthens the recovered producer path:

```text
game -> DirectInput Escape -> Logitech force-feedback driver
     -> internal feature adapter -> wheel HID++ feature
```

It also identifies the safest future integration boundary. A native C++
producer using a real `LPDIRECTINPUTDEVICE8` and the driver's typed in-process
ABI is more faithful than manufacturing a raw HID packet. Text-bearing OLED
layouts still contain live MSVC `std::string` objects, so they must not be
packed manually from C# or replayed as captured process memory.

## Safety Boundary

This inspection does not authorize sending outer command `4`, any display
setter, or any raw HID++ function-3 report. A future physical validation should
first use a legitimate Logitech/game producer if one is found. If an explicit
test is later approved, begin with support queries and stop on any unexpected
force feedback, device movement, or display behavior.
