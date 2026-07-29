# RS50 Build K OLED Layout Gallery Success

## Session

- Date: 2026-07-28
- Device: Logitech RS50, VID `0x046D`, PID `0xC276`
- Initial OLED state: Dynamic showing the firmware Test fallback
- G HUB: closed
- iRacing: closed
- Execution: one separately authorized Build K run
- Physical transport: shared HID++ MI_01 COL01/COL03
- PCAP: `2026-07-28_rs50_build_k_layout_gallery.pcapng`
- PCAP size: 1,518,484 bytes
- PCAP SHA-256:
  `1BC5F3A79BF986A47E8171803552011DF0212B55A64E129D988552E7E094CF7F`
- Video: `gallery-video.mov`
- Video size: 173,675,930 bytes
- Video SHA-256:
  `F018A5626132FA2656D853EB65BCFF7B9F44B6062082087ADD8463815AA41A90`

The raw captures remain local and must not be committed. The phone video
contains private device/location metadata and must not be uploaded without
first creating a metadata-stripped derivative.

## Operator Observation

The operator confirmed:

- all ten layouts visibly advanced in A-J order;
- bars, graphics, and text sizes changed between layouts;
- there was no wheel movement, torque impulse, unexpected resistance, LED
  change, input loss, or disconnect.

The opening video frames show four striped vertical bars labelled
`C`, `B`, `G`, and `H`. This is the normal firmware Test fallback that was
already visible before Build K; it is not Layout A output. Layout B later
reproduced that same Test composition.

## USB Evidence

The complete PCAP contains 19,966 USB frames. Offline extraction reconstructed
9,980 HID reports:

| Direction/report | Count |
|---|---:|
| HOST `0x10` | 1 |
| HOST `0x12` | 10 |
| DEVICE `0x08` | 9,958 |
| DEVICE `0x12` | 11 |

The only host HID reports were:

1. one Root discovery of public feature `0x8130`;
2. ten canonical feature-`0x8130`, function-`3` setters for Layouts A-J.

All eleven host requests had an exact response-header match. There were no
unmatched host requests and no additional host HID reports.

| Operation | Host frame | ACK frame | ACK latency |
|---|---:|---:|---:|
| Discovery | 19923 | 19925 | 2.351 ms |
| Layout A | 19927 | 19929 | 2.210 ms |
| Layout B | 19931 | 19933 | 2.394 ms |
| Layout C | 19935 | 19937 | 2.799 ms |
| Layout D | 19939 | 19941 | 2.769 ms |
| Layout E | 19943 | 19945 | 2.906 ms |
| Layout F | 19947 | 19949 | 2.388 ms |
| Layout G | 19951 | 19953 | 2.353 ms |
| Layout H | 19955 | 19957 | 2.546 ms |
| Layout I | 19959 | 19961 | 3.069 ms |
| Layout J | 19963 | 19965 | 2.358 ms |

Setter intervals were 3.005647-3.019032 seconds. Because the complete host
set consists only of the eleven transactions above, Build K produced zero
`0x8123`, `0x807A`, `0x807B`, MI_02, subdevice, FFB, LED, or unknown-feature
host operations.

## Video Evidence

The local MOV is 58.65 seconds at 1920 x 1080 and approximately 59.95 fps,
with a 90-degree display rotation. Representative stable frames identify the
layouts unambiguously:

| Layout | Approx. video time | Observed output |
|---|---:|---|
| Baseline Test | 0.5 s | four striped vertical bars `C/B/G/H` |
| A | 11.5 s | completely blank/black OLED; panel power state unknown |
| B | 14.5 s | the same four-bar `C/B/G/H` Test composition |
| C | 17.5 s | one horizontal striped gauge, solid to about 50% |
| D | 20.5 s | `LAYOUT D`, a 25% main gauge, and a 75% thin indicator |
| E | 23.5 s | `LAYOUTE` left, `E1` right, plus the same two indicators |
| F | 26.5 s | medium `F`, separator, very large `123` |
| G | 29.5 s | very large `G`, separator, medium `456` |
| H | 32.5 s | small centered first row and larger second row |
| I | 35.5 s | four rows; rows 1/3 small and rows 2/4 larger/right-aligned |
| J | 38.5 s | four centered rows; rows 1/3 small and rows 2/4 larger |

Layouts C-E physically confirm the statically recovered normalized gauge
mapping. The unfilled portion of the main gauge uses a fixed diagonal-stripe
background; the supplied value replaces it from left to right with a solid
fill. D/E's second value controls the separate thin lower indicator.

Layouts F/G physically confirm that font size is selected by layout: the same
one-character/three-character field shape swaps the 27 px and 37 px firmware
fonts. H/I/J physically confirm the 9 px and 18 px text hierarchy and their
fixed alignment rules.

No arbitrary icon, bitmap, host-selected font, or host-selected coordinate was
observed. Layout B's Test bars are firmware-owned and data-free. Reserved
left-side fields in H/I were blank in this stationary session; no additional
decoration should be claimed without a separate legitimate state correlation.

Layout A accepts no function-3 data after its layout index, and its renderer
produced a completely black frame. It is therefore a blank layout, not a
host-drawable canvas through feature `0x8130`. The video cannot establish
whether the OLED controller or panel power was disabled, so "electrically
off" is not claimed.

## Accepted Conclusion

K1 passed all Build K acceptance criteria:

- one discovery and ten setters, all exactly acknowledged;
- visible A-J progression in order;
- clear visual evidence for gauges and five layout-selected font sizes;
- no unrelated host operation;
- no adverse physical behavior reported.

For the confirmed Dynamic interface, A-J are a fixed firmware-rendered visual
vocabulary, not a raw framebuffer API. LogiDynamicDash can choose a layout and
update its bounded values/text. It cannot use feature `0x8130` to choose an
arbitrary typeface, font size, coordinate, color, bitmap, or drawing command.
