[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$inspectorPath =
    Join-Path $repositoryRoot "scripts\Inspect-Rs50OledFirmwareVisuals.py"

if (-not (Test-Path -LiteralPath $inspectorPath -PathType Leaf)) {
    throw "The RS50 OLED firmware inspector was not found."
}

$source = Get-Content -LiteralPath $inspectorPath -Raw

$forbiddenPatterns = @{
    "device or HID library" =
        "\b(?:hid|hidraw|pyusb|usb\.core|win32|ctypes)\b"
    "process or network access" =
        "\b(?:subprocess|socket|urllib|requests|httpx)\b"
    "shell execution" =
        "\b(?:system|popen|spawn|exec|eval)\s*\("
    "ambient firmware path" =
        "ProgramData|LGHUB"
}

foreach ($entry in $forbiddenPatterns.GetEnumerator()) {
    if ($source -match $entry.Value) {
        throw "The inspector contains forbidden $($entry.Key)."
    }
}

$requiredPatterns = @(
    "EXPECTED_SHA256\s*=",
    "62c152d68ba0873b7330401050a2dd96e099cfb6e648759f4329d59e382d3d9d",
    "DFU_WRAPPER_SIZE\s*=\s*0x20",
    "IMAGE_BASE\s*=\s*0x08010000",
    "FRAMEBUFFER_WIDTH\s*=\s*128",
    "FRAMEBUFFER_HEIGHT\s*=\s*64",
    "FRAMEBUFFER_BITS_PER_PIXEL\s*=\s*1",
    "FRAMEBUFFER_BYTES\s*=\s*0x400",
    "--firmware",
    "required=True",
    "if digest != EXPECTED_SHA256",
    "byte_count != expected_bytes"
)

foreach ($pattern in $requiredPatterns) {
    if ($source -notmatch $pattern) {
        throw "The inspector is missing a required validation boundary."
    }
}

foreach ($descriptor in @(
        "0x08044631, 9",
        "0x08044636, 16",
        "0x08043032, 18",
        "0x08043037, 27",
        "0x0804303C, 37")) {
    if ([regex]::Matches(
            $source,
            [regex]::Escape($descriptor)).Count -ne 1) {
        throw "The inspector font descriptor set changed."
    }
}

if ([regex]::Matches(
        $source,
        "FontDefinition\s*\(").Count -ne 5) {
    throw "The inspector must contain exactly five font definitions."
}

Write-Output "RS50 OLED offline firmware-inspector audit passed."
Write-Output "  Input: explicit DFU path with exact SHA-256 validation"
Write-Output "  Output: JSON metadata and optional local BMP only"
Write-Output "  Fonts: five fixed descriptors; 96 glyph records each"
Write-Output "  HID, device, process, shell, and network access: absent"
