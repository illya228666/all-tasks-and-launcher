# Launcher IO firmware / Прошивки

| Version | HELLO | POLL reply | Desktop output commands |
| --- | --- | --- | --- |
| v1 | LAUNCHER_IO 1 | INPUT NONE / BUTTON_PRESSED | INDICATOR ON/OFF |
| v2 | LAUNCHER_IO 2 | INPUT NONE / LEFT_PRESSED / RIGHT_PRESSED / BOTH_PRESSED | LEDS 0..7 |
| v3 | LAUNCHER_IO 3 | FADE 0..255 | none |

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
