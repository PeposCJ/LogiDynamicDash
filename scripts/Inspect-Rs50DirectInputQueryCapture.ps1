[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateScript({ Test-Path -LiteralPath $_ -PathType Leaf })]
    [string] $PcapPath,

    [Parameter(Mandatory)]
    [ValidateRange(1, 127)]
    [int] $DeviceAddress,

    [DateTimeOffset] $FromUtc,

    [DateTimeOffset] $ToUtc,

    [string] $TsharkPath = "C:\Program Files\Wireshark\tshark.exe"
)

$ErrorActionPreference = "Stop"

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$exportScript = Join-Path $PSScriptRoot "Export-Rs50HidReports.ps1"
$explorerProject = Join-Path `
    $repositoryRoot `
    "LogiDynamicExplorer\LogiDynamicExplorer.csproj"

if (-not (Test-Path -LiteralPath $exportScript -PathType Leaf)) {
    throw "The HID report export script was not found at '$exportScript'."
}

if (-not (Test-Path -LiteralPath $explorerProject -PathType Leaf)) {
    throw "The offline explorer project was not found at '$explorerProject'."
}

$exportArguments = @{
    PcapPath = $PcapPath
    DeviceAddress = $DeviceAddress
    TsharkPath = $TsharkPath
}

if ($PSBoundParameters.ContainsKey("FromUtc")) {
    $exportArguments.FromUtc = $FromUtc
}

if ($PSBoundParameters.ContainsKey("ToUtc")) {
    $exportArguments.ToUtc = $ToUtc
}

$reports = @(& $exportScript @exportArguments)

Write-Output "RS50 DirectInput query capture inspection"
Write-Output "Capture: $((Resolve-Path -LiteralPath $PcapPath).Path)"
Write-Output "USB device address: $DeviceAddress"
Write-Output "Extracted HOST/DEVICE HID reports: $($reports.Count)"
Write-Output ""

if ($reports.Count -eq 0) {
    Write-Output "No HID reports were extracted for this device address."
    Write-Output "Verify the USBPcap interface and device address."
    exit 2
}

$reports |
    & dotnet run `
        --project $explorerProject `
        --configuration Release `
        --no-build `
        --no-restore `
        -- `
        --analyze-reports -

if ($LASTEXITCODE -ne 0) {
    throw "The offline report analyzer failed with exit code $LASTEXITCODE."
}

Write-Output ""
Write-Output "Interpretation boundary:"
Write-Output "  This command only reads the saved capture."
Write-Output "  It does not transmit a display layout or telemetry."
Write-Output "  A captured setter proves OLED output only when matched with the"
Write-Output "  native result and an independent physical observation."
