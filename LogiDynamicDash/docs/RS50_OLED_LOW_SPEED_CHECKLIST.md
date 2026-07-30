# RS50 OLED Build L Low-Speed Checklist

## Scope

Build L is one 15-second controlled pit-lane validation after the successful
stationary production gate. It is not a full lap, public moving-car support,
permission to exceed 20 km/h, or permission to retry automatically.

The application only reads iRacing telemetry and writes OLED frames. It does
not command steering, pedals, FFB, LEDs, or vehicle controls.

## Safety envelope

- Start stopped in the pit lane with the brake held.
- Keep only the intended simulator session active.
- Target 5–15 km/h in a clear, straight pit-lane area.
- Never intentionally exceed 15 km/h.
- The application rejects telemetry above 20 km/h before sending another
  OLED frame and closes the hardware session.
- Stop the car before the 15-second application window ends.
- Abort by stopping the car immediately if FFB, LEDs, steering, pedals,
  buttons, connection, or OLED behavior becomes abnormal.

## Preparation

1. Use the reviewed Build L commit and its Release binary.
2. Close G HUB normally.
3. Close every other iRacing simulator or replay window.
4. Open the intended session and position the car stopped in a clear pit-lane
   area.
5. Select `Settings -> Home Screen -> Dynamic` on the RS50.
6. Confirm normal baseline FFB, LEDs, steering, pedals, buttons, connection,
   and no unexpected torque or movement.
7. Start a phone video that includes the OLED and, when practical, the
   simulator speed.
8. Wireshark and USBPcap are not required; the stationary gate already
   confirmed transport and exact acknowledgements. Preserve both sanitized
   local logs after the run.

## One authorized execution

From the repository root:

```powershell
.\LogiDynamicDash\bin\Release\net10.0\LogiDynamicDash.exe `
  --enable-rs50-oled-low-speed-trial `
  --confirm-ghub-closed `
  --confirm-iracing-running `
  --confirm-controlled-pit-lane `
  --confirm-rs50-dynamic-selected `
  --confirm-15-second-limit `
  --confirm-maximum-20-kmh `
  --acknowledge-stop-on-speed-limit `
  --config .\.tmp\stationary-test.json `
  --confirm-settings
```

During the single run:

1. Observe `0 KMH` and `N` while stopped.
2. Select first gear and roll smoothly at 5–15 km/h.
3. Verify speed, gear, and the graphical gauge update.
4. Stop and hold the brake before the application exits.

Do not repeat the command unless a new execution is explicitly authorized.

## Acceptance

Accept only if:

- the process completes its 15-second bound or safely rejects an exceeded
  limit;
- displayed speed and gear track iRacing without implausible values;
- the graphical gauge changes with telemetry;
- sanitized application telemetry never exceeds the armed 20 km/h limit
  during an accepted run;
- OLED results are acknowledged, unchanged, or rate-limited;
- FFB, LEDs, steering, pedals, buttons, and simulator connection remain
  normal;
- there is no unexpected motion, torque impulse, resistance change, or
  disconnect.

Passing Build L authorizes planning the next bounded driving stage. It does
not by itself enable a full-lap or unrestricted production mode.
