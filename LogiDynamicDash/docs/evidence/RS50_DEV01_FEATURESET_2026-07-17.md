# RS50 Device 0x01 FeatureSet Evidence

## Session

- Date: 2026-07-17
- Device: Logitech RS50, VID `0x046D`, PID `0xC276`
- Host state: G HUB started while MI_01 COL02 was open for passive input
  monitoring
- Explorer behavior: input reads only; no output or feature reports sent
- Result: 167 input reports received, including 51 from device index `0x01`

The local raw log also contained device identity responses. Those responses are
intentionally excluded from this sanitized evidence note and must not be
committed.

## FeatureSet Responses

These responses have report ID `0x11`, device index `0x01`, FeatureSet index
`0x01`, and function/SW byte `0x1D`. Bytes 4-5 contain the feature ID, byte 6
contains the feature flags, and byte 7 contains the feature version.

| Feature ID | Flags | Version | Classification |
|---|---:|---:|---|
| `0x0011` | `0x00` | 0 | Public; semantics not assigned here |
| `0x00C3` | `0x00` | 1 | Public; SecureDFU in the upstream specification |
| `0x1602` | `0x00` | 0 | Public; unknown |
| `0x1807` | `0x70` | 4 | Hidden, engineering, and manufacturing-deactivatable |
| `0x180B` | `0x70` | 0 | Hidden, engineering, and manufacturing-deactivatable |
| `0x18A2` | `0x00` | 0 | Public; unknown OLED candidate |
| `0x18B1` | `0x70` | 0 | Hidden, engineering, and manufacturing-deactivatable |
| `0x1E00` | `0x40` | 0 | Hidden |
| `0x1E02` | `0x60` | 0 | Hidden and engineering |
| `0x1EB0` | `0x70` | 0 | Hidden, engineering, and manufacturing-deactivatable |
| `0x8091` | `0x00` | 0 | Public; per-key/LED matrix upstream |
| `0x8093` | `0x00` | 0 | Public; unknown OLED candidate |
| `0x9315` | `0x70` | 0 | Hidden, engineering, and manufacturing-deactivatable; unknown |

Feature flag interpretations follow the upstream protocol specification. A
public flag does not prove that a feature controls the OLED, and a candidate
classification does not authorize sending requests.

## Sanitized Raw Reports

```text
11 01 01 1D 00 11 00 00 00 00 00 00 00 00 00 00 00 00 00 00
11 01 01 1D 00 C3 00 01 00 00 00 00 00 00 00 00 00 00 00 00
11 01 01 1D 16 02 00 00 00 00 00 00 00 00 00 00 00 00 00 00
11 01 01 1D 18 07 70 04 00 00 00 00 00 00 00 00 00 00 00 00
11 01 01 1D 18 0B 70 00 00 00 00 00 00 00 00 00 00 00 00 00
11 01 01 1D 18 A2 00 00 00 00 00 00 00 00 00 00 00 00 00 00
11 01 01 1D 18 B1 70 00 00 00 00 00 00 00 00 00 00 00 00 00
11 01 01 1D 1E 00 40 00 00 00 00 00 00 00 00 00 00 00 00 00
11 01 01 1D 1E 02 60 00 00 00 00 00 00 00 00 00 00 00 00 00
11 01 01 1D 1E B0 70 00 00 00 00 00 00 00 00 00 00 00 00 00
11 01 01 1D 80 91 00 00 00 00 00 00 00 00 00 00 00 00 00 00
11 01 01 1D 80 93 00 00 00 00 00 00 00 00 00 00 00 00 00 00
11 01 01 1D 93 15 70 00 00 00 00 00 00 00 00 00 00 00 00 00
```

## Interpretation

The passive session independently confirms that device index `0x01` responds
on report ID `0x11`. It also confirms the upstream `0x8091`, `0x18A2`, and
`0x9315` observations and adds a public `0x8093` response not highlighted in
the earlier project correspondence.

The safest candidates for further offline research are `0x18A2` and `0x8093`
because both advertised public flags. `0x9315` advertised hidden, engineering,
and manufacturing-deactivatable flags and must not be probed without stronger
documentation and explicit approval.

Upstream reference:
[mescon/logitech-trueforce-linux-driver protocol specification](https://github.com/mescon/logitech-trueforce-linux-driver/blob/master/docs/PROTOCOL_SPECIFICATION.md#53-sub-device-addressing-hid-dev_idx-0x01--0x02--0x05)
