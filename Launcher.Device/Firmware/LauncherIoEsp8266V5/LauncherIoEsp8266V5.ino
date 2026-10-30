#include "BoardConfig.h"

// LAUNCHER_IO 5: right-digit-only by default, 0..9 + manual seven-segment mask.
// Animations and refreshing run on the ESP. Do not drive a whole digit's
// return current directly through a GPIO: use an appropriately rated driver.
const uint8_t digitPatterns[10] = {
  0b00111111, 0b00000110, 0b01011011, 0b01001111, 0b01100110,
  0b01101101, 0b01111101, 0b00000111, 0b01111111, 0b01101111
};
// Bit 0=A (top), 1=B, 2=C, 3=D, 4=E, 5=F, 6=G (middle).
enum DisplayMode { HOLD, UP, DOWN, BOUNCE, TEST, MANUAL };
DisplayMode mode = HOLD;
const uint8_t highestValue = digitConnected[0] ? 99 : 9;
const uint8_t enabledCount = (digitConnected[0] ? 1 : 0) + (digitConnected[1] ? 1 : 0);
uint8_t value = 4;
uint8_t manualMask = 0x66; // last entered mask, independent of numeric value
uint8_t rightVisibleMask = digitPatterns[4];
uint8_t brightness = defaultBrightness;
uint16_t intervalMs = defaultIntervalMs;
bool ascending = true;
uint32_t previousStepMs = 0, testStartMs = 0, previousSlotUs = 0;
uint8_t currentDigit = 1;
bool lit = false;
char line[32];
uint8_t lineLength = 0;
bool tooLong = false;

void digitsOff() {
  for (uint8_t i = 0; i < 2; ++i) {
    if (digitConnected[i]) digitalWrite(digitPins[i], digitActiveHigh ? LOW : HIGH);
  }
}
void segmentsWrite(uint8_t mask) {
  for (uint8_t i = 0; i < 7; ++i) {
    bool on = (mask & (1U << i)) != 0;
    digitalWrite(segmentPins[i], on == segmentActiveHigh ? HIGH : LOW);
  }
}
uint8_t nextDigit() {
  if (digitConnected[0] && digitConnected[1]) return currentDigit ^ 1;
  return digitConnected[1] ? 1 : 0;
}
uint32_t segmentTestDuration() { return (uint32_t)enabledCount * 7UL * 200UL; }
uint8_t visiblePattern(uint8_t digit) {
  if (mode == MANUAL) return digit == 1 ? manualMask : 0;
  if (mode != TEST) return digitPatterns[digit == 0 ? value / 10 : value % 10];
  uint32_t elapsed = millis() - testStartMs;
  uint32_t walkDuration = segmentTestDuration();
  if (elapsed < walkDuration) {
    uint8_t step = elapsed / 200;
    uint8_t target = digitConnected[0] ? step / 7 : 1;
    return target == digit ? (1U << (step % 7)) : 0;
  }
  if (elapsed < walkDuration + 750) return 0x7F;
  return digitPatterns[digit == 0 ? value / 10 : value % 10];
}
void refreshDisplay() {
  if (!enabledCount) return;
  uint32_t now = micros();
  if ((uint32_t)(now - previousSlotUs) >= digitSlotUs) {
    digitsOff(); // blank commons before switching shared segments
    previousSlotUs = now;
    currentDigit = nextDigit();
    uint8_t mask = visiblePattern(currentDigit);
    segmentsWrite(mask);
    if (currentDigit == 1) rightVisibleMask = mask;
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
    uint32_t walkDuration = segmentTestDuration();
    if (elapsed >= walkDuration + 750 + 2000) {
      mode = HOLD;
      value = 4;
      previousStepMs = now;
    } else if (elapsed >= walkDuration + 750) {
      uint32_t steps = elapsed - (walkDuration + 750);
      value = (uint8_t)((steps / (2000UL / (highestValue + 1))) % (highestValue + 1));
    }
    return;
  }
  if (mode == HOLD || mode == MANUAL || (uint32_t)(now - previousStepMs) < intervalMs) return;
  previousStepMs = now;
  if (mode == UP) value = value == highestValue ? 0 : value + 1;
  if (mode == DOWN) value = value == 0 ? highestValue : value - 1;
  if (mode == BOUNCE) {
    if (value == highestValue) ascending = false;
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
const char* modeName() {
  switch (mode) {
    case HOLD: return "HOLD"; case UP: return "UP"; case DOWN: return "DOWN";
    case BOUNCE: return "BOUNCE"; case TEST: return "TEST";
    case MANUAL: return "MANUAL";
  }
  return "HOLD";
}
void command(const char* request) {
  if (!strcmp(request, "HELLO")) { Serial.println("LAUNCHER_IO 5"); return; }
  if (!strcmp(request, "POLL")) {
    Serial.print("DISPLAY "); Serial.print(value);
    Serial.print(' '); Serial.print(brightness);
    Serial.print(' '); Serial.print(modeName());
    Serial.print(' '); Serial.print(intervalMs);
    Serial.print(' '); Serial.println(rightVisibleMask); // mask of physical right digit
    return;
  }
  int n;
  if (!strncmp(request, "SET ", 4) && number(request + 4, 0, highestValue, n)) {
    value = (uint8_t)n; mode = HOLD; previousStepMs = millis();
    Serial.println("OK SET"); return;
  }
  if (!strncmp(request, "SEG ", 4) && number(request + 4, 0, 127, n)) {
    manualMask = (uint8_t)n; mode = MANUAL;
    Serial.println("OK SEG"); return;
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
    else if (!strcmp(chosen, "BOUNCE")) { mode = BOUNCE; ascending = value != highestValue; }
    else { Serial.println("ERR COMMAND"); return; }
    previousStepMs = millis();
    Serial.println("OK MODE"); return;
  }
  if (!strcmp(request, "TEST")) {
    mode = TEST; testStartMs = millis(); value = 0;
    Serial.println("OK TEST"); return;
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
  // The disconnected left-digit GPIO is never configured as an output.
  for (uint8_t i = 0; i < 2; ++i) {
    if (!digitConnected[i]) continue;
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
