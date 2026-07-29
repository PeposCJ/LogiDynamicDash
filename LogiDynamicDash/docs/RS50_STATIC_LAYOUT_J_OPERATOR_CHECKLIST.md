# RS50 Static Layout J Setter Checklist

This checklist governs exactly one Build C call. The payload is fixed:

```text
LOGIDYNAMICDASH
RS50
OLED LINK
TEST 1
```

There is no live telemetry, caller-controlled text, loop, SetIdle, raw HID
write, or other layout setter.

## 1. Preconditions

1. Keep G HUB and its agent/updater processes closed.
2. Wake the RS50 and keep it awake for the entire capture.
3. On the wheel select `Settings -> HomeScreen -> Dynamic`.
4. Confirm Dynamic visibly shows the Test fallback.
5. Do not touch the wheel after this confirmation unless a safety stop is
   required.

Any unexpected torque, LED, OLED, or wheel-position behavior is an immediate
stop condition.

## 2. Baseline

1. Start a new USBPcap capture on the RS50 controller.
2. Record the current RS50 USB address.
3. Capture at least five seconds without running the bridge.
4. Stop and save it as:

```text
2026-07-27_rs50_static_layout_j_baseline.pcapng
```

## 3. Setter Capture

Start a fresh capture on the same controller. Confirm again that the RS50 is
awake and Dynamic shows Test.

Do not run any earlier support query in this process. Execute exactly:

```powershell
.\artifacts\native\Release\Rs50DirectInputQuery.exe `
  --set-static-layout-j `
  --confirm-standard-data-format `
  --confirm-exclusive-acquire `
  --confirm-layout-j-static-text `
  --confirm-transmit-static-setter
```

Run it once only. Do not retry in the same capture, regardless of result.

## 4. Observe

For ten seconds after the call:

1. Read the complete console result without launching another command.
2. Observe whether the four fixed strings replace the Test fallback.
3. Observe LEDs, torque, and wheel position.
4. Do not use SetIdle or G HUB to clean up.
5. Stop and save the capture as:

```text
2026-07-27_rs50_static_layout_j_attempt_1.pcapng
```

Report the console output and exact physical result, including whether the
text persisted, disappeared, or changed after a wheel button press.

## 5. Stop Conditions

Stop without retrying if:

- any HRESULT is nonzero;
- product identity is not VID `046D`, PID `C276`;
- inner command is not `22`;
- the wheel moves, torque changes, or LEDs behave unexpectedly;
- USB analysis does not show the expected `0x8130` function-`3` transaction;
- the device response is unmatched or reports an error.

Only offline capture analysis may determine the next step. A visible static
result is required before any live telemetry API or repeated update loop is
implemented.
