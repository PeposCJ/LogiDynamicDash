# RS50 Shared HID++ iRacing Coexistence Build I

## Status

Build I compiles a separate stationary-coexistence executable. Its corrected,
high-buffer I1 capture succeeded on 2026-07-28: all five frames rendered,
iRacing centering and shift LEDs worked before and after, and the operator
observed no movement, torque impulse, input loss, or other side effect.

Build I does not reference the iRacing SDK and reads no telemetry. It only
changes the test precondition from H1: iRacing is running with the car stopped
in the pits.

`LogiDynamicDash` does not reference Build I, the physical transport, or
HidSharp. Building or running the dashboard cannot activate this route.

## Fixed Sequence

After eight exact ordered arming arguments, Build I performs one discovery and
exactly five fixed Layout J setters at 1 Hz:

```text
IRACING COEXIST
BUILD I
FRAME n OF 5
1 HZ
```

All five sends and four delays are spelled out in source. There is no execution
loop, retry, timer, task, telemetry, caller text, or caller-controlled rate,
count, duration, feature, function, layout, report ID, or raw data.

Any discovery, setter, acknowledgement, or delay failure closes both streams
and prevents every later frame.

## Arming Contract

All arguments must appear exactly in this order:

```text
--arm-rs50-shared-hidpp-coexistence
--confirm-ghub-closed
--confirm-iracing-running
--confirm-car-stationary-in-pits
--confirm-rs50-awake
--confirm-dynamic-selected
--confirm-usbpcap-running
--confirm-one-hz-five-fixed-frames
```

Run the source audit with:

```powershell
.\scripts\Test-Rs50SharedHidppCoexistenceSurface.ps1
```

## Offline Validation

The standard xUnit project compiles with the Build I tests, but local Windows
Code Integrity policy ID `{0283ac0f-fff1-49ae-ada1-8a933130cad6}` blocked
`testhost.exe` from dynamically loading the newly built unsigned test DLL
(events 3033/3077). The policy was not disabled or modified.

A separate fake-only verifier references the Build I runner and neutral
protocol, but not the physical transport. Direct execution passed:

- exact arming rejection before factory use;
- one discovery and five fixed frames;
- exactly four delays;
- fixed text and counters;
- stop and disposal on third-frame failure;
- stop and disposal on delay failure.

Run it with:

```powershell
dotnet run --project `
  .\Rs50SharedHidppCoexistence.Verification\Rs50SharedHidppCoexistence.Verification.csproj `
  -c Release --no-restore
```

## Physical I1 Trial — Passed

The validated procedure was:

1. close G HUB;
2. open iRacing and enter the car;
3. remain stopped in the pits, select neutral, and hold the brake;
4. confirm normal centering with one small, gentle steering displacement;
5. briefly rev in neutral and confirm the shift LEDs respond;
6. return to idle, keep the car stationary, and select Dynamic.

Start USBPcap and record at least 10 seconds of baseline traffic before the
single Build I execution. Keep the engine idling during the five OLED frames.
After Build I closes:

1. continue capturing;
2. repeat the same small centering check;
3. briefly rev in neutral and confirm the shift LEDs still respond;
4. capture at least 10 more seconds;
5. stop and save the capture without driving.

Every physical and offline acceptance condition passed:

- all five counters visibly render in order;
- centering and LED response remain equivalent before and after;
- one discovery plus exactly five setters and acknowledgements;
- display writes use only interface 1 endpoint-0 HID `SET_REPORT`;
- no `RESET_ALL → SET_GLOBAL_GAINS → RESET_ALL` lifecycle around Build I;
- no discontinuity attributable to the display sequence in legitimate
  simulator FFB/LED traffic;
- no wheel movement, torque impulse, input loss, or simulator disconnect.

Capture:

```text
2026-07-28_rs50_build_i_iracing_stationary_coexistence_attempt2.pcapng
```

The first Wireshark-managed capture visibly passed but dropped three of the
five setters amid dense TRUEFORCE traffic. It was not accepted as protocol
evidence. Attempt 2 used USBPcapCMD filtered to RS50 address 3 with a 128 MB
buffer and captured the complete sequence.

Attempt 2 contains exactly one discovery and five setters, with six exact
responses. Setter intervals were 1.013111, 1.010585, 1.004519, and 1.007545
seconds. Acknowledgements arrived within 2.358–12.096 ms.

The entire capture contains zero endpoint-0 control reports to runtime index
`0x10`, previously mapped to feature `0x8123` Force Feedback. During a
25-second window around Build I, host endpoint-`0x03` TRUEFORCE submissions
continued every second at 990–1000 transfers/s.

Runtime-`0x0B` rev-light operations appear during both the pre-check and
post-check, but not during the five idle OLED frames. There is no
`RESET_ALL → SET_GLOBAL_GAINS → RESET_ALL` lifecycle or simulator traffic
discontinuity attributable to Build I.

Moving-car use, live telemetry, and a full lap remain prohibited until a
separate bounded telemetry stage is implemented and validated offline.
