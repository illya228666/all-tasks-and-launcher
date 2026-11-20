#pragma once
constexpr uint8_t segmentPins[] = { D1, D2, D3, D4, D5, D6, D7 }; // shared A–G
constexpr uint8_t digitPins[] = { D0, D8 }; // left and right common cathodes
constexpr uint32_t segmentSlotUs = 350; // about 204 Hz with all 14 segments lit
constexpr uint8_t defaultBrightness = 100; // each segment can use at most one slot per frame
constexpr uint16_t defaultIntervalMs = 500;
