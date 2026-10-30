#include "BoardConfig.h"

// LAUNCHER_IO 5: standalone two-digit display (00..99).
// USB commands adjust state; multiplexing and animations remain on the ESP.
// Each LED segment needs a resistor; digit commons need appropriately rated
// switches/drivers. Do NOT wire a whole digit's return current to a bare GPIO.
const uint8_t digitPatterns[10] = {
  0b00111111, 0b00000110, 0b01011011, 0b01001111, 0b01100110,
  0b01101101, 0b01111101, 0b00000111, 0b01111111, 0b01101111
};
enum DisplayMode { HOLD, UP, DOWN, BOUNCE, TEST };
DisplayMode mode = HOLD;
uint8_t value = 42;
uint8_t brightness = defaultBrightness;
uint16_t intervalMs = defaultIntervalMs;
bool ascending = true;
uint32_t previousStepMs = 0, testStartMs = 0;
uint32_t previousSlotUs = 0;
uint8_t currentDigit = 0;
bool lit = false;

char line[32];
uint8_t lineLength = 0;
bool tooLong = false;

void digitsOff() {
  for (uint8_t i = 0; i < 2; ++i)
    digitalWrite(digitPins[i], digitActiveHigh ? LOW : HIGH);
}
void segmentsOff() {
  for (uint8_t i = 0; i < 7; ++i)
    digitalWrite(segmentPins[i], segmentActiveHigh ? LOW : HIGH);
}
void segmentsWrite(uint8_t mask) {
  for (uint8_t i = 0; i < 7; ++i) {
    bool on = (mask & (1U << i)) != 0;
    digitalWrite(segmentPins[i], on == segmentActiveHigh ? HIGH : LOW);
  }
}
void refreshDisplay() {
  uint32_t now = micros();
  if ((uint32_t)(now - previousSlotUs) >= digitSlotUs) {
    digitsOff(); // blank before changing the shared segment bus (no ghosting)
    previousSlotUs = now;
    currentDigit ^= 1;
    uint8_t digit = currentDigit == 0 ? value / 10 : value % 10;
    uint8_t mask = digitPatterns[digit];
    if (mode == TEST) {
      uint32_t elapsed = millis() - testStartMs;
      if (elapsed < 14UL * 200) {
        uint8_t step = elapsed / 200;
        // Walk every segment separately on each digit.
        mask = (currentDigit == step / 7) ? (1U << (step % 7)) : 0;
      } else if (elapsed < 14UL * 200 + 750) {
        mask = 0x7F; // full 88 for a short visual check
      }
    }
    segmentsWrite(mask);
    digitalWrite(digitPins[currentDigit], digitActiveHigh ? HIGH : LOW);
    lit = true;
  }
  uint32_t onTime = (digitSlotUs * (uint32_t)brightness) / 100;
  if (lit && (uint32_t)(now - previousSlotUs) >= onTime) {
    digitsOff();
    lit = false;
  }
}
void advance() {
  uint32_t now = millis();
  if (mode == TEST) {
    uint32_t elapsed = now - testStartMs;
    if (elapsed >= 14UL * 200 + 750 + 3000) {
      mode = HOLD;
      value = 42;
      previousStepMs = now;
    } else if (elapsed >= 14UL * 200 + 750) {
      value = (elapsed - (14UL * 200 + 750)) / 30;
    } else if (elapsed >= 14UL * 200) value = 88;
    return;
  }
  if (mode == HOLD || (uint32_t)(now - previousStepMs) < intervalMs) return;
  previousStepMs += intervalMs;
  // Clamp the catch-up work if the serial connection has stalled.
  if ((uint32_t)(now - previousStepMs) >= intervalMs) previousStepMs = now;
  if (mode == UP) value = (value + 1) % 100;
  if (mode == DOWN) value = value == 0 ? 99 : value - 1;
  if (mode == BOUNCE) {
    if (value == 99) ascending = false;
    if (value == 0) ascending = true;
    value = ascending ? value + 1 : value - 1;
  }
}
bool number(const char* text, int low, int high, int &out) {
  if (*text == 0) return false;
  long v = 0;
  for (const char* p = text; *p; ++p) {
    if (*p < '0' || *p > '9') return false;
    v = v * 10 + *p - '0';
    if (v > high) return false;
  }
  if (v < low) return false;
  out = (int)v;
  return true;
}
void command(const char* request) {
  if (!strcmp(request, "HELLO")) { Serial.println("LAUNCHER_IO 5"); return; }
  if (!strcmp(request, "POLL")) {
    Serial.print("DISPLAY "); Serial.print(value);
    Serial.print(' '); Serial.print(brightness);
    Serial.print(' ');
    Serial.print(mode == HOLD ? "HOLD" : mode == UP ? "UP" :
                 mode == DOWN ? "DOWN" : mode == BOUNCE ? "BOUNCE" : "TEST");
    Serial.print(' '); Serial.println(intervalMs);
    return;
  }
  int n;
  if (!strncmp(request, "SET ", 4) && number(request + 4, 0, 99, n)) {
    value = (uint8_t)n; mode = HOLD; previousStepMs = millis();
    Serial.println("OK SET"); return;
  }
  if (!strncmp(request, "BRIGHT ", 7) && number(request + 7, 1, 100, n)) {
    brightness = (uint8_t)n; Serial.println("OK BRIGHT"); return;
  }
  if (!strncmp(request, "SPEED ", 6) && number(request + 6, 100, 2000, n)) {
    intervalMs = (uint16_t)n; previousStepMs = millis();
    Serial.println("OK SPEED"); return;
  }
  if (!strncmp(request, "MODE ", 5)) {
    const char* chosen = request + 5;
    if (!strcmp(chosen, "HOLD")) mode = HOLD;
    else if (!strcmp(chosen, "UP")) mode = UP;
    else if (!strcmp(chosen, "DOWN")) mode = DOWN;
    else if (!strcmp(chosen, "BOUNCE")) { mode = BOUNCE; ascending = value != 99; }
    else { Serial.println("ERR COMMAND"); return; }
    previousStepMs = millis();
    Serial.println("OK MODE"); return;
  }
  if (!strcmp(request, "TEST")) {
    mode = TEST; testStartMs = millis(); Serial.println("OK TEST"); return;
  }
  Serial.println("ERR COMMAND");
}
void processSerial() {
  for (uint8_t i = 0; i < 48 && Serial.available(); ++i) {
    char ch = (char)Serial.read();
    if (ch == '\r') continue;
    if (ch == '\n') {
      if (tooLong) Serial.println("ERR LINE_TOO_LONG");
      else if (lineLength) { line[lineLength] = 0; command(line); }
      tooLong = false; lineLength = 0; continue;
    }
    if (tooLong) continue;
    if (lineLength >= sizeof(line) - 1) { tooLong = true; lineLength = 0; }
    else line[lineLength++] = ch;
  }
}
void setup() {
  // Set inactive output levels before enabling GPIO drivers.
  for (uint8_t i = 0; i < 2; ++i) {
    digitalWrite(digitPins[i], digitActiveHigh ? LOW : HIGH);
    pinMode(digitPins[i], OUTPUT);
  }
  for (uint8_t i = 0; i < 7; ++i) {
    digitalWrite(segmentPins[i], segmentActiveHigh ? LOW : HIGH);
    pinMode(segmentPins[i], OUTPUT);
  }
  Serial.begin(115200);
  previousSlotUs = micros() - digitSlotUs;
  previousStepMs = millis();
}
void loop() {
  refreshDisplay();
  advance();
  processSerial();
  yield();
}
