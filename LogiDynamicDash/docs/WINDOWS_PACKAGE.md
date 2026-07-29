# Windows Package

## Artifact

GitHub Actions produces a framework-dependent `LogiDynamicDash-win-x64`
artifact after build, tests, formatting, safety audit, and dependency audit
all pass.

The artifact is unsigned and is not a release. It is retained temporarily for
review and offline preview. It must not be presented as an official Logitech
product.

## Requirements

- 64-bit Windows
- Microsoft .NET 10 Runtime, x64
- iRacing only for live console monitoring or a separately authorized
  stationary OLED test
- G HUB is not required for preview or simulation

## Integrity

`SHA256SUMS.txt` contains a SHA-256 hash for every packaged file except the
manifest itself. Verify a file in PowerShell with:

```powershell
Get-FileHash -Algorithm SHA256 .\LogiDynamicDash.exe
```

Compare the reported hash with the matching manifest line before use.

## Hardware-Free Use

Copy `logidynamicdash.example.json` to a writable location and edit the copy.
Then run:

```powershell
.\LogiDynamicDash.exe --preview-all --config .\my-dashboard.json
.\LogiDynamicDash.exe --simulate-all --config .\my-dashboard.json
```

These modes never enumerate or open HID devices.

Running without arguments starts the console telemetry monitor:

```powershell
.\LogiDynamicDash.exe
```

## Hardware Route

The package contains a bounded stationary-validation route, but its presence
is not authorization to run it. Follow
`RS50_OLED_PRODUCTION_STATIONARY_CHECKLIST.md` from the repository and obtain
fresh authorization first.

No moving-car or full-lap mode is enabled in this package.
