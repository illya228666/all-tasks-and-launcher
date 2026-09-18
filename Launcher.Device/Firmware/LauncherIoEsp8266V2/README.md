# Launcher IO v2

Board profile: **NodeMCU 1.0 (ESP-12E Module)**.

This version adds two debounced/chorded buttons and three host-controlled LEDs.
A short single-button gesture produces exactly one left/right event. Pressing both
buttons within the 90 ms chord window produces one combined event instead of two
single events.

The desktop side identifies this firmware by `LAUNCHER_IO 2` and automatically
uses the v2 event set.


## Final pin/input configuration

- LEDs: D1 / D2 / D3
- left button: D6, active LOW with `INPUT_PULLUP`
- right button: D7, active HIGH with external pull-down

The debounce/chord layer compares each button against its own pressed level, so
LEFT/RIGHT/BOTH events work even though the two inputs use opposite polarities.
