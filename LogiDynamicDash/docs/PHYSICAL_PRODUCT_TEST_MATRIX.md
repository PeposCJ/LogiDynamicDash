# Physical Product Test Matrix

This is the release-validation matrix for the `0.3.0-alpha` daily-use
runtime. It records expected coverage, not experimental protocol research.
Raw USB captures and historical discovery notes remain on the research branch.

## Preconditions

- Use the packaged build from the candidate commit.
- Set the RS50 home screen to `Dynamic`.
- Close G HUB before selecting **Start dashboard**.
- Confirm steering, pedals, buttons, FFB, and rev LEDs are normal before and
  after every session.
- USBPcap is not required unless a new protocol regression appears.

## Core lifecycle

| Scenario | Expected result |
|---|---|
| Start before RS50 is awake | GUI waits; no crash |
| Wake or reconnect RS50 | OLED connects within the retry window |
| Start before iRacing | OLED connects and GUI waits for telemetry |
| Enter and leave the car | Telemetry resumes without restarting the app |
| Change iRacing session | Car and category update automatically |
| Stop from GUI | HID streams close and status becomes stopped |
| Open G HUB after Stop | G HUB can reclaim the wheel normally |

## Driving coverage

Run at least one normal session for each available category:

| Category | Primary checks |
|---|---|
| Sports Car | speed, gear, RPM, brake bias, last lap |
| Formula Car | high-RPM scaling, gear, speed, last lap |
| Oval | stable compact layout, speed, gear, last lap |
| Dirt Oval | readability while steering rapidly, reconnect |
| Dirt Road | rapid gear changes, RPM, speed, last lap |

For every category confirm:

- the large bar follows RPM;
- the small bar follows speed;
- KMH or MPH matches the selected setting;
- `LAST LAP` appears for the configured duration;
- the detected car and category are correct;
- no unexpected steering, torque, FFB, LED, input, or connection behavior.

## Endurance and recovery

- Run one 30-minute session with ordinary driving.
- Allow the wheel to sleep, then wake it and confirm automatic recovery.
- Disconnect and reconnect the wheel once while stationary.
- Move from pits to track and back without restarting LogiDynamicDash.
- Close one iRacing session and start another with a different category.

## Compatibility statement

Passing this matrix validates the tested RS50 hardware and firmware only.
Logitech Pro Wheel support remains unverified until tested on physical
hardware.
