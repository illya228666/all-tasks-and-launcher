# Launcher IO v5: Live Control / manual A–G

Current hardware has **only the RIGHT digit connected**. Firmware defaults to
`digitConnected = { false, true }` and NEVER configures the left digit GPIO.
The demo therefore counts **0–9** on the right, not 00–99.

## Physical safety and configuration

`BoardConfig.h` contains **example pins, not verified wiring**. Match A–G and
right-digit common to the actual module datasheet. Leave the left common pin
disconnected. Confirm correct common-anode/cathode polarity, seven individual
current-limiting resistors and a properly rated digit driver: seven lit
segments can exceed the allowable current of a bare GPIO. Verify boot straps
D3/GPIO0, D4/GPIO2, D8/GPIO15 are not driven to invalid levels at boot.
Do not rely on brightness as protection against excessive LED current.

When the left digit is physically wired and its driver confirmed, change
`digitConnected[0]` to true, then adjust Windows limits/preview for two digits.

## Live Control

- Live slider 0–9 and +/- buttons. SET stops animations.
- UP / DOWN / BOUNCE animations, 100–2000 ms steps, autonomous ESP loop.
- **Manual mode:** click A, B, C, D, E, F or G directly on the virtual RIGHT
  digit (or its A–G buttons). Each click toggles that individual LED segment.
  "Alle an", "Alle aus", and "Zahlenmodus" (back to HOLD).
- 1–100% brightness PWM, hardware limits permitting.
- TEST sequentially lights every one of the seven right-digit segments,
  lights all seven, then sweeps the values 0–9.
- POLL returns the actual right-segment pattern, used to synchronize WPF.

## Serial protocol

115200 baud, 8N1, LF (CRLF accepted).

```text
HELLO           -> LAUNCHER_IO 5
POLL            -> DISPLAY 4 20 HOLD 500 102
SET 7           -> OK SET
SEG 65          -> OK SEG        # A + G ; enters MANUAL mode
POLL            -> DISPLAY 7 20 MANUAL 500 65
MODE HOLD       -> OK MODE       # exit MANUAL; display 7
MODE UP         -> OK MODE
MODE DOWN       -> OK MODE
MODE BOUNCE     -> OK MODE
BRIGHT 20       -> OK BRIGHT
SPEED 300       -> OK SPEED
TEST            -> OK TEST
```

In `SEG mask`, bit 0=A (top), bit 1=B, bit 2=C, bit 3=D,
bit 4=E, bit 5=F, bit 6=G (middle); valid range is 0–127.
`DISPLAY` is: value, brightness%, mode, interval-ms, right-digit bitmask.
Malformed/unsupported commands return `ERR COMMAND` and too-long lines
`ERR LINE_TOO_LONG`.

Open `LauncherIoEsp8266V5.ino` with the ESP8266 Arduino core, confirm
`BoardConfig.h`, and flash after disconnecting COM in EspController.
Example build commands:

```powershell
arduino-cli compile --fqbn esp8266:esp8266:nodemcuv2 Launcher.Device/Firmware/LauncherIoEsp8266V5
dotnet build EspController/EspController.csproj -c Release
```

Verify A–G individually with low brightness, one connected digit only,
manual masks 0/1/127, automatic counting 0..9, and WPF synchronization.
