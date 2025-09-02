#ifndef QTR_H
#define QTR_H

#include <QTRSensors.h>
#include <Arduino.h>
#include "defines.h"

extern const uint8_t QTRPins[];
extern QTRSensors qtr;

void qtrCalibrate();
void readQTRSensors(u16_t *values);
void printQTRSensorValues(u16_t *values);

#endif // QTR_H