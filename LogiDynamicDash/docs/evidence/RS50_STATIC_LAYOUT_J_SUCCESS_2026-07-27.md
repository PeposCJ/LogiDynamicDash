# RS50 Static Layout J OLED Success

Date: 2026-07-27 local / 2026-07-28 UTC

## Result

Build C demonstrated the complete physical Dynamic OLED path on a Logitech
RS50:

```text
application
  -> DirectInput Escape command 4 / inner command 22
  -> signed Logitech force-feedback driver
  -> HID++ public feature 0x8130, function 3
  -> firmware-rendered Layout J
  -> visible OLED text
```

This is the project's first confirmed write to the Dynamic OLED. It is a
single static update, not live telemetry.

## Guarded Invocation

The operator explicitly authorized one execution while G HUB was closed, the
RS50 was awake, Dynamic showed the Test fallback, and USBPcap was recording.
The native bridge contained only one fixed setter payload:

```text
game-facing argument 1: LOGIDYNAMICDASH
game-facing argument 2: RS50
game-facing argument 3: OLED LINK
game-facing argument 4: TEST 1
```

The call ran once from `2026-07-28T04:53:02.469Z` to
`2026-07-28T04:53:02.474Z`:

```text
Product: Logitech G HUB RS50 (USB)
VID: 0x046D
PID: 0xC276
Acquired: 1
Cooperative HRESULT: 0x00000000
Data format HRESULT: 0x00000000
Acquire HRESULT: 0x00000000
Escape HRESULT: 0x00000000
Unacquire HRESULT: 0x00000000
Inner command: 22
```

No retry, SetIdle, or cleanup write was sent.

## Visual Evidence

The operator reported `FUNCIONO!` and supplied `IMG_2822.jpeg`. The photograph
shows these four centered rows from top to bottom:

```text
RS50
LOGIDYNAMI
TEST 1
OLED LINK
```

The second row is exactly ten characters. This is not random corruption: it
matches the captured function-`3` payload and the previously recovered
`19/10/19/10` wire-field capacities.

The physical result establishes that the Logitech DirectInput adapter maps
the four game-facing Layout J strings into visual/wire fields as follows:

| Visual row / wire field | Capacity | Game-facing argument |
| ---: | ---: | ---: |
| 1 | 19 | 2 |
| 2 | 10 | 1 |
| 3 | 19 | 4 |
| 4 | 10 | 3 |

Therefore a future DirectInput API that accepts desired visual rows
`R1/R2/R3/R4` must construct its x64 MSVC input strings in the order
`R2/R1/R4/R3`. The existing transport-free function-`3` encoder remains
wire-oriented and already uses visual order.

## USBPcap Evidence

Baseline:
`2026-07-27_rs50_static_layout_j_baseline.pcapng`

Setter capture:
`2026-07-27_rs50_static_layout_j_attempt_1.pcapng`

The baseline contained zero HID++ reports. The setter capture contained:

- 52 parsed HID++ reports: 26 HOST and 26 DEVICE;
- 26 exact response-header matches;
- zero unmatched HOST requests;
- Root discovery of public feature `0x8130` at runtime index `0x12`;
- one function-`0` layout-count request and response;
- ten function-`1` descriptor requests and responses;
- exactly one function-`3` setter request and response.

The analyzer decoded the sole HOST function-`3` request as:

```text
set layout J: texts "RS50", "LOGIDYNAMI", "TEST 1", "OLED LINK"
```

The DEVICE returned a matched function-`3` response with no HID++ error. The
visible OLED text and captured bytes independently agree.

## Boundary and Next Gate

This result confirms static Layout J output through the installed Logitech
driver. It does not yet establish a safe sustained update rate, device-loss
behavior, expiry timing, or live telemetry loop.

The operator explicitly confirmed that no torque, LED, or wheel-movement
change occurred; the OLED was the only observed physical change. This closes
the Build C safety gate for this RS50 and installed driver build.

Build D may now implement a caller-controlled native Layout J API that
validates visual-row limits, performs the proven argument permutation,
coalesces identical frames, and initially caps output at 5 Hz. Physical live
telemetry still requires a separately approved captured test.
