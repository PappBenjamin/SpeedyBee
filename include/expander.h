#ifndef EXPANDER_H
#define EXPANDER_H

#include "main_defs_includes.h"

bool setupExpander(Adafruit_MCP23X17 &mcp);
int checkExpanderInterrupt(Adafruit_MCP23X17 &mcp);

#endif // EXPANDER_H