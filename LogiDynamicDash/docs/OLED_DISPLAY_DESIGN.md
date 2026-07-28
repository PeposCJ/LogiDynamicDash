# OLED Display Design

This document defines the intended behavior of the Dynamic OLED display in LogiDynamicDash.

The goal is not to show every available telemetry value. The display should present only information that can be understood with a quick glance while driving.

Protocol research has replaced the original framebuffer assumption. The first
implementation targets firmware-rendered Layout J: four centered text fields
with maximum lengths `19/10/19/10`. The application now produces those exact
four presentation lines offline; physical output is still unverified.

## Design principles

- Keep the normal driving screen simple.
- Show no more than two primary values at the same time.
- Use large text for the most important value.
- Avoid showing information already represented effectively by the wheel LEDs.
- Use temporary screens for adjustments and lap information.
- Use overlays for brief driving-assistance interventions.
- Use full-screen alerts only for important conditions.
- Do not depend on color because the OLED is monochrome.
- Respect the recovered Layout J text limits rather than assuming pixel access.
- Allow users to disable optional alerts and overlays.

## Normal driving screen

The first universal layout should display:

```text
SPEED
186 KMH
GEAR
4
```

Primary value:

- Gear

Secondary value:

- Speed

The initial formatter uses km/h. A later setting may select mph, but both units
must never be shown at the same time.

Numeric RPM should not be displayed permanently because the wheel LEDs can already represent engine speed more effectively.

## Temporary adjustment screens

When the driver changes an adjustable setting, the OLED should temporarily replace the normal screen.

Example:

```text
BRAKE BIAS
52.3%


```

Initial behavior:

- Show the adjusted value immediately.
- Keep it visible for approximately two seconds.
- Return automatically to the normal driving screen.
- Restart the timer if the value changes again.

Possible future adjustments:

- Brake bias
- Traction control level
- ABS level
- Front anti-roll bar
- Rear anti-roll bar
- Engine map

Not every adjustment is available in every car.

## Lap information

When the driver completes a lap, show the last lap time temporarily.

Example:

```text
LAST LAP
1:32.481


```

Initial behavior:

- Show the last lap for approximately three seconds.
- Return automatically to the normal driving screen.

A future performance page may show lap delta:

```text
 DELTA
 -0.124
```

The plus or minus sign must remain visible because the display cannot rely on color.

## Pit information

While entering or driving through the pit lane, the display may prioritize current speed and the pit speed limit.

Example:

```text
 PIT   71
 LIMIT 72
```

The exact format and units will be configurable later.

## Driving-assistance indicators

Brief overlays should indicate when a driving assistance system is actively intervening.

Possible indicators:

```text
ABS
TC
SC
```

Where:

- `ABS` means anti-lock braking intervention.
- `TC` means traction control intervention.
- `SC` means stability control intervention.

These indicators should describe active intervention, not merely that the system is enabled or configured.

### Visual behavior

The recovered game-data protocol selects typed firmware layouts; it does not
expose a pixel framebuffer or partial-region inversion. The first version may
show a bounded text label in Layout J. Flashing, borders, and inverted regions
remain unsupported ideas unless another verified layout provides them.

### Minimum visibility time

Some telemetry events may last only a few milliseconds. A valid intervention should remain visible long enough for the driver to perceive it.

Initial target:

```text
Minimum indicator visibility: 150 ms
```

This value must be tested while driving and may become configurable.

### Simultaneous interventions

If more than one assistance system intervenes at the same time, the display should be able to show multiple indicators:

```text
ABS  TC
```

The interface should avoid rapidly replacing one indicator with another.

### Telemetry confidence

Each indicator must have a confidence level:

- **Direct:** iRacing provides a clear active-intervention value.
- **Inferred:** the program estimates intervention from multiple telemetry values.
- **Unavailable:** no reliable signal has been identified.

The application must not label an inferred value as confirmed without validation.

Current research targets:

| System | Intended behavior | Status |
|---|---|---|
| ABS | Show while ABS is actively intervening | Direct telemetry must be validated |
| TC | Show while traction control is actively intervening | Signal requires research and validation |
| SC | Show while stability control is actively intervening | Availability requires research |

## Full-screen priority alerts

Important conditions may replace all normal information temporarily.

Examples:

```text
 YELLOW
```

```text
 PIT LIMIT
```

```text
 LOW FUEL
```

```text
 DISCONNECTED
```

Provisional priority order:

1. Critical connection or device errors
2. Race-control and pit alerts
3. Low-fuel and vehicle warnings
4. Driving-assistance overlays
5. Temporary adjustment screens
6. Lap information
7. Normal driving screen

The final priority system must prevent lower-priority information from hiding a critical alert.

## Profile ideas

Different types of racing may require different normal screens.

Potential profiles:

- Road
- Oval
- Formula
- Endurance
- Custom

### Road and formula

Likely priorities:

- Gear
- Speed
- Delta
- Temporary car-setting changes

### Oval

Likely priorities:

- Delta
- Fuel remaining
- Estimated laps of fuel
- Pit speed
- Last lap

Gear may be less important during long periods in the same gear.

### Endurance

Likely priorities:

- Fuel remaining
- Estimated laps or time remaining
- Pit information
- Driver or stint information
- Warnings

Profiles are a future feature. The first version should remain universal and simple.

## Development console versus OLED

The development console and the OLED have different purposes.

### Development console

- Exact Layout J four-line preview
- Diagnostic framing around that preview
- Connection state and unavailable telemetry represented with bounded text

### OLED

- Information useful while driving
- Minimal text
- Large readable values
- Clear priorities
- No unnecessary diagnostic information

Values such as connection state and on-track state may remain visible in the console without being shown permanently on the OLED.

## First implementation scope

The first OLED implementation should aim for:

1. Normal gear and speed screen
2. Temporary brake-bias screen
3. Temporary last-lap screen
4. Connection/waiting alert
5. ABS intervention indicator, only if a reliable signal is confirmed
6. Configurable km/h or mph after physical text output is stable

## Open technical questions

The following still require controlled validation:

- Whether DirectInput outer command `4` activates the physical RS50 OLED
- Safe update frequency; the first live stage will begin at 5 Hz
- Whether G HUB must be running after its DirectInput driver is installed
- The Logitech PRO Wheel VID/PID, capability response, and compatibility
- Behavior when another game or application owns the display
- Visible font sizing and readability for all four Layout J rows
- The semantic intent of Logitech's other typed layouts C-I
- Four-minute fallback timing after the last successful update

These questions should be answered through official documentation, SDK access, controlled testing, or protocol research.
