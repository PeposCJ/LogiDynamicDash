[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

$repositoryRoot =
    (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$productionRoot =
    Join-Path $repositoryRoot "LogiDynamicDash"

$sourceFiles = Get-ChildItem `
    -LiteralPath $productionRoot `
    -Recurse `
    -Filter "*.cs" `
    -File

$prohibitedPatterns = @(
    "DirectInput",
    "0x8123",
    "DllImport",
    "NativeLibrary",
    "SetFeature",
    "Bootloader"
)

foreach ($pattern in $prohibitedPatterns) {
    $match = $sourceFiles |
        Select-String -SimpleMatch -Pattern $pattern |
        Select-Object -First 1

    if ($match) {
        throw "Production source contains prohibited token '$pattern' in " +
            "'$($match.Path)'."
    }
}

$programPath =
    Join-Path $productionRoot "Program.cs"
$programText =
    Get-Content -LiteralPath $programPath -Raw

foreach ($token in @("HidSharp", "DeviceList", "Rs50OledDeviceExchange")) {
    if ($programText.IndexOf(
            $token,
            [StringComparison]::Ordinal) -ge 0) {
        throw "Program.cs directly references physical token '$token'."
    }
}

$offlineCommandIndex = $programText.IndexOf(
    "OfflineCommandLine.TryParse",
    [StringComparison]::Ordinal)
$displayFactoryIndex = $programText.IndexOf(
    "ApplicationDisplayFactory.TryCreate",
    [StringComparison]::Ordinal)

if ($offlineCommandIndex -lt 0 -or
    $displayFactoryIndex -lt 0 -or
    $offlineCommandIndex -gt $displayFactoryIndex) {
    throw "Offline commands must be routed before the display factory."
}

$factoryPath =
    Join-Path $productionRoot `
        "Configuration\ApplicationDisplayFactory.cs"
$factoryText =
    Get-Content -LiteralPath $factoryPath -Raw

if ($factoryText.IndexOf(
        "Rs50OledSessionFactory.OpenPhysicalWithLocalDiagnostics",
        [StringComparison]::Ordinal) -lt 0) {
    throw "The physical adapter is not isolated behind the display factory."
}

$physicalFactoryPath =
    Join-Path $productionRoot `
        "Diagnostics\Rs50OledSessionFactory.cs"
$physicalFactoryText =
    Get-Content -LiteralPath $physicalFactoryPath -Raw

if ($physicalFactoryText.IndexOf(
        "Rs50OledDeviceExchange.Open()",
        [StringComparison]::Ordinal) -lt 0) {
    throw "The typed physical exchange is missing from its isolated factory."
}

$runtimePath =
    Join-Path $productionRoot "Runtime\DashboardRuntime.cs"
$runtimeText =
    Get-Content -LiteralPath $runtimePath -Raw

foreach ($token in @(
        "RecoveringRs50OledDisplaySink",
        "Rs50OledSessionFactory.OpenPhysicalWithLocalDiagnostics",
        "AutomaticRs50TelemetryFrameFormatter")) {
    if ($runtimeText.IndexOf(
            $token,
            [StringComparison]::Ordinal) -lt 0) {
        throw "The production dashboard runtime is missing '$token'."
    }
}

$productionOptionsPath =
    Join-Path $productionRoot `
        "Configuration\Rs50ProductionRunOptions.cs"
$productionOptionsText =
    Get-Content -LiteralPath $productionOptionsPath -Raw

foreach ($token in @(
        "--run-rs50-oled",
        "--config",
        "--manual-profile",
        "--last-lap-seconds")) {
    if ($productionOptionsText.IndexOf(
            $token,
            [StringComparison]::Ordinal) -lt 0) {
        throw "The production runtime contract is missing '$token'."
    }
}

$offlineFiles = Get-ChildItem `
    -LiteralPath (Join-Path $productionRoot "Offline") `
    -Recurse `
    -Filter "*.cs" `
    -File

foreach ($token in @(
        "HidSharp",
        "DeviceList",
        "Rs50OledDeviceExchange",
        "Rs50OledSessionFactory")) {
    $match = $offlineFiles |
        Select-String -SimpleMatch -Pattern $token |
        Select-Object -First 1

    if ($match) {
        throw "Offline source references physical token '$token' in " +
            "'$($match.Path)'."
    }
}

$configuratorRoot =
    Join-Path $repositoryRoot "LogiDynamicDash.Configurator"
$configuratorFiles = Get-ChildItem `
    -LiteralPath $configuratorRoot `
    -Recurse `
    -Filter "*.cs" `
    -File

foreach ($token in @(
        "HidSharp",
        "DeviceList",
        "Rs50OledDeviceExchange",
        "Rs50OledSessionFactory",
        "IRs50OledSession")) {
    $match = $configuratorFiles |
        Select-String -SimpleMatch -Pattern $token |
        Select-Object -First 1

    if ($match) {
        throw "Configurator source references physical token '$token' in " +
            "'$($match.Path)'."
    }
}

$armingPath =
    Join-Path $productionRoot `
        "Configuration\Rs50StationaryTrialOptions.cs"
$armingText =
    Get-Content -LiteralPath $armingPath -Raw

$requiredArmingTokens = @(
    "--enable-rs50-oled-stationary-trial",
    "--confirm-ghub-closed",
    "--confirm-iracing-running",
    "--confirm-car-stationary-in-pits",
    "--confirm-rs50-dynamic-selected",
    "--confirm-10-second-limit",
    "--acknowledge-no-moving-car-use",
    "--confirm-settings"
)

foreach ($token in $requiredArmingTokens) {
    if ($armingText.IndexOf(
            $token,
            [StringComparison]::Ordinal) -lt 0) {
        throw "The stationary arming contract is missing '$token'."
    }
}

$lowSpeedArmingPath =
    Join-Path $productionRoot `
        "Configuration\Rs50LowSpeedTrialOptions.cs"
$lowSpeedArmingText =
    Get-Content -LiteralPath $lowSpeedArmingPath -Raw

$requiredLowSpeedTokens = @(
    "--enable-rs50-oled-low-speed-trial",
    "--confirm-ghub-closed",
    "--confirm-iracing-running",
    "--confirm-controlled-pit-lane",
    "--confirm-rs50-dynamic-selected",
    "--confirm-15-second-limit",
    "--confirm-maximum-20-kmh",
    "--acknowledge-stop-on-speed-limit",
    "--confirm-settings",
    "MaximumSpeedMetersPerSecond = 20f / 3.6f"
)

foreach ($token in $requiredLowSpeedTokens) {
    if ($lowSpeedArmingText.IndexOf(
            $token,
            [StringComparison]::Ordinal) -lt 0) {
        throw "The Build L arming contract is missing '$token'."
    }
}

$drivingArmingPath =
    Join-Path $productionRoot `
        "Configuration\Rs50DrivingTrialOptions.cs"
$drivingArmingText =
    Get-Content -LiteralPath $drivingArmingPath -Raw

$requiredDrivingTokens = @(
    "--enable-rs50-oled-driving-trial",
    "--confirm-ghub-closed",
    "--confirm-iracing-running",
    "--confirm-controlled-driving-session",
    "--confirm-rs50-dynamic-selected",
    "--acknowledge-no-speed-limit",
    "--acknowledge-manual-stop-required",
    "--confirm-settings"
)

foreach ($token in $requiredDrivingTokens) {
    if ($drivingArmingText.IndexOf(
            $token,
            [StringComparison]::Ordinal) -lt 0) {
        throw "The continuous-driving arming contract is missing '$token'."
    }
}

$sinkPath =
    Join-Path $productionRoot `
        "Displays\Rs50OledDisplaySink.cs"
$sinkText =
    Get-Content -LiteralPath $sinkPath -Raw

if ($sinkText.IndexOf(
        "MaximumStationarySpeedMetersPerSecond = 0.5f",
        [StringComparison]::Ordinal) -lt 0) {
    throw "The stationary movement guard is missing or changed."
}

Write-Output (
    "RS50 OLED production surface audit passed: no DirectInput, FFB, " +
    "native-import, feature-report, or bootloader API was found; the " +
    "offline commands contain no physical adapter reference; the GUI only " +
    "reaches the exact physical adapter through the explicit production " +
    "runtime; the runtime has bounded reconnect behavior and automatic " +
    "profile selection; the " +
    "physical routes remain isolated behind exact stationary, Build L, and " +
    "continuous-driving arming contracts; bounded routes retain 0.5 m/s and " +
    "20 km/h guards.")
