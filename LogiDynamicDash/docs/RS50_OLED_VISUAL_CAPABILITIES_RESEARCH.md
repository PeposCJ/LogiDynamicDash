# RS50/PRO OLED Visual Capabilities Research

## Current Conclusion - Awaiting Physical A-J Gallery

The confirmed Dynamic protocol is a typed firmware renderer, not a host
framebuffer.

Public feature `0x8130` exposes exactly four functions:

1. return the layout count;
2. return one fixed layout descriptor;
3. clear pending Dynamic data;
4. select one layout and supply its bounded fields.

No `0x8130` request contains a font identifier, font size, pixel coordinates,
bitmap, image format, drawing opcode, color, or arbitrary-length data stream.
The largest payload is 59 bytes of layout index plus fixed text fields.

Therefore:

- bars and fixed graphics are available through selected firmware layouts;
- font size/style and placement can change indirectly with the layout;
- the host can change the values/text inside a layout;
- the host cannot choose an arbitrary font, size, position, or graphic through
  the confirmed protocol.

This is a strong protocol conclusion, but not yet proof that no separate,
undocumented framebuffer feature exists anywhere in the wheel/rim firmware.

## Recovered Visual Primitives

Static analysis of RS50 firmware
`U165.04_B0039-gf6375b51` identifies ten renderers:

| Layout | Host-controlled fields | Static visual evidence |
|---|---|---|
| A | none | fixed/data-free renderer |
| B | none | complex fixed renderer using internal wheel state |
| C | one normalized byte | one firmware-drawn gauge/bar |
| D | two normalized bytes, one text | two gauges/bars plus text |
| E | two normalized bytes, two texts | two gauges/bars plus text |
| F | 1-char and 3-char text | fixed large/specialized composition |
| G | 1-char and 3-char text | different fixed composition |
| H | 21-char and 10-char text | two text regions with distinct geometry |
| I | four texts | centered/right-aligned text plus fixed decoration/glyphs |
| J | four texts | four centered text rows, least prescriptive layout |

The numeric conversion is:

```text
wire byte = round(clamp(game value, 0.0, 1.0) * 255)
drawn extent = wire byte * 118 / 255 pixels
```

The recovered placement confirms the intended visual roles:

- C draws one 118 x 10 horizontal gauge at `(5, 27)`;
- D/E draw the same 118 x 10 gauge at `(5, 38)` plus a second 118 x 2
  indicator at `(5, 50)`;
- F/G place a one-character and three-character value on opposite sides of a
  fixed 1 x 28 separator, swapping which side receives the 37 px font;
- H combines two text regions with a fixed 23 x 18 left-side field;
- I repeats that decorated left-field/text composition twice;
- J contains only four centered text rows, at vertical positions 2, 12, 35,
  and 45.

Firmware text rendering measures strings using fixed font descriptors and then
places them at hard-coded coordinates. Different renderers reference different
font descriptors, confirming multiple firmware font sizes/styles. There is no
field that lets the host select those descriptors.

The descriptors expose five exact raster heights:

| Firmware descriptor | Raster height | Glyph widths | Confirmed layout use |
|---|---:|---:|---|
| `0x08044631` | 9 px | 3-9 px | H/I/J first and third text regions |
| `0x08044636` | 16 px | 5-17 px | D/E text regions |
| `0x08043032` | 18 px | 4-18 px | H/I/J second and fourth text regions |
| `0x08043037` | 27 px | 6-27 px | F second text; G first text |
| `0x0804303C` | 37 px | 8-37 px | F first text; G second text |

This establishes that font size really can change, but only by choosing a
layout whose renderer references the desired built-in descriptor. The wire
request still has no font-selector field. Layouts F and G deliberately swap
the 27 px and 37 px fonts between their one-character and three-character
fields.

Each font is a proportional one-bit bitmap font. A glyph record contains its
pixel width, bitmap byte count, and a pointer to row-major raster data; it
contains no family/style name. Inspection of representative glyphs shows the
same squared, sans-serif racing-display design at five raster sizes rather
than five host-selectable TrueType/OpenType faces. Lowercase input is converted
to uppercase by the protocol handler, non-printable/non-ASCII input becomes
`?`, and the usable host repertoire is therefore the firmware's printable
single-byte uppercase-oriented glyph set rather than Unicode.

The clear phase passes exactly `0x400` bytes to the display buffer routine.
Together with the renderers' 0-127 horizontal and 0-63 vertical bounds, this
identifies an internal 128 x 64 one-bit framebuffer (`128 * 64 / 8 = 1024`
bytes). That framebuffer exists inside firmware, but no recovered host command
accepts or exposes its bytes.

## Reproducible Offline Font Inspection

[`scripts/Inspect-Rs50OledFirmwareVisuals.py`](../../scripts/Inspect-Rs50OledFirmwareVisuals.py)
verifies the official DFU SHA-256 before reading anything, validates all 96
glyph records in each of the five fonts, reports their dimensions as JSON,
and can render a local representative BMP. It imports no HID/device library
and never opens the RS50.

```powershell
python .\scripts\Inspect-Rs50OledFirmwareVisuals.py `
  --firmware "C:\ProgramData\LGHUB\depots\794591\rs50_racing_wheel_dfu\rs50_main_v165_4_39.dfu" `
  --bmp-output ".tmp\rs50_oled_font_samples.bmp"
```

The generated BMP is intentionally local and uncommitted because its pixels
are extracted from Logitech firmware.

## Negative Evidence for a Framebuffer

- Complete G HUB startup captures contain no sustained large host payloads.
- Normal startup enumerates Display Game Data but does not transmit a Dynamic
  frame.
- The installed public Wheel SDK and TRUEFORCE SDK expose no OLED, image,
  font, or layout API.
- G HUB's `DisplayGameData` implementation serializes only layouts A-J into
  feature `0x8130` function 3.
- The base firmware registry gives `0x8130` exactly four handlers; none is a
  bitmap upload or arbitrary draw command.
- The protocol descriptors report only fixed byte/text capacities.
- The installed G HUB depot contains one RS50 main firmware and one drive
  firmware. Its DFU manifests expose no separate rim/display firmware image
  or OLED update endpoint.

## Unknown Rim-Module Features

The attached display/rim subdevice (`dev_idx 0x01`) advertises public features
`0x18A2`, `0x8091`, and `0x8093`, plus hidden/engineering feature `0x9315`.
`0x8091` is associated with the button/LED matrix. No semantics have been
established for `0x18A2`, `0x8093`, or `0x9315`.

Important boundaries:

- G HUB enumerated `0x18A2` and `0x8093` but made no operational request to
  either during the captured startup.
- The installed G HUB agent exposes no named implementation class for those
  IDs, unlike `Feature8130DisplayGameData`.
- No captured traffic resembles a framebuffer upload.
- `0x9315` advertises hidden, engineering, and
  manufacturing-deactivatable flags.

Unknown functions will not be probed by guessing. A safe next lead requires
one of:

- a firmware image for the rim/display module;
- a legitimate G HUB/game capture that actually uses the feature;
- a named implementation or protocol description;
- read-only metadata that establishes function semantics.

## Physical Evidence Needed

Build K will display the confirmed A-J catalog with fixed values and text for
three seconds each. Video plus USBPcap will establish:

- actual bar/gauge geometry;
- built-in icons and decoration;
- relative font sizes/styles;
- useful telemetry roles for each fixed layout;
- whether A/B expose live internal state.

After K1, a smaller glyph test may be justified to map printable characters.
It will not test arbitrary binary bytes or unknown subdevice functions.

## Decision Rule

The project will claim arbitrary graphics or font control only if a bounded,
documented command is found and independently observed in legitimate traffic
or firmware. Until then, the production design should treat A-J as the entire
supported visual vocabulary and map telemetry to the best firmware layout.

References:

- Logitech RS50 setup guide:
  <https://www.logitech.com/content/dam/support/simulation/rs50/rs50-ps-and-pc-setup-guide-amr.pdf>
- mescon protocol specification:
  <https://github.com/mescon/logitech-trueforce-linux-driver/blob/master/docs/PROTOCOL_SPECIFICATION.md>
- local evidence:
  `evidence/RS50_FEATURE_8130_DISPLAY_GAME_DATA_2026-07-22.md`
