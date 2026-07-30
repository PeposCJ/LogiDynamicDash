# Windows Package

## Artifact

GitHub Actions produces framework-dependent `LogiDynamicDash-win-x64` and
self-contained `LogiDynamicDash-win-x64-self-contained` artifacts after
build, tests, formatting, safety audit, dependency audit, and package smoke
tests all pass.

The artifact is an unsigned `0.3.0-alpha` candidate. It must not be presented
as an official Logitech product.

## Requirements

- 64-bit Windows
- Microsoft .NET 10 Runtime, x64, only for the framework-dependent artifact
- iRacing for live dashboard telemetry
- G HUB closed while LogiDynamicDash owns the OLED interface

## Integrity

`SHA256SUMS.txt` contains a SHA-256 hash for every packaged file except the
manifest itself. Verify a file in PowerShell with:

```powershell
Get-FileHash -Algorithm SHA256 .\LogiDynamicDash.exe
```

Compare the reported hash with the matching manifest line before use.
`sbom.spdx.json` records the application and direct runtime dependencies in
SPDX 2.3 format.

## Hardware-Free Use

Copy `logidynamicdash.example.json` to a writable location and edit the copy.
Then run:

```powershell
.\LogiDynamicDash.exe --preview-all --config .\my-dashboard.json
.\LogiDynamicDash.exe --simulate-all --config .\my-dashboard.json
.\LogiDynamicDash.exe --replay --config .\my-dashboard.json `
  --telemetry .\replays\mode-transitions.json
.\LogiDynamicDash.exe --record-telemetry --output .\my-session.json `
  --duration-seconds 300
```

These routes never enumerate or open HID devices. Recording requires iRacing
telemetry and writes at most 5 Hz for 1–1,800 seconds. Preview, simulation,
and replay run as package smoke tests before either artifact is uploaded.

`LogiDynamicDash.Configurator.exe` edits and previews configurations without
hardware until the user explicitly selects **Start dashboard**. While running,
it reports OLED, iRacing, car, and category state; applies automatic category
or CarID profiles; and reconnects the exact validated OLED interface after
sleep or disconnection. **Stop** disposes the OLED streams. The included
`replays\session-identity.json` remains a hardware-free identity example.

Running without arguments starts the console telemetry monitor:

```powershell
.\LogiDynamicDash.exe
```

## Hardware Route

For daily use, open the configurator and select **Start dashboard**. The
equivalent console route is:

```powershell
.\LogiDynamicDash.exe --run-rs50-oled `
  --config .\logidynamicdash.example.json
```

The package retains separately armed engineering-validation routes for
maintainers. Their historical procedures and evidence remain on the research
branch and they are not part of ordinary product use.
