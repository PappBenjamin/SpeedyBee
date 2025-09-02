#ifndef EXPANDER_H
#define EXPANDER_H

#include "defines.h"
#include <Arduino.h>
#include <Adafruit_MCP23X17.h>

extern Adafruit_MCP23X17 mcp;

bool setupExpander();
int checkExpanderInterrupt();

#endif // EXPANDER_H