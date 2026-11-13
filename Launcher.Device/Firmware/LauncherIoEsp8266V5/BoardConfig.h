#pragma once
// EXAMPLE GPIO map: it is NOT verified against the Fritzing screenshot.
// Only the RIGHT digit is currently connected. The left digit pin is not
// configured as an output or driven. Change digitConnected[0] to true ONLY
// after physically connecting a properly rated digit driver.
// Physical segments: A(top), B(upper-right), C(lower-right), D(bottom),
// E(lower-left), F(upper-left), G(middle).
const uint8_t segmentPins[7] = { D1, D2, D3, D5, D6, D7, D8 };
const uint8_t digitPins[2] = { D0, D4 }; // left (unused), right (example!)
constexpr bool digitConnected[2] = { false, true };
const bool segmentActiveHigh = true; // verify common-anode/cathode hardware
const bool digitActiveHigh = false;  // verify digit driver polarity
const uint32_t digitSlotUs = 2500;   // 400 Hz single-digit refresh
const uint8_t defaultBrightness = 20;   // low default duty cycle
const uint16_t defaultIntervalMs = 500;
