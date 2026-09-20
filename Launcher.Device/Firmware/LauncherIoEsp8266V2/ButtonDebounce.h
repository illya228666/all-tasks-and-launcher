#pragma once
#include <stdint.h>

class ButtonDebounce
{
    static const uint32_t debounceMs = 30;
    bool raw = false;
    bool stable = false;
    uint32_t changedAt = 0;
public:
    void reset(bool value, uint32_t now) { raw = stable = value; changedAt = now; }
    void update(bool value, uint32_t now)
    {
        if (raw != value) { raw = value; changedAt = now; }
        if (raw != stable && uint32_t(now - changedAt) >= debounceMs) stable = raw;
    }
    bool pressed() const { return stable; }
};
