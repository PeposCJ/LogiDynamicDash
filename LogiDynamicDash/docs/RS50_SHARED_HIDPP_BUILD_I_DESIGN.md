# RS50 Shared HID++ iRacing Coexistence Build I

## Status

Build I compiles a separate stationary-coexistence executable but has not been
run against the physical RS50. No Build I device enumeration, stream open,
request, or report transmission has occurred.

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

## Future Physical I1 Trial

Compiling Build I does not authorize running it.

Before starting USBPcap:

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

Physical and offline acceptance requires:

- all five counters visibly render in order;
- centering and LED response remain equivalent before and after;
- one discovery plus exactly five setters and acknowledgements;
- display writes use only interface 1 endpoint-0 HID `SET_REPORT`;
- no `RESET_ALL → SET_GLOBAL_GAINS → RESET_ALL` lifecycle around Build I;
- no discontinuity attributable to the display sequence in legitimate
  simulator FFB/LED traffic;
- no wheel movement, torque impulse, input loss, or simulator disconnect.

Moving-car use, live telemetry, and a full lap remain prohibited.
