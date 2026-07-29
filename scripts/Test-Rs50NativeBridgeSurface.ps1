[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$bridgeSource = Join-Path `
    $repositoryRoot `
    "Rs50DirectInputBridge\src\rs50_display_bridge.cpp"
$bridgeHeader = Join-Path `
    $repositoryRoot `
    "Rs50DirectInputBridge\include\rs50_display_bridge.h"
$bridgeBinary = Join-Path `
    $repositoryRoot `
    "artifacts\native\$Configuration\Rs50DirectInputBridge.dll"

if (-not (Test-Path -LiteralPath $bridgeBinary -PathType Leaf)) {
    throw "Bridge binary does not exist: $bridgeBinary"
}

$source = Get-Content -Raw -LiteralPath $bridgeSource
$header = Get-Content -Raw -LiteralPath $bridgeHeader

if ($source -notmatch 'DisplayEscapeCommand\s*=\s*4\s*;' -or
    $source -notmatch 'QueryDisplaySupportCommand\s*=\s*2\s*;' -or
    $source -notmatch 'QueryLayoutJSupportCommand\s*=\s*12\s*;' -or
    $source -notmatch 'StaticLayoutJCommand\s*=\s*22\s*;') {
    throw "The bridge does not contain the audited command constants."
}

if ($source -notmatch 'DISCL_EXCLUSIVE\s*\|\s*DISCL_FOREGROUND' -or
    $source -notmatch 'SetDataFormat\s*\(\s*&c_dfDIJoystick2\s*\)' -or
    $source -notmatch '->Acquire\s*\(' -or
    $source -notmatch '->Unacquire\s*\(') {
    throw "The bridge does not contain the audited exclusive lifecycle."
}

$escapeCallCount = [regex]::Matches($source, '->Escape\s*\(').Count
$dataFormatCallCount = [regex]::Matches($source, '->SetDataFormat\s*\(').Count
$acquireCallCount = [regex]::Matches($source, '->Acquire\s*\(').Count
$unacquireCallCount = [regex]::Matches($source, '->Unacquire\s*\(').Count

if ($escapeCallCount -ne 1 -or
    $dataFormatCallCount -ne 1 -or
    $acquireCallCount -ne 1 -or
    $unacquireCallCount -ne 1) {
    throw (
        "Unexpected native call counts: " +
        "Escape=$escapeCallCount, SetDataFormat=$dataFormatCallCount, " +
        "Acquire=$acquireCallCount, " +
        "Unacquire=$unacquireCallCount.")
}

$forbiddenSourcePatterns = @(
    '\bSetIdle\b',
    '\bSetLayout[A-I]\b',
    '\bQueryLayout[A-I]\b',
    '\bHidD_',
    '\bWriteFile\s*\(',
    '\bGetDeviceState\s*\(',
    '\bGetDeviceData\s*\(',
    '\bPoll\s*\('
)

foreach ($pattern in $forbiddenSourcePatterns) {
    if ($source -match $pattern) {
        throw "Forbidden native bridge surface matched: $pattern"
    }
}

$vswherePath =
    "C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe"
$visualStudioPath = & $vswherePath `
    -latest `
    -products "*" `
    -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 `
    -property installationPath
$toolsetRoot = Join-Path $visualStudioPath "VC\Tools\MSVC"
$toolset = Get-ChildItem `
    -LiteralPath $toolsetRoot `
    -Directory |
    Sort-Object Name -Descending |
    Select-Object -First 1
$dumpbinPath = Join-Path `
    $toolset.FullName `
    "bin\Hostx64\x64\dumpbin.exe"

$exports = & $dumpbinPath /exports $bridgeBinary | Out-String
$expectedExports = @(
    "rs50_display_abi_version",
    "rs50_display_begin_layout_j_stream",
    "rs50_display_close",
    "rs50_display_end_layout_j_stream",
    "rs50_display_open",
    "rs50_display_query_layout_j_support",
    "rs50_display_query_support",
    "rs50_display_set_layout_j_frame",
    "rs50_display_set_static_layout_j",
    "rs50_display_status_message"
)

foreach ($name in $expectedExports) {
    if ($exports -notmatch "\b$([regex]::Escape($name))\b") {
        throw "Expected guarded bridge export is missing: $name"
    }
}

$setterExports = @(
    [regex]::Matches($exports, '\brs50_display_set_[A-Za-z0-9_]+\b').
        Value |
    Sort-Object -Unique
)
if ($setterExports.Count -ne 2 -or
    $setterExports[0] -ne "rs50_display_set_layout_j_frame" -or
    $setterExports[1] -ne "rs50_display_set_static_layout_j") {
    throw "The bridge exports an unexpected display setter surface."
}

$imports = & $dumpbinPath /imports $bridgeBinary | Out-String
if ($imports -notmatch '\bDINPUT8\.dll\b') {
    throw "The bridge does not import the expected DirectInput runtime."
}

if ($imports -match '\bHID\.dll\b' -or
    $imports -match '\bSETUPAPI\.dll\b') {
    throw "The guarded bridge unexpectedly imports a raw HID API."
}

foreach ($text in @("LOGIDYNAMICDASH", "RS50", "OLED LINK", "TEST 1")) {
    if ($source -notmatch [regex]::Escape('"' + $text + '"')) {
        throw "The fixed Layout J text is missing: $text"
    }
}

if ($header -notmatch
    'rs50_display_set_static_layout_j\s*\(\s*rs50_display_handle\s*\*\s*\w+\s*,\s*rs50_display_static_layout_j_result\s*\*\s*\w+\s*\)\s*noexcept') {
    throw "The fixed setter unexpectedly accepts caller-controlled text."
}

if ($header -notmatch
    'rs50_display_set_layout_j_frame\s*\(\s*rs50_display_handle\s*\*\s*\w+\s*,\s*const\s+rs50_display_layout_j_frame\s*\*\s*\w+\s*,\s*rs50_display_stream_result\s*\*\s*\w+\s*\)\s*noexcept') {
    throw "The dynamic setter does not expose the audited fixed-frame ABI."
}

Write-Output "Native guarded bridge surface audit passed."
Write-Output "Exports: $($expectedExports -join ', ')"
Write-Output (
    "Only the fixed and validated Layout J setters are exported; " +
    "no raw HID surface was found.")
