// Launcher IO firmware for ESP8266 / NodeMCU 1.0
// Protocol version: LAUNCHER_IO 1
//
// Hardware behavior intentionally matches the original sketch:
// - button is connected to D1 and is considered pressed when digitalRead(D1) == HIGH
// - built-in LED is active LOW
// - no internal pull-up/pull-down is enabled (INPUT, not INPUT_PULLUP)

const int buttonPin = D1;
const int ledPin = LED_BUILTIN;

const unsigned long debounceMs = 30;
const size_t commandBufferSize = 64;

int rawButtonState = LOW;
int stableButtonState = LOW;
unsigned long rawButtonChangedAt = 0;

bool indicatorEnabled = false;
uint8_t pendingButtonPresses = 0;

char commandBuffer[commandBufferSize];
size_t commandLength = 0;
bool discardCommand = false;

void updateLed()
{
    // LED_BUILTIN on NodeMCU is active LOW. The physical button keeps the
    // original visual feedback; the host-controlled indicator is additive.
    const bool shouldBeOn = indicatorEnabled || stableButtonState == HIGH;
    digitalWrite(ledPin, shouldBeOn ? LOW : HIGH);
}

void serviceButton()
{
    const int current = digitalRead(buttonPin);
    const unsigned long now = millis();

    if (current != rawButtonState)
    {
        rawButtonState = current;
        rawButtonChangedAt = now;
    }

    if (current != stableButtonState && now - rawButtonChangedAt >= debounceMs)
    {
        stableButtonState = current;

        if (stableButtonState == HIGH && pendingButtonPresses < 255)
            ++pendingButtonPresses;

        updateLed();
    }
}

void handleCommand(const char* command)
{
    if (strcmp(command, "HELLO") == 0)
    {
        // A new host session starts clean; connection itself must never look
        // like a button press.
        pendingButtonPresses = 0;
        Serial.println("LAUNCHER_IO 1");
        return;
    }

    if (strcmp(command, "POLL") == 0)
    {
        if (pendingButtonPresses > 0)
        {
            --pendingButtonPresses;
            Serial.println("INPUT BUTTON_PRESSED");
        }
        else
        {
            Serial.println("INPUT NONE");
        }
        return;
    }

    if (strcmp(command, "INDICATOR ON") == 0)
    {
        indicatorEnabled = true;
        updateLed();
        Serial.println("OK INDICATOR");
        return;
    }

    if (strcmp(command, "INDICATOR OFF") == 0)
    {
        indicatorEnabled = false;
        updateLed();
        Serial.println("OK INDICATOR");
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
    pinMode(ledPin, OUTPUT);
    pinMode(buttonPin, INPUT);

    rawButtonState = digitalRead(buttonPin);
    stableButtonState = rawButtonState;
    rawButtonChangedAt = millis();
    updateLed();

    Serial.begin(115200);
}

void loop()
{
    serviceButton();
    serviceSerial();
    yield();
}
