[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$sessionDirectory = Join-Path $repositoryRoot "LogiDynamicDash\Hidpp"
$protocolDirectory =
    Join-Path $repositoryRoot "Rs50SharedHidppProtocol"
$programPath = Join-Path $repositoryRoot "LogiDynamicDash\Program.cs"
$projectPath = Join-Path $repositoryRoot "LogiDynamicDash\LogiDynamicDash.csproj"

if (-not (Test-Path -LiteralPath $sessionDirectory -PathType Container) -or
    -not (Test-Path -LiteralPath $protocolDirectory -PathType Container)) {
    throw "The Build E HID++ source directories were not found."
}

$sourceFiles = @(
    Get-ChildItem -LiteralPath $sessionDirectory -Filter "*.cs" -File
    Get-ChildItem -LiteralPath $protocolDirectory -Filter "*.cs" -File
)
$source = ($sourceFiles | Get-Content -Raw) -join "`n"
$program = Get-Content -LiteralPath $programPath -Raw
$project = Get-Content -LiteralPath $projectPath -Raw

$forbiddenPatterns = @{
    "HidSharp device API" = "\b(HidSharp|HidDevice|HidStream|DeviceList)\b"
    "Win32 HID/device API" = "\b(CreateFile|WriteFile|HidD_|SetupDi)\w*"
    "DirectInput lifecycle" = "\b(DirectInput|Acquire|Unacquire)\b"
    "force-feedback feature" = "0x8123"
    "RPM/LIGHTSYNC feature" = "0x807[AB]"
    "unknown 0x813x feature" = "0x813[1-9A-Fa-f]"
    "native import" = "\bDllImport\b"
}

foreach ($entry in $forbiddenPatterns.GetEnumerator()) {
    if ($source -match $entry.Value) {
        throw "Build E contains forbidden $($entry.Key)."
    }
}

if ($project -match "HidSharp") {
    throw "The dashboard project must not reference a physical HID package."
}

if ($program -match "Rs50SharedHidpp|HidppDisplay") {
    throw "Build E must not be reachable from the application CLI."
}

if ($source -notmatch
    "DisplayFeatureId\s*=\s*0x8130\s*;" -or
    $source -notmatch
    "RootGetFeatureFunction\s*=\s*0x00\s*;" -or
    $source -notmatch
    "SetLayoutFunction\s*=\s*0x03\s*;" -or
    $source -notmatch
    "LayoutJIndex\s*=\s*0x09\s*;") {
    throw "Build E is missing its exact 0x8130/Layout J constants."
}

$productionImplementations = [regex]::Matches(
    $source,
    ":\s*IRs50HidppDisplayExchange\b").Count

if ($productionImplementations -ne 0) {
    throw "Build E must not contain a physical exchange implementation."
}

$publicTypeCount = [regex]::Matches(
    $source,
    "\bpublic\s+(?:sealed\s+|static\s+)?(?:class|interface|enum|record|struct)\b"
).Count

if ($publicTypeCount -ne 0) {
    throw "Build E must not expose a public protocol or transport type."
}

Write-Output "RS50 shared HID++ Build E surface audit passed."
Write-Output "  Feature: 0x8130 only"
Write-Output "  Operations: Root discovery and typed A-J setters only"
Write-Output "  Physical HID implementation: absent"
Write-Output "  DirectInput, FFB, RPM, LIGHTSYNC, and CLI routes: absent"
