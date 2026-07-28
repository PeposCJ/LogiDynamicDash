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
- Build D completed its first guarded end-to-end physical trial. iRacing
  telemetry rendered `SPEED / 0 KMH / GEAR / N` on the Dynamic OLED for ten
  seconds with exact matched USB responses. The trial also exposed a blocking
  coexistence failure: iRacing's shift LEDs stopped updating and normal FFB
  centering was absent afterward. Returning to the iRacing main menu and
  reloading the circuit restored the LEDs. No moving-car or full-lap test is
  authorized until the exclusive DirectInput lifecycle is redesigned or
  replaced.
- Offline decoding identified the lifecycle traffic as HID++ `0x8123`
  `RESET_ALL`, `SET_GLOBAL_GAINS(0xFFFF)`, then `RESET_ALL`. The next research
  candidate is a strictly typed shared transport for display feature `0x8130`
  on the RS50's separate HID++ interface, avoiding DirectInput acquisition and
  the dedicated real-time FFB interface.
- Build E implements the shared `0x8130` codec and fail-closed 5 Hz session
  offline. It can only construct Root discovery and Layout J function-`3`
  transactions, validates exact 64-byte responses, and has no physical HID
  implementation in its protocol/session assemblies or CLI route.
- Build F compiles a separate, unreferenced HidSharp adapter gated to the
  RS50's exact MI_01 COL01 (`FF43:0701`, 7-byte `0x10`) and COL03
  (`FF43:0704`, 64-byte `0x12`) unique collections. Its fake-only tests cover
  routing, matching, disposal, uniqueness, identity, usage, length, and path
  failures. The dashboard does not reference or copy the transport assembly,
  and no Build F stream has been opened.
- Build G compiles a separate one-shot executable with six exact ordered
  confirmations. It performs one feature discovery and one fixed Layout J
  setter, then closes both streams. It has no loop, telemetry, caller text, or
  application route. After a safe failed preflight exposed Windows's distinct
  collection paths, the corrected physical attempt succeeded: the OLED showed
  all four fixed lines exactly, USBPcap captured only the expected endpoint-0
  discovery and Layout J `SET_REPORT` operations with exact acknowledgements,
  and the operator observed no LED, FFB, torque, or wheel-position change.
- Build H compiles a separate bounded-stream executable for the next baseline.
  It spells out exactly five fixed frames at 1 Hz with no loop, retry,
  telemetry, or caller-controlled values. Its H1 physical baseline passed:
  all five counters rendered in order, USBPcap contained exactly one discovery
  plus five endpoint-0 setters and their exact acknowledgements, and the
  operator observed no LED, FFB, torque, or wheel-position change.
- Build I compiles a separate coexistence trial for iRacing with
  the car stationary in the pits. It sends only five fixed 1 Hz frames and
  imports no telemetry SDK. Its high-buffer physical capture passed: OLED
  frames, centering, and rev LEDs all remained correct; TRUEFORCE endpoint
  traffic stayed continuous; and no `0x8123` reset lifecycle occurred.
- Build J compiles a separate, not-yet-executed stationary telemetry trial.
  It subscribes only to `IsOnTrackCar`, `Gear`, and `Speed`, displays live
  speed/gear for ten seconds, suppresses duplicates, caps changes at 5 Hz,
  and fails closed above 0.5 m/s or on missing/invalid telemetry. Its
  fake-only verifier passes and the dashboard does not reference this route.

Build the guarded native bridge and its non-hardware tests with:

```powershell
.\scripts\Build-Rs50DirectInputBridge.ps1 -Configuration Release
```

Audit the offline Build E shared-HID++ surface with:

```powershell
.\scripts\Test-Rs50SharedHidppSurface.ps1
```

Audit the disconnected Build F physical-adapter surface with:

```powershell
.\scripts\Test-Rs50SharedHidppTransportSurface.ps1
```

Audit the unexecuted Build G one-shot surface with:

```powershell
.\scripts\Test-Rs50SharedHidppOneShotSurface.ps1
```

Audit the unexecuted Build H bounded-stream surface with:

```powershell
.\scripts\Test-Rs50SharedHidppBoundedStreamSurface.ps1
```

Audit the unexecuted Build I stationary-coexistence surface with:

```powershell
.\scripts\Test-Rs50SharedHidppCoexistenceSurface.ps1
```

Audit the unexecuted Build J stationary-telemetry surface with:

```powershell
.\scripts\Test-Rs50SharedHidppTelemetryTrialSurface.ps1
```
