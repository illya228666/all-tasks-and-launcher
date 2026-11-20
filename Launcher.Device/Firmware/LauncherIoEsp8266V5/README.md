# Launcher IO v5 — два разряда с общим катодом

| Контакт индикатора | NodeMCU |
| --- | --- |
| A обоих разрядов | D1 |
| B обоих разрядов | D2 |
| C обоих разрядов | D3 |
| D обоих разрядов | D4 |
| E обоих разрядов | D5 |
| F обоих разрядов | D6 |
| G обоих разрядов | D7 |
| Общий катод левого разряда | D0 |
| Общий катод правого разряда | D8 |

Семь общих линий A–G подключаются через отдельные токоограничивающие резисторы.
Сегмент включается уровнем HIGH, выбранный катод — уровнем LOW. Прошивка
перебирает только горящие сегменты. Поэтому через катод в каждый момент течёт
ток одного сегмента, а цифры с меньшим количеством горящих сегментов выглядят ярче.
Подберите резисторы по индикатору и пиковому току GPIO.
[Параметры GPIO ESP8266EX](https://documentation.espressif.com/0a-esp8266ex_datasheet_en.pdf)

При обычном сбросе ESP8266 проверяет D3 (GPIO0) и D4 (GPIO2) на HIGH,
а D8 (GPIO15) на LOW. Подключённый индикатор должен сохранять эти уровни
во время загрузки. [Режимы загрузки ESP8266](https://arduino-esp8266.readthedocs.io/en/3.1.2/boards.html#boot-messages-and-modes)

## Управление

- Цифровой режим показывает 00–99, десятки слева, единицы справа. `SET` останавливает анимацию.
- `UP`, `DOWN` и `BOUNCE` перебирают 00–99 с шагом 100–2000 мс.
- `TEST` проходит A–G слева, затем справа, показывает все сегменты обоих разрядов и перебирает 00–99.
- Ручной режим `SEG mask` управляет сегментами правого разряда. Левый разряд в этом режиме погашен.
- `BRIGHT 1`…`BRIGHT 100` задаёт долю времени свечения сегмента в его слоте. По умолчанию 100% слота.
- Для задания конкретного числа 10–99 отправьте `SET` по USB Serial. Слайдер существующего ESP Controller задаёт 0–9.

## Serial protocol

115200 baud, 8N1, LF (CRLF accepted).

```text
HELLO           -> LAUNCHER_IO 5
POLL            -> DISPLAY 4 100 HOLD 500 102
SET 42          -> OK SET
POLL            -> DISPLAY 42 100 HOLD 500 91
SEG 65          -> OK SEG        # A + G ; enters MANUAL mode
POLL            -> DISPLAY 42 100 MANUAL 500 65
MODE HOLD       -> OK MODE       # exit MANUAL; display 42
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

Скетч `LauncherIoEsp8266V5.ino` собирается для **NodeMCU 1.0 (ESP-12E Module)**.
Его можно загрузить через ESP Controller или через Arduino IDE после освобождения COM-порта.

```powershell
arduino-cli compile --fqbn esp8266:esp8266:nodemcuv2 Launcher.Device/Firmware/LauncherIoEsp8266V5
```
