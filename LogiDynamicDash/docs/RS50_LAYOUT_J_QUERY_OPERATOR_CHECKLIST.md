# RS50 Layout J Capability Query Checklist

Use this checklist only after the successful general-support evidence in
`RS50_DIRECTINPUT_QUERY_BUILD_A3_SUCCESS_2026-07-27.md`.

This stage queries Layout J capability with documented inner command `12`. It
does not compile a display setter, layout payload, force-feedback effect,
input-state read, raw HID write, or retry.

## 1. Verify the guarded build

From the repository root:

```powershell
.\scripts\Build-Rs50DirectInputBridge.ps1 -Configuration Release
```

Require:

- prerequisite checker `READY`;
- ABI `4`;
- exact six-export query-only surface;
- one `SetDataFormat`, one `Acquire`, one `Escape`, and two guarded
  `Unacquire` source paths;
- no setter is selected by this command, and there is no generic setter,
  polling, device-state read, or raw HID import;
- all incomplete and wrong confirmation sequences refused.

The build and its automated tests do not open the RS50.

## 2. Prepare the physical state

1. Keep `LGHUBUpdaterService` stopped and G HUB closed.
2. Wake the RS50.
3. Select `Settings -> HomeScreen -> Dynamic`.
4. Confirm Dynamic shows the normal Test fallback.
5. Center the rim, keep hands clear, and record normal torque/LED state.

Stop if any starting condition differs.

## 3. Capture baseline and query

1. Capture the RS50 USBPcap interface for five seconds without touching the
   wheel.
2. Stop and save the baseline.
3. Immediately start a second capture on the same interface.
4. Confirm the USB device address did not change.
5. Leave the second capture running.

## 4. Arm exactly one Layout J query

Only after the user explicitly authorizes the standard format, exclusive
acquisition, and one Layout J capability query, execute:

```powershell
.\artifacts\native\Release\Rs50DirectInputQuery.exe `
  --query-layout-j-support `
  --confirm-standard-data-format `
  --confirm-exclusive-acquire `
  --confirm-transmit-layout-query
```

The guarded call uses:

- outer Escape command `4`;
- inner query command `12`;
- 12-byte version-1 input;
- exact 10-byte output capacity;
- sentinel `0xA5` in every output byte;
- `c_dfDIJoystick2`;
- exclusive foreground Acquire;
- immediate Unacquire.

Do not repeat the command, even if it fails.

## 5. Validate and stop

Require all of the following before interpreting support:

- cooperative, data-format, Acquire, Escape, and Unacquire HRESULTs are zero;
- inner command is `12`;
- capacity remains `10 -> 10`;
- output byte zero is boolean `0` or `1`;
- output bytes 1-9 remain `A5`;
- no OLED, torque, LED, or wheel-position change occurs.

Observe for ten seconds, stop the capture, and save it. Any failed release,
changed trailing byte, physical change, unexpected transaction count, or
identity mismatch is a stop condition.

Static recovery predicts one feature-`0x8130` function-`0` request followed by
ten function-`1` descriptor requests when the layout cache is first populated.
Treat a different count as evidence to investigate, not permission to retry or
send a setter.

Analyze the saved capture offline before designing Build C.
