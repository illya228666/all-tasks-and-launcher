# Launcher IO v5 · 2-digit 7-segment Live Control

A standalone ESP8266/NodeMCU firmware for a **two-digit, seven-segment display**
(with or without unused decimal points). Windows does not refresh the display:
the ESP multiplexes both digits continuously and runs the animations locally.

## Before flashing / vor dem Flashen

The provided Fritzing screenshot is not enough to determine the display's
**exact part number, common-anode vs. common-cathode polarity, or the two common
digit control pins**. The seven resistors suggest shared segment lines.

**BoardConfig.h is an example mapping, not a verified map of the screenshot.**
Match the seven segment A–G leads and two digit enables against your actual
part's datasheet before applying power. The default map is:

| Function | Example NodeMCU pin |
| --- | --- |
| A B C D E F G | D1 D2 D3 D5 D6 D7 D8 |
| Tens/ones enable | D0 / D4 |
| Segment active | HIGH (common cathode example) |
| Digit active | LOW (common cathode example) |

Every segment needs its own current-limiting resistor. A selected digit
may conduct the current of **up to seven LEDs**, so its common pin needs an
appropriately rated transistor/driver rather than a bare ESP GPIO. Check
the driver polarity and current limits before flashing. GPIO0 (D3),
GPIO2 (D4), and GPIO15 (D8) are boot-strap pins; the external circuit must
not force invalid boot levels. Brightness starts at 20% duty cycle; this is
**not** a substitute for correct resistor values/drivers.

## Demo

* Live slider and ± buttons: set 00–99; new manual value stops animation.
* Autonomous UP / DOWN / BOUNCE modes; speed 100–2000 ms per step.
* BRIGHT 1–100 for PWM duty cycle (requires safe electrical design).
* TEST: each individual segment on each digit (~2.8 s), full 88 (~0.75 s),
  then 00→99 (~3 s); returns to 42.
* WPF mirror reads POLL and never invents hardware state.

## Serial protocol

115200 baud, 8N1, LF (CRLF accepted).

```text
HELLO          -> LAUNCHER_IO 5
POLL           -> DISPLAY 42 20 HOLD 500
SET 17         -> OK SET
BRIGHT 20      -> OK BRIGHT
SPEED 300      -> OK SPEED
MODE UP        -> OK MODE
MODE DOWN      -> OK MODE
MODE BOUNCE    -> OK MODE
MODE HOLD      -> OK MODE
TEST           -> OK TEST
```

Malformed/unsupported commands return `ERR COMMAND`; too-long lines
return `ERR LINE_TOO_LONG`. Polling does not advance the animation;
the independent Arduino loop does.

Open `LauncherIoEsp8266V5.ino` in Arduino IDE (NodeMCU 1.0 / ESP-12E)
and upload after releasing the COM port in ESP Controller.

Build (with ESP8266 Arduino core installed):
```powershell
arduino-cli compile --fqbn esp8266:esp8266:nodemcuv2 Launcher.Device/Firmware/LauncherIoEsp8266V5
dotnet build EspController/EspController.csproj -c Release
```

Hardware validation: confirm stable 00 and 88, each of the 14 segments in
TEST, 0 and 99 in Live Control, rapid slider changes, animations while USB
is disconnected, then reconnection. Software cannot prove a schematic works
without testing it on the physical board.
