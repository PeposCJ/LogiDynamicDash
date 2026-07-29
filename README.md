# LogiDynamicDash
Community telemetry display for the Dynamic OLED screen on Logitech PRO Racing Wheel and RS50.

The production branch now contains an offline-only, strictly typed encoder for
the ten confirmed firmware-rendered OLED layouts A-J. It discovers public
HID++ feature `0x8130` at runtime, validates exact acknowledgements, and does
not expose arbitrary feature IDs, functions, report bytes, graphics, fonts, or
device access.

Physical HID transport is intentionally not connected to the application yet.
The independently validated research remains separate from production code,
and moving-car hardware validation is postponed.
