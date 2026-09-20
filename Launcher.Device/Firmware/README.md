# Launcher IO firmware / Прошивки

| Version | HELLO | POLL input | Output |
| --- | --- | --- | --- |
| v1 | LAUNCHER_IO 1 | INPUT NONE / BUTTON_PRESSED | INDICATOR ON/OFF → OK INDICATOR |
| v2 | LAUNCHER_IO 2 | INPUT NONE / LEFT_PRESSED / RIGHT_PRESSED / BOTH_PRESSED | LEDS 0..7 → OK LEDS |

Each input name in the table has the `INPUT ` prefix. Commands end with LF; CRLF is accepted.
v1 firmware remains unchanged. v2 has no desktop action bindings: all three input events are diagnostic only.
The v2 LED API accepts red=1, yellow=2, green=4; indicator is a v1-only capability.

RU: Версия выбирается при HELLO. GPIO и полярность принадлежат только BoardConfig.h;
изменение проводов не требует новой версии протокола. Несовместимое изменение обмена требует новой версии.
DE: HELLO wählt die Version. GPIO und Polarität gehören nur zu BoardConfig.h;
andere Verdrahtung braucht keine neue Protokollversion, inkompatibler Nachrichtenaustausch schon.

See [v2 configuration and semantics](LauncherIoEsp8266V2/README.md).
