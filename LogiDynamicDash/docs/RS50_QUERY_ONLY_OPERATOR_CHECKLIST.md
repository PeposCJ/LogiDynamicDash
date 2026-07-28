# RS50 Query-Only Operator Checklist

Use this checklist for the first physical DirectInput validation. The test
transmits one documented support query. It does not contain a display setter,
raw HID write, layout query, or force-feedback effect. Build A3 sets the
standard joystick data format, acquires the RS50 exclusively only around the
single Escape call, and then releases it.

Do not combine this test with a layout query or text update.

## 1. Prepare the wheel

1. Connect and power the RS50 normally.
2. Close G HUB and confirm no G HUB update or agent process is restarting.
3. On the wheel, open `Settings -> HomeScreen -> Dynamic`.
4. Confirm Dynamic visibly shows the normal Test fallback.
5. Center the wheel and keep hands clear of the rim.
6. Record whether torque, LEDs, and wheel position are normal.

Stop if the visible starting state is different.

## 2. Verify the guarded build

From the repository root:

```powershell
.\scripts\Build-Rs50DirectInputBridge.ps1 -Configuration Release
```

Require all of these results:

- prerequisite checker reports `READY`;
- native export/import audit passes;
- safety tests say no DirectInput or HID device was opened;
- unarmed invocations are refused;
- `--describe` reports ABI `4`.

The build command is non-transmitting.

## 3. Start the evidence capture

1. Start a short baseline Wireshark/USBPcap capture on the interface containing
   VID `046D`, PID `C276`.
2. Do not start G HUB.
3. Wait five seconds without pressing wheel controls, then stop and save the
   baseline capture.
4. Start a second capture on the same interface. This is the query capture.
5. Note the RS50 USB device address for later device-scoped export and confirm
   it did not change between captures.

Raw captures remain local and are ignored by Git.

## 4. Arm exactly one support query

Only after the user explicitly says that the capture is running and Dynamic
still shows Test, execute:

```powershell
.\artifacts\native\Release\Rs50DirectInputQuery.exe `
  --query-display-support `
  --confirm-standard-data-format `
  --confirm-exclusive-acquire `
  --confirm-transmit-query
```

Do not repeat the command, even if it fails. Preserve the console output. The
guarded CLI prints UTC timestamps immediately before and after the DirectInput
call. The extractor preserves each USB frame's epoch time for correlation.

## 5. Observe and stop

1. For ten seconds, do not touch G HUB or any wheel control.
2. Record whether the OLED, torque, LEDs, or wheel position changed.
3. Stop and save the capture.
4. Record:
   - sanitized product name and VID/PID;
   - cooperative-level, data-format, Acquire, Escape, and Unacquire HRESULTs;
   - output capacity before/after;
   - raw output byte and interpreted support boolean;
   - RS50-scoped host/device traffic;
   - visible OLED result.

Any OLED change, force, wheel movement, failed data-format or Unacquire call,
non-boolean output byte, changed output capacity, unexpected transaction count,
or identity mismatch is a stop condition. Do not proceed to Layout J query in
the same session.

## 6. Inspect the saved capture offline

```powershell
.\scripts\Inspect-Rs50DirectInputQueryCapture.ps1 `
  -PcapPath .\capture.pcapng `
  -DeviceAddress <USB_DEVICE_ADDRESS> `
  -FromUtc "<QUERY_CALL_STARTED_UTC>" `
  -ToUtc "<QUERY_CALL_COMPLETED_UTC>"
```

This command only reads the saved capture. It extracts HOST/DEVICE HID reports
scoped to the selected USB address and feeds them to the offline batch
analyzer. It discovers feature/runtime mappings from the capture when present;
it does not assume runtime index `0x12` is universal. Record the native query
result and physical OLED observation separately.

Compare the query capture with the five-second baseline:

```powershell
.\scripts\Compare-Rs50DirectInputQueryCaptures.ps1 `
  -BaselinePcapPath .\baseline.pcapng `
  -QueryPcapPath .\query.pcapng `
  -DeviceAddress <USB_DEVICE_ADDRESS> `
  -QueryFromUtc "<QUERY_CALL_STARTED_UTC>" `
  -QueryToUtc "<QUERY_CALL_COMPLETED_UTC>"
```

The comparison ignores frame numbers and timestamps and reports positive exact
HID++ report-count deltas. A delta is only a difference: background traffic and
unequal capture durations can also produce one. Do not treat the comparison
alone as proof of OLED support or as authorization for a setter.

Use the two UTC values printed by the guarded CLI. Both bounds are required
when filtering; supplying only one or reversing them is rejected.

## 7. Decide the next build

- If the call is non-mutating and the traffic matches the documented support
  request, review and commit the evidence before separately approving Layout J
  capability query `12`.
- If anything differs, close the process and investigate statically. Do not
  add a setter or retry on the same DirectInput object.
