# RS50 Shared HID++ Visual Layout Gallery Build K

## Purpose and Status

Build K is an offline-validated and physically accepted gallery for all ten
firmware-rendered Display Game Data layouts. K1 ran exactly once on
2026-07-28 and answered:

- which layouts contain bars, gauges, fixed graphics, or decoration;
- which text rows use visibly different font sizes or styles;
- how the two normalized values in layouts C-E affect the graphics;
- whether A and B expose useful fixed telemetry presentations.

It does not test an arbitrary framebuffer or unknown rim-module feature.

## Fixed Sequence

After one discovery, Build K shows each layout for exactly three seconds:

| Layout | Fixed fields |
|---|---|
| A | no data |
| B | no data |
| C | value `128/255` |
| D | values `64/255`, `191/255`; text `LAYOUT D` |
| E | values `64/255`, `191/255`; texts `E1`, `LAYOUTE` |
| F | texts `F`, `123` |
| G | texts `G`, `456` |
| H | texts `LAYOUT H WIDE TEST`, `H SECOND` |
| I | four rows identifying Layout I |
| J | four rows identifying Layout J |

All ten calls and all ten waits are spelled out in source. There is no loop,
retry, telemetry, caller-selected layout, caller text, caller value, raw HID,
DirectInput, FFB, LED operation, or subdevice request.

## Arming Contract

All arguments must appear exactly in this order:

```text
--arm-rs50-shared-hidpp-layout-gallery
--confirm-ghub-closed
--confirm-iracing-closed
--confirm-rs50-awake
--confirm-dynamic-selected
--confirm-usbpcap-running
--confirm-video-recording
--confirm-ten-layouts-three-seconds-each
```

Run the offline source audit with:

```powershell
.\scripts\Test-Rs50SharedHidppLayoutGallerySurface.ps1
```

## Proposed K1 Physical Procedure

K1 completed under a fresh operator authorization. This procedure is retained
as the exact historical protocol; do not repeat it without a new reason,
design review, and authorization.

Files:

```text
2026-07-28_rs50_build_k_layout_gallery.pcapng
2026-07-28_rs50_build_k_layout_gallery.mp4
```

Preparation:

1. close G HUB and iRacing;
2. connect and wake the RS50;
3. select Dynamic and confirm it shows the firmware Test fallback;
4. start a stable video recording focused tightly on the OLED;
5. start USBPcapCMD filtered to the RS50 address with a 128 MB buffer;
6. record at least ten seconds of baseline;
7. place the terminal where its `showing Layout X` lines are visible or read
   each label aloud for the video.

Run Build K exactly once. Do not touch the wheel, OLED settings, USB cable, or
other controls during the 30-second sequence.

After completion:

1. continue video and USB capture for at least ten seconds;
2. stop and save both files;
3. confirm there was no wheel movement, torque impulse, LED change, input loss,
   disconnect, or unexpected resistance;
4. return to Test and Dynamic once to confirm normal firmware navigation;
5. report each layout using the observation table below.

## Observation Table

| Layout | Visible result | Bars/graphics | Text/font observations |
|---|---|---|---|
| A | blank OLED | none | none |
| B | firmware Test screen | four striped vertical bars `C/B/G/H` | fixed small labels |
| C | one horizontal gauge | 50% solid, remainder diagonally striped | none |
| D | text plus two indicators | 25% main gauge; 75% thin lower indicator | one 16 px text region |
| E | split text plus two indicators | same 25%/75% indicators | left/right 16 px regions |
| F | split numeric composition | fixed vertical separator | 27 px left; 37 px right |
| G | reversed numeric composition | fixed vertical separator | 37 px left; 27 px right |
| H | two-row composition | reserved left field blank in K1 | 9 px centered; 18 px right-aligned |
| I | four-row composition | two reserved left fields blank in K1 | alternating 9/18 px; mixed center/right alignment |
| J | four-row composition | none | alternating 9/18 px; all rows centered |

## Acceptance

Accept K1 only if:

- exactly one discovery and ten function-3 setters receive exact ACKs;
- visible layouts progress A-J in order for three seconds each;
- the video is clear enough to compare graphics and typography;
- USBPcap contains no `0x8123`, `0x807A`, `0x807B`, MI_02, or subdevice host
  operation attributable to Build K;
- the wheel remains physically normal throughout and after the trial.

Any unexpected response or physical effect stops further testing.

K1 met every acceptance condition. Complete evidence is in
[`evidence/RS50_BUILD_K_LAYOUT_GALLERY_SUCCESS_2026-07-28.md`](evidence/RS50_BUILD_K_LAYOUT_GALLERY_SUCCESS_2026-07-28.md).
