# RS50 G HUB Startup USB Evidence

## Session

- Date: 2026-07-18
- Device: Logitech RS50, VID `0x046D`, PID `0xC276`
- Initial state: G HUB closed; no wheel controls changed
- Action: start G HUB normally and wait for RS50 detection
- Capture scope: the physical RS50 USB address only, across all interfaces
- Capture result: 1,800 packets over 51.09 seconds

The complete PCAP remains local and must not be committed. Device paths,
serial numbers, user names, and unrelated USB traffic are not included here.

## Host Output Summary

After G HUB started, the host sent these HID++ payload sizes to the RS50:

| Report ID | Payload length | Count | Classification |
|---|---:|---:|---|
| `0x10` | 7 bytes | 378 | Short HID++ requests |
| `0x11` | 20 bytes | 3 | Long HID++ requests |

No 64-byte host output report and no sustained large-payload stream appeared.
The three long requests were isolated configuration transactions rather than a
display-rate stream:

```text
11 FF 0F 2B 0A 01 02 03 04 05 06 07 08 09 0A 00 00 00 00 00
11 FF 11 2B 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
11 FF 11 2B 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
```

Each received an immediate response. Their semantics remain unknown; the
observation does not justify replaying them.

## Device 0x01 Feature Enumeration

G HUB enumerated the public candidate features through FeatureSet:

| Runtime index | Feature ID | Flags | Version |
|---|---|---:|---:|
| `0x09` | `0x18A2` | `0x00` | 0 |
| `0x0E` | `0x8091` | `0x00` | 0 |
| `0x0F` | `0x8093` | `0x00` | 0 |

The corresponding sanitized responses were:

```text
11 01 01 1B 18 A2 00 00 00 00 00 00 00 00 00 00 00 00 00 00
11 01 01 1B 80 91 00 00 00 00 00 00 00 00 00 00 00 00 00 00
11 01 01 1B 80 93 00 00 00 00 00 00 00 00 00 00 00 00 00 00
```

After enumeration, G HUB sent no request to device `0x01` runtime index
`0x09`, `0x0E`, or `0x0F`. Its subsequent device `0x01` requests used only
runtime indices `0x02`, `0x03`, and `0x05`.

## Base Device Display Game Data Enumeration

The same capture contains a stronger candidate on base device index `0xFF`:

| Runtime index | Feature ID | Flags | Version | Operational host calls |
|---|---|---:|---:|---:|
| `0x12` | `0x8130` | `0x00` | 0 | 0 |

The matched sanitized transaction is:

```text
HOST   10 FF 01 1B 12 00 00
DEVICE 12 FF 01 1B 81 30 00 00 ...
```

Later static inspection of the installed G HUB agent identified the exact
class name `Feature8130DisplayGameData`. That evidence was not available when
this capture was first summarized; it moves `0x8130` ahead of the device
`0x01` candidates. Complete details are in
[`RS50_FEATURE_8130_DISPLAY_GAME_DATA_2026-07-22.md`](RS50_FEATURE_8130_DISPLAY_GAME_DATA_2026-07-22.md).

## Interpretation

This capture distinguishes feature discovery from feature use. G HUB learned
that `0x18A2`, `0x8091`, and `0x8093` exist, but did not invoke any of them
during normal startup and RS50 detection.

The absence of candidate-feature calls, 64-byte host output, or a sustained
large-payload stream is evidence against G HUB sending a Dynamic OLED frame at
startup. It does not prove that these features can never carry display data:
an external telemetry producer or an active game integration may be required
before a Dynamic transport is exercised.

No request from this capture may be replayed without separate documentation,
safety review, and explicit approval.

## Offline Batch Analyzer Corroboration

The capture was later passed in memory through the explorer's offline batch
analyzer. No complete report export was written to the repository. It parsed
788 reports without invalid lines:

| Direction | Report ID | Count |
|---|---:|---:|
| Host to device | `0x10` | 378 |
| Host to device | `0x11` | 3 |
| Device to host | `0x11` | 171 |
| Device to host | `0x12` | 236 |

The grouped host headers independently reproduced the earlier device `0x01`
result: runtime feature indices `0x00`, `0x01`, `0x02`, `0x03`, and `0x05`
were present, while `0x09`, `0x0E`, and `0x0F` were absent. This confirms that
the enumerated display candidates were not invoked during the startup session.

After its FeatureSet reconstruction was added, the analyzer also reproduced
the base mapping `runtime 0x12 -> feature 0x8130` and counted zero operational
host requests to it.

The analyzer found 171 device reports with an exact preceding host header
match. Another 210 host HID++ requests had no exact match under the conservative
rule requiring the same device, feature, function, and software ID. Some use
the broadcast device index `0xFF`, so this number must not be interpreted as
210 missing physical responses.
