#pragma once

const int fadeInputPin = A0;
const int fadeLedPin = D4;

// Fresh v3 board: external LED on D4 is active HIGH.
// Set to true only when using the NodeMCU built-in D4/GPIO2 LED.
const bool fadeLedActiveLow = false;
