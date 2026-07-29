[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateScript({ Test-Path -LiteralPath $_ -PathType Leaf })]
    [string] $BaselinePcapPath,

    [Parameter(Mandatory)]
    [ValidateScript({ Test-Path -LiteralPath $_ -PathType Leaf })]
    [string] $QueryPcapPath,

    [Parameter(Mandatory)]
    [ValidateRange(1, 127)]
    [int] $DeviceAddress,

    [DateTimeOffset] $QueryFromUtc,

    [DateTimeOffset] $QueryToUtc,

    [string] $TsharkPath = "C:\Program Files\Wireshark\tshark.exe"
)

$ErrorActionPreference = "Stop"

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$exportScript = Join-Path $PSScriptRoot "Export-Rs50HidReports.ps1"
$explorerProject = Join-Path `
    $repositoryRoot `
    "LogiDynamicExplorer\LogiDynamicExplorer.csproj"
$temporaryDirectory = Join-Path `
    $repositoryRoot `
    "artifacts\capture-analysis"
$temporaryId = [Guid]::NewGuid().ToString("N")
$baselineReportsPath = Join-Path `
    $temporaryDirectory `
    "$temporaryId-baseline.tsv"
$queryReportsPath = Join-Path `
    $temporaryDirectory `
    "$temporaryId-query.tsv"

New-Item -ItemType Directory -Path $temporaryDirectory -Force | Out-Null

try {
    $baselineReports = @(
        & $exportScript `
            -PcapPath $BaselinePcapPath `
            -DeviceAddress $DeviceAddress `
            -TsharkPath $TsharkPath
    )
    [IO.File]::WriteAllLines(
        $baselineReportsPath,
        [string[]] $baselineReports)

    $queryExportArguments = @{
        PcapPath = $QueryPcapPath
        DeviceAddress = $DeviceAddress
        TsharkPath = $TsharkPath
    }

    if ($PSBoundParameters.ContainsKey("QueryFromUtc")) {
        $queryExportArguments.FromUtc = $QueryFromUtc
    }

    if ($PSBoundParameters.ContainsKey("QueryToUtc")) {
        $queryExportArguments.ToUtc = $QueryToUtc
    }

    $queryReports = @(& $exportScript @queryExportArguments)
    [IO.File]::WriteAllLines(
        $queryReportsPath,
        [string[]] $queryReports)

    Write-Output "RS50 DirectInput query capture comparison"
    Write-Output "USB device address: $DeviceAddress"
    Write-Output ""

    & dotnet run `
        --project $explorerProject `
        --configuration Release `
        --no-build `
        --no-restore `
        -- `
        --compare-reports `
        $baselineReportsPath `
        $queryReportsPath

    if ($LASTEXITCODE -ne 0) {
        throw "The offline report comparison failed with exit code $LASTEXITCODE."
    }
}
finally {
    if (Test-Path -LiteralPath $baselineReportsPath) {
        Remove-Item -LiteralPath $baselineReportsPath -Force
    }

    if (Test-Path -LiteralPath $queryReportsPath) {
        Remove-Item -LiteralPath $queryReportsPath -Force
    }
}
