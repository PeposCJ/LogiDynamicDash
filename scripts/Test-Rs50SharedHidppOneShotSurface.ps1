[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$oneShotDirectory =
    Join-Path $repositoryRoot "Rs50SharedHidppOneShot"
$oneShotProject =
    Join-Path $oneShotDirectory "Rs50SharedHidppOneShot.csproj"
$dashboardProject =
    Join-Path $repositoryRoot "LogiDynamicDash\LogiDynamicDash.csproj"
$programPath =
    Join-Path $repositoryRoot "LogiDynamicDash\Program.cs"
$appDependencies =
    Join-Path $repositoryRoot `
        "LogiDynamicDash\bin\Release\net10.0\LogiDynamicDash.deps.json"

if (-not (Test-Path -LiteralPath $oneShotProject -PathType Leaf)) {
    throw "The Build G one-shot project was not found."
}

$sourceFiles = @(
    Get-ChildItem -LiteralPath $oneShotDirectory -Filter "*.cs" -File
)
$source = ($sourceFiles | Get-Content -Raw) -join "`n"
$project = Get-Content -LiteralPath $oneShotProject -Raw
$dashboard = Get-Content -LiteralPath $dashboardProject -Raw
$program = Get-Content -LiteralPath $programPath -Raw

$forbiddenPatterns = @{
    "direct HID access" =
        "\b(HidSharp|GetHidDevices|TryOpen|SetFeature|GetFeature|Read|Write)\b"
    "native or DirectInput access" =
        "\b(DllImport|CreateFile|HidD_|DirectInput|Acquire|Unacquire)\b"
    "telemetry or simulator integration" =
        "\b(iRacing|Telemetry|MonitorAsync|SDK)\b"
    "force-feedback or LED feature" =
        "\b(ForceFeedback|FFB|RPM|LIGHTSYNC|Led)\b|0x(8123|807A|807B)"
    "loop or scheduler" =
        "\b(for|foreach|while)\s*\(|\bdo\s*\{|" +
        "\b(Timer|Thread|Task|Parallel)\b"
    "unapproved collection" =
        "\b(mi_00|mi_02|col02)\b"
}

foreach ($entry in $forbiddenPatterns.GetEnumerator()) {
    if ($source -match $entry.Value) {
        throw "Build G contains forbidden $($entry.Key)."
    }
}

$requiredArguments = @(
    "--arm-rs50-shared-hidpp",
    "--confirm-ghub-closed",
    "--confirm-rs50-awake",
    "--confirm-dynamic-selected",
    "--confirm-usbpcap-running",
    "--confirm-one-fixed-frame"
)

foreach ($argument in $requiredArguments) {
    if ([regex]::Matches(
            $source,
            [regex]::Escape('"' + $argument + '"')).Count -ne 1) {
        throw "Build G must contain each arming argument exactly once."
    }
}

$requiredLines = @(
    "RS50 SHARED HIDPP",
    "BUILD G",
    "ONE SHOT ONLY",
    "USBPCAP"
)

foreach ($line in $requiredLines) {
    if ([regex]::Matches(
            $source,
            [regex]::Escape('"' + $line + '"')).Count -ne 1) {
        throw "Build G must contain each fixed display line exactly once."
    }
}

if ([regex]::Matches($source, "\.Exchange\s*\(").Count -ne 2 -or
    [regex]::Matches($source, "\.CreateDiscovery\s*\(").Count -ne 1 -or
    [regex]::Matches($source, "\.CreateLayoutJ\s*\(").Count -ne 1 -or
    [regex]::Matches(
        $source,
        "\.ParseDiscoveryResponse\s*\(").Count -ne 1 -or
    [regex]::Matches(
        $source,
        "\.ParseLayoutJAcknowledgement\s*\(").Count -ne 1) {
    throw "Build G must perform exactly one closed discovery and setter."
}

if ($source -notmatch
        "using\s+IRs50HidppDisplayExchange\s+exchange\s*=" -or
    $source -notmatch
        "arguments\.SequenceEqual\(\s*ArmingArguments") {
    throw "Build G is missing deterministic disposal or exact arming."
}

$projectReferences = [regex]::Matches(
    $project,
    "<ProjectReference\b").Count
$packageReferences = [regex]::Matches(
    $project,
    "<PackageReference\b").Count

if ($projectReferences -ne 2 -or
    $packageReferences -ne 0 -or
    $project -notmatch
        "Rs50SharedHidppProtocol\\Rs50SharedHidppProtocol\.csproj" -or
    $project -notmatch
        "Rs50SharedHidppTransport\\Rs50SharedHidppTransport\.csproj") {
    throw "Build G has an unexpected dependency surface."
}

if ($dashboard -match "Rs50SharedHidppOneShot|SharedHidppTransport" -or
    $program -match
        "Rs50SharedHidppOneShot|Rs50HidppDeviceExchange") {
    throw "LogiDynamicDash must not reference or construct Build G."
}

if (Test-Path -LiteralPath $appDependencies -PathType Leaf) {
    $dependencies = Get-Content -LiteralPath $appDependencies -Raw
    if ($dependencies -match
            "Rs50SharedHidppOneShot|Rs50SharedHidppTransport|HidSharp") {
        throw "Build G or its physical dependencies leaked into the app."
    }
}

$publicTypeCount = [regex]::Matches(
    $source,
    "\bpublic\s+(?:sealed\s+|static\s+|partial\s+)?" +
        "(?:class|interface|enum|record|struct)\b"
).Count

if ($publicTypeCount -ne 0) {
    throw "Build G must not expose a public type."
}

Write-Output "RS50 shared HID++ Build G one-shot audit passed."
Write-Output "  Arming: six exact ordered confirmations"
Write-Output "  Operations: one discovery + one fixed Layout J setter"
Write-Output "  Loops, telemetry, raw HID, DirectInput, FFB, and LEDs: absent"
Write-Output "  LogiDynamicDash application reference: absent"
