# Launcher IO v6 — D0–D8

Прошивка для **NodeMCU 1.0 (ESP-12E Module)**. В ESP Controller выберите
**Launcher IO v6** и нажмите **Kompilieren & Flashen**. Если на плате ещё нет
распознаваемой прошивки Launcher IO, загрузите `LauncherIoEsp8266V6.ino` через
Arduino IDE. После подключения по USB панель **Digitale Ausgänge D0–D8**
показывает установленные платой уровни. Нажмите **LOW** или **HIGH** на нужном пине.

Все девять пинов настраиваются как выходы с логикой 3,3 В. После сброса D3 и D4
начинают с HIGH, остальные с LOW. Отключение GUI само по себе уровни не меняет.
Перед прошивкой проверьте, что внешняя схема не подаёт напряжение на эти выходы
и не мешает загрузке ESP8266. Для обычной загрузки D3 (GPIO0) и D4 (GPIO2)
должны быть HIGH, D8 (GPIO15) — LOW. [Документация ESP8266 Arduino Core](https://arduino-esp8266.readthedocs.io/en/3.1.2/boards.html#boot-messages-and-modes)

USB Serial, 115200 бод, окончание строки LF или CRLF:

```text
HELLO       -> LAUNCHER_IO 6
PIN D8 HIGH -> OK PIN D8 HIGH
PIN D8 LOW  -> OK PIN D8 LOW
POLL        -> PINS 24
```

В `PINS` бит 0 соответствует D0, бит 8 — D8. Установленный бит означает HIGH.
Прошивка возвращает заданные уровни, без измерения напряжения на пинах.
Ошибочная команда получает `ERR COMMAND` и не меняет выходы.
