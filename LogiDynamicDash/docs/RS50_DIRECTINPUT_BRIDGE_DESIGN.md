# RS50 DirectInput Display Bridge Design

## Purpose

Define the safest implementation route from .NET telemetry to the RS50
Dynamic OLED without constructing undocumented raw HID reports.

The recovered Logitech driver ABI is an in-process C++ interface reached by
`IDirectInputDevice8::Escape`. Text layouts contain live x64 MSVC
`std::string` objects, so a managed process must not imitate those objects with
byte arrays or pointers. A native x64 bridge should own every C++ object and
expose only plain C-compatible functions to the .NET application.

This document is a design and validation gate. The guarded x64 bridge is
compiled and covered by non-hardware safety tests. It exposes the two proven
queries plus one fixed Layout J setter; the setter has not been physically
invoked.

## Build Prerequisites

Run the read-only
[`Test-Rs50DirectInputBridgePrerequisites.ps1`](../../scripts/Test-Rs50DirectInputBridgePrerequisites.ps1)
before adding a native project. It checks the x64 process, MSVC tools, Windows
SDK `dinput.h`, and the registered Logitech force-feedback COM server's hash
and Authenticode signer without enumerating or opening hardware.

The first 2026-07-22 workstation check was not ready because MSVC and the
Windows SDK headers were absent. After the user approved and installed the
minimal configuration, the checker confirms x64 MSVC, SDK `dinput.h`, the
registered Logitech driver, audited hash, and Authenticode signature. The
guarded bridge now compiles with warnings treated as errors.

The repository root includes `LogiDynamicDash.vsconfig` with only the three
required selections: Desktop development with C++, the latest stable x64/x86
MSVC tools, and Windows 11 SDK 10.0.26100. Import that file from Visual Studio
Installer when automated modification is unavailable; unrelated optional
workloads are intentionally excluded.

## Proven ABI

The public `DIEFFESCAPE` wrapper follows the normal Windows ABI. On x64 it is
40 bytes: `dwSize` at `0x00`, `dwCommand` at `0x04`, `lpvInBuffer` at `0x08`,
`cbInBuffer` at `0x10`, `lpvOutBuffer` at `0x18`, and `cbOutBuffer` at `0x20`.
The x86 form is 24 bytes with those fields at `0x00/0x04/0x08/0x0C/0x10/0x14`.
The native bridge should include `dinput.h`, set `dwSize` to
`sizeof(DIEFFESCAPE)`, and let the compiler provide this outer layout. The
explorer contains non-marshallable size models and tests for both forms solely
to detect documentation or offset regressions.

The installed x64 force-feedback driver validates this input envelope:

```text
offset 0x00: uint32 structure size
offset 0x04: uint32 version = 1
offset 0x08: uint8 inner command
offset 0x09: three padding/reserved bytes
offset 0x0C: command payload
```

The structures use four-byte packing. The evidence is exact rather than a
compiler guess:

| Layout input | Payload | Size | Key offsets |
|---|---|---:|---|
| A/B | none | 12 | command `0x08` |
| C | one float | 16 | value `0x0C` |
| D | two floats, one string | 52 | floats `0x0C/0x10`, string `0x14` |
| E | two floats, two strings | 84 | strings `0x14/0x34` |
| F/G/H | two strings | 76 | strings `0x0C/0x2C` |
| I/J | four strings | 140 | strings `0x0C/0x2C/0x4C/0x6C` |

An x64 MSVC `std::string` occupies 32 bytes. The explorer models it as an
opaque 32-byte field only to test the recovered offsets. Managed code must
never populate or marshal that field.

## Implemented Query-Only Boundary

The managed side now has the intended presentation boundary:

```text
iRacing snapshot -> display mode -> LayoutJTelemetryFormatter
                 -> validated LayoutJFrame -> IDisplaySink
```

`LayoutJFrame` enforces the firmware-derived `19/10/19/10` lengths and byte
range `0x20..0x7F` before a sink receives it. The console already consumes
this interface; the future RS50 sink will forward the same validated frame to
the native bridge and will not reinterpret telemetry.

The native bridge exports a deliberately small C ABI:

```cpp
extern "C" {
    uint32_t rs50_display_abi_version();
    int rs50_display_open(
        uintptr_t owner_window,
        rs50_display_handle** handle);
    int rs50_display_query_support(
        rs50_display_handle* handle,
        rs50_display_capabilities* capabilities);
    int rs50_display_query_layout_j_support(
        rs50_display_handle* handle,
        rs50_display_capabilities* capabilities);
    const wchar_t* rs50_display_status_message(int status);
    void rs50_display_close(rs50_display_handle* handle);
}
```

`owner_window` is a process-owned top-level `HWND` represented without leaking
a Windows header into the C ABI. Zero and message-only windows are rejected.
The implemented validation build exposes only ABI metadata, `open`, the
general support query, the Layout J capability query, status text, and `close`.
No setter or generic command surface is compiled into it. The guarded CLI
requires the query selector plus `--confirm-standard-data-format`,
`--confirm-exclusive-acquire`, and a query-specific transmission confirmation
before it creates the owner window.

The bridge owns:

- `IDirectInput8` and `IDirectInputDevice8` lifetimes
- device enumeration and identity checks
- validation and use of the caller's process-owned top-level window handle
- all packed request structures
- all live `std::string` objects
- conversion of driver `HRESULT` values into stable bridge error codes
- serialized calls and cleanup

The .NET application owns telemetry, presentation mapping, and update cadence.
It passes bounded ASCII text to the bridge and never sees a DirectInput or HID
pointer. Its current display path is capped at 5 Hz and wraps the sink with
value-based frame deduplication. A failed sink call is not remembered, so the
same frame remains eligible for a controlled retry instead of being silently
dropped. The iRacing adapter publishes immutable snapshot records and
serializes status and telemetry callbacks, preventing a native call
from observing a partially updated snapshot or running concurrently with a
second callback.

## Native Structure Rules

The implementation must use the same x64 MSVC runtime ABI as the installed
driver and assert every recovered boundary at compile time:

```cpp
#pragma pack(push, 4)
struct DisplayHeader {
    std::uint32_t size;
    std::uint32_t version;
    std::uint8_t command;
    std::uint8_t reserved[3];
};

struct LayoutJInput {
    DisplayHeader header;
    std::string line1;
    std::string line2;
    std::string line3;
    std::string line4;
};
#pragma pack(pop)

static_assert(sizeof(DisplayHeader) == 12);
static_assert(sizeof(std::string) == 32);
static_assert(offsetof(LayoutJInput, line1) == 12);
static_assert(offsetof(LayoutJInput, line4) == 108);
static_assert(sizeof(LayoutJInput) == 140);
```

Do not use `memcpy`, manual pointer patching, a C# representation, or a
captured process buffer as a substitute for real constructed strings.

## Device Selection Gates

Before any Escape call, the bridge must require all of the following:

1. Windows x64 process.
2. DirectInput game-controller device with Logitech VID `0x046D` and the
   explicitly supported RS50 PID `0xC276`.
3. Force-feedback driver GUID corresponding to the installed Logitech HID++
   driver, not a generic or compatibility-mode device.
4. Registered COM server with a valid Logitech Inc Authenticode signature and
   the statically audited SHA-256. A changed driver hash requires a new static
   audit before use.
5. Exactly one matching device. Ambiguity is an error.
6. A visible process-owned foreground top-level window. Build A established
   that `DISCL_NONEXCLUSIVE | DISCL_BACKGROUND` cannot reach Escape:
   DirectInput returned `DIERR_NOTEXCLUSIVEACQUIRED`. Build A2 therefore uses
   `DISCL_EXCLUSIVE | DISCL_FOREGROUND` for the bounded query lifecycle.

PRO Wheel PIDs must not be guessed or accepted until observed and documented.

Read VID/PID through `DIPROP_VIDPID` (`LOWORD` vendor, `HIWORD` product), as
the official Logitech sample does for product identity. Compare
`DIDEVICEINSTANCE::guidFFDriver` before calling `Escape`, as Microsoft
explicitly requires for vendor-specific commands. Do not rely on product-name
text or enumeration order.

Logitech's official independent console sample creates the device and calls
`Escape` without setting a data format or acquiring it, but explicitly warns
that its LED call will fail because it has no active window handle. Microsoft
documents acquisition as mandatory for input-state reads, not for
[`Escape`](https://learn.microsoft.com/en-us/previous-versions/windows/desktop/ee417891%28v%3Dvs.85%29).
Build A recorded both results and returned `DIERR_NOTEXCLUSIVEACQUIRED`
(`0x80040205`) while leaving its output sentinel unchanged. That runtime result
supersedes the earlier assumption. Build A2 records cooperative, Acquire,
Escape, and Unacquire results, acquires only immediately before the one support
query, and releases immediately afterward.

## Validation Sequence

### Build A: Query Only

The historical Build A variant compiled no setters. Its explicitly armed mode performed
exactly one outer command `4`, inner command `2`,
with version `1`, a 12-byte input buffer, and a one-byte output buffer.

The safe build command is:

```powershell
.\scripts\Build-Rs50DirectInputBridge.ps1 -Configuration Release
```

It runs native argument/gate tests and CLI `--describe` only. It never supplies
a valid owner window and therefore cannot create DirectInput or enumerate the
RS50. The transmitting CLI mode remains a separate physical validation step.
Follow
[`RS50_QUERY_ONLY_OPERATOR_CHECKLIST.md`](RS50_QUERY_ONLY_OPERATOR_CHECKLIST.md)
for the exact preconditions, capture, one-shot command, observations, and stop
conditions.

Build A used nonexclusive/background cooperation and no acquisition. Its first
physical call on 2026-07-27 returned `DIERR_NOTEXCLUSIVEACQUIRED`; the RS50 was
also asleep, so the run is not a valid OLED observation. Build A is retired.
See
[`evidence/RS50_DIRECTINPUT_QUERY_BUILD_A_2026-07-27.md`](evidence/RS50_DIRECTINPUT_QUERY_BUILD_A_2026-07-27.md).

### Build A2: Exclusive Query Only

Build A2 preserves the same sole outer/inner query and adds only the DirectInput
precondition proven by Build A:

1. show and foreground a process-owned top-level window;
2. set `DISCL_EXCLUSIVE | DISCL_FOREGROUND`;
3. call `Acquire`;
4. send the one general-support query;
5. call `Unacquire` before interpreting the result;
6. defensively release again during close only if the first release failed.

Its CLI additionally requires `--confirm-exclusive-acquire`. It still compiles
no layout query, setter, effect, raw HID path, or retry.

The first physical Build A2 call returned `DIERR_INVALIDPARAM` from `Acquire`.
Its capture contained no HOST HID++ reports. Microsoft documents that a data
format must be set before acquisition, even when no input state will be read.
Build A2 is retired. See
[`evidence/RS50_DIRECTINPUT_QUERY_BUILD_A2_2026-07-27.md`](evidence/RS50_DIRECTINPUT_QUERY_BUILD_A2_2026-07-27.md).

### Build A3: Standard Format, Exclusive Query Only

Build A3 adds the one missing DirectInput precondition:

1. call `SetDataFormat(&c_dfDIJoystick2)`;
2. record its HRESULT and stop before Acquire if it fails;
3. otherwise retain the Build A2 Acquire/query/Unacquire lifecycle.

Its CLI also requires `--confirm-standard-data-format`. It does not poll or
read device state, configure axes, create effects, issue a layout query, or
compile a display setter.

Record:

- selected VID/PID and sanitized product name
- window/cooperative-level setup and whether the device was acquired
- DirectInput `HRESULT`
- returned output byte and post-call `cbOutBuffer`
- device-scoped USB traffic
- whether the OLED, torque, or wheel position changed

Stop after the call. Any display change, force feedback, or wheel movement is
an immediate failure.

Build A3 succeeded physically. Its capture matched 15 requests to 15 responses,
discovered public `0x8130` at runtime `0x12`, and showed no operational request
to that runtime. The support result was `1` and the operator observed no
physical change.

### Build B: Layout Capability Queries

Only after Build A3 is confirmed non-mutating and explicitly approved, query
layouts A-J one at a time. Build B currently exposes only Layout J query `12`;
there is no generic command export. Allocate the exact minimum output capacity recovered
from the driver: `1,1,1,1,4,6,6,6,6,10,10` bytes for commands 2-12.
Prefill every buffer with a sentinel. Static disassembly shows that every
query writes only byte `0`; larger minimum capacities are validation gates,
not defined capability fields. Treat any changed trailing byte as an ABI
mismatch and stop.

Run Layout J query `12` first, alone, with its exact ten-byte output buffer.
This avoids using Layout A query `3` as the cache trigger; command `3` has a
driver null-dereference path for malformed output buffers. No negative or
undersized-buffer tests are permitted. The first valid layout query lazily
loads capabilities by sending function `0` once and function `1` once for
every returned layout. The known RS50 count predicts eleven feature
transactions, after which the other layout queries should use the same
in-memory cache. A different transaction count is diagnostic evidence and
must not be followed by a setter.

The implemented CLI requires the distinct
`--confirm-transmit-layout-query` token. It reports all ten output bytes and
rejects success unless bytes 1-9 remain at sentinel `0xA5`. Follow
[`RS50_LAYOUT_J_QUERY_OPERATOR_CHECKLIST.md`](RS50_LAYOUT_J_QUERY_OPERATOR_CHECKLIST.md)
for its isolated physical validation.

The driver's initialization flag is set before the requests and is not reset
on its visible failure paths. If the first load times out, returns a different
count, or yields an invalid descriptor, close and recreate the DirectInput
device before any retry. Do not treat subsequent false results on the same
handle as independent evidence that a layout is unsupported.

Build B physically completed one Layout J query on 2026-07-27. DirectInput
returned `Supported: 1`, only byte zero changed in the exact ten-byte output,
and USBPcap matched all 11 predicted `0x8130` exchanges with no unmatched
reports. The returned Layout J descriptor was ID `10`, capacities
`19/10/19/10`. Build C remains gated on the operator's explicit physical
observation, which subsequently confirmed no physical change. See
[`evidence/RS50_LAYOUT_J_QUERY_SUCCESS_2026-07-27.md`](evidence/RS50_LAYOUT_J_QUERY_SUCCESS_2026-07-27.md).

### Build C: Single Setter

Only after query results and USB traffic match the static model, compile one
setter in a separate build. Layout J is the preferred first text validation
because firmware renders four neutral centered text rows. Use static text,
not live telemetry, and stop immediately after one call.

The first payload is fixed so the observation is unambiguous:

```text
line 1: LOGIDYNAMICDASH
line 2: RS50
line 3: OLED LINK
line 4: TEST 1
```

Build C implements only this payload. The exported function accepts a handle
and a result structure but no text parameters. It uses command `22`, the
verified 140-byte x64 MSVC input ABI, the shared one-shot
Acquire/Escape/Unacquire lifecycle, and no output buffer. ABI size and all
four `std::string` offsets are compile-time assertions.

The one physical Build C call succeeded. The OLED displayed
`RS50 / LOGIDYNAMI / TEST 1 / OLED LINK`, while USBPcap recorded exactly one
matched `0x8130` function-`3` setter with those same wire strings. This proves
that the DirectInput adapter maps game-facing inputs `1/2/3/4` to visual rows
`2/1/4/3`. A future caller-controlled native API must accept visual rows,
enforce `19/10/19/10`, and permute its internal MSVC strings to
`row2/row1/row4/row3`. See
[`evidence/RS50_STATIC_LAYOUT_J_SUCCESS_2026-07-27.md`](evidence/RS50_STATIC_LAYOUT_J_SUCCESS_2026-07-27.md).

Before the call, select Dynamic on the wheel and confirm that it shows the Test
fallback. After the single call, touch neither G HUB nor the wheel for ten
seconds while recording the display and USB capture. Do not send SetIdle to
clean up; stop the process and use the wheel's normal HomeScreen selector if a
manual screen change is needed.

### Build D: Telemetry

After a visible static setter is confirmed:

1. Start at 5 Hz, coalesce telemetry, and skip identical frames. Increase only
   after a scoped capture shows clean acknowledgements and stable OLED output.
2. Map speed/unit and gear/value into Layout J.
3. Enforce the firmware limits `19/10/19/10` before crossing the native ABI.
4. Stop updates on device loss or any failed Escape call.
5. Observe the nominal four-minute firmware fallback after stopping.

The Build D implementation now provides an ABI-6 session surface:

- `rs50_display_begin_layout_j_stream`
- `rs50_display_set_layout_j_frame`
- `rs50_display_end_layout_j_stream`

The public frame is 68 bytes and contains explicit lengths plus fixed visual
row capacities `19/10/19/10`. Every unused byte and both reserved bytes must
be zero. Only printable ASCII `0x20..0x7F` is accepted. The native bridge
constructs the live x64 MSVC strings in the physically proven order
`row2/row1/row4/row3`.

The first successful frame starts a monotonic 200 ms gate. Identical frames
are suppressed before that gate; changed frames arriving sooner are reported
as rate-limited without calling Escape. Any Escape failure marks the session
failed and prevents further writes. Explicit end and handle close both release
exclusive acquisition.

The managed application duplicates the safety boundary: its normal
no-argument mode remains console-only, while RS50 output requires four exact
ordered arguments and automatically cancels after ten seconds. Its concrete
native transport is behind an injected interface, so unit tests never load
the DLL or create a window.

No Build D physical execution has occurred. Follow
[`RS50_DYNAMIC_TELEMETRY_OPERATOR_CHECKLIST.md`](RS50_DYNAMIC_TELEMETRY_OPERATOR_CHECKLIST.md)
only after separate authorization.

## Explicit Non-Goals

- no raw HID++ sender
- no HID report replay
- no unknown command fuzzing
- no firmware writes
- no compatibility-mode PID guessing
- no merge to `main` before physical output and cleanup behavior are verified
