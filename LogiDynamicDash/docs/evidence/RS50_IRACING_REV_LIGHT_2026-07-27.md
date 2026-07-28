# RS50 iRacing Rev-Light Capture Evidence — 2026-07-27

## Scope

This note preserves the sanitized findings from four controlled USB captures of a Logitech RS50 while iRacing was running.

The rev-light investigation was not the project's final objective. It was used as a visible, repeatable protocol target to learn how the RS50 discovers HID++ features, initializes them, streams live state, and acknowledges host writes. Those lessons are directly relevant to the ongoing Dynamic OLED investigation.

Raw `.pcapng` files remain local and are intentionally not committed.

## Test Conditions

- Device: Logitech RS50
- USB VID/PID: `046D:C276`
- Game: iRacing
- Capture tools: Wireshark + USBPcap
- Capture scope: RS50 USB traffic
- G HUB application window: closed
- Logitech background components: left running normally
- Rev-light strip: visibly active during the live RPM tests

Closing the G HUB window does not imply that Logitech background services were stopped. The captures therefore establish wire behavior, but they should not be used to claim that the traffic originated from the visible G HUB application itself.

## Capture Set

- `2026-07-27_rs50_iracing_led_initialization.pcapng`
- `2026-07-27_rs50_iracing_ack_responses.pcapng`
- `2026-07-27_rs50_iracing_shift_overrev.pcapng`
- `2026-07-27_rs50_iracing_pit_limiter.pcapng`

## Feature Discovery and Startup

The startup capture began before iRacing was launched and preserved the one-time setup traffic that steady-state captures had missed.

Feature `0x807A` was discovered and assigned runtime feature index `0x0B` in the captured session.

The observed startup sequence was:

```text
10ff0b0c000000   fn0
10ff0b1c000000   fn1
10ff0b2c000000   fn2
10ff0b0c000000   fn0
11ff0b6c...      then the fn2 + fn6 live stream
This sequence provided first-party evidence that an extra fn3 command previously carried in a third-party-derived implementation was not part of the normal RS50 arm sequence.
The maintainer of mescon/logitech-trueforce-linux-driver identified that extra fn3 as SET_EFFECT. In that implementation it could switch the wheel to effect 2 and overwrite the user's active LIGHTSYNC effect, which had required a separate snapshot/restore workaround.
The startup capture allowed both the incorrect command and the workaround to be removed.
Live Rev-Light Stream
The live rev display uses feature 0x807A with the short 0x10 plus long 0x11 write pair.
The level field is 0..10:
0 = strip dark
10 = all ten LEDs lit
The steady-state cadence is approximately 60 Hz, or one update pair roughly every 16 ms while the feed is active.
The capture comparison also exposed a bug in the external Linux telemetry feeder: it had been capped near 6 Hz, making RPM sweeps visibly lag. The maintainer corrected the feeder to approximately 60 Hz after comparing it with this capture.
0x12 Response Pattern
The dedicated ACK capture showed a 0x12 response corresponding to every tested host write in the rev-light stream.
The structural pattern is:
host 0x10 write
→ device 0x12 response
→ host 0x11 write
→ device 0x12 response
→ next update cycle
This is important for future Dynamic OLED work: candidate display traffic should be analyzed bidirectionally, and host writes should be correlated with their device responses rather than studied in isolation.
The capture establishes a consistent acknowledgement pattern. It does not, by itself, prove the exact host-side blocking or flow-control policy.
Redline Behavior
The dedicated over-rev capture confirmed that redline does not require a separate flash command or a value above the normal range.
At redline the live feed simply reaches:
LL = 10
No LL value above 0x0A and no distinct redline-specific function were observed.
This matches the physical behavior: the strip fills completely at redline rather than switching to a separate protocol effect.
iRacing Pit-Limiter Behavior
The pit-limiter capture isolated a second visible behavior using the same live level mechanism.
When the full strip visibly flashed, the feed alternated between:
LL = 10
LL = 0
LL = 10
LL = 0
...
The complete ON/OFF cycle is approximately 1.2 Hz.
No separate flash effect or alternate feature was required. The pit-limiter indication can therefore be reproduced by a telemetry feeder using the already understood level command and timing the 10 ↔ 0 alternation itself.
External Review
These captures were reviewed with the maintainer of:
mescon/logitech-trueforce-linux-driver
Discussion:
https://github.com/mescon/logitech-trueforce-linux-driver/issues/20
The maintainer reported that the startup capture fixed a real arm-sequence bug, the ACK capture confirmed a 0x12 response for every write, the redline capture confirmed plain LL = 10, and the pit-limiter capture confirmed full-strip 10/0 alternation at about 1.2 Hz.
No further rev-light captures were requested after these findings.
Relevance to Dynamic OLED Research
The important result for LogiDynamicDash is methodological.
A known visible RS50 behavior showed that a useful protocol investigation can be decomposed into:
feature discovery
→ runtime feature index
→ one-time initialization
→ live command stream
→ device responses
→ controlled physical-state comparison
The Dynamic OLED investigation should use the same structure.
In particular:
Capture before any candidate display producer becomes active.
Distinguish FeatureSet enumeration from actual feature invocation.
Preserve host-to-device and device-to-host traffic together.
Correlate physical OLED state changes with exact runtime feature/function traffic.
Keep unknown writes, replay, fuzzing, firmware operations, and speculative display commands out of the research path until semantics are established.
This evidence does not identify an OLED transport.
It provides a validated workflow and a reference example of how a proprietary RS50 feature behaves when it is successfully discovered and used.