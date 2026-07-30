# LogiDynamicDash
Community telemetry display research for Logitech racing-wheel Dynamic OLED
screens. RS50 is the only physically confirmed device; PRO support remains a
future compatibility target and is not currently claimed.

The production branch now contains an offline-tested, strictly typed path for
the ten confirmed firmware-rendered OLED layouts A-J. It discovers public
HID++ feature `0x8130` at runtime, validates exact acknowledgements, and does
not expose arbitrary feature IDs, functions, report bytes, graphics, or fonts.

The independently validated research remains separate from production code.
The stationary production gate has passed on physical RS50 hardware. The
compiled hardware routes remain disabled behind exact command-line arming:
the approved ten-second stationary route and a separate 15-second Build L
route that fails closed above 20 km/h. Both gates have passed on physical
RS50 hardware. A third manually stopped continuous-driving route has no speed
or duration ceiling but retains fail-closed transport/protocol handling. That
route has also passed on the physical RS50 through 200.31 km/h, six forward
gears, missing-ACK recovery, manual shutdown, and a live last-lap page
transition.

Build and run the safe console-only mode:

```powershell
dotnet build .\LogiDynamicDash.slnx -c Release
dotnet run --project .\LogiDynamicDash\LogiDynamicDash.csproj
```

Run the offline test suite:

```powershell
dotnet test .\LogiDynamicDash.slnx -c Release
```

Copy `logidynamicdash.example.json` and edit the copy to select layout A-J
independently for normal, brake-bias, last-lap, and connection pages, plus
KMH or MPH, maximum RPM, and the full-scale speed for the secondary gauge.
Validate every layout without HID:

```powershell
LogiDynamicDash.exe --preview-all --config .\my-dashboard.json
LogiDynamicDash.exe --simulate-all --config .\my-dashboard.json
LogiDynamicDash.exe --replay --config .\my-dashboard.json `
  --telemetry .\replays\mode-transitions.json
LogiDynamicDash.exe --record-telemetry --output .\my-session.json `
  --duration-seconds 300
```

Preview prints the four application modes for every layout. Simulation runs
30 seconds of virtual telemetry through the real formatter, session, rate
limit, protocol encoder, and a simulated acknowledgement exchange. Neither
command enumerates or opens HID devices. Replay passes strict, deterministic
telemetry scenarios through the same application controller and formatter.
Included scenarios cover acceleration, temporary pages, and disconnect/recovery.
The offline safety and failure coverage is summarized in
[`LogiDynamicDash/docs/OFFLINE_FAULT_MATRIX.md`](LogiDynamicDash/docs/OFFLINE_FAULT_MATRIX.md).

`LogiDynamicDash.Configurator.exe` provides a hardware-free Windows editor for
per-mode layouts, speed units, gauge scales, reviewed Sports Car, Formula Car,
Oval, Dirt Oval, and Dirt Road defaults, and typed previews. Live iRacing
session metadata records the official event category and exact driver car
identity (`CarID`, path, names, class, and electric flag) without guessing from
track or driving behavior. The configurator can inspect a schema 2 telemetry
replay, show car → category → recommendation, and apply the recommendation
only after an explicit click. Try `replays/session-identity.json` without
hardware. Product scope, discipline rationale, and Free/Pro planning are
documented in
[`LogiDynamicDash/docs/PRODUCT_AND_MONETIZATION_PLAN.md`](LogiDynamicDash/docs/PRODUCT_AND_MONETIZATION_PLAN.md).

Do not run a hardware route without its reviewed checklist and fresh
authorization. The stationary result, Build L procedure, continuous-driving
procedure, and complete arming contracts are documented in
[`LogiDynamicDash/docs`](LogiDynamicDash/docs).
