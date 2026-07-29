# LogiDynamicDash
Community telemetry display for the Dynamic OLED screen on Logitech PRO Racing Wheel and RS50.

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

Copy `logidynamicdash.example.json` and edit the copy to select layout A-J,
KMH or MPH, maximum RPM, and the full-scale speed for the secondary gauge.
Validate every layout without HID:

```powershell
LogiDynamicDash.exe --preview-all --config .\my-dashboard.json
LogiDynamicDash.exe --simulate-all --config .\my-dashboard.json
```

Preview prints the four application modes for every layout. Simulation runs
30 seconds of virtual telemetry through the real formatter, session, rate
limit, protocol encoder, and a simulated acknowledgement exchange. Neither
command enumerates or opens HID devices.

Do not run the stationary hardware route without a separately reviewed
checklist, fresh authorization, and capture. Its complete arming contract is
documented in
[`LogiDynamicDash/docs/RS50_OLED_PRODUCTION_ARCHITECTURE.md`](LogiDynamicDash/docs/RS50_OLED_PRODUCTION_ARCHITECTURE.md).
