#ifndef QTR_H
#define QTR_H

#include <QTRSensors.h>
#include <Arduino.h>


extern const uint8_t QTRPins[];
extern int QTRSensorCount;

void qtrCalibrate(QTRSensors &qtr);
void readQTRSensors(QTRSensors &qtr, u16_t *values);
void printQTRSensorValues(u16_t *values);

#endif // QTR_H