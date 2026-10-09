// ESP8266 / NodeMCU 1.0: autonomous dice, protocol LAUNCHER_IO 4.
#include "BoardConfig.h"

const size_t commandBufferSize = 32;
char commandBuffer[commandBufferSize];
size_t commandLength = 0;
bool discardCommand = false;

uint32_t rollSequence = 0;
uint32_t lastRollMs = 0;
uint8_t diceValue = 1;

// Bits follow diceLedPins. Popcount of each mask equals its dice value.
const uint8_t diceMasks[] = {
    0,
    0b0001000, // 1: centre
    0b1000001, // 2: opposite corners
    0b1001001, // 3: opposite corners + centre
    0b1100011, // 4: four corners
    0b1101011, // 5: four corners + centre
    0b1110111  // 6: two columns of three
};

void rollDice(uint32_t now)
{
    // ESP8266 Arduino random() uses the hardware RNG when randomSeed isn't set.
    diceValue = static_cast<uint8_t>(random(1, 7));
    ++rollSequence;
    lastRollMs = now;
    const uint8_t mask = diceMasks[diceValue];
    for (size_t index = 0; index < diceLedCount; ++index)
    {
        const bool on = (mask & (1U << index)) != 0;
        digitalWrite(diceLedPins[index], (on != diceLedActiveLow[index]) ? HIGH : LOW);
    }
}

void handleCommand(const char* command)
{
    if (strcmp(command, "HELLO") == 0)
    {
        Serial.println("LAUNCHER_IO 4");
        return;
    }
    if (strcmp(command, "POLL") == 0)
    {
        Serial.print("DICE ");
        Serial.print(rollSequence);
        Serial.print(' ');
        Serial.println(diceValue);
        return;
    }
    Serial.println("ERR UNKNOWN_COMMAND");
}

void serviceSerial()
{
    // Bound each batch so a continuous stream cannot starve the roll timer.
    for (size_t count = 0; count < 64 && Serial.available() > 0; ++count)
    {
        const char character = static_cast<char>(Serial.read());
        if (character == '\r') continue;
        if (character == '\n')
        {
            if (discardCommand)
            {
                discardCommand = false;
                commandLength = 0;
                Serial.println("ERR LINE_TOO_LONG");
                continue;
            }
            commandBuffer[commandLength] = '\0';
            if (commandLength > 0) handleCommand(commandBuffer);
            commandLength = 0;
            continue;
        }
        if (discardCommand) continue;
        if (commandLength + 1 >= commandBufferSize)
        {
            discardCommand = true;
            commandLength = 0;
            continue;
        }
        commandBuffer[commandLength++] = character;
    }
}

void setup()
{
    for (size_t index = 0; index < diceLedCount; ++index)
    {
        digitalWrite(diceLedPins[index], diceLedActiveLow[index] ? HIGH : LOW);
        pinMode(diceLedPins[index], OUTPUT);
    }
    Serial.begin(115200);
    rollDice(millis());
}

void loop()
{
    const uint32_t now = millis();
    // Unsigned subtraction remains correct across the millis() wraparound.
    if (static_cast<uint32_t>(now - lastRollMs) >= rollIntervalMs) rollDice(now);
    serviceSerial();
    yield();
}
