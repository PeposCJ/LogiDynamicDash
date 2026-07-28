# RS50 Shared HID++ Visual Layout Gallery Build K

## Purpose and Status

Build K is an offline-validated, not-yet-executed physical gallery for all ten
firmware-rendered Display Game Data layouts. It exists to answer:

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

Do not execute until a fresh operator authorization is supplied.

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
| A | pending | pending | pending |
| B | pending | pending | pending |
| C | pending | pending | pending |
| D | pending | pending | pending |
| E | pending | pending | pending |
| F | pending | pending | pending |
| G | pending | pending | pending |
| H | pending | pending | pending |
| I | pending | pending | pending |
| J | pending | pending | pending |

## Acceptance

Accept K1 only if:

- exactly one discovery and ten function-3 setters receive exact ACKs;
- visible layouts progress A-J in order for three seconds each;
- the video is clear enough to compare graphics and typography;
- USBPcap contains no `0x8123`, `0x807A`, `0x807B`, MI_02, or subdevice host
  operation attributable to Build K;
- the wheel remains physically normal throughout and after the trial.

Any unexpected response or physical effect stops further testing.
