#pragma once

// Seven external LEDs, one at each position of a dice face:
// D1       D2
// D3  D5   D6
// D7       D8
// D3/GPIO0: 3.3V -> resistor -> LED -> GPIO (active LOW, preserves boot HIGH).
// Other LEDs: GPIO -> resistor -> LED -> GND (active HIGH).
const int diceLedPins[] = { D1, D2, D3, D5, D6, D7, D8 };
const bool diceLedActiveLow[] = { false, false, true, false, false, false, false };
const size_t diceLedCount = sizeof(diceLedPins) / sizeof(diceLedPins[0]);
static_assert(sizeof(diceLedActiveLow) / sizeof(diceLedActiveLow[0]) == diceLedCount, "Each LED needs a polarity.");

// WPF animation takes 2100 ms; hold each result for 5000 ms.
const uint32_t rollIntervalMs = 5000;
static_assert(rollIntervalMs >= 3000, "Allow the desktop dice animation to finish.");
