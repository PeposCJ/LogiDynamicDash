# RS50 Build J2 Stationary Telemetry Success

## Classification

- Physical OLED and coexistence result: **confirmed success**
- Native process result: **confirmed success**
- Host transcript evidence: **confirmed**
- Independent USB transaction evidence: **confirmed**
- Stationary telemetry gate: **complete**
- Moving-car or full-lap authorization: **not granted by this result alone**

Build J2 ran exactly once on 2026-07-29 under a fresh operator authorization.
G HUB was closed, iRacing was running, the car was stopped in the pits in
neutral with the brake held, the RS50 was awake, Dynamic showed the firmware
Test fallback, and direct USBPcapCMD capture plus video were active.

## Local Evidence

The raw artifacts remain local and ignored:

| Artifact | Size | SHA-256 |
|---|---:|---|
| `2026-07-29_rs50_build_j2_stationary_telemetry_direct.pcap` | 215,941,404 bytes | `37F3F7D348A3DDA75B4F389776586E38179F2F472662DF9E53C5739461261922` |
| `.tmp/rs50-build-j-transcripts/rs50-build-j-transcript-20260729-081420-1587314Z.jsonl` | 1,144 bytes | `00352CC5CE7B201E73FDA2219A617AC0AC44490014B2C762F9A425DD3CD05E60` |
| `IMG_2847.MOV` | 40,328,228 bytes | `B7D951C874B9CCB535EBE007AC1C907273D25A685449F8F190433DDB3BD470CD` |

The MOV contains private phone, device, and location metadata. It and the raw
PCAP must not be committed or uploaded.

## Native and Physical Result

The separately compiled process reported:

```text
Build J local transcript: .tmp/rs50-build-j-transcripts\rs50-build-j-transcript-20260729-081420-1587314Z.jsonl
Build J completed: 1 stationary telemetry frame(s) were acknowledged and the HID streams were closed.
```

The OLED displayed the live Layout J frame:

```text
SPEED
0 KMH
GEAR
N
```

The video confirms that this frame remained stable. Its opening already shows
the telemetry frame, so the video alone does not establish the precise
Test-to-telemetry transition. The operator reported no physical change or
disconnect and confirmed normal LEDs and FFB after the trial.

## Exact Transcript

The bounded write-through transcript contains exactly four events:

1. Root discovery request:
   `10FF000A813000`
2. Root discovery response, exact header match:
   `12FF000A1200` followed by zero padding
3. Layout J setter:
   `12FF123A095350454544000000000000000000000000000030204B4D480000000000474541520000000000000000000000000000004E00000000000000000000`
4. Layout J response, exact header match:
   `12FF123A00` followed by zero padding

The setter decodes to layout ID `9`, then the four wire fields
`SPEED`, `0 KMH`, `GEAR`, and `N`. Transcript exchange timings were 8,268
microseconds for discovery and 2,726 microseconds for the setter.

The request timestamp is recorded immediately before entering the physical
exchange, not at the USB bus. This is visible on the first transaction, where
stream startup precedes the bus request. The setter timestamps differ from
the corresponding bus timestamps by less than 0.6 ms.

## Independent Direct USB Capture

USBPcapCMD captured the RS50 address directly with a 128 MiB buffer and full
65,535-byte snap length. `capinfos` validated the resulting file:

- capture duration: 534.559233 seconds;
- 3,121,328 packets;
- 166,000,132 captured data bytes;
- strict time order: true;
- one USBPcap interface.

The complete decoder reconstructed 1,048,412 HID reports:

- 2,421 host reports;
- 1,045,991 device reports;
- 2,421 exact matched HID++ response headers;
- zero unmatched host requests;
- zero invalid records.

The relevant bus frames are:

| Frame | Epoch | Operation |
|---:|---:|---|
| 1,807,248 | 1785312860.413341 | Root discovery request |
| 1,807,265 | 1785312860.415635 | Root discovery response |
| 1,808,610 | 1785312860.640016 | Layout J setter |
| 1,808,627 | 1785312860.642658 | Layout J response |

Bus request-to-response latency was 2.294 ms for discovery and 2.642 ms for
the setter. Every byte in both PCAP request/response pairs matches the local
transcript. The PCAP contains exactly one operational `0x8130` request and no
display transaction absent from the transcript.

USBPcapCMD did not stop on the operator's initial interrupt. Its exact capture
process tree was terminated, after which no USBPcapCMD process remained.
`capinfos` then read the file successfully and confirmed the packet count,
timestamps, full snap length, strict ordering, and hashes above. The OLED
transaction occurred more than three minutes before the capture ended, so
termination of the capture tail does not affect the accepted transaction.

## FFB and LED Coexistence

The capture does not contain the destructive DirectInput lifecycle:

```text
RESET_ALL -> SET_GLOBAL_GAINS(0xFFFF) -> RESET_ALL
```

It does contain one matched `0x8123 RESET_ALL` operation:

- request frame 3,080,866 at epoch `1785313072.904763`;
- request bytes `10FF101E000000`;
- response frame 3,080,881 at epoch `1785313072.907045`;
- response bytes `12FF101E` followed by zero padding.

This isolated request used SW-ID `0xE`, not Build J's SW-ID `0xA`, and occurred
212.265 seconds after the Layout J setter. There was no `SET_GLOBAL_GAINS`
operation and no second reset. It is therefore unrelated to the Build J
transaction and is not the exclusive DirectInput lifecycle previously shown
to disrupt iRacing. Normal real-time RS50 traffic continued, and the operator
confirmed normal LEDs and FFB.

## Decision

J2 closes J1's missing-capture gap. The live stationary telemetry frame is
independently supported by the physical observation, exact bounded host
transcript, and byte-identical USB request/response pairs.

This result completes the stationary telemetry gate. It supports designing a
new bounded moving-car stage, but it does not itself authorize a full lap.
The next physical stage must define a short duration, low speed, fixed update
rate, explicit stop conditions, and fresh operator authorization before any
moving-car execution.
