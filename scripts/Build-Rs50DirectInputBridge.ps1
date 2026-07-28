[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

& (Join-Path $PSScriptRoot "Test-Rs50DirectInputBridgePrerequisites.ps1")
if ($LASTEXITCODE -ne 0) {
    throw "Native bridge prerequisites are not ready."
}

$vswherePath =
    "C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe"
$visualStudioPath = & $vswherePath `
    -latest `
    -products "*" `
    -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 `
    -property installationPath

if (-not $visualStudioPath) {
    throw "Visual Studio with x64/x86 MSVC tools was not found."
}

$msbuildPath = Join-Path `
    $visualStudioPath `
    "MSBuild\Current\Bin\MSBuild.exe"
$repositoryRoot = Split-Path -Parent $PSScriptRoot

function Invoke-NativeBuild {
    param(
        [Parameter(Mandatory)]
        [string]$ProjectPath
    )

    # The Codex host currently supplies both Path and PATH. MSBuild's
    # .NET Framework tool task rejects that duplicate. This child cmd
    # keeps the existing Path value but exposes it under one spelling only.
    $buildCommand =
        'set "SAVED=!Path!" & ' +
        'set "PATH=" & ' +
        'set "Path=" & ' +
        'set "Path=!SAVED!" & ' +
        '"' + $msbuildPath + '" ' +
        '"' + $ProjectPath + '" ' +
        '/m /t:Rebuild ' +
        "/p:Configuration=$Configuration " +
        '/p:Platform=x64 /verbosity:minimal'

    Push-Location $repositoryRoot
    try {
        & cmd.exe /v:on /d /c $buildCommand
        if ($LASTEXITCODE -ne 0) {
            throw "MSBuild failed for $ProjectPath."
        }
    }
    finally {
        Pop-Location
    }
}

Invoke-NativeBuild (Join-Path `
    $repositoryRoot `
    "Rs50DirectInputQuery\Rs50DirectInputQuery.vcxproj")
Invoke-NativeBuild (Join-Path `
    $repositoryRoot `
    "Rs50DirectInputBridge.Tests\Rs50DirectInputBridge.Tests.vcxproj")

$outputDirectory = Join-Path `
    $repositoryRoot `
    "artifacts\native\$Configuration"
$testsPath = Join-Path `
    $outputDirectory `
    "Rs50DirectInputBridge.Tests.exe"
$queryPath = Join-Path `
    $outputDirectory `
    "Rs50DirectInputQuery.exe"

& (Join-Path $PSScriptRoot "Test-Rs50NativeBridgeSurface.ps1") `
    -Configuration $Configuration
if ($LASTEXITCODE -ne 0) {
    throw "Native bridge surface audit failed."
}

& $testsPath
if ($LASTEXITCODE -ne 0) {
    throw "Native safety tests failed."
}

& $queryPath
if ($LASTEXITCODE -ne 2) {
    throw "Query tool did not refuse an unarmed invocation."
}

& $queryPath --query-display-support
if ($LASTEXITCODE -ne 2) {
    throw "Query tool accepted only one confirmation argument."
}

& $queryPath --query-display-support --confirm-exclusive-acquire
if ($LASTEXITCODE -ne 2) {
    throw "Query tool accepted acquisition without transmission confirmation."
}

& $queryPath `
    --query-display-support `
    --confirm-exclusive-acquire `
    --confirm-transmit-query
if ($LASTEXITCODE -ne 2) {
    throw "Query tool accepted the retired Build A2 argument sequence."
}

& $queryPath `
    --query-display-support `
    --confirm-standard-data-format `
    --confirm-exclusive-acquire
if ($LASTEXITCODE -ne 2) {
    throw "Query tool accepted Build A3 without transmission confirmation."
}

& $queryPath --query-display-support --confirm-transmit-query
if ($LASTEXITCODE -ne 2) {
    throw "Query tool accepted the obsolete non-acquiring argument sequence."
}

& $queryPath --query-layout-j-support
if ($LASTEXITCODE -ne 2) {
    throw "Query tool accepted an unconfirmed Layout J query."
}

& $queryPath `
    --query-layout-j-support `
    --confirm-standard-data-format `
    --confirm-exclusive-acquire
if ($LASTEXITCODE -ne 2) {
    throw "Query tool accepted Layout J without transmission confirmation."
}

& $queryPath `
    --query-layout-j-support `
    --confirm-standard-data-format `
    --confirm-exclusive-acquire `
    --confirm-transmit-query
if ($LASTEXITCODE -ne 2) {
    throw "Layout J query accepted the general-query confirmation token."
}

& $queryPath --set-static-layout-j
if ($LASTEXITCODE -ne 2) {
    throw "Static Layout J setter accepted an unconfirmed invocation."
}

& $queryPath `
    --set-static-layout-j `
    --confirm-standard-data-format `
    --confirm-exclusive-acquire `
    --confirm-layout-j-static-text
if ($LASTEXITCODE -ne 2) {
    throw "Static Layout J setter accepted missing transmit confirmation."
}

& $queryPath `
    --set-static-layout-j `
    --confirm-standard-data-format `
    --confirm-exclusive-acquire `
    --confirm-transmit-static-setter
if ($LASTEXITCODE -ne 2) {
    throw "Static Layout J setter accepted missing payload confirmation."
}

& $queryPath `
    --set-static-layout-j `
    --confirm-standard-data-format `
    --confirm-exclusive-acquire `
    --confirm-layout-j-static-text `
    --confirm-transmit-layout-query
if ($LASTEXITCODE -ne 2) {
    throw "Static Layout J setter accepted the query confirmation token."
}

& $queryPath --describe
if ($LASTEXITCODE -ne 0) {
    throw "Query tool metadata check failed."
}

Write-Output ""
Write-Output "Native guarded Build C verified without transmitting."
Write-Output "No valid owner window was passed to the bridge."
Write-Output "No DirectInput object or HID device was opened."
