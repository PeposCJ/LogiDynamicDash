# RS50 Game RPM Live-Feed Capture

## Result

A controlled idle/revving USB capture confirmed that a game plus G HUB can
stream live telemetry-derived state to the RS50. In this session the stream
targeted the RPM LED feature only. It did not call the Dynamic OLED feature.

This is useful negative evidence: a working game integration and visibly
changing RPM LEDs are not sufficient to activate `DisplayGameData`.

## Local Capture Metadata

The raw captures remain local and are excluded from Git.

| Capture | Duration | SHA-256 |
|---|---:|---|
| `2026-07-22_rev_idle.pcapng` | 18.121068 s | `47F189ABBFB92A1B0EF2CA51C654264390C7194912BCE1233DBDDC91B73A5413` |
| `2026-07-22_rev_revving.pcapng` | 35.300351 s | `85838269B8FC070B6FB51D49D0E4CD376202D7CAE28DC59CCE074923194FAEDD` |

Both captures contained the physical RS50 at USB bus 1, address 4, VID
`0x046D`, PID `0xC276`.

## Offline Analysis

The idle capture contained no host HID++ reports to the RS50 during the
selected interval. The revving capture contained 1,452 host reports associated
with base runtime index `0x0B`:

- 726 short function-`2` reports with one fixed signature;
- 726 long function-`6` reports with 11 distinct signatures;
- the changing function-`6` field covered every state from 0 through 10;
- the activity ran from 4.211090 to 30.314614 seconds in the capture.

The previously reconstructed startup FeatureSet catalog maps base runtime
`0x0B` to public feature `0x807A`, named `RPM Indicator` by G HUB.

Direct packet filtering found zero host reports to base runtime `0x12` in both
captures. That runtime maps to feature `0x8130`, `Display Game Data`.

## Interpretation Boundary

The capture proves that the telemetry producer and G HUB were capable of live
wheel updates during the session. It does not identify the game telemetry
source, establish the OLED HomeScreen selection, or prove that this game knows
about the Dynamic display API. Therefore it is a negative `0x8130` result, not
evidence that the OLED feature is unusable.

The next decisive passive test still requires a legitimate producer that is
known to request a display layout, followed by a device-scoped capture looking
for runtime `0x12`, functions `0`, `1`, or `3`.
