#include "qtr.h"

const uint8_t QTRPins[] = {10, 14, 13, 12, 11};

void qtrCalibrate()
{
  Serial.println("Calibrating QTR Sensors...");
  qtr.setTypeRC();
  qtr.setSensorPins(QTRPins, QTRSensorCount);

  for (int i = 0; i < 150; i++)
  {
    qtr.calibrate();
  }
}

void readQTRSensors(u16_t *values)
{
  qtr.readLineBlack(values);
}

void printQTRSensorValues(u16_t *values)
{
  Serial.print("QTR Sensor ");
  for (int i = 0; i < QTRSensorCount; i++)
  {
    Serial.print(values[i]);
    Serial.print(" ");
  }
  Serial.println();
}