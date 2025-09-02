#include "qtr.h"

const uint8_t QTRPins[] = {14, 13, 12, 11, 10};
int QTRSensorCount = 5;




void qtrCalibrate(){
    qtr.setTypeRC();
  qtr.setSensorPins(QTRPins, QTRSensorCount);

  for (int i = 0; i < 300; i++)
  {
    qtr.calibrate();
  }
}


void readQTRSensors( u16_t *values)
{
  qtr.read(values);
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