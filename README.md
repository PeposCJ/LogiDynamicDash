# LogiDynamicDash

Community project researching telemetry display support for the Dynamic OLED
screen on Logitech PRO Racing Wheel and RS50.

## Current status

- The main application reads iRacing telemetry and formats an exact four-line
  Layout J presentation (`19/10/19/10`) for its console preview, including
  speed/gear, brake bias, last lap, and bounded connection states. Its display
  path is capped at the planned initial hardware rate of 5 Hz and suppresses
  identical frames before they reach a sink.
- The research application can passively monitor supported RS50 HID input
  collections, decode saved HID++ reports, reconstruct FeatureSet catalogs,
  and count runtime-feature use without accessing HID hardware.
- A read-only Wireshark/USBPcap extractor can feed device-scoped captures
  directly into the offline analyzer.
- A query-capture inspection script combines extraction and offline analysis
  without opening the RS50 or transmitting HID data.
- An offline baseline/query comparison reports exact HID++ traffic deltas
  without attributing background differences to the display.
- A transport-free encoder can build and validate the recovered function-`3`
  parameters for layouts A-J. It deliberately omits the HID report header,
  device address, and runtime feature index, so it cannot transmit to hardware.
- A second transport-free contract records the recovered DirectInput command
  numbers and minimum input/output sizes, including outer command `4` and
  support queries 2-12. Both x86 and x64 Logitech drivers independently
  confirm that outer selector. The contract contains no DirectInput or HID
  calls.
- Verified managed ABI models reproduce the installed x64 driver's packed
  structure sizes and offsets without constructing live C++ strings. An x64
  MSVC guarded bridge now implements the DirectInput boundary without raw HID,
  C# pointer emulation, force-feedback effects, or a generic display surface.
  Build
  A proved exclusive acquisition is required; Build A2 proved a data format is
  required first; Build A3 uses `c_dfDIJoystick2`, Acquire, one support query,
  and Unacquire.
- A read-only prerequisite checker verifies the local MSVC, Windows SDK, and
  Logitech DirectInput-driver environment before any native bridge is added.
  `LogiDynamicDash.vsconfig` supplies the minimal Visual Studio workload,
  stable x64/x86 MSVC tools, and Windows 11 SDK needed by that bridge.
- The strongest current lead is public HID++ feature `0x8130`, advertised by
  the RS50 and named `DisplayGameData` inside G HUB and its embedded
  DirectInput/FFB driver. Firmware and driver analysis recovered ten typed,
  firmware-rendered layouts (A-J) and the exact normalized conversion from
  game floats to gauge bytes; semantic assignment and physical OLED activation
  still require a controlled runtime validation.
- A guarded physical DirectInput support query now succeeds on the RS50:
  `SetDataFormat`, exclusive Acquire, outer command `4` / inner command `2`,
  and Unacquire all returned success. The device returned `Supported: 1`;
  USBPcap independently captured discovery of public feature `0x8130` at
  runtime `0x12`, with no operational display call and no physical change.
- The separately guarded Build B completed one non-setter Layout J capability
  query (`inner 12`). The RS50 returned `Supported: 1`, preserved all nine
  trailing sentinel bytes, and USBPcap matched the predicted 11 `0x8130`
  exchanges. The device reported Layout J ID `10` with four text capacities
  `19/10/19/10`; the operator confirmed no OLED, LED, torque, or wheel-position
  change.
- Guarded Build C now compiles one fixed Layout J setter containing only
  `LOGIDYNAMICDASH / RS50 / OLED LINK / TEST 1`. It has no caller-controlled
  text, repeat loop, idle command, raw HID path, or other setter.
- Build C physically succeeded. DirectInput command `22` produced exactly one
  matched `0x8130` function-`3` transaction, and the RS50 visibly rendered
  `RS50 / LOGIDYNAMI / TEST 1 / OLED LINK`. This confirms static OLED output
  and reveals the driver argument-to-row permutation `2/1/4/3`. The operator
  observed no torque, LED, or wheel-movement change.
- Build D now has an offline-validated dynamic session API. It accepts only
  canonical visual rows with limits `19/10/19/10`, performs the proven
  `row2/row1/row4/row3` DirectInput permutation, suppresses identical frames,
  rate-limits changed frames to 5 Hz, and stops a session after any Escape
  failure. The managed iRacing sink requires four exact arming arguments and
  automatically ends after ten seconds.
- Live telemetry output has not yet been physically executed or validated.

Build the guarded native bridge and its non-hardware tests with:

```powershell
.\scripts\Build-Rs50DirectInputBridge.ps1 -Configuration Release
```
