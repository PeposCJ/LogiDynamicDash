[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$trialDirectory =
    Join-Path $repositoryRoot "Rs50SharedHidppTelemetryTrial"
$verificationDirectory =
    Join-Path $repositoryRoot "Rs50SharedHidppTelemetryTrial.Verification"
$trialProject =
    Join-Path $trialDirectory "Rs50SharedHidppTelemetryTrial.csproj"
$verificationProject =
    Join-Path $verificationDirectory `
        "Rs50SharedHidppTelemetryTrial.Verification.csproj"
$dashboardProject =
    Join-Path $repositoryRoot "LogiDynamicDash\LogiDynamicDash.csproj"
$programPath =
    Join-Path $repositoryRoot "LogiDynamicDash\Program.cs"
$appDependencies =
    Join-Path $repositoryRoot `
        "LogiDynamicDash\bin\Release\net10.0\LogiDynamicDash.deps.json"

foreach ($path in @(
        $trialProject,
        $verificationProject,
        $dashboardProject,
        $programPath)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "A required Build J audit input was not found."
    }
}

$source = (
    Get-ChildItem -LiteralPath $trialDirectory -Filter "*.cs" -File |
        Get-Content -Raw
) -join "`n"
$project = Get-Content -LiteralPath $trialProject -Raw
$verificationSource = (
    Get-ChildItem -LiteralPath $verificationDirectory -Filter "*.cs" -File |
        Get-Content -Raw
) -join "`n"
$verificationProjectSource =
    Get-Content -LiteralPath $verificationProject -Raw
$dashboard = Get-Content -LiteralPath $dashboardProject -Raw
$program = Get-Content -LiteralPath $programPath -Raw

$forbiddenPatterns = @{
    "native or DirectInput access" =
        "\b(DllImport|CreateFile|HidD_|DirectInput|Acquire|Unacquire)\b"
    "force-feedback or LED access" =
        "\b(ForceFeedback|FFB|RPM|LIGHTSYNC|Led)\b|0x(8123|807A|807B)"
    "raw HID operation" =
        "\b(GetHidDevices|TryOpen|SetFeature|GetFeature|" +
        "HidStream|DeviceList)\b"
    "caller-controlled display data" =
        "\b(Parse|TryParse)\s*\(\s*arguments|--(?:speed|gear|text|rate)"
    "unapproved collection" =
        "\b(mi_00|mi_02|col02)\b"
}

foreach ($entry in $forbiddenPatterns.GetEnumerator()) {
    if ($source -match $entry.Value) {
        throw "Build J contains forbidden $($entry.Key)."
    }
}

$requiredArguments = @(
    "--arm-rs50-shared-hidpp-telemetry",
    "--confirm-ghub-closed",
    "--confirm-iracing-running",
    "--confirm-car-stationary-in-pits",
    "--confirm-rs50-awake",
    "--confirm-dynamic-selected",
    "--confirm-usbpcap-running",
    "--confirm-video-recording",
    "--confirm-10-second-telemetry-trial"
)

foreach ($argument in $requiredArguments) {
    if ([regex]::Matches(
            $source,
            [regex]::Escape('"' + $argument + '"')).Count -ne 1) {
        throw "Build J must contain each arming argument exactly once."
    }
}

$requiredTelemetryVariables = @(
    "TelemetryVar.IsOnTrackCar",
    "TelemetryVar.Gear",
    "TelemetryVar.Speed"
)

foreach ($variable in $requiredTelemetryVariables) {
    if ([regex]::Matches(
            $source,
            [regex]::Escape($variable)).Count -ne 1) {
        throw "Build J must request exactly the three approved variables."
    }
}

if ([regex]::Matches(
        $source,
        "TelemetryVar\.").Count -ne 3 -or
    $source -notmatch
        "RequiredTelemetryVars\s*\(\s*\[") {
    throw "Build J has an unexpected telemetry subscription."
}

if ([regex]::Matches(
        $source,
        "\.CreateDiscovery\s*\(").Count -ne 1 -or
    [regex]::Matches(
        $source,
        "\.CreateLayoutJ\s*\(").Count -ne 1 -or
    [regex]::Matches(
        $source,
        "exchange\.Exchange\s*\(").Count -ne 2) {
    throw "Build J must expose only typed discovery and Layout J calls."
}

if ($source -notmatch
        "CancelAfter\s*\(\s*TimeSpan\.FromSeconds\s*\(\s*10\s*\)" -or
    $source -notmatch
        "TimeSpan\.FromMilliseconds\s*\(\s*200\s*\)" -or
    $source -notmatch
        "MaximumStationarySpeedMetersPerSecond\s*=\s*0\.5f" -or
    $source -notmatch
        "arguments\.SequenceEqual\(\s*ArmingArguments" -or
    $source -notmatch
        "using\s+IRs50HidppDisplayExchange\s+exchange\s*=") {
    throw "Build J duration, rate, stop limit, arming, or disposal changed."
}

foreach ($field in @("SPEED", "GEAR")) {
    if ([regex]::Matches(
            $source,
            [regex]::Escape('"' + $field + '"')).Count -ne 1) {
        throw "Build J fixed display labels changed."
    }
}

$projectReferences = [regex]::Matches(
    $project,
    "<ProjectReference\b").Count
$packageReferences = [regex]::Matches(
    $project,
    "<PackageReference\b").Count

if ($projectReferences -ne 2 -or
    $packageReferences -ne 1 -or
    $project -notmatch
        'PackageReference Include="SVappsLAB\.iRacingTelemetrySDK"' -or
    $project -notmatch
        "Rs50SharedHidppProtocol\\Rs50SharedHidppProtocol\.csproj" -or
    $project -notmatch
        "Rs50SharedHidppTransport\\Rs50SharedHidppTransport\.csproj" -or
    $project -match
        "LogiDynamicDash\\LogiDynamicDash\.csproj") {
    throw "Build J has an unexpected dependency surface."
}

if ($verificationProjectSource -match
        "Rs50SharedHidppTransport|HidSharp" -or
    $verificationSource -match
        "Rs50HidppDeviceExchange|HidSharp|GetHidDevices|TryOpen") {
    throw "The Build J verifier must remain fake-only."
}

if ($dashboard -match
        "Rs50SharedHidppTelemetryTrial|SharedHidppTransport" -or
    $program -match
        "BuildJTelemetryTrial|Rs50HidppDeviceExchange") {
    throw "LogiDynamicDash must not reference or construct Build J."
}

if (Test-Path -LiteralPath $appDependencies -PathType Leaf) {
    $dependencies = Get-Content -LiteralPath $appDependencies -Raw
    if ($dependencies -match
            "Rs50SharedHidppTelemetryTrial|" +
            "Rs50SharedHidppTransport|HidSharp") {
        throw "Build J or its physical dependencies leaked into the app."
    }
}

$publicTypeCount = [regex]::Matches(
    $source,
    "\bpublic\s+(?:sealed\s+|static\s+|partial\s+)?" +
        "(?:class|interface|enum|record|struct)\b"
).Count

if ($publicTypeCount -ne 0) {
    throw "Build J must not expose a public type."
}

Write-Output "RS50 shared HID++ Build J telemetry audit passed."
Write-Output "  Arming: nine exact ordered stationary confirmations"
Write-Output "  Telemetry: IsOnTrackCar, Gear, and Speed only"
Write-Output "  Limits: 10 seconds, 5 Hz, deduplicated, stop above 0.5 m/s"
Write-Output "  Operations: typed discovery and Layout J setter only"
Write-Output "  DirectInput, FFB, LEDs, and raw HID: absent"
Write-Output "  Fake verifier: no physical transport reference"
Write-Output "  LogiDynamicDash application reference: absent"
