#ifndef EXPANDER_H
#define EXPANDER_H

#include "defines.h"
#include <Arduino.h>
#include <Adafruit_MCP23X17.h>

bool setupExpander(Adafruit_MCP23X17 &mcp);
int checkExpanderInterrupt(Adafruit_MCP23X17 &mcp);

#endif // EXPANDER_H