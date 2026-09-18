// Launcher IO firmware for ESP8266 / NodeMCU 1.0
// Protocol version: LAUNCHER_IO 2
//
// Wiring follows 2xInput_GPIO.jpg:
// - green LED  -> D1
// - yellow LED -> D2
// - red LED    -> D3
// - right button -> D6
// - left button  -> D7
//
// Buttons use the external resistors from the breadboard and are pressed HIGH.
// LEDs are active HIGH.
//
// NOTE: D3 is GPIO0, a boot-strap pin on ESP8266. This matches the supplied
// breadboard scheme. If the board ever boots into flashing mode, move the red
// LED signal to a non-strap pin (for example D5) and change redLedPin below.

const int greenLedPin = D1;
const int yellowLedPin = D2;
const int redLedPin = D3;
const int rightButtonPin = D6;
const int leftButtonPin = D7;

const unsigned long debounceMs = 30;
const unsigned long chordWindowMs = 90;
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

struct DebouncedButton
{
    int pin;
    int rawState;
    int stableState;
    unsigned long rawChangedAt;
};

DebouncedButton leftButton;
DebouncedButton rightButton;

InputEvent inputQueue[inputQueueCapacity];
uint8_t inputHead = 0;
uint8_t inputTail = 0;
uint8_t inputCount = 0;

bool gestureActive = false;
bool gestureQueued = false;
uint8_t gestureSeenMask = 0;
unsigned long gestureStartedAt = 0;

char commandBuffer[commandBufferSize];
size_t commandLength = 0;
bool discardCommand = false;

void setLeds(uint8_t mask)
{
    digitalWrite(redLedPin, (mask & 1) != 0 ? HIGH : LOW);
    digitalWrite(yellowLedPin, (mask & 2) != 0 ? HIGH : LOW);
    digitalWrite(greenLedPin, (mask & 4) != 0 ? HIGH : LOW);
}

void initializeButton(DebouncedButton& button, int pin)
{
    button.pin = pin;
    pinMode(pin, INPUT);
    button.rawState = digitalRead(pin);
    button.stableState = button.rawState;
    button.rawChangedAt = millis();
}

void serviceButton(DebouncedButton& button)
{
    const int current = digitalRead(button.pin);
    const unsigned long now = millis();

    if (current != button.rawState)
    {
        button.rawState = current;
        button.rawChangedAt = now;
    }

    if (current != button.stableState && now - button.rawChangedAt >= debounceMs)
        button.stableState = current;
}

uint8_t currentButtonMask()
{
    uint8_t mask = 0;
    if (leftButton.stableState == HIGH)
        mask |= leftMask;
    if (rightButton.stableState == HIGH)
        mask |= rightMask;
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
    const unsigned long now = millis();
    const uint8_t mask = currentButtonMask();

    if (mask == 0)
    {
        if (gestureActive && !gestureQueued)
            queueGesture(gestureSeenMask);

        gestureActive = false;
        gestureQueued = false;
        gestureSeenMask = 0;
        return;
    }

    if (!gestureActive)
    {
        gestureActive = true;
        gestureQueued = false;
        gestureSeenMask = mask;
        gestureStartedAt = now;

        if (mask == bothMask)
        {
            queueGesture(bothMask);
            gestureQueued = true;
        }
        return;
    }

    gestureSeenMask |= mask;

    if (!gestureQueued && gestureSeenMask == bothMask)
    {
        queueGesture(bothMask);
        gestureQueued = true;
        return;
    }

    if (!gestureQueued && now - gestureStartedAt >= chordWindowMs)
    {
        queueGesture(gestureSeenMask);
        gestureQueued = true;
    }
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

    initializeButton(leftButton, leftButtonPin);
    initializeButton(rightButton, rightButtonPin);

    Serial.begin(115200);
}

void loop()
{
    serviceButton(leftButton);
    serviceButton(rightButton);
    serviceGesture();
    serviceSerial();
    yield();
}
