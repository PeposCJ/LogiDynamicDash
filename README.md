# LogiDynamicDash
Community telemetry display research for Logitech racing-wheel Dynamic OLED
screens. RS50 is the only physically confirmed device; PRO support remains a
future compatibility target and is not currently claimed.

The production branch now contains an offline-tested, strictly typed path for
the ten confirmed firmware-rendered OLED layouts A-J. It discovers public
HID++ feature `0x8130` at runtime, validates exact acknowledgements, and does
not expose arbitrary feature IDs, functions, report bytes, graphics, or fonts.

The independently validated research remains separate from production code.
The only compiled hardware route is an explicit, ten-second stationary
validation gate; moving-car hardware validation remains postponed and is
rejected by the application.

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
per-mode layouts, speed units, gauge scales, reviewed Road/Oval defaults, and
typed previews. Product scope, discipline rationale, and Free/Pro planning are
documented in
[`LogiDynamicDash/docs/PRODUCT_AND_MONETIZATION_PLAN.md`](LogiDynamicDash/docs/PRODUCT_AND_MONETIZATION_PLAN.md).

Do not run the stationary hardware route without a separately reviewed
checklist, fresh authorization, and capture. Its complete arming contract is
documented in
[`LogiDynamicDash/docs/RS50_OLED_PRODUCTION_ARCHITECTURE.md`](LogiDynamicDash/docs/RS50_OLED_PRODUCTION_ARCHITECTURE.md).
