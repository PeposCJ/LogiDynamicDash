# Offline Fault Matrix

This matrix records deterministic production tests. It does not replace the
postponed physical validation stages.

| Fault or pressure case | Expected behavior | Automated evidence |
|---|---|---|
| Missing or duplicate HID collection | Reject before opening a usable exchange | `Rs50OledDeviceExchangeTests` |
| Wrong usage or report length | Reject exact collection contract | `Rs50OledDeviceExchangeTests` |
| Short read | Fail the transaction; never retry its write | `Rs50OledDeviceExchangeTests` |
| Sixteen unrelated responses | Stop bounded read loop after one write | `Rs50OledDeviceExchangeTests` |
| Discovery transport failure | Permanently fault session | `Rs50OledSessionTests` |
| Invalid layout acknowledgement | Permanently fault session | `Rs50OledSessionTests` |
| Changed frame inside 200 ms | Queue latest ordinary frame | `Rs50OledFrameSchedulerTests` |
| Disconnect frame inside 200 ms | Preserve critical frame ahead of ordinary telemetry | `Rs50OledFrameSchedulerTests` |
| Moving, missing, negative, or non-finite on-track speed | Fault sink before OLED send | `Rs50OledDisplaySinkTests` |
| Display send failure | Enter Faulted; reject retry and reopen | `Rs50OledDisplaySinkTests` |
| Corrupt, duplicate, unknown, oversized, or out-of-range configuration | Reject before physical session construction | `Rs50OledConfigurationFileTests` |
| Corrupt, ambiguous, backward-time, or non-finite replay | Reject offline input | `TelemetryReplayTests` |
| Diagnostic storage write failure | Propagate safe failure; never mask original transport failure | `SanitizedRs50OledDiagnosticsTests` |
| Requested cancellation | Stop and dispose cleanly | `LogiDynamicDashApplicationTests` |
| Concurrent telemetry/status callbacks | Serialize display rendering | `LogiDynamicDashApplicationTests` |
| One million virtual submissions | Remain single-consumer without a pending leak | `Rs50OledFrameSchedulerTests` |

There is deliberately no automatic reopen or reconnect test because production
does not implement either behavior. Recovery after a fault requires disposal
and a new explicitly armed process.
