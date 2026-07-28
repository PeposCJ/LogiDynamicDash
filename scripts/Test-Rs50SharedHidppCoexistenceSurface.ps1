[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$coexistenceDirectory =
    Join-Path $repositoryRoot "Rs50SharedHidppCoexistence"
$verificationDirectory =
    Join-Path $repositoryRoot "Rs50SharedHidppCoexistence.Verification"
$coexistenceProject =
    Join-Path $coexistenceDirectory "Rs50SharedHidppCoexistence.csproj"
$verificationProject =
    Join-Path $verificationDirectory `
        "Rs50SharedHidppCoexistence.Verification.csproj"
$dashboardProject =
    Join-Path $repositoryRoot "LogiDynamicDash\LogiDynamicDash.csproj"
$programPath =
    Join-Path $repositoryRoot "LogiDynamicDash\Program.cs"
$appDependencies =
    Join-Path $repositoryRoot `
        "LogiDynamicDash\bin\Release\net10.0\LogiDynamicDash.deps.json"

foreach ($projectPath in @($coexistenceProject, $verificationProject)) {
    if (-not (Test-Path -LiteralPath $projectPath -PathType Leaf)) {
        throw "A Build I project was not found."
    }
}

$sourceFiles = @(
    Get-ChildItem -LiteralPath $coexistenceDirectory -Filter "*.cs" -File
)
$source = ($sourceFiles | Get-Content -Raw) -join "`n"
$project = Get-Content -LiteralPath $coexistenceProject -Raw
$verificationSource = (
    Get-ChildItem -LiteralPath $verificationDirectory -Filter "*.cs" -File |
        Get-Content -Raw
) -join "`n"
$verificationProjectSource =
    Get-Content -LiteralPath $verificationProject -Raw
$dashboard = Get-Content -LiteralPath $dashboardProject -Raw
$program = Get-Content -LiteralPath $programPath -Raw

$forbiddenPatterns = @{
    "direct HID access" =
        "\b(HidSharp|GetHidDevices|TryOpen|SetFeature|GetFeature|Read|Write)\b"
    "native or DirectInput access" =
        "\b(DllImport|CreateFile|HidD_|DirectInput|Acquire|Unacquire)\b"
    "telemetry integration" =
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
        throw "Build I contains forbidden $($entry.Key)."
    }
}

$requiredArguments = @(
    "--arm-rs50-shared-hidpp-coexistence",
    "--confirm-ghub-closed",
    "--confirm-iracing-running",
    "--confirm-car-stationary-in-pits",
    "--confirm-rs50-awake",
    "--confirm-dynamic-selected",
    "--confirm-usbpcap-running",
    "--confirm-one-hz-five-fixed-frames"
)

foreach ($argument in $requiredArguments) {
    if ([regex]::Matches(
            $source,
            [regex]::Escape('"' + $argument + '"')).Count -ne 1) {
        throw "Build I must contain each arming argument exactly once."
    }
}

$requiredText = @(
    "IRACING COEXIST",
    "BUILD I",
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
        throw "Build I must contain each fixed display field exactly once."
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
        "\.CreateLayoutJ\s*\(").Count -ne 1) {
    throw "Build I must contain one discovery and exactly five fixed setters."
}

if ([regex]::Matches(
        $source,
        "Thread\.Sleep\s*\(").Count -ne 1 -or
    [regex]::Matches(
        $source,
        "TimeSpan\.FromSeconds\s*\(\s*1\s*\)").Count -ne 1) {
    throw "Build I must implement only the fixed one-second delay."
}

if ($source -notmatch
        "using\s+IRs50HidppDisplayExchange\s+exchange\s*=" -or
    $source -notmatch
        "arguments\.SequenceEqual\(\s*ArmingArguments") {
    throw "Build I is missing deterministic disposal or exact arming."
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
    throw "Build I has an unexpected dependency surface."
}

if ($verificationProjectSource -match
        "Rs50SharedHidppTransport|HidSharp" -or
    $verificationSource -match
        "Rs50HidppDeviceExchange|HidSharp|GetHidDevices|TryOpen") {
    throw "The Build I verifier must remain fake-only."
}

if ($dashboard -match
        "Rs50SharedHidppCoexistence|SharedHidppTransport" -or
    $program -match
        "Rs50SharedHidppCoexistence|Rs50HidppDeviceExchange") {
    throw "LogiDynamicDash must not reference or construct Build I."
}

if (Test-Path -LiteralPath $appDependencies -PathType Leaf) {
    $dependencies = Get-Content -LiteralPath $appDependencies -Raw
    if ($dependencies -match
            "Rs50SharedHidppCoexistence|" +
            "Rs50SharedHidppTransport|HidSharp") {
        throw "Build I or its physical dependencies leaked into the app."
    }
}

$publicTypeCount = [regex]::Matches(
    $source,
    "\bpublic\s+(?:sealed\s+|static\s+|partial\s+)?" +
        "(?:class|interface|enum|record|struct)\b"
).Count

if ($publicTypeCount -ne 0) {
    throw "Build I must not expose a public type."
}

Write-Output "RS50 shared HID++ Build I coexistence audit passed."
Write-Output "  Arming: eight exact ordered stationary confirmations"
Write-Output "  Operations: one discovery + five fixed Layout J setters"
Write-Output "  Timing: four fixed one-second waits; no execution loop"
Write-Output "  Telemetry, raw HID, DirectInput, FFB, and LEDs: absent"
Write-Output "  Fake verifier: no physical transport reference"
Write-Output "  LogiDynamicDash application reference: absent"
