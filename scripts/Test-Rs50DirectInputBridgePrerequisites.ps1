[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

Write-Output "RS50 DirectInput bridge prerequisite check"
Write-Output "This script does not enumerate, open, or write to HID hardware."
Write-Output ""

$is64Bit = [Environment]::Is64BitOperatingSystem -and
    [Environment]::Is64BitProcess
Write-Output ("[" + $(if ($is64Bit) { "OK" } else { "MISSING" }) +
    "] 64-bit Windows process")

$vswherePath =
    "C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe"
$visualStudioPath = $null
$vcToolsPath = $null

if (Test-Path -LiteralPath $vswherePath -PathType Leaf) {
    $visualStudioPath = & $vswherePath `
        -latest `
        -products "*" `
        -property installationPath
    $vcToolsPath = & $vswherePath `
        -latest `
        -products "*" `
        -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 `
        -property installationPath
}

if ($visualStudioPath) {
    Write-Output "[OK] Visual Studio: $visualStudioPath"
} else {
    Write-Output "[MISSING] Visual Studio installation"
}

if ($vcToolsPath) {
    Write-Output "[OK] MSVC x86/x64 build tools"
} else {
    Write-Output "[MISSING] MSVC x86/x64 build tools"
}

$sdkHeaders = @()
$sdkIncludeRoot = "C:\Program Files (x86)\Windows Kits\10\Include"

if (Test-Path -LiteralPath $sdkIncludeRoot -PathType Container) {
    $sdkHeaders = Get-ChildItem `
        -LiteralPath $sdkIncludeRoot `
        -Recurse `
        -Filter dinput.h `
        -File `
        -ErrorAction SilentlyContinue
}

if ($sdkHeaders.Count -gt 0) {
    Write-Output "[OK] Windows SDK dinput.h: $($sdkHeaders[-1].FullName)"
} else {
    Write-Output "[MISSING] Windows SDK dinput.h"
}

$driverClsid = "{62B43F0E-E7DB-4329-8C13-A966D84A289F}"
$driverRegistryPath =
    "Registry::HKEY_CLASSES_ROOT\CLSID\$driverClsid\InProcServer32"
$expectedDriverHash =
    "17AB8FBB23FD549CCCCDB72A502C3BDCD984F80B6C40E48027C25512D0405A7F"
$driverPath = $null

if (Test-Path -LiteralPath $driverRegistryPath) {
    $driverPath = (Get-ItemProperty -LiteralPath $driverRegistryPath)."(default)"
}

$driverExists = $driverPath -and
    (Test-Path -LiteralPath $driverPath -PathType Leaf)
$driverTrusted = $false
$driverHashMatches = $false

if ($driverExists) {
    $driverVersion = (Get-Item -LiteralPath $driverPath).VersionInfo.FileVersion
    $driverHash = (Get-FileHash -LiteralPath $driverPath -Algorithm SHA256).Hash
    $driverHashMatches = $driverHash -eq $expectedDriverHash
    $driverSignature = Get-AuthenticodeSignature -LiteralPath $driverPath
    $driverSigner = $driverSignature.SignerCertificate.Subject
    $driverTrusted = $driverSignature.Status -eq "Valid" -and
        $driverSigner -like "*O=Logitech Inc*"
    Write-Output "[OK] Logitech force-feedback driver: $driverPath"
    Write-Output "     Version: $driverVersion"
    Write-Output "     CLSID: $driverClsid"
    Write-Output (
        "[" + $(if ($driverHashMatches) { "OK" } else { "MISMATCH" }) +
        "] SHA-256: $driverHash"
    )
    Write-Output (
        "[" + $(if ($driverTrusted) { "OK" } else { "MISMATCH" }) +
        "] Authenticode signer: $driverSigner"
    )
} else {
    Write-Output "[MISSING] Logitech force-feedback COM driver $driverClsid"
}

$ready = $is64Bit -and $vcToolsPath -and
    $sdkHeaders.Count -gt 0 -and $driverExists -and $driverTrusted -and
    $driverHashMatches

Write-Output ""

if ($ready) {
    Write-Output "READY: guarded native bridge can be compiled."
    exit 0
}

Write-Output "NOT READY: do not add or compile the native bridge yet."

if (-not $vcToolsPath -or $sdkHeaders.Count -eq 0) {
    Write-Output (
        "Install Visual Studio Desktop development with C++, including " +
        "MSVC x64/x86 tools and a Windows SDK."
    )
    Write-Output "Workload ID: Microsoft.VisualStudio.Workload.NativeDesktop"

    $repositoryConfig = Join-Path `
        (Split-Path -Parent $PSScriptRoot) `
        "LogiDynamicDash.vsconfig"

    if (Test-Path -LiteralPath $repositoryConfig -PathType Leaf) {
        Write-Output "Visual Studio Installer config: $repositoryConfig"
    }
}

exit 1
