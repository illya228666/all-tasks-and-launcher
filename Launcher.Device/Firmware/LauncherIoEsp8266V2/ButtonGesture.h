#pragma once
#include <stdint.h>

// One event per gesture. BOTH requires overlapping presses within the chord window.
class ButtonGesture
{
    static const uint32_t chordWindowMs = 90;
    bool waitForRelease = true;
    uint8_t first = 0;
    uint32_t startedAt = 0;
public:
    void reset() { waitForRelease = true; first = 0; }
    uint8_t update(uint8_t mask, uint32_t now)
    {
        if (waitForRelease)
        {
            if (mask == 0) waitForRelease = false;
            return 0;
        }
        if (first == 0)
        {
            if (mask == 0) return 0;
            first = mask;
            startedAt = now;
        }
        uint8_t result = 0;
        if (mask == 3 && uint32_t(now - startedAt) <= chordWindowMs) result = 3;
        else if (mask == 0 || uint32_t(now - startedAt) >= chordWindowMs) result = first;
        if (result != 0)
        {
            first = 0;
            waitForRelease = mask != 0;
        }
        return result;
    }
};
