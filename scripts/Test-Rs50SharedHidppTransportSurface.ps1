[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$transportDirectory =
    Join-Path $repositoryRoot "Rs50SharedHidppTransport"
$transportProject =
    Join-Path $transportDirectory "Rs50SharedHidppTransport.csproj"
$dashboardProject =
    Join-Path $repositoryRoot "LogiDynamicDash\LogiDynamicDash.csproj"
$programPath =
    Join-Path $repositoryRoot "LogiDynamicDash\Program.cs"
$appDependencies =
    Join-Path $repositoryRoot `
        "LogiDynamicDash\bin\Release\net10.0\LogiDynamicDash.deps.json"

if (-not (Test-Path -LiteralPath $transportProject -PathType Leaf)) {
    throw "The Build F transport project was not found."
}

$sourceFiles = @(
    Get-ChildItem -LiteralPath $transportDirectory -Filter "*.cs" -File
)
$source = ($sourceFiles | Get-Content -Raw) -join "`n"
$project = Get-Content -LiteralPath $transportProject -Raw
$dashboard = Get-Content -LiteralPath $dashboardProject -Raw
$program = Get-Content -LiteralPath $programPath -Raw

$forbiddenPatterns = @{
    "native device API" = "\b(CreateFile|WriteFile|HidD_|SetupDi|DllImport)\w*"
    "DirectInput lifecycle" = "\b(DirectInput|Acquire|Unacquire)\b"
    "force-feedback surface" = "\b(ForceFeedback|FFB)\b|0x8123"
    "LED surface" = "\b(RPM|LIGHTSYNC|Led)\b|0x807[AB]"
    "unapproved collection" = "mi_0[02]|col02"
    "feature or output report API" = "\b(SetFeature|GetFeature)\b"
}

foreach ($entry in $forbiddenPatterns.GetEnumerator()) {
    if ($source -match $entry.Value) {
        throw "Build F contains forbidden $($entry.Key)."
    }
}

if ($project -notmatch
    '<PackageReference Include="HidSharp" Version="2\.6\.4"\s*/>' -or
    $project -notmatch
    'Include="\.\.\\Rs50SharedHidppProtocol\\Rs50SharedHidppProtocol\.csproj"\s*/>') {
    throw "Build F has an unexpected project dependency surface."
}

$packageCount = [regex]::Matches(
    $project,
    "<PackageReference\b").Count

if ($packageCount -ne 1) {
    throw "Build F must reference exactly one package."
}

if ($dashboard -match
        '<ProjectReference[^>]+Rs50SharedHidppTransport' -or
    $program -match "Rs50HidppDeviceExchange|SharedHidppTransport") {
    throw "The application must not reference or construct Build F."
}

if (Test-Path -LiteralPath $appDependencies -PathType Leaf) {
    $dependencies = Get-Content -LiteralPath $appDependencies -Raw
    if ($dependencies -match "Rs50SharedHidppTransport|HidSharp") {
        throw "The physical transport leaked into the application output."
    }
}

if ($source -notmatch "LogitechVendorId\s*=\s*0x046D\s*;" -or
    $source -notmatch "Rs50ProductId\s*=\s*0xC276\s*;" -or
    $source -notmatch "ShortCollectionUsage\s*=\s*0xFF430701\s*;" -or
    $source -notmatch "VeryLongCollectionUsage\s*=\s*0xFF430704\s*;" -or
    $source -notmatch '"mi_01&col01"' -or
    $source -notmatch '"mi_01&col03"') {
    throw "Build F is missing an exact RS50 collection identity gate."
}

$exchangeImplementations = [regex]::Matches(
    $source,
    ":\s*IRs50HidppDisplayExchange\b").Count

if ($exchangeImplementations -ne 1) {
    throw "Build F must contain exactly one closed exchange implementation."
}

$deviceEnumerationCount = [regex]::Matches(
    $source,
    "\.GetHidDevices\s*\(").Count

if ($deviceEnumerationCount -ne 1) {
    throw "Build F must have exactly one VID/PID-scoped HID enumeration."
}

$tryOpenCount = [regex]::Matches(
    $source,
    "\.TryOpen\s*\(").Count

if ($tryOpenCount -ne 1) {
    throw "Build F must have exactly one collection open call site."
}

$publicTypeCount = [regex]::Matches(
    $source,
    "\bpublic\s+(?:sealed\s+|static\s+|partial\s+)?(?:class|interface|enum|record|struct)\b"
).Count

if ($publicTypeCount -ne 0) {
    throw "Build F must not expose a public transport type."
}

Write-Output "RS50 shared HID++ Build F transport audit passed."
Write-Output "  Collections: MI_01 COL01 (FF43:0701) + COL03 (FF43:0704)"
Write-Output "  VID/PID: 046D:C276 only; exact lengths and sibling path required"
Write-Output "  Application/CLI reference: absent"
Write-Output "  DirectInput, FFB, LEDs, MI_00/MI_02, native HID APIs: absent"
