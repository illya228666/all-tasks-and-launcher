#pragma once
// Example wiring ONLY: verify BOTH common pins and segment order against your
// display's datasheet/Fritzing project BEFORE connecting the ESP to power.
// Physical segments: A (top), B (upper right), C (lower right), D (bottom),
// E (lower left), F (upper left), G (middle).
const uint8_t segmentPins[7] = { D1, D2, D3, D5, D6, D7, D8 };
const uint8_t digitPins[2] = { D0, D4 }; // tens, ones: example, NOT inferred from photo
const bool segmentActiveHigh = true;    // true: common cathode; false: common anode
const bool digitActiveHigh = false;     // invert for common anode
const uint32_t digitSlotUs = 2500;      // 200 complete two-digit refreshes / second
const uint8_t defaultBrightness = 20;   // low default duty cycle
const uint16_t defaultIntervalMs = 500;
