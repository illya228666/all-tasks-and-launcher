# Launcher IO firmware (ESP8266 / NodeMCU 1.0)

`LauncherIoEsp8266.ino` is the firmware counterpart for `SerialEspBoardController`.
It uses no extra Arduino libraries.

## Hardware assumptions

- Board profile: **NodeMCU 1.0 (ESP-12E Module)**
- Button input: `D1`
- Pressed level: `HIGH` (same behavior as the original test sketch)
- Indicator: `LED_BUILTIN` (active LOW on NodeMCU)
- Button mode: `INPUT`; the firmware deliberately does not enable an internal pull-up/pull-down.
- Serial speed: `115200`

## Protocol `LAUNCHER_IO 1`

The PC is the requester; the ESP only replies to commands. Button presses are debounced and queued until the PC polls them, so a short press is not lost between polls.

| PC -> device | Device -> PC |
| --- | --- |
| `HELLO` | `LAUNCHER_IO 1` |
| `POLL` | `INPUT NONE` or `INPUT BUTTON_PRESSED` |
| `INDICATOR ON` | `OK INDICATOR` |
| `INDICATOR OFF` | `OK INDICATOR` |

`HELLO` starts a fresh host session and clears stale queued button presses. This prevents opening a serial port from being interpreted as user input.

## Flashing

Open `LauncherIoEsp8266/LauncherIoEsp8266.ino` in Arduino IDE, select **NodeMCU 1.0 (ESP-12E Module)** and upload it normally. The desktop application discovers the matching serial device by the `HELLO` response, so the COM port is not hardcoded.
