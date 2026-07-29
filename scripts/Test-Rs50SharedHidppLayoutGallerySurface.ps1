[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$galleryDirectory =
    Join-Path $repositoryRoot "Rs50SharedHidppLayoutGallery"
$galleryProject =
    Join-Path $galleryDirectory "Rs50SharedHidppLayoutGallery.csproj"
$protocolDirectory =
    Join-Path $repositoryRoot "Rs50SharedHidppProtocol"
$transportDirectory =
    Join-Path $repositoryRoot "Rs50SharedHidppTransport"
$dashboardProject =
    Join-Path $repositoryRoot "LogiDynamicDash\LogiDynamicDash.csproj"
$programPath =
    Join-Path $repositoryRoot "LogiDynamicDash\Program.cs"
$appDependencies =
    Join-Path $repositoryRoot `
        "LogiDynamicDash\bin\Release\net10.0\LogiDynamicDash.deps.json"

foreach ($path in @(
        $galleryProject,
        $dashboardProject,
        $programPath)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "A required Build K audit input was not found."
    }
}

$gallerySource = (
    Get-ChildItem -LiteralPath $galleryDirectory -Filter "*.cs" -File |
        Get-Content -Raw
) -join "`n"
$protocolSource = (
    Get-ChildItem -LiteralPath $protocolDirectory -Filter "*.cs" -File |
        Get-Content -Raw
) -join "`n"
$transportSource = (
    Get-ChildItem -LiteralPath $transportDirectory -Filter "*.cs" -File |
        Get-Content -Raw
) -join "`n"
$project = Get-Content -LiteralPath $galleryProject -Raw
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
    "telemetry integration" =
        "\b(Telemetry|MonitorAsync|RequiredTelemetryVars|SDK)\b"
    "caller-controlled display data" =
        "\b(Parse|TryParse)\s*\(\s*arguments|" +
        "--(?:layout|value|text|rate|duration)"
    "unapproved feature or device" =
        "0x(?:18A2|8093|9315|813[1-9A-Fa-f])|" +
        "\b(?:device|dev)[-_ ]?0x0[125]\b"
}

foreach ($entry in $forbiddenPatterns.GetEnumerator()) {
    if ($gallerySource -match $entry.Value) {
        throw "Build K contains forbidden $($entry.Key)."
    }
}

$requiredArguments = @(
    "--arm-rs50-shared-hidpp-layout-gallery",
    "--confirm-ghub-closed",
    "--confirm-iracing-closed",
    "--confirm-rs50-awake",
    "--confirm-dynamic-selected",
    "--confirm-usbpcap-running",
    "--confirm-video-recording",
    "--confirm-ten-layouts-three-seconds-each"
)

foreach ($argument in $requiredArguments) {
    if ([regex]::Matches(
            $gallerySource,
            [regex]::Escape('"' + $argument + '"')).Count -ne 1) {
        throw "Build K must contain each arming argument exactly once."
    }
}

foreach ($layout in [char[]]"ABCDEFGHIJ") {
    if ([regex]::Matches(
            $gallerySource,
            "CreateLayout$layout\s*\(").Count -ne 1) {
        throw "Build K must contain one fixed Layout $layout setter."
    }
}

if ([regex]::Matches(
        $gallerySource,
        "delay\.WaitThreeSeconds\s*\(\s*\)").Count -ne 10 -or
    [regex]::Matches(
        $gallerySource,
        "Thread\.Sleep\s*\(").Count -ne 1 -or
    $gallerySource -notmatch
        "TimeSpan\.FromSeconds\s*\(\s*3\s*\)" -or
    $gallerySource -match
        "\b(for|foreach|while)\s*\(|\bdo\s*\{|" +
        "\b(Timer|Task|Parallel)\b") {
    throw "Build K must contain ten fixed waits and no execution loop."
}

if ([regex]::Matches(
        $gallerySource,
        "\.CreateDiscovery\s*\(").Count -ne 1 -or
    $gallerySource -notmatch
        "arguments\.SequenceEqual\(\s*ArmingArguments" -or
    $gallerySource -notmatch
        "using\s+IRs50HidppDisplayExchange\s+exchange\s*=") {
    throw "Build K discovery, exact arming, or disposal changed."
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
    throw "Build K has an unexpected dependency surface."
}

foreach ($layout in [char[]]"ABCDEFGHIJ") {
    if ($protocolSource -notmatch "CreateLayout$layout\s*\(") {
        throw "The typed protocol is missing Layout $layout."
    }
}

if ($transportSource -notmatch
        "SetLayoutA\s+or[\s\S]*SetLayoutJ" -or
    $transportSource -notmatch
        "request\[3\]\s*!=\s*0x3A") {
    throw "The transport is missing the closed A-J/function-3 gate."
}

if ($dashboard -match
        "Rs50SharedHidppLayoutGallery|SharedHidppTransport" -or
    $program -match
        "BuildKLayoutGallery|Rs50HidppDeviceExchange") {
    throw "LogiDynamicDash must not reference or construct Build K."
}

if (Test-Path -LiteralPath $appDependencies -PathType Leaf) {
    $dependencies = Get-Content -LiteralPath $appDependencies -Raw
    if ($dependencies -match
            "Rs50SharedHidppLayoutGallery|" +
            "Rs50SharedHidppTransport|HidSharp") {
        throw "Build K or its physical dependencies leaked into the app."
    }
}

$publicTypeCount = [regex]::Matches(
    $gallerySource,
    "\bpublic\s+(?:sealed\s+|static\s+|partial\s+)?" +
        "(?:class|interface|enum|record|struct)\b"
).Count

if ($publicTypeCount -ne 0) {
    throw "Build K must not expose a public type."
}

Write-Output "RS50 shared HID++ Build K layout-gallery audit passed."
Write-Output "  Arming: eight exact ordered confirmations"
Write-Output "  Operations: one discovery + fixed Layouts A-J"
Write-Output "  Timing: ten fixed three-second displays; no loop"
Write-Output "  Values and text: fixed in source; no caller control"
Write-Output "  Unknown rim features, DirectInput, FFB, LEDs: absent"
Write-Output "  LogiDynamicDash application reference: absent"
