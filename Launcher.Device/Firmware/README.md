# Launcher IO firmware versions

The desktop receiver negotiates the firmware protocol with `HELLO` and selects
the matching input decoder automatically.

| Firmware | Arduino sketch | Identity | Inputs | Outputs |
| --- | --- | --- | --- | --- |
| v1 | `LauncherIoEsp8266/LauncherIoEsp8266.ino` | `LAUNCHER_IO 1` | one button | built-in indicator |
| v2 | `LauncherIoEsp8266V2/LauncherIoEsp8266V2.ino` | `LAUNCHER_IO 2` | left, right, both | red/yellow/green LEDs |

## Protocol v2

PC -> device:

- `HELLO`
- `POLL`
- `LEDS 0` ... `LEDS 7`

Device -> PC:

- `LAUNCHER_IO 2`
- `INPUT NONE`
- `INPUT LEFT_PRESSED`
- `INPUT RIGHT_PRESSED`
- `INPUT BOTH_PRESSED`
- `OK LEDS`

LED mask bits are: red = 1, yellow = 2, green = 4. Masks can be combined.

## v2 wiring

The v2 sketch follows `2xInput_GPIO.jpg`:

- green LED: D1
- yellow LED: D2
- red LED: D3
- right button: D6
- left button: D7

Buttons are active HIGH and use the external resistors shown on the breadboard.

D3 is GPIO0, which is an ESP8266 boot-strap pin. The sketch intentionally keeps
the supplied wiring. If the board starts in flashing mode, move the red LED signal
to a non-strap pin such as D5 and change `redLedPin` in the v2 sketch.
