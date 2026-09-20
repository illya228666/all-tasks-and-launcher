// Launcher IO firmware for ESP8266 / NodeMCU 1.0
// Protocol version: LAUNCHER_IO 2
//
// Final wiring:
// - green LED    -> D1
// - yellow LED   -> D2
// - red LED      -> D3
// - left button  -> D6
// - right button -> D7
//
// Input wiring is intentionally asymmetric:
// - left: active LOW, uses ESP8266 INPUT_PULLUP
// - right: active HIGH, uses the external pull-down resistor
// LEDs are active HIGH.
//
// NOTE: D3 is GPIO0, a boot-strap pin on ESP8266. This matches the supplied
// breadboard scheme. If the board ever boots into flashing mode, move the red
// LED signal to a non-strap pin (for example D5) and change redLedPin below.

#include "BoardConfig.h"
#include "ButtonDebounce.h"
#include "ButtonGesture.h"

const size_t commandBufferSize = 64;
const uint8_t inputQueueCapacity = 8;

const uint8_t leftMask = 1;
const uint8_t rightMask = 2;
const uint8_t bothMask = leftMask | rightMask;

enum InputEvent : uint8_t
{
    inputNone = 0,
    inputLeftPressed,
    inputRightPressed,
    inputBothPressed
};

ButtonDebounce leftButton;
ButtonDebounce rightButton;
ButtonGesture gesture;

InputEvent inputQueue[inputQueueCapacity];
uint8_t inputHead = 0;
uint8_t inputTail = 0;
uint8_t inputCount = 0;

char commandBuffer[commandBufferSize];
size_t commandLength = 0;
bool discardCommand = false;

void setLeds(uint8_t mask)
{
    digitalWrite(redLedPin, (mask & 1) != 0 ? HIGH : LOW);
    digitalWrite(yellowLedPin, (mask & 2) != 0 ? HIGH : LOW);
    digitalWrite(greenLedPin, (mask & 4) != 0 ? HIGH : LOW);
}

uint8_t currentButtonMask()
{
    uint8_t mask = 0;
    if (leftButton.pressed()) mask |= leftMask;
    if (rightButton.pressed()) mask |= rightMask;
    return mask;
}

void enqueueInput(InputEvent input)
{
    if (input == inputNone || inputCount >= inputQueueCapacity)
        return;

    inputQueue[inputTail] = input;
    inputTail = (inputTail + 1) % inputQueueCapacity;
    ++inputCount;
}

InputEvent dequeueInput()
{
    if (inputCount == 0)
        return inputNone;

    const InputEvent input = inputQueue[inputHead];
    inputHead = (inputHead + 1) % inputQueueCapacity;
    --inputCount;
    return input;
}

void clearInputQueue()
{
    inputHead = 0;
    inputTail = 0;
    inputCount = 0;
}

void queueGesture(uint8_t mask)
{
    if (mask == bothMask)
        enqueueInput(inputBothPressed);
    else if ((mask & leftMask) != 0)
        enqueueInput(inputLeftPressed);
    else if ((mask & rightMask) != 0)
        enqueueInput(inputRightPressed);
}

void serviceGesture()
{
    queueGesture(gesture.update(currentButtonMask(), millis()));
}

void writeInput(InputEvent input)
{
    switch (input)
    {
        case inputLeftPressed:
            Serial.println("INPUT LEFT_PRESSED");
            break;
        case inputRightPressed:
            Serial.println("INPUT RIGHT_PRESSED");
            break;
        case inputBothPressed:
            Serial.println("INPUT BOTH_PRESSED");
            break;
        default:
            Serial.println("INPUT NONE");
            break;
    }
}

void handleCommand(const char* command)
{
    if (strcmp(command, "HELLO") == 0)
    {
        clearInputQueue();
        gesture.reset();
        setLeds(0);
        Serial.println("LAUNCHER_IO 2");
        return;
    }

    if (strcmp(command, "POLL") == 0)
    {
        writeInput(dequeueInput());
        return;
    }

    if (strncmp(command, "LEDS ", 5) == 0
        && command[5] >= '0'
        && command[5] <= '7'
        && command[6] == '\0')
    {
        setLeds(static_cast<uint8_t>(command[5] - '0'));
        Serial.println("OK LEDS");
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
    pinMode(redLedPin, OUTPUT);
    pinMode(yellowLedPin, OUTPUT);
    pinMode(greenLedPin, OUTPUT);
    setLeds(0);

    pinMode(leftButtonPin, leftButtonMode);
    pinMode(rightButtonPin, rightButtonMode);
    leftButton.reset(digitalRead(leftButtonPin) == leftPressedLevel, millis());
    rightButton.reset(digitalRead(rightButtonPin) == rightPressedLevel, millis());
    gesture.reset();

    Serial.begin(115200);
}

void loop()
{
    const uint32_t now = millis();
    leftButton.update(digitalRead(leftButtonPin) == leftPressedLevel, now);
    rightButton.update(digitalRead(rightButtonPin) == rightPressedLevel, now);
    serviceGesture();
    serviceSerial();
    yield();
}
