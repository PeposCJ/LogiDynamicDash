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

$factoryPath =
    Join-Path $productionRoot `
        "Configuration\ApplicationDisplayFactory.cs"
$factoryText =
    Get-Content -LiteralPath $factoryPath -Raw

if ($factoryText.IndexOf(
        "Rs50OledDeviceExchange.Open()",
        [StringComparison]::Ordinal) -lt 0) {
    throw "The physical adapter is not isolated behind the display factory."
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
    "physical route remains isolated behind the exact stationary arming " +
    "contract and 0.5 m/s guard.")
