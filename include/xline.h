#ifndef XLINE_H
#define XLINE_H

#include <Arduino.h>
#include "defines.h"

void xlineCalibrate();
void readXLineSensors(uint16_t *values);
int16_t xlineReadLinePosition(const uint16_t *values);
void printXLineSensorValues(const uint16_t *values);

#endif // XLINE_H