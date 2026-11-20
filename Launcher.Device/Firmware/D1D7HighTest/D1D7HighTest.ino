// NodeMCU: D1-D7 constantly HIGH, no Launcher IO protocol.
void setup()
{
    const uint8_t outputPins[] = { D1, D2, D3, D4, D5, D6, D7 };
    for (uint8_t pin : outputPins)
    {
        digitalWrite(pin, HIGH);
        pinMode(pin, OUTPUT);
    }
}

void loop()
{
    delay(1000);
}
