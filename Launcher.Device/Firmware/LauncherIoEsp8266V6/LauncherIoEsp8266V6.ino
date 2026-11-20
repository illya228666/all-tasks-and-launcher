#include <Arduino.h>
#include <string.h>

constexpr uint8_t pins[] = { D0, D1, D2, D3, D4, D5, D6, D7, D8 };
constexpr uint8_t pinCount = sizeof(pins) / sizeof(pins[0]);
// D3 (GPIO0) and D4 (GPIO2) are high for a normal flash boot.
uint16_t pinLevels = (1U << 3) | (1U << 4);
char line[16];
uint8_t lineLength = 0;
bool invalidLine = false;

void handleCommand(const char* request) {
  if (strcmp(request, "HELLO") == 0) {
    Serial.println(F("LAUNCHER_IO 6"));
    return;
  }
  if (strcmp(request, "POLL") == 0) {
    Serial.print(F("PINS "));
    Serial.println(pinLevels);
    return;
  }
  if (strncmp(request, "PIN D", 5) == 0 && request[5] >= '0' && request[5] < '0' + pinCount) {
    bool high = strcmp(request + 6, " HIGH") == 0;
    if (high || strcmp(request + 6, " LOW") == 0) {
      uint8_t pin = request[5] - '0';
      digitalWrite(pins[pin], high ? HIGH : LOW);
      uint16_t bit = 1U << pin;
      pinLevels = high ? pinLevels | bit : pinLevels & ~bit;
      Serial.print(F("OK "));
      Serial.println(request);
      return;
    }
  }
  Serial.println(F("ERR COMMAND"));
}

void setup() {
  for (uint8_t pin = 0; pin < pinCount; ++pin) {
    digitalWrite(pins[pin], (pinLevels & (1U << pin)) != 0 ? HIGH : LOW);
    pinMode(pins[pin], OUTPUT);
  }
  Serial.begin(115200);
}

void loop() {
  for (uint8_t i = 0; i < 48 && Serial.available(); ++i) {
    char ch = (char)Serial.read();
    if (ch == '\n') {
      if (invalidLine) Serial.println(F("ERR COMMAND"));
      else {
        if (lineLength > 0 && line[lineLength - 1] == '\r') --lineLength;
        line[lineLength] = '\0';
        if (lineLength > 0) handleCommand(line);
      }
      lineLength = 0;
      invalidLine = false;
    } else if (!invalidLine) {
      if (ch == '\0' || lineLength == sizeof(line) - 1) invalidLine = true;
      else line[lineLength++] = ch;
    }
  }
}
