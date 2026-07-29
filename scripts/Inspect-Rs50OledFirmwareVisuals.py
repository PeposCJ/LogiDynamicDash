#!/usr/bin/env python3
"""Inspect RS50 OLED font metadata without opening or writing to a device."""

from __future__ import annotations

import argparse
import hashlib
import json
import struct
import sys
from dataclasses import dataclass
from pathlib import Path


EXPECTED_SHA256 = (
    "62c152d68ba0873b7330401050a2dd96e099cfb6e648759f4329d59e382d3d9d"
)
DFU_WRAPPER_SIZE = 0x20
IMAGE_BASE = 0x08010000
FRAMEBUFFER_WIDTH = 128
FRAMEBUFFER_HEIGHT = 64
FRAMEBUFFER_BITS_PER_PIXEL = 1
FRAMEBUFFER_BYTES = 0x400
PRINTABLE_START = 0x20
PRINTABLE_END = 0x7F


@dataclass(frozen=True)
class FontDefinition:
    descriptor_address: int
    expected_height: int
    layout_use: str


FONTS = (
    FontDefinition(0x08044631, 9, "H/I/J rows 1 and 3"),
    FontDefinition(0x08044636, 16, "D/E text"),
    FontDefinition(0x08043032, 18, "H/I/J rows 2 and 4"),
    FontDefinition(0x08043037, 27, "F field 2; G field 1"),
    FontDefinition(0x0804303C, 37, "F field 1; G field 2"),
)


@dataclass(frozen=True)
class Glyph:
    width: int
    height: int
    bitmap: bytes

    def is_set(self, x: int, y: int) -> bool:
        stride = (self.width + 7) // 8
        return bool(self.bitmap[(y * stride) + (x // 8)] & (1 << (x % 8)))


@dataclass(frozen=True)
class ParsedFont:
    definition: FontDefinition
    glyph_table_address: int
    glyphs: dict[int, Glyph]


def image_offset(address: int, image_length: int, size: int = 1) -> int:
    offset = address - IMAGE_BASE
    if offset < 0 or offset + size > image_length:
        raise ValueError(f"address 0x{address:08X} is outside the image")
    return offset


def read_u32(image: bytes, address: int) -> int:
    return struct.unpack_from(
        "<I",
        image,
        image_offset(address, len(image), 4),
    )[0]


def parse_font(image: bytes, definition: FontDefinition) -> ParsedFont:
    descriptor_offset = image_offset(
        definition.descriptor_address,
        len(image),
        5,
    )
    height = image[descriptor_offset]
    if height != definition.expected_height:
        raise ValueError(
            f"font 0x{definition.descriptor_address:08X} has unexpected "
            f"height {height}"
        )

    table_address = read_u32(image, definition.descriptor_address + 1)
    glyphs: dict[int, Glyph] = {}
    for character in range(PRINTABLE_START, PRINTABLE_END + 1):
        record_address = table_address + ((character - PRINTABLE_START) * 6)
        record_offset = image_offset(record_address, len(image), 6)
        width = image[record_offset]
        byte_count = image[record_offset + 1]
        bitmap_address = struct.unpack_from(
            "<I",
            image,
            record_offset + 2,
        )[0]
        expected_bytes = height * ((width + 7) // 8)
        if width == 0 or byte_count != expected_bytes:
            raise ValueError(
                f"glyph 0x{character:02X} in font "
                f"0x{definition.descriptor_address:08X} has invalid geometry"
            )
        bitmap_offset = image_offset(bitmap_address, len(image), byte_count)
        glyphs[character] = Glyph(
            width,
            height,
            image[bitmap_offset : bitmap_offset + byte_count],
        )

    return ParsedFont(definition, table_address, glyphs)


def sanitize_text(value: str) -> str:
    result: list[str] = []
    for character in value:
        code = ord(character)
        if 0x61 <= code <= 0x7A:
            code -= 0x20
        if code < PRINTABLE_START or code > PRINTABLE_END:
            code = ord("?")
        result.append(chr(code))
    return "".join(result)


def text_width(font: ParsedFont, text: str) -> int:
    return sum(font.glyphs[ord(character)].width for character in text)


def render_text(
    pixels: list[list[bool]],
    font: ParsedFont,
    text: str,
    origin_x: int,
    origin_y: int,
    scale: int,
) -> None:
    cursor_x = origin_x
    for character in sanitize_text(text):
        glyph = font.glyphs[ord(character)]
        for y in range(glyph.height):
            for x in range(glyph.width):
                if not glyph.is_set(x, y):
                    continue
                for scaled_y in range(scale):
                    for scaled_x in range(scale):
                        pixels[origin_y + (y * scale) + scaled_y][
                            cursor_x + (x * scale) + scaled_x
                        ] = True
        cursor_x += glyph.width * scale


def write_bmp(path: Path, fonts: tuple[ParsedFont, ...]) -> None:
    scale = 2
    sample = "A0? RPM 123"
    margin = 16
    row_gap = 14
    widths = [text_width(font, sample) * scale for font in fonts]
    width = max(widths) + (margin * 2)
    height = (
        sum(font.definition.expected_height * scale for font in fonts)
        + (row_gap * (len(fonts) - 1))
        + (margin * 2)
    )
    pixels = [[False for _ in range(width)] for _ in range(height)]

    y = margin
    for font in fonts:
        render_text(pixels, font, sample, margin, y, scale)
        y += (font.definition.expected_height * scale) + row_gap

    row_stride = ((width * 3) + 3) & ~3
    pixel_bytes = bytearray()
    for row in reversed(pixels):
        for enabled in row:
            value = 0xFF if enabled else 0x00
            pixel_bytes.extend((value, value, value))
        pixel_bytes.extend(b"\x00" * (row_stride - (width * 3)))

    file_header_size = 14
    dib_header_size = 40
    pixel_offset = file_header_size + dib_header_size
    file_size = pixel_offset + len(pixel_bytes)
    header = struct.pack(
        "<2sIHHI",
        b"BM",
        file_size,
        0,
        0,
        pixel_offset,
    )
    dib = struct.pack(
        "<IiiHHIIiiII",
        dib_header_size,
        width,
        height,
        1,
        24,
        0,
        len(pixel_bytes),
        2835,
        2835,
        0,
        0,
    )
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(header + dib + pixel_bytes)


def main() -> int:
    parser = argparse.ArgumentParser(
        description=(
            "Validate and inspect the five embedded RS50 OLED bitmap fonts. "
            "This tool performs file I/O only."
        )
    )
    parser.add_argument(
        "--firmware",
        required=True,
        type=Path,
        help="Path to rs50_main_v165_4_39.dfu",
    )
    parser.add_argument(
        "--bmp-output",
        type=Path,
        help="Optional path for a local representative font-sample BMP",
    )
    arguments = parser.parse_args()

    firmware = arguments.firmware.read_bytes()
    digest = hashlib.sha256(firmware).hexdigest()
    if digest != EXPECTED_SHA256:
        raise ValueError(
            f"unexpected firmware SHA-256 {digest}; expected {EXPECTED_SHA256}"
        )
    if len(firmware) <= DFU_WRAPPER_SIZE:
        raise ValueError("firmware image is missing")
    image = firmware[DFU_WRAPPER_SIZE:]

    fonts = tuple(parse_font(image, definition) for definition in FONTS)
    if arguments.bmp_output is not None:
        write_bmp(arguments.bmp_output, fonts)

    report = {
        "firmware_sha256": digest.upper(),
        "framebuffer": {
            "width": FRAMEBUFFER_WIDTH,
            "height": FRAMEBUFFER_HEIGHT,
            "bits_per_pixel": FRAMEBUFFER_BITS_PER_PIXEL,
            "bytes": FRAMEBUFFER_BYTES,
        },
        "host_text_normalization": {
            "lowercase": "converted to uppercase",
            "unsupported": "replaced with ?",
            "wire_character_range": "0x20..0x7F",
            "unicode": False,
        },
        "fonts": [
            {
                "descriptor": f"0x{font.definition.descriptor_address:08X}",
                "height_pixels": font.definition.expected_height,
                "glyph_table": f"0x{font.glyph_table_address:08X}",
                "glyph_count": len(font.glyphs),
                "minimum_width_pixels": min(
                    glyph.width for glyph in font.glyphs.values()
                ),
                "maximum_width_pixels": max(
                    glyph.width for glyph in font.glyphs.values()
                ),
                "layout_use": font.definition.layout_use,
            }
            for font in fonts
        ],
        "sample_bmp": (
            str(arguments.bmp_output.resolve())
            if arguments.bmp_output is not None
            else None
        ),
    }
    print(json.dumps(report, indent=2))
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (OSError, ValueError) as exception:
        print(f"error: {exception}", file=sys.stderr)
        raise SystemExit(1) from None
