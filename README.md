# LogiDynamicDash
Community telemetry dashboard for Logitech racing-wheel Dynamic OLED screens.
RS50 is the only physically confirmed device; PRO support remains a future
compatibility target and is not currently claimed.

The production branch now contains an offline-tested, strictly typed path for
the ten confirmed firmware-rendered OLED layouts A-J. It discovers public
HID++ feature `0x8130` at runtime, validates exact acknowledgements, and does
not expose arbitrary feature IDs, functions, report bytes, graphics, or fonts.

Physical validation evidence and discovery artifacts remain on the dedicated
research branch. Production contains the validated protocol, automated tests,
and a daily-use runtime that waits for iRacing and the RS50, reconnects after
sleep or disconnection, and selects a profile from exact CarID and official
iRacing category.

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

`LogiDynamicDash.Configurator.exe` provides the Windows editor and dashboard
launcher for per-mode layouts, speed units, gauge scales, reviewed Sports Car,
Formula Car, Oval, Dirt Oval, and Dirt Road defaults, and typed previews. Live iRacing
session metadata records the official event category and exact driver car
identity (`CarID`, path, names, class, and electric flag) without guessing from
track or driving behavior. The configurator can inspect a schema 2 telemetry
replay, show car → category → recommendation, and apply the recommendation
only after an explicit click. It can save category or exact-CarID overrides,
configure the `LAST LAP` duration, and explicitly start or stop OLED output.
Try `replays/session-identity.json` without hardware. Product scope,
discipline rationale, and Free/Pro planning are
documented in
[`LogiDynamicDash/docs/PRODUCT_AND_MONETIZATION_PLAN.md`](LogiDynamicDash/docs/PRODUCT_AND_MONETIZATION_PLAN.md).

See
[`LogiDynamicDash/docs/GETTING_STARTED.md`](LogiDynamicDash/docs/GETTING_STARTED.md)
for normal use. Historical physical-test procedures and raw evidence remain on
the research branch.
