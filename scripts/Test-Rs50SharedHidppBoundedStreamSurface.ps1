[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$streamDirectory =
    Join-Path $repositoryRoot "Rs50SharedHidppBoundedStream"
$streamProject =
    Join-Path $streamDirectory "Rs50SharedHidppBoundedStream.csproj"
$dashboardProject =
    Join-Path $repositoryRoot "LogiDynamicDash\LogiDynamicDash.csproj"
$programPath =
    Join-Path $repositoryRoot "LogiDynamicDash\Program.cs"
$appDependencies =
    Join-Path $repositoryRoot `
        "LogiDynamicDash\bin\Release\net10.0\LogiDynamicDash.deps.json"

if (-not (Test-Path -LiteralPath $streamProject -PathType Leaf)) {
    throw "The Build H bounded-stream project was not found."
}

$sourceFiles = @(
    Get-ChildItem -LiteralPath $streamDirectory -Filter "*.cs" -File
)
$source = ($sourceFiles | Get-Content -Raw) -join "`n"
$project = Get-Content -LiteralPath $streamProject -Raw
$dashboard = Get-Content -LiteralPath $dashboardProject -Raw
$program = Get-Content -LiteralPath $programPath -Raw

$forbiddenPatterns = @{
    "direct HID access" =
        "\b(HidSharp|GetHidDevices|TryOpen|SetFeature|GetFeature|Read|Write)\b"
    "native or DirectInput access" =
        "\b(DllImport|CreateFile|HidD_|DirectInput|Acquire|Unacquire)\b"
    "telemetry or simulator integration" =
        "\b(Telemetry|MonitorAsync|RequiredTelemetryVars|SDK)\b"
    "force-feedback or LED feature" =
        "\b(ForceFeedback|FFB|RPM|LIGHTSYNC|Led)\b|0x(8123|807A|807B)"
    "unbounded execution" =
        "\b(for|foreach|while)\s*\(|\bdo\s*\{|" +
        "\b(Timer|Task|Parallel)\b"
    "unapproved collection" =
        "\b(mi_00|mi_02|col02)\b"
}

foreach ($entry in $forbiddenPatterns.GetEnumerator()) {
    if ($source -match $entry.Value) {
        throw "Build H contains forbidden $($entry.Key)."
    }
}

$requiredArguments = @(
    "--arm-rs50-shared-hidpp-bounded-stream",
    "--confirm-ghub-closed",
    "--confirm-iracing-closed",
    "--confirm-rs50-awake",
    "--confirm-dynamic-selected",
    "--confirm-usbpcap-running",
    "--confirm-one-hz-five-fixed-frames"
)

foreach ($argument in $requiredArguments) {
    if ([regex]::Matches(
            $source,
            [regex]::Escape('"' + $argument + '"')).Count -ne 1) {
        throw "Build H must contain each arming argument exactly once."
    }
}

$requiredText = @(
    "RS50 SHARED HIDPP",
    "BUILD H",
    "FRAME 1 OF 5",
    "FRAME 2 OF 5",
    "FRAME 3 OF 5",
    "FRAME 4 OF 5",
    "FRAME 5 OF 5",
    "1 HZ"
)

foreach ($text in $requiredText) {
    if ([regex]::Matches(
            $source,
            [regex]::Escape('"' + $text + '"')).Count -ne 1) {
        throw "Build H must contain each fixed display field exactly once."
    }
}

if ([regex]::Matches(
        $source,
        "SendFrame\s*\(\s*exchange").Count -ne 5 -or
    [regex]::Matches(
        $source,
        "delay\.WaitOneSecond\s*\(\s*\)").Count -ne 4 -or
    [regex]::Matches(
        $source,
        "\.CreateDiscovery\s*\(").Count -ne 1 -or
    [regex]::Matches(
        $source,
        "\.CreateLayoutJ\s*\(").Count -ne 1 -or
    [regex]::Matches(
        $source,
        "\.ParseDiscoveryResponse\s*\(").Count -ne 1 -or
    [regex]::Matches(
        $source,
        "\.ParseLayoutJAcknowledgement\s*\(").Count -ne 1) {
    throw "Build H must contain one discovery and exactly five fixed setters."
}

if ([regex]::Matches(
        $source,
        "Thread\.Sleep\s*\(").Count -ne 1 -or
    [regex]::Matches(
        $source,
        "TimeSpan\.FromSeconds\s*\(\s*1\s*\)").Count -ne 1) {
    throw "Build H must implement only the fixed one-second delay."
}

if ($source -notmatch
        "using\s+IRs50HidppDisplayExchange\s+exchange\s*=" -or
    $source -notmatch
        "arguments\.SequenceEqual\(\s*ArmingArguments") {
    throw "Build H is missing deterministic disposal or exact arming."
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
    throw "Build H has an unexpected dependency surface."
}

if ($dashboard -match
        "Rs50SharedHidppBoundedStream|SharedHidppTransport" -or
    $program -match
        "Rs50SharedHidppBoundedStream|Rs50HidppDeviceExchange") {
    throw "LogiDynamicDash must not reference or construct Build H."
}

if (Test-Path -LiteralPath $appDependencies -PathType Leaf) {
    $dependencies = Get-Content -LiteralPath $appDependencies -Raw
    if ($dependencies -match
            "Rs50SharedHidppBoundedStream|" +
            "Rs50SharedHidppTransport|HidSharp") {
        throw "Build H or its physical dependencies leaked into the app."
    }
}

$publicTypeCount = [regex]::Matches(
    $source,
    "\bpublic\s+(?:sealed\s+|static\s+|partial\s+)?" +
        "(?:class|interface|enum|record|struct)\b"
).Count

if ($publicTypeCount -ne 0) {
    throw "Build H must not expose a public type."
}

Write-Output "RS50 shared HID++ Build H bounded-stream audit passed."
Write-Output "  Arming: seven exact ordered confirmations"
Write-Output "  Operations: one discovery + five fixed Layout J setters"
Write-Output "  Timing: four fixed one-second waits; no execution loop"
Write-Output "  Telemetry, raw HID, DirectInput, FFB, and LEDs: absent"
Write-Output "  LogiDynamicDash application reference: absent"
