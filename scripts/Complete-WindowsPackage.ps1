[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $PackageRoot,

    [Parameter(Mandatory)]
    [string] $Version
)

$ErrorActionPreference = "Stop"

$repositoryRoot =
    (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$resolvedPackageRoot =
    (Resolve-Path -LiteralPath $PackageRoot).Path

Copy-Item `
    -LiteralPath (Join-Path $repositoryRoot "logidynamicdash.example.json") `
    -Destination $resolvedPackageRoot `
    -Force
Copy-Item `
    -LiteralPath (Join-Path $repositoryRoot "README.md") `
    -Destination $resolvedPackageRoot `
    -Force
Copy-Item `
    -LiteralPath (Join-Path $repositoryRoot "THIRD_PARTY_NOTICES.md") `
    -Destination $resolvedPackageRoot `
    -Force
Copy-Item `
    -LiteralPath (
        Join-Path $repositoryRoot "LogiDynamicDash\docs\WINDOWS_PACKAGE.md") `
    -Destination $resolvedPackageRoot `
    -Force
Copy-Item `
    -LiteralPath (
        Join-Path $repositoryRoot `
            "LogiDynamicDash\docs\RS50_OLED_PRODUCTION_STATIONARY_CHECKLIST.md") `
    -Destination $resolvedPackageRoot `
    -Force
Copy-Item `
    -LiteralPath (
        Join-Path $repositoryRoot `
            "LogiDynamicDash\docs\OFFLINE_FAULT_MATRIX.md") `
    -Destination $resolvedPackageRoot `
    -Force
Copy-Item `
    -LiteralPath (
        Join-Path $repositoryRoot `
            "LogiDynamicDash\docs\PRODUCT_AND_MONETIZATION_PLAN.md") `
    -Destination $resolvedPackageRoot `
    -Force
$replayDestination = Join-Path $resolvedPackageRoot "replays"
New-Item -ItemType Directory -Path $replayDestination -Force | Out-Null
Copy-Item `
    -Path (Join-Path $repositoryRoot "replays\*") `
    -Destination $replayDestination `
    -Recurse `
    -Force

& (Join-Path $PSScriptRoot "New-PackageSbom.ps1") `
    -OutputPath (Join-Path $resolvedPackageRoot "sbom.spdx.json") `
    -Version $Version

$executable = Join-Path $resolvedPackageRoot "LogiDynamicDash.exe"
$configurator =
    Join-Path $resolvedPackageRoot "LogiDynamicDash.Configurator.exe"
$configuration =
    Join-Path $resolvedPackageRoot "logidynamicdash.example.json"
$replay =
    Join-Path $resolvedPackageRoot "replays\mode-transitions.json"

if (-not (Test-Path -LiteralPath $configurator -PathType Leaf)) {
    throw "The Windows package is missing LogiDynamicDash.Configurator.exe."
}

& $executable --preview-all --config $configuration | Out-Null
if ($LASTEXITCODE -ne 0) {
    throw "Packaged preview smoke test failed with exit code $LASTEXITCODE."
}

& $executable --simulate-all --config $configuration | Out-Null
if ($LASTEXITCODE -ne 0) {
    throw "Packaged simulation smoke test failed with exit code $LASTEXITCODE."
}

& $executable `
    --replay `
    --config $configuration `
    --telemetry $replay |
    Out-Null
if ($LASTEXITCODE -ne 0) {
    throw "Packaged replay smoke test failed with exit code $LASTEXITCODE."
}

Get-ChildItem -LiteralPath $resolvedPackageRoot -File -Recurse |
    Sort-Object FullName |
    Get-FileHash -Algorithm SHA256 |
    ForEach-Object {
        $relativePath =
            $_.Path.Substring($resolvedPackageRoot.Length).TrimStart("\")
        "$($_.Hash)  $relativePath"
    } |
    Set-Content `
        -LiteralPath (Join-Path $resolvedPackageRoot "SHA256SUMS.txt") `
        -Encoding ascii
