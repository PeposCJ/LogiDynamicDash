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
- bounded multiplexed-ACK scanning without request retries;
- fail-closed session lifecycle;
- identical-frame suppression, 5 Hz change limit, and latest-frame scheduler;
- km/h and mph telemetry formatting for all layouts;
- independent per-mode layout selection;
- injectable application orchestration and deterministic telemetry replay;
- copied telemetry snapshots and a 200 ms display heartbeat;
- bounded hardware-free telemetry recording;
- hardware-free Windows configuration GUI with all five current iRacing
  category recommendations;
- exact iRacing event-category and driver-car identity capture;
- explicit Disabled/Opening/Active/Faulted/Stopped lifecycle without reconnect;
- console plus optional OLED display composition;
- strict persistent JSON configuration;
- offline A-J preview and end-to-end virtual telemetry simulation;
- sanitized JSONL diagnostics without report bytes or device identity;
- exact command-line arming contracts;
- automatic cancellation for stationary and Build L trials;
- per-route speed-envelope rejection before the next frame is transmitted.

After correcting the initial redirected-console failure, the separately
authorized stationary production gate passed on the physical RS50 on
2026-07-30. Live iRacing telemetry displayed `0 KMH`, neutral, and a graphical
gauge; acknowledgements, shutdown, FFB, LEDs, controls, and connection were
normal. The complete result is documented in
`RS50_OLED_PRODUCTION_STATIONARY_RESULT_2026-07-30.md`.

## Offline Validation

The current production branch passes:

- 156 unit and integration tests, including one million scheduler submissions;
- Release build with warnings treated as errors;
- `dotnet format --verify-no-changes`;
- `git diff --check`;
- the production-surface audit in
  `scripts/Test-Rs50OledProductionSurface.ps1`;
- direct and transitive NuGet vulnerability audit with no known vulnerable
  package reported by the configured sources;
- invalid-command smoke test, which exits with code 2 before constructing a
  session.

The deterministic 30-second simulation processes 601 updates for each layout.
Static layouts A/B transmit once; the dynamic layouts transmit between 85 and
132 acknowledged frames, remaining below the theoretical 151-frame 5 Hz
ceiling. Separate stress tests cover 20,000 virtual changed frames, one
million serialized scheduler submissions, and six virtual hours at 20 Hz.

The same build, test, format, surface-audit, and vulnerability steps run in
the Windows GitHub Actions workflow for pushes and pull requests. After those
checks pass, CI publishes framework-dependent and self-contained `win-x64`
artifacts. Each includes example configuration, replay scenarios, notices,
an SPDX 2.3 SBOM, SHA-256 manifest, and successful packaged-executable smoke
tests. CI does not sign or release either artifact.

## Data Flow

```text
iRacing telemetry + authoritative session category/car identity
      |
      v
ITelemetrySource -> application lifecycle and 200 ms refresh coordinator
      |
      v
DisplayController (normal / brake bias / last lap / connection)
      |
      v
Rs50TelemetryFrameFormatter (typed Layout A-J frame)
      |
      v
Rs50OledFrameScheduler (single consumer / latest pending / critical priority)
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

The identity is isolated behind a confirmed-device descriptor. That boundary
allows a future PRO descriptor only after its VID/PID and complete collection
contract are physically confirmed; production contains no guessed PRO ID.

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

The scheduler retains the newest ordinary frame while the session is rate
limited. A queued connection-problem frame cannot be overwritten by ordinary
telemetry before it is acknowledged. A 200 ms application heartbeat flushes
pending frames even when no later telemetry callback arrives. All submissions
remain serialized.

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

The configuration file is a strict schema-versioned JSON object:

```json
{
  "schemaVersion": 2,
  "layouts": {
    "normal": "E",
    "brakeBias": "H",
    "lastLap": "J",
    "connectionProblem": "H"
  },
  "speedUnit": "KMH",
  "maximumRpm": 8000,
  "gaugeMaximumSpeed": 300
}
```

Unknown, duplicate, missing, invalid-type, out-of-range, commented, or
oversized configurations are rejected. Preview and simulation are available
without HID:

```text
--preview-all --config <json-path>
--simulate-all --config <json-path>
--replay --config <json-path> --telemetry <json-path>
--record-telemetry --output <json-path> --duration-seconds <1-1800>
```

Schema 1 remains accepted and maps its single layout to all four modes.
Replay files are strict, bounded JSON and never construct a physical adapter.
Recording samples copied telemetry at no more than 5 Hz and never constructs a
physical adapter. New recordings use replay schema 2 to retain session
category and car identity; replay schema 1 remains accepted. The Windows
configurator edits and previews the same strict configuration and can inspect
schema 2 identity offline. It enables a recommended profile only for a current
official category with an exact `CarID`; applying it requires a separate user
click. None of these paths holds a physical-session reference.
The deterministic failure coverage is listed in
`docs/OFFLINE_FAULT_MATRIX.md`.

The compiled hardware route requires these ten arguments in this exact order:

```text
--enable-rs50-oled-stationary-trial
--confirm-ghub-closed
--confirm-iracing-running
--confirm-car-stationary-in-pits
--confirm-rs50-dynamic-selected
--confirm-10-second-limit
--acknowledge-no-moving-car-use
--config <json-path>
--confirm-settings
```

The process cancels automatically after ten seconds. While iRacing reports
`IsOnTrackCar == true`, missing, negative, non-finite, or greater-than-0.5 m/s
speed stops the application before another OLED frame is sent.

This route is compiled for a future stationary production smoke test. Its
presence is not authorization to run it.

When that route is eventually authorized, it writes a local sanitized JSONL
diagnostic under the user's local application-data directory. Events contain
only UTC timestamp, operation, layout, typed result, elapsed microseconds, or
exception type. They never contain HID paths, serial numbers, raw requests,
raw responses, payload text, or exception messages.

## Remaining Gates

Before the draft production PR can be enabled for general driving:

1. preserve the successful Build L evidence in
   `RS50_OLED_LOW_SPEED_RESULT_2026-07-30.md`;
2. design a separately bounded higher-speed/full-lap stage;
3. validate that stage before enabling unrestricted moving use;
4. design reconnection and long-duration ownership only after the relevant
   physical stages pass.

The draft may be reviewed as disabled-by-default code before those physical
gates, but it should not advertise moving-car support.
