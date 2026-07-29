# RS50 Hardware Validation Plan

This plan defines a safe, repeatable session for examining the Logitech RS50
when physical hardware is available.

The session is read-only. It must not send output reports, set feature reports,
update firmware, replay captured traffic, or run USB fuzzing.

## Session Goals

1. Confirm the actual HID descriptor inventory for VID `0x046D` and PID
   `0xC276`.
2. Determine whether G HUB changes the available HID collections or report
   descriptors.
3. Gather evidence for or against the firmware-rendered settings UI
   hypothesis.
4. Separate configuration traffic from any future Dynamic OLED evidence.

## Information to Record

Record only information that is useful to the protocol research:

- RS50 firmware version
- G HUB version
- Collection name
- Usage Page and Usage
- Input, output, and feature report IDs and lengths
- Whether G HUB was open or closed
- OLED mode shown on the wheel
- The single physical action performed during an observation
- Timestamped decoded and raw report bytes when passive monitoring is used

Do not record or publish:

- Device paths
- Serial numbers
- Usernames or machine names
- Complete USB captures
- Unrelated USB device traffic

Raw diagnostic logs must remain local and must not be committed.

## Stop Conditions

Stop the session immediately if any step would require:

- Writing an output report
- Setting a feature report
- Updating or modifying firmware
- Replaying G HUB traffic
- Opening a generic HID write console
- Sending an undocumented command
- Continuing after unexpected force feedback or device movement

Document the stop condition instead of attempting to work around it.

## Phase 1: Baseline Inventory

### Test 1A: G HUB Open

1. Power on the RS50 and select PC mode.
2. Start G HUB and wait until it recognizes the wheel.
3. Run `LogiDynamicExplorer --inventory`.
4. Record the sanitized descriptor inventory.
5. Confirm that the explorer reports that no device streams were opened.

### Test 1B: G HUB Closed

1. Close G HUB normally.
2. Do not stop Logitech services or drivers.
3. Run `LogiDynamicExplorer --inventory` again.
4. Compare collections, usages, report IDs, and lengths with Test 1A.

Expected value: this comparison can show whether G HUB changes device exposure,
but it cannot identify the OLED protocol by itself.

## Phase 2: Firmware-Rendered Settings UI

### Test 2A: On-Wheel Settings Without G HUB

1. Keep G HUB closed.
2. Open the settings menu using only the wheel controls.
3. Navigate between existing settings without changing their values.
4. Record whether the OLED menus continue to render normally.

Evidence interpretation:

- If the complete settings UI remains available, this supports local firmware
  rendering.
- If the UI becomes unavailable, a host-side dependency remains possible.

### Test 2B: One Physical Setting Change

Baseline result (2026-07-17): with G HUB closed and no physical controls
changed, a 10-second passive read of MI_01 COL03 received zero input reports.
The generated local log contained the collection name, VID/PID, timestamps, and
report counts only; it did not contain a device path or serial number.

1. Start passive monitoring on MI_01 COL03.
2. Change exactly one known setting using the wheel controls.
3. Stop monitoring after the changed report is observed.
4. Preserve both the decoded text and raw hexadecimal report locally.
5. Restore the setting manually if needed; do not restore it by replaying USB
   traffic.

Suggested first setting: RPM Brightness, because parameter `0x0A` has a direct
integer encoding and is already decoded.

## Phase 3: G HUB Configuration Behavior

### Test 3A: One G HUB Setting Change

1. Start G HUB.
2. Start passive monitoring on MI_01 COL03.
3. Change only RPM Brightness once in G HUB.
4. Stop monitoring after the corresponding input report is observed or after
   a short idle period.
5. Compare the result with Test 2B.

This test can identify configuration reports echoed by the device. The passive
explorer cannot observe host-to-device traffic sent by another process.

## Phase 4: Dynamic Mode Baseline

### Test 4A: Dynamic Mode With G HUB Open

1. Select Dynamic mode using the wheel controls.
2. Keep G HUB open without changing settings.
3. Observe the OLED state and passively monitor one collection at a time.
4. Record whether any input reports change when the display falls back to
   `Test`.

### Test 4B: Dynamic Mode With G HUB Closed

1. Close G HUB normally while leaving the wheel in Dynamic mode.
2. Repeat the same passive observations.
3. Compare the OLED behavior and input reports with Test 4A.

Do not treat the absence of input reports as proof that a collection cannot
carry display output. HID input and output directions are independent.

## Results Template

Use one row per controlled observation:

| Test | G HUB | OLED mode | Collection | Physical action | Result | Confidence |
|---|---|---|---|---|---|---|
| 1A | Open | Settings | All | Inventory only | Five collections matched the documented inventory | Confirmed |
| 1B | Closed | Settings | All | Inventory only | Identical to 1A; G HUB did not change the descriptors | Confirmed |
| 2A | Closed | Settings | None | Navigate menus | All menus and the display selector rendered normally; Dynamic showed `Test` | Confirmed observation |
| 2B | Closed | Settings | MI_01 COL03 | Change RPM Brightness 99 to 98 | One `0x12` report carried parameter `0x0A` with value 98 | Confirmed |
| 3A | Open | Settings | MI_01 COL03 | Change RPM Brightness in G HUB | Pending | Unknown |
| 4A-COL02 | Open | HomeScreen | MI_01 COL02 | Select Profile, Torque, Test, and Dynamic | Four choices produced four identical dev `0x02` groups with payload sequence `0`, `2`, `1`; no screen ID appeared | Confirmed |
| 4A-COL03 | Open | Settings/HomeScreen | MI_01 COL03 | Five Settings-button presses, then controlled screen choices | `0x17` `01 00` means Settings open; `01 01` means Settings closed/HomeScreen restored; no selected-screen ID appeared | Confirmed |
| 4A-BUTTON-COL02 | Open | Settings/HomeScreen | MI_01 COL02 | Five Settings-button presses without navigation | Exactly two `0`, `2`, `1` groups aligned with the two Settings closes; openings were silent | Confirmed |
| 4B-COL03 | Closed | Settings/HomeScreen | MI_01 COL03 | Three Settings-button presses | Exactly `0x0100`, `0x0101`, `0x0100`; no configuration snapshot followed the close | Confirmed |
| 4B-COL02 | Closed | Settings/HomeScreen | MI_01 COL02 | Three Settings-button presses | Zero reports; unlike G HUB-open captures, Settings closes caused no `0`, `2`, `1` groups | Confirmed |
| 5A | Starting | Settings | MI_01 COL02 | Start G HUB normally | 167 reports; dev `0x01` FeatureSet catalog observed on `0x11` | Confirmed |
| 6A | Starting | HomeScreen | All RS50 interfaces | Start G HUB normally under device-scoped USB capture | G HUB enumerated `0x18A2`, `0x8091`, and `0x8093` but did not invoke their runtime indices; no 64-byte host output or sustained display stream appeared | Confirmed |
| 6B | Starting | HomeScreen | All RS50 interfaces | Re-analyze 6A with FeatureSet reconstruction | Base runtime `0x12` maps to public feature `0x8130`; G HUB static name is `DisplayGameData`; zero operational calls during startup | Confirmed |
| 6C | Open | Unknown | All RS50 interfaces | Capture a game/G HUB session with visibly changing RPM LEDs | Runtime `0x0B` (`RPM Indicator`) streamed states 0-10; runtime `0x12` (`DisplayGameData`) received zero host reports | Confirmed negative evidence |
| 7A | Open | Dynamic | All RS50 interfaces | Run a legitimate telemetry-capable producer; do not inject HID reports | Pending: look specifically for host calls to dev `0xFF`, runtime `0x12`, especially function `3` | Unknown |
| 7B | Open | Dynamic | None | After a successful 7A, stop the legitimate producer and time the fallback | Static expectation: pending Dynamic data expires after approximately 240 seconds | Unknown |
| J1 | Closed | Dynamic | MI_01 COL01/COL03 | One authorized 10-second stationary iRacing telemetry trial with video and USBPcap | Video: `SPEED / 0 KMH / GEAR / N`; centering, RPM LEDs, inputs, and connection normal; PCAP retained discovery but not setter/ACK | Physical confirmed; USB inconclusive |
| J2 | Closed | Dynamic | MI_01 COL01/COL03 | One authorized repeat of the same bounded stationary trial with direct USBPcapCMD and host transcript | `SPEED / 0 KMH / GEAR / N`; transcript and PCAP contain byte-identical discovery/setter/ACK pairs; normal LEDs and FFB; no destructive reset/gain/reset lifecycle | Confirmed |
| K1 | Closed | Dynamic | MI_01 COL01/COL03 | One separately authorized fixed A-J gallery with video and USBPcap | All layouts rendered in order; 1 discovery + 10 setters + 11 exact ACKs; no unrelated host operation or observed physical side effect | Confirmed |

## Phase 5: Legitimate Dynamic Producer Capture

Feature `0x8130` is now the primary target. This phase must observe software
that legitimately activates the feature; it must not imitate the statically
inferred function-`3` writes.

### Test 7A: Activation Capture

1. Put the wheel on the Dynamic HomeScreen and verify the Test fallback.
2. Start a USB capture scoped to the physical RS50 address.
3. Start G HUB normally, then start exactly one candidate game or official
   telemetry producer.
4. Generate a short, controlled sequence: neutral, first gear, one speed
   change, then stop.
5. Stop the producer and capture without changing wheel settings.
6. Analyze host reports to device `0xFF`, runtime index `0x12`.

Decisive evidence would be function-`3` updates whose payload changes correlate
with the controlled gear or speed sequence. Optional function-`0`/`1`
capability reads may precede them, but static firmware analysis shows they are
not a required setter handshake. If no `0x12` calls appear, record the candidate
as a negative result; do not broaden the capture or replay unrelated reports.

### Test 7B: Passive Expiry Timing

Run this only after Test 7A has legitimately populated the OLED:

1. Stop the producer immediately after a visible, stable Dynamic update.
2. Do not close G HUB, change screens, press wheel controls, or send traffic.
3. Measure elapsed wall-clock time until the OLED leaves the received layout
   or returns to its fallback.
4. Record the observed duration and resulting screen.

Static firmware analysis predicts approximately 240 seconds because function
`3` loads `240000` and the counter is decremented by the 1 ms SysTick-driven
display task. This passive observation tests that prediction without replaying
or injecting a report.

## Phase 6: Future DirectInput Support Probe

This phase is **not** part of the read-only session above. Run it only after
the user explicitly approves transmitting documented support-query commands.
Do not combine it with a layout setter.

Static analysis of both installed x86 and x64 force-feedback drivers shows
that outer DirectInput Escape command `4` accepts version `1` and these
non-setter inner commands:

| Inner command | Query | Minimum input | Minimum output |
|---:|---|---:|---:|
| 2 | General display support | 12 | 1 |
| 3-5 | Layout A-C support | 12 | 1 |
| 6 | Layout D support | 12 | 4 |
| 7-10 | Layout E-H support | 12 | 6 |
| 11-12 | Layout I-J support | 12 | 10 |

The driver writes the boolean support result only to output byte zero. Prefill
the complete buffer with a sentinel and require all trailing bytes to remain
unchanged. Build A created a process-owned top-level window, requested
nonexclusive background cooperation, and sent command `2` exactly once.
DirectInput returned `DIERR_NOTEXCLUSIVEACQUIRED` and left the sentinel
unchanged. Build A2 used a visible foreground window plus bounded exclusive
acquisition, but `Acquire` returned `DIERR_INVALIDPARAM` because no data format
had been set; its capture contained no HOST HID++ report. Build A3 first calls
`SetDataFormat(&c_dfDIJoystick2)`, then retains the same bounded acquisition and
release. Record cooperative, data-format, Acquire, Escape, and Unacquire
results, returned bytes, and scoped USB traffic, then stop. Do not proceed to
layout queries in the same run unless the command is confirmed non-mutating
and the user approves the next step.
Setter commands `1` and `13-22` remain out of scope.

Static tracing confirms that command `2` is not merely a cached capability
check: the driver submits an asynchronous feature request and waits up to
200 ms for its result. Treat it as transmitted device traffic and capture it;
do not describe this phase as passive or read-only USB monitoring.

Build A3 physically completed this phase on 2026-07-27. All DirectInput
HRESULTs were successful, output byte zero was `1`, and the operator observed
no physical change. USBPcap matched 15 HOST requests to 15 DEVICE responses,
including Root discovery of public feature `0x8130` at runtime `0x12`; there
were zero operational requests to that runtime. See
[`evidence/RS50_DIRECTINPUT_QUERY_BUILD_A3_SUCCESS_2026-07-27.md`](evidence/RS50_DIRECTINPUT_QUERY_BUILD_A3_SUCCESS_2026-07-27.md).

The first later A-J layout query has a larger expected footprint. It lazily
sends feature function `0` once, then function `1` for every reported layout
and caches the descriptors. The RS50's known count of ten predicts eleven
transactions in that first layout-query capture and no equivalent refresh for
the remaining layout queries in the same feature-object lifetime.
Because the driver marks this cache initialized before the exchange, any
timeout or partial result requires closing and recreating the DirectInput
device before a retry.

Use Layout J query `12` with its exact ten-byte output as that first query.
Never test a null or undersized output: Layout A query `3` contains an unsafe
malformed-buffer path in the installed driver. Boundary fuzzing is explicitly
out of scope.

Build B executed that single Layout J query on 2026-07-27. All DirectInput
HRESULTs succeeded, output byte zero was `1`, and bytes 1-9 retained sentinel
`0xA5`. USBPcap reconstructed exactly the predicted eleven operational
`0x8130` exchanges: one layout-count query and ten descriptor queries. The
RS50 reported Layout J ID `10` with capacities `19/10/19/10`. The operator's
explicit physical observation confirmed no OLED, LED, torque, or
wheel-position change. This permits Build C to be compiled and audited, but
its physical execution still requires separate approval and capture.
See
[`evidence/RS50_LAYOUT_J_QUERY_SUCCESS_2026-07-27.md`](evidence/RS50_LAYOUT_J_QUERY_SUCCESS_2026-07-27.md).

## Evidence Levels

- **Confirmed:** reproduced on physical hardware with a controlled test.
- **Likely:** supported by multiple observations but not independently proven.
- **Unknown:** insufficient or conflicting evidence.

At the end of the query phase, no result yet confirmed OLED write support; a
separate, explicitly approved setter phase was required.

Build C subsequently completed that separately approved phase. One fixed
Layout J setter produced a matched `0x8130` function-`3` exchange and visible
OLED text. The capture decoded `RS50 / LOGIDYNAMI / TEST 1 / OLED LINK`,
exactly matching the supplied photograph. The operator observed no torque,
LED, or wheel-movement change. Static output is therefore confirmed; live or
repeated telemetry remains a separate gate. See
[`evidence/RS50_STATIC_LAYOUT_J_SUCCESS_2026-07-27.md`](evidence/RS50_STATIC_LAYOUT_J_SUCCESS_2026-07-27.md).
