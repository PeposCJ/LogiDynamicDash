# LogiDynamicDash

Community project researching telemetry display support for the Dynamic OLED
screen on Logitech PRO Racing Wheel and RS50.

## Current status

- The main application reads iRacing telemetry and simulates the intended OLED
  layout in the console.
- The research application can passively monitor supported RS50 HID input
  collections and decode saved `0x11`/`0x12` reports without accessing HID
  hardware.
- Output to the physical Dynamic OLED has not been demonstrated and is not
  currently supported.
