#ifndef DISPLAY_H
#define DISPLAY_H

#include <Adafruit_GFX.h>
#include <Adafruit_SSD1306.h>
#include "defines.h"

extern Adafruit_SSD1306 display;

void displayInit();
void displayClear();
void displayPrint(const char *text);

#endif // DISPLAY_H