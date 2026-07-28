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

if (-not (Test-Path -LiteralPath $TsharkPath -PathType Leaf)) {
    throw "tshark was not found at '$TsharkPath'."
}

$resolvedPcapPath = (Resolve-Path -LiteralPath $PcapPath).Path
$displayFilter = "usb.device_address == $DeviceAddress"
$hasFromUtc = $PSBoundParameters.ContainsKey("FromUtc")
$hasToUtc = $PSBoundParameters.ContainsKey("ToUtc")

if ($hasFromUtc -xor $hasToUtc) {
    throw "FromUtc and ToUtc must be supplied together."
}

if ($hasFromUtc) {
    if ($FromUtc -gt $ToUtc) {
        throw "FromUtc must be earlier than or equal to ToUtc."
    }

    $invariantCulture = [Globalization.CultureInfo]::InvariantCulture
    $fromEpoch = ($FromUtc.ToUnixTimeMilliseconds() / 1000.0).ToString(
        "F3",
        $invariantCulture)
    $toEpoch = ($ToUtc.ToUnixTimeMilliseconds() / 1000.0).ToString(
        "F3",
        $invariantCulture)
    $displayFilter +=
        " && frame.time_epoch >= $fromEpoch && frame.time_epoch <= $toEpoch"
}

$tsharkArguments = @(
    "-r", $resolvedPcapPath,
    "-Y", $displayFilter,
    "-T", "fields",
    "-E", "separator=/t",
    "-e", "frame.number",
    "-e", "frame.time_epoch",
    "-e", "usb.src",
    "-e", "usb.dst",
    "-e", "usb.data_fragment",
    "-e", "usbhid.data"
)

$rows = & $TsharkPath @tsharkArguments

if ($LASTEXITCODE -ne 0) {
    throw "tshark failed with exit code $LASTEXITCODE."
}

foreach ($row in $rows) {
    $fields = $row -split "`t", 6

    if ($fields.Count -lt 6) {
        continue
    }

    $frameNumber = $fields[0]
    $frameTime = $fields[1]
    $source = $fields[2]
    $destination = $fields[3]
    $hostData = $fields[4]
    $deviceData = $fields[5]

    if ($source -eq "host" -and $hostData) {
        "HOST`t$frameNumber`t$frameTime`t$hostData"
    }
    elseif ($destination -eq "host" -and $deviceData) {
        "DEVICE`t$frameNumber`t$frameTime`t$deviceData"
    }
}
