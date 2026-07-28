# RS50 Shared HID++ Physical Adapter Build F

## Status

Build F compiles a physical HID++ exchange adapter but does not connect it to
LogiDynamicDash. The application has no project reference, CLI argument,
factory, reflection path, or copied transport DLL. No Build F stream has been
opened and no report has been transmitted.

Build F exists in a separate `Rs50SharedHidppTransport` library. Both it and
the dashboard depend on the neutral `Rs50SharedHidppProtocol` library, so
there is no dependency cycle and the physical library cannot become active
merely by building or running the dashboard.

## Collection Gates

Read-only inventory on the physical RS50 established:

```text
MI_01 COL01  usage FF43:0701  input/output 0x10, 7 bytes
MI_01 COL03  usage FF43:0704  input/output 0x12, 64 bytes
```

The adapter requires exactly one of each and rejects before opening a stream
unless all of these match:

- VID `0x046D`;
- PID `0xC276`;
- path marker `mi_01&col01` or `mi_01&col03`;
- exactly one expected top-level usage;
- exact maximum input and output report lengths.

Missing, duplicate, or malformed collections fail closed. If more than one
RS50 is connected, its additional COL01/COL03 instances make the uniqueness
gate reject before opening either stream. MI_00, COL02, and MI_02 are never
selected.

The first Build G preflight proved that Windows assigns different device-path
instance segments to the RS50's top-level collections. Textual equality between
the COL01 and COL03 paths is therefore not a valid sibling-device test. The
gate relies instead on the exact VID/PID, MI_01 collection marker, usage,
length, and system-wide uniqueness observed in the catalog.

## Exchange Routing

The adapter accepts only Build E's closed transaction object:

| Transaction | Output collection | Response collection |
|---|---|---|
| Root discovery of `0x8130` | COL01 | COL03 |
| Layout J function `3` | COL03 | COL03 |

Before every write it reconstructs or checks the complete canonical envelope.
It cannot accept a caller-selected feature, function, report ID, layout, or
raw byte array.

COL03 input may also carry unrelated HID++ notifications. After one write,
the adapter reads at most 16 reports and returns only:

- the exact response header for that transaction; or
- a HID++ error naming that same feature/function/software ID.

Short reads, timeouts, open failures, or 16 unrelated reports fail the
session. The Build E layer then performs its stricter response-body validation
and permanently faults after any failure.

## Physical API Boundary

Only the separate transport assembly references HidSharp. Its concrete wrapper
uses:

- VID/PID-scoped `GetHidDevices`;
- descriptor reads;
- one `TryOpen` call site;
- bounded `Read` and `Write`;
- one-second read/write timeouts;
- deterministic disposal of both streams.

`HidStream.Write` follows the Windows user-mode output-report model documented
for `WriteFile`, which Microsoft recommends for continuously sending output
reports to a HID collection. Build F deliberately does not call
`HidD_SetOutputReport`, an API Microsoft recommends only for setting device
state and notes may be unsupported by some devices.

These API semantics do not establish the bus-level transfer selected by the
Windows HID stack. In particular, they do not prove that this RS50 write will
appear as an endpoint-0 control `SET_REPORT` rather than another transfer. That
remains a required USBPcap observation before accepting the physical route.

It contains no:

- DirectInput or Acquire/Unacquire;
- MI_00 or MI_02 route;
- force-feedback, RPM, LIGHTSYNC, profile, firmware, or settings feature;
- Win32/PInvoke HID API;
- `SetFeature`/`GetFeature`;
- public type;
- application reference.

Run:

```powershell
.\scripts\Test-Rs50SharedHidppTransportSurface.ps1
```

## Test Coverage

Build F tests use fake catalogs and streams only. They verify:

- discovery goes only to COL01;
- Layout J goes only to COL03;
- responses come only from COL03;
- unrelated notifications are skipped with a 16-report bound;
- matching HID++ errors are returned for Build E to reject;
- truncated responses fail;
- both streams are disposed;
- partial-open failure disposes the first stream;
- every identity, usage, length, path-marker, and uniqueness gate.

## Gate Before First Physical Use

No physical test is authorized by this build. The next stage must add a
separate one-shot executable or arming route that:

1. cannot stream or loop;
2. sends one fixed, visually obvious Layout J frame;
3. requires G HUB closed, RS50 awake, Dynamic selected, USBPcap running, and
   separate explicit authorization;
4. automatically disposes both HID streams after the acknowledgement;
5. does not run iRacing during the first shared-transport setter.

Offline acceptance of that capture requires:

- one Root discovery request/response;
- one Layout J setter/acknowledgement;
- endpoint-0 HID `SET_REPORT` for host HID++ output;
- zero `0x8123`, `0x807A`, or `0x807B` operations;
- zero MI_02 / endpoint-`0x03` host output;
- no LED, FFB, torque, input, or wheel-position side effect.

A live telemetry stream and a full lap remain prohibited until separate
stationary coexistence tests pass.

## Primary References

- [Microsoft: Sending HID Reports](https://learn.microsoft.com/en-us/windows-hardware/drivers/hid/sending-hid-reports)
- [Microsoft: HidD_SetOutputReport](https://learn.microsoft.com/en-us/windows-hardware/drivers/ddi/hidsdi/nf-hidsdi-hidd_setoutputreport)
