# Launcher IO firmware / Прошивки

| Version | HELLO | POLL reply | Desktop output commands |
| --- | --- | --- | --- |
| v1 | LAUNCHER_IO 1 | INPUT NONE / BUTTON_PRESSED | INDICATOR ON/OFF |
| v2 | LAUNCHER_IO 2 | INPUT NONE / LEFT_PRESSED / RIGHT_PRESSED / BOTH_PRESSED | LEDS 0..7 |
| v3 | LAUNCHER_IO 3 | FADE 0..255 | none |
| v4 | LAUNCHER_IO 4 | DICE sequence value (1..6) | none |
| v5 | LAUNCHER_IO 5 | DISPLAY value brightness mode interval | SET / BRIGHT / SPEED / MODE / TEST |

Commands end with LF; CRLF is accepted. v1 and v2 firmware remain unchanged.

v3 is deliberately independent from v2 hardware. Its board uses a potentiometer on
A0 and one LED on D4. The ESP8266 maps the A0 reading from 0..1023 to fade 0..255,
applies that value to D4 PWM and reports the same value on every POLL.

RU: Версия выбирается при HELLO. Несовместимый формат обмена получает новую версию;
поэтому v3 не обязан сохранять проводку или функции v2.
DE: HELLO wählt die Version. Ein inkompatibles Nachrichtenformat erhält eine neue
Version; v3 muss deshalb weder Verdrahtung noch Funktionen von v2 beibehalten.

See [v2 configuration and semantics](LauncherIoEsp8266V2/README.md) and
[v3 fade configuration and semantics](LauncherIoEsp8266V3/README.md).

[v4 autonomous dice](LauncherIoEsp8266V4/README.md) is independent of v1–v3.
It rolls every 5000 ms and lights a dice pattern on D1, D2, D3, D5, D6, D7, D8.
ESP Controller animates each new sequence for 2100 ms and lands on the ESP result.

[v5 two-digit display and hardware setup](LauncherIoEsp8266V5/README.md) supports right-digit 0–9, manual A–G segment toggles, autonomous animations and segment self-test.
