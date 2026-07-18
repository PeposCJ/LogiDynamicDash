# RS50 Dynamic Selector Passive Evidence

## Session 1: MI_01 COL02

- Date: 2026-07-17
- Device: Logitech RS50, VID `0x046D`, PID `0xC276`
- Host state: G HUB open
- Collection: MI_01 COL02
- Explorer behavior: 45-second input read only; no output or feature reports
  sent
- Physical sequence: Dynamic to Test to Dynamic using the wheel controls
- OLED observation: after returning to Dynamic, the Test fallback appeared
  immediately
- Result: 12 changed input reports, all from device index `0x02`

The local log contains no report with device index `0x01`. No device identity,
path, serial number, username, or absolute timestamp is included in this
sanitized note.

## Sanitized Raw Reports

The first and second six-report groups had the same byte sequence. The groups
were separated by approximately nine seconds and correlated with the two
selector transitions.

```text
11 02 0E 9D 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
11 02 0E 10 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
11 02 0E 9D 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
11 02 0E 10 02 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
11 02 0E 9D 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
11 02 0E 10 01 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00

11 02 0E 9D 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
11 02 0E 10 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
11 02 0E 9D 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
11 02 0E 10 02 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
11 02 0E 9D 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
11 02 0E 10 01 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
```

## Interpretation

The selector interaction caused a repeatable input notification on device
index `0x02`, feature index `0x0E`. The payload values `0`, `2`, and `1` are
recorded but intentionally remain unnamed because the passive evidence is not
sufficient to assign semantics.

The immediate Test fallback confirms the wheel's no-producer behavior in
Dynamic mode with G HUB open. It does not show whether G HUB sent any output:
the explorer observed only device-to-host input reports. It also does not prove
that feature index `0x0E` or device index `0x02` carries OLED pixels or live
telemetry.

## Session 2: MI_01 COL03

- Host state: G HUB open
- Explorer behavior: 60-second input read only; no output or feature reports
  sent
- Starting state: Dynamic mode showing the Test fallback
- Physical sequence: open Settings, navigate to HomeScreen, select Test, and
  close Settings; repeat to select Dynamic
- Final OLED observation: Dynamic mode continued showing the Test fallback
- Result: 44 input reports received and 38 changed reports logged

An earlier zero-report COL03 capture is excluded because the physical sequence
was not performed during its monitoring window.

### Parameter 0x17 Sequence

The two `0x17` pairs correlated with the two complete Settings interactions:

| Relative event | Report prefix | Observation |
|---|---|---|
| Open Settings before Test | `12 FF 17 00 01 00` | Settings became visible |
| About 5.8 seconds later | `12 FF 17 00 01 01` | Settings closed; Test HomeScreen restored; followed by a settings snapshot |
| Open Settings before Dynamic | `12 FF 17 00 01 00` | Settings became visible |
| About 5.8 seconds later | `12 FF 17 00 01 01` | Settings closed; Dynamic HomeScreen restored; followed by a settings snapshot |

Each report was 64 bytes; all bytes after the shown six-byte prefix were zero.
The complete raw capture remains local and is intentionally not committed.

### Interpretation

The corrected physical-action timeline shows that `0x0100` and `0x0101` do not
identify Test and Dynamic. They correlate with opening Settings and closing
Settings to restore the configured HomeScreen, respectively.

The periodic pair and settings snapshots are configuration/status traffic, not
a framebuffer or sustained telemetry stream. Because the explorer observes
only device-to-host input, this session still provides no visibility into any
G HUB output report that may precede the notifications.

## Session 3: Profile, Torque, Test, and Dynamic on COL03

- Host state: G HUB open
- Explorer behavior: 90-second input read only; no output or feature reports
  sent
- Starting state: Dynamic mode showing the Test fallback
- Physical sequence: select Profile, Torque, Test, and Dynamic once each, with
  controlled pauses
- Visual observations:
  - Profile displayed the active profile name
  - Torque displayed the torque screen
  - Test displayed the Test screen
  - Dynamic displayed the Test fallback
- Result: 88 input reports received and 76 changed reports logged

Opening Settings before each selection emitted `12 FF 17 00 01 00`. Closing
Settings to show the configured HomeScreen emitted `12 FF 17 00 01 01`,
followed by the same configuration snapshot pattern. All remaining bytes in
those four 64-byte `0x17` reports were zero.

This four-option control confirms that parameter `0x17` represents Settings
visibility, not the chosen HomeScreen. COL03 exposed no distinct value for
Profile, Torque, Test, or Dynamic. The raw log remains local and is
intentionally not committed.

## Session 4: Profile, Torque, Test, and Dynamic on COL02

- Host state: G HUB open
- Explorer behavior: 90-second input read only; no output or feature reports
  sent
- Physical sequence: select Profile, Torque, Test, and Dynamic once each, in
  that order, with controlled pauses
- Result: 24 changed input reports arranged as four identical six-report
  groups

Every group came from device index `0x02`, feature index `0x0E`, and contained
the following sequence:

```text
11 02 0E 9D 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
11 02 0E 10 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
11 02 0E 9D 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
11 02 0E 10 02 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
11 02 0E 9D 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
11 02 0E 10 01 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
```

The group repeated once per selected screen with no byte that distinguished
Profile, Torque, Test, or Dynamic. The payload values `0`, `2`, and `1` remain
intentionally unnamed. Together with Session 3, this confirms that COL02 and
COL03 expose interaction/configuration notifications but no selected-screen ID
and no OLED content stream.

## Session 5: Five Settings-Button Presses on COL03

- Date: 2026-07-18
- Host state: G HUB open
- Explorer behavior: 75-second input read only; no output or feature reports
  sent
- Physical sequence: press and release the Settings button beside the OLED
  exactly five times, approximately three seconds apart
- Selector actions: none
- Final visual state: Settings visible
- Result: 45 input reports received and 39 changed reports logged

The five button presses produced this exact alternating parameter `0x17`
sequence:

```text
12 FF 17 00 01 00  # Settings opened
12 FF 17 00 01 01  # Settings closed; HomeScreen restored
12 FF 17 00 01 00  # Settings opened
12 FF 17 00 01 01  # Settings closed; HomeScreen restored
12 FF 17 00 01 00  # Settings opened; final visible state
```

All bytes after the shown six-byte prefixes were zero. Each `0x0101` close was
followed by the same settings snapshot pattern. This button-only control
confirms the state mapping independently of Profile, Torque, Test, or Dynamic
selection. The raw log remains local and is intentionally not committed.

## Session 6: Five Settings-Button Presses on COL02

- Date: 2026-07-18
- Host state: G HUB open
- Explorer behavior: 75-second input read only; no output or feature reports
  sent
- Physical sequence: press and release the Settings button beside the OLED
  exactly five times, approximately three seconds apart
- Selector actions: none
- Final visual state: Settings visible
- Result: 12 changed reports arranged as two identical six-report groups

An earlier zero-report COL02 capture is excluded because no physical button
presses were performed during its monitoring window.

The valid capture produced one `0`, `2`, `1` group after the second press and
one after the fourth press. Those were the two presses that closed Settings and
restored the HomeScreen. The first, third, and fifth presses opened Settings
and produced no COL02 reports.

```text
11 02 0E 9D 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
11 02 0E 10 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
11 02 0E 9D 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
11 02 0E 10 02 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
11 02 0E 9D 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
11 02 0E 10 01 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
```

The group is therefore classified as a Settings-close/HomeScreen-restore
notification. Its internal `0`, `2`, and `1` values remain unnamed because
their individual meanings are not established. The raw log remains local and
is intentionally not committed.

## Session 7: Three Settings-Button Presses With G HUB Closed

- Date: 2026-07-18
- Host state: G HUB closed normally; Logitech services were not stopped
- Collection: MI_01 COL03
- Explorer behavior: 60-second input read only; no output or feature reports
  sent
- Starting visual state: configured HomeScreen visible
- Physical sequence: press and release the Settings button exactly three
  times, with controlled pauses
- Final visual state: Settings visible
- Result: exactly three changed reports

```text
12 FF 17 00 01 00  # Settings opened
12 FF 17 00 01 01  # Settings closed; HomeScreen restored
12 FF 17 00 01 00  # Settings opened; final visible state
```

All bytes after the shown six-byte prefixes were zero. Unlike the equivalent
G HUB-open observations, no settings snapshot followed `0x0101`. This confirms
that the wheel emits the `0x17` Settings state independently of G HUB and
supports the inference that G HUB actively queries the configuration after a
close notification. The passive explorer did not observe those host-to-device
queries directly. The raw log remains local and is intentionally not
committed.

## Session 8: COL02 Control With G HUB Closed

- Date: 2026-07-18
- Host state: G HUB closed normally; Logitech services were not stopped
- Collection: MI_01 COL02
- Explorer behavior: 60-second input read only; no output or feature reports
  sent
- Starting visual state: configured HomeScreen visible
- Physical sequence: press and release the Settings button exactly three
  times, with controlled pauses
- Final visual state: Settings visible
- Result: zero input reports

The same three physical transitions produced `0x0100`, `0x0101`, and `0x0100`
on COL03 in Session 7. COL02 remained completely silent. In the G HUB-open
controls, every Settings close produced a six-report `0`, `2`, `1` group on
COL02.

This A/B result confirms that the COL02 groups are responses caused by G HUB
activity rather than unsolicited firmware notifications. It also distinguishes
the firmware-originated Settings state on COL03 from the host-triggered
configuration-query traffic observed when G HUB is open. The zero-report raw
log remains local and is intentionally not committed.
