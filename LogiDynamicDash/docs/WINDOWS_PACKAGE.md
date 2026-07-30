# Windows Package

## Artifact

GitHub Actions produces framework-dependent `LogiDynamicDash-win-x64` and
self-contained `LogiDynamicDash-win-x64-self-contained` artifacts after
build, tests, formatting, safety audit, dependency audit, and package smoke
tests all pass.

The artifact is unsigned and is not a release. It is retained temporarily for
review and offline preview. It must not be presented as an official Logitech
product.

## Requirements

- 64-bit Windows
- Microsoft .NET 10 Runtime, x64, only for the framework-dependent artifact
- iRacing only for live console monitoring or a separately authorized
  bounded OLED test
- G HUB is not required for preview or simulation

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

`LogiDynamicDash.Configurator.exe` is the hardware-free graphical editor. It
can apply Sports Car, Formula Car, Oval, Dirt Oval, and Dirt Road
recommendations, preview all four display modes, and open/save strict JSON
configurations. Its **Inspect telemetry replay** action displays the exact car,
category, track context, and profile decision from a schema 2 replay. Applying
that recommendation remains a separate explicit action. The included
`replays\session-identity.json` is a hardware-free example. The configurator
cannot arm or access the OLED.

Running without arguments starts the console telemetry monitor:

```powershell
.\LogiDynamicDash.exe
```

## Hardware Route

The package contains separately armed stationary, 15-second Build L, and
manually stopped continuous-driving validation routes, but their presence is
not authorization to run them. Follow the matching checklist from the
repository and obtain fresh authorization first.

Build L fails closed above 20 km/h. The continuous-driving route has no speed
or duration ceiling and requires `Ctrl+C` for normal shutdown, while retaining
fail-closed invalid-telemetry, transport, and protocol behavior.
