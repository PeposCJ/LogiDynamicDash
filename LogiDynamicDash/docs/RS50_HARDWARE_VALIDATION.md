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
| 4B | Closed | Dynamic | One at a time | No change | Pending | Unknown |
| 5A | Starting | Settings | MI_01 COL02 | Start G HUB normally | 167 reports; dev `0x01` FeatureSet catalog observed on `0x11` | Confirmed |

## Evidence Levels

- **Confirmed:** reproduced on physical hardware with a controlled test.
- **Likely:** supported by multiple observations but not independently proven.
- **Unknown:** insufficient or conflicting evidence.

No result from this plan confirms OLED write support. A separate, explicitly
approved phase will be required before the project sends any report to the
RS50.
