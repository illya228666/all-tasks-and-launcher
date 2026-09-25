// Launcher IO firmware for ESP8266 / NodeMCU 1.0
// Protocol version: LAUNCHER_IO 3
//
// v3 is intentionally minimal:
// - potentiometer on A0 provides fade
// - one LED on D4 mirrors the same fade through PWM
// - Launcher receives the same 0..255 value via POLL

#include "BoardConfig.h"

const size_t commandBufferSize = 32;

char commandBuffer[commandBufferSize];
size_t commandLength = 0;
bool discardCommand = false;

uint8_t fadeValue = 0;

uint8_t readFade()
{
    // ESP8266 analogRead(A0) returns 0..1023. Divide by four to get 0..255.
    return static_cast<uint8_t>(analogRead(fadeInputPin) >> 2);
}

void writeFadePwm()
{
    const int pwm = fadeLedActiveLow ? 255 - fadeValue : fadeValue;
    analogWrite(fadeLedPin, pwm);
}

void updateFade()
{
    const uint8_t next = readFade();
    if (next == fadeValue)
        return;

    fadeValue = next;
    writeFadePwm();
}

void handleCommand(const char* command)
{
    if (strcmp(command, "HELLO") == 0)
    {
        updateFade();
        Serial.println("LAUNCHER_IO 3");
        return;
    }

    if (strcmp(command, "POLL") == 0)
    {
        updateFade();
        Serial.print("FADE ");
        Serial.println(fadeValue);
        return;
    }

    Serial.println("ERR UNKNOWN_COMMAND");
}

void serviceSerial()
{
    while (Serial.available() > 0)
    {
        const char character = static_cast<char>(Serial.read());

        if (character == '\r')
            continue;

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
            if (commandLength > 0)
                handleCommand(commandBuffer);
            commandLength = 0;
            continue;
        }

        if (discardCommand)
            continue;

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
    pinMode(fadeLedPin, OUTPUT);
    analogWriteRange(255);

    fadeValue = readFade();
    writeFadePwm();

    Serial.begin(115200);
}

void loop()
{
    updateFade();
    serviceSerial();
    yield();
}
