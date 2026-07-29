# RS50 OLED Production Architecture

## Status

The production implementation is complete through the offline integration
gate. It is independently written from the confirmed interoperability
specification and does not merge or copy the research branch history.

Completed offline components:

- typed firmware-rendered layouts A-J;
- Root discovery of public HID++ feature `0x8130`;
- exact request and acknowledgement validation;
- strict RS50 MI_01 COL01/COL03 collection selection;
- fail-closed session lifecycle;
- identical-frame suppression and 5 Hz change limit;
- km/h and mph telemetry formatting for all layouts;
- console plus optional OLED display composition;
- exact command-line arming contract;
- ten-second automatic cancellation;
- moving-car rejection before the next frame is transmitted.

No production executable from this branch has been run against hardware.

## Offline Validation

The current production branch passes:

- 72 unit and integration tests;
- Release build with warnings treated as errors;
- `dotnet format --verify-no-changes`;
- `git diff --check`;
- the production-surface audit in
  `scripts/Test-Rs50OledProductionSurface.ps1`;
- direct and transitive NuGet vulnerability audit with no known vulnerable
  package reported by the configured sources;
- invalid-command smoke test, which exits with code 2 before constructing a
  session.

## Data Flow

```text
iRacing telemetry
      |
      v
DisplayController (normal / brake bias / last lap / connection)
      |
      v
Rs50TelemetryFrameFormatter (typed Layout A-J frame)
      |
      v
Rs50OledSession (deduplicate / 5 Hz / fail closed)
      |
      v
Rs50OledProtocol (closed 0x8130 request)
      |
      v
Rs50OledDeviceExchange (exact MI_01 collections)
```

The console-only route ends before `Rs50OledSession` and never invokes the
session factory, device catalog, or HidSharp.

## Hardware Boundary

The physical adapter accepts only `Rs50OledTransaction` instances created by
the closed protocol encoder. It does not expose a raw HID write API.

Collection selection requires exactly one match for each confirmed endpoint:

| Role | Path marker | Usage | Input | Output |
|---|---|---:|---:|---:|
| Root short report | `mi_01&col01` | `0xFF430701` | 7 | 7 |
| Very-long display report | `mi_01&col03` | `0xFF430704` | 64 | 64 |

Both collections must also match Logitech VID `0x046D` and RS50 PID `0xC276`.
Ambiguous, missing, additional-usage, or wrong-length collections fail before
opening a stream.

Each transaction performs one write. It reads at most 16 very-long reports to
find the exact matching response or matching HID++ error. It does not retry a
write, acquire DirectInput, invoke feature `0x8123`, or interact with FFB,
TRUEFORCE, LEDs, profiles, or firmware.

## Session Boundary

The session:

- discovers `0x8130` rather than assuming runtime index `0x12`;
- validates public flags and protocol version zero;
- sends only typed layouts A-J;
- suppresses the last acknowledged frame when unchanged;
- limits changed frames to one every 200 ms;
- validates an exact zero-body acknowledgement;
- permanently faults after any transport, protocol, or acknowledgement
  failure;
- requires disposal and explicit process restart after a failure;
- never reconnects or retries silently.

## Layout Mapping

All ten confirmed layouts are selectable:

| Layout | Production mapping |
|---|---|
| A | Blank firmware layout |
| B | Firmware Test layout |
| C | RPM gauge |
| D | RPM gauge, speed indicator, compact status text |
| E | RPM gauge, speed indicator, visual-left speed, visual-right gear |
| F | One-character gear/status and three-character speed/value |
| G | Same fields as F with the firmware's opposite font emphasis |
| H | Two-row speed/gear or temporary status page |
| I | Four-row mixed-alignment telemetry/status page |
| J | Four-row centered telemetry/status page |

RPM and speed gauges clamp to the protocol's normalized byte range.
Maximum RPM and maximum gauge speed are configuration values. Invalid,
negative, or non-finite telemetry produces placeholders and empty gauges.

## Disabled-by-Default Integration

Running with no arguments starts only the existing console display:

```powershell
dotnet run --project .\LogiDynamicDash\LogiDynamicDash.csproj
```

Any nonempty argument list that does not exactly match the stationary arming
contract is rejected before constructing a HID session.

The compiled hardware route requires these 16 arguments in this exact order:

```text
--enable-rs50-oled-stationary-trial
--confirm-ghub-closed
--confirm-iracing-running
--confirm-car-stationary-in-pits
--confirm-rs50-dynamic-selected
--confirm-10-second-limit
--acknowledge-no-moving-car-use
--layout <A-J>
--speed-unit <KMH|MPH>
--maximum-rpm <1000-30000>
--gauge-maximum-speed <10-500>
--confirm-settings
```

The process cancels automatically after ten seconds. While iRacing reports
`IsOnTrackCar == true`, missing, negative, non-finite, or greater-than-0.5 m/s
speed stops the application before another OLED frame is sent.

This route is compiled for a future stationary production smoke test. Its
presence is not authorization to run it.

## Remaining Gates

Before the draft production PR can be enabled for general driving:

1. review a stationary production checklist;
2. obtain fresh physical authorization;
3. capture and verify one bounded production run;
4. confirm OLED, FFB, LEDs, inputs, and simulator connection remain normal;
5. keep the separately planned low-speed Build L postponed until explicitly
   resumed;
6. design reconnection and long-duration ownership only after the relevant
   physical stages pass.

The draft may be reviewed as disabled-by-default code before those physical
gates, but it should not advertise moving-car support.
