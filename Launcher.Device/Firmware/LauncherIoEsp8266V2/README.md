# Launcher IO v2 — ESP8266 / NodeMCU 1.0

Open LauncherIoEsp8266V2.ino together with its three adjacent headers in the Arduino IDE.
Select NodeMCU 1.0 (ESP-12E Module), ESP8266 core, 115200 baud serial protocol.

BoardConfig.h is the source of truth for wiring:

| Signal | Pin | Convention |
| --- | --- | --- |
| Green LED | D1 | active HIGH |
| Yellow LED | D2 | active HIGH |
| Red LED | D3 | active HIGH |
| Left button | D6 | active LOW, INPUT_PULLUP |
| Right button | D7 | active HIGH, external pull-down |

D3/GPIO0 is a boot-strap pin; this rewrite preserves the existing circuit.

Button levels settle for 30 ms. The 90 ms chord window starts at the first debounced press.
BOTH requires simultaneous debounced levels within that window. A short single press emits on
release; a held single emits when the window expires. No repeat occurs before full release.

At boot and after HELLO, wait for release before accepting another gesture. HELLO clears queued
and pending input and switches all LEDs off. Eight input events are buffered; when full, new events
are discarded while queued events keep their order. POLL consumes one event.

Lines longer than 63 characters are discarded through the next LF and receive ERR LINE_TOO_LONG.
Unknown commands and invalid masks receive ERR UNKNOWN_COMMAND. Time subtraction uses uint32_t
so debounce and gesture timing work across millis() rollover.

ButtonDebounce.h and ButtonGesture.h have no Arduino dependency. Final verification of this
rewrite is limited to the desktop build. ESP8266 compilation, upload and electrical operation
were not performed. The circuit and protocol remain documented here for a later hardware run.
