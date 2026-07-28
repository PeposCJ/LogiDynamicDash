# RS50 Guarded DirectInput Bridge

This x64 native bridge is the executable query validation stage for the
recovered Logitech `DisplayGameData` interface. It contains exactly one
Escape call site and three explicitly bounded command choices: general
display-support query `2`, Layout J capability query `12`, and one fixed
Layout J setter `22`, all inside DirectInput Escape command `4`.

There is no caller-controlled text, generic command surface, idle command,
raw HID write, repeat loop, or force-feedback effect. The sole setter always
uses `LOGIDYNAMICDASH / RS50 / OLED LINK / TEST 1`. Build A proved exclusive
acquisition is required, and
Build A2 proved DirectInput requires a data format before acquisition. Build
A3 sets the standard `c_dfDIJoystick2` format, then performs the bounded
lifecycle: Acquire, one support query, Unacquire.

## Build and safe tests

From the repository root:

```powershell
.\scripts\Build-Rs50DirectInputBridge.ps1 -Configuration Release
```

`Rs50DirectInputBridge.slnx` contains the three native projects for IDE use;
the PowerShell command remains the authoritative build and safety-test entry
point.

The script:

1. verifies x64 MSVC, Windows SDK, driver registration, audited SHA-256, and
   Authenticode;
2. builds the DLL, guarded query executable, and native safety tests;
3. audits the DLL export/import surface for exactly the fixed setter and no
   raw HID APIs;
4. runs only invalid-argument tests, unarmed refusal checks, and `--describe`.

Those tests do not create DirectInput, enumerate a controller, or open HID
hardware.

## Runtime safety gates

Before any operation can reach `IDirectInputDevice8::Escape`, the bridge
requires:

- a process-owned top-level window;
- the exact audited Logitech driver registration, SHA-256, and valid
  Authenticode signature;
- exactly one DirectInput controller with VID `046D`, PID `C276`, and
  force-feedback driver GUID
  `{62B43F0E-E7DB-4329-8C13-A966D84A289F}`;
- a visible process-owned foreground window;
- successful exclusive foreground cooperative level;
- successful standard `c_dfDIJoystick2` data format;
- an unused bridge handle.

The bridge sets the standard joystick format, acquires immediately before
`Escape`, and unacquires immediately afterward. The handle close path performs
a defensive release if the first release failed. Cooperative, data-format,
Acquire, Escape, and Unacquire HRESULTs are reported. The output starts with
sentinel `0xA5`; a successful call is rejected as an ABI mismatch unless it
leaves an exact one-byte buffer containing boolean `0` or `1`.

Layout J uses an exact ten-byte output capacity. Only byte zero is defined;
bytes 1-9 must remain at sentinel `0xA5` or the bridge rejects the result.

## Physical validation gate

Do not run transmitting mode as part of a build or automated test. It requires
the user present, G HUB state recorded, Dynamic selected, and a device-scoped
USB capture:

```powershell
.\artifacts\native\Release\Rs50DirectInputQuery.exe `
  --query-display-support `
  --confirm-standard-data-format `
  --confirm-exclusive-acquire `
  --confirm-transmit-query
```

All three confirmation arguments are required in addition to the query
selector. One process can attempt the query only once. Close and recreate the
process only for a separately approved new trial.

The separately authorized Layout J capability mode is documented in
[`RS50_LAYOUT_J_QUERY_OPERATOR_CHECKLIST.md`](../LogiDynamicDash/docs/RS50_LAYOUT_J_QUERY_OPERATOR_CHECKLIST.md).

The fixed static setter requires a new, separate authorization and follows
[`RS50_STATIC_LAYOUT_J_OPERATOR_CHECKLIST.md`](../LogiDynamicDash/docs/RS50_STATIC_LAYOUT_J_OPERATOR_CHECKLIST.md).

Follow the complete
[`RS50_QUERY_ONLY_OPERATOR_CHECKLIST.md`](../LogiDynamicDash/docs/RS50_QUERY_ONLY_OPERATOR_CHECKLIST.md)
instead of running the executable ad hoc.
