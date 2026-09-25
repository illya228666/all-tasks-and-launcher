# Launcher IO v3 — ESP8266 / NodeMCU 1.0

v3 is a clean rebuild for the fade lesson. It does **not** preserve the v2 buttons,
D1-D3 LEDs, input queue or `LEDS` command.

## Hardware

| Signal | Pin | Convention |
| --- | --- | --- |
| Potentiometer wiper | A0 | analog input |
| Fade LED | D4 / GPIO2 | external LED active HIGH by default |

Connect the potentiometer outer legs to the board's analog reference range and GND,
and the middle/wiper leg to A0. `analogRead(A0)` produces `0..1023`; firmware maps
that directly to `0..255` by dividing by four.

`BoardConfig.h` contains only A0, D4 and LED polarity. If the NodeMCU built-in D4
LED is used instead, set `fadeLedActiveLow = true`.

GPIO2 is a boot-strap pin, so an external circuit must not hold D4 LOW while the
ESP8266 boots.

## Fade

The potentiometer is the source of truth:

```text
A0:   0 ................. 1023
fade: 0 ................. 255
```

The same fade value is used simultaneously for:

1. PWM brightness on D4.
2. The value reported to Launcher.

There is no autonomous fade animation and `HELLO` does not reset the value.

## Protocol

```text
HELLO
LAUNCHER_IO 3

POLL
FADE 127
```

`POLL` returns exactly one current fade value from `0` to `255`. There are no
v3 button events and no desktop-to-board output commands.

Launcher maps fade `0` to the current detached-hat size and fade `255` to twice
that size. An attached hat keeps its normal size. Disconnecting the v3 controller
resets the desktop multiplier to the minimum.
