# RS50 Build J Stationary Telemetry Result

## Classification

- Physical OLED and coexistence result: **confirmed success**
- Native process result: **confirmed success**
- Complete USB transaction evidence: **inconclusive**
- Moving-car or full-lap authorization: **not granted**

Build J ran exactly once on 2026-07-29 under a separate operator
authorization. G HUB was closed, iRacing was running, the car was stopped in
the pits in neutral with the brake held, the RS50 was awake, Dynamic showed
the firmware Test fallback, and video plus USBPcap were active.

## Local Evidence

- PCAP:
  `2026-07-29_rs50_build_j_iracing_stationary_telemetry_10s.pcapng`
- PCAP size: 77,120,020 bytes
- PCAP SHA-256:
  `9E03A2187204FC546C1F58B4B2B492CE53E3FBEF02F3B9AA51F8C5324C75FA53`
- Video: `IMG_2843.MOV`
- Video size: 90,779,705 bytes
- Video SHA-256:
  `A97F7A2A86CEEA284E1826E9A13DEF7C1534E84D62D162570D69CBFA695D20A7`

Both files remain local and ignored. The MOV contains private phone,
device, and location metadata and must not be committed or uploaded.

## Native and Physical Result

The separately compiled process reported:

```text
Build J completed: 1 stationary telemetry frame(s) were acknowledged and the HID streams were closed.
```

The 51.65-second video begins with the normal four-bar `C/B/G/H` Test
fallback. Between approximately 2.25 and 2.50 seconds, the OLED changes to
the expected live Layout J frame:

```text
SPEED
0 KMH
GEAR
N
```

It remains stable for the trial. This independently confirms that the
connected iRacing telemetry source supplied the stopped state and that the
shared HID++ route activated the intended OLED layout.

The operator reported no movement, torque impulse, unexpected resistance,
input loss, or disconnect. Afterward, a small steering displacement returned
normally to center and a brief neutral rev produced normal RPM LED behavior.

## USB Capture Result

The PCAP spans 2026-07-29 01:19:04.125116 through
01:22:23.286784 and contains:

- 869,037 USB packets;
- 48,265,516 captured data bytes;
- 261,595 reconstructed HID reports;
- 227 host and 261,368 device reports;
- continuous normal RS50 input and iRacing-related traffic;
- one Root discovery of public feature `0x8130` at runtime index `0x12`;
- no `0x8123` reset/gain/reset lifecycle or other host feature operation
  attributable to Build J.

The discovery request is at frame 188911 and its exact response is at frame
188922, an approximately 2.034 ms latency.

However, exhaustive searches of reconstructed reports, raw control data,
interrupt data, and the literal `SPEED`/`GEAR` payload found no Layout J
setter and no matching function-3 acknowledgement. The cause is unknown.
Possible capture loss or a transfer-visibility gap is not established because
the PCAP contains no interface statistics with a dropped-packet count.

The process result and video prove that a request reached the wheel and
changed the OLED. They do not permit inventing missing USB evidence.
Therefore the strict requirement for a complete setter/ACK pair is not met
by this capture.

## Decision

J1 proves the physical stationary telemetry path and its safe coexistence in
this session. It does not provide the complete USB audit trail required to
advance directly to moving-car or full-lap testing.

Do not repeat J1 merely to obtain a cleaner capture. Before any new physical
run, add host-side timestamped request/response evidence and determine a
capture method that can independently retain the known 64-byte transaction.
Any repetition requires a new design review and a fresh operator
authorization.
