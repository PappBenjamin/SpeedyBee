#ifndef _IMU_H_
#define _IMU_H_

#include <Wire.h>
#include <Arduino.h>

#define INC_ADDRESS 0x68
#define ACC_CONF 0x20 // Page 91
#define GYR_CONF 0x21 // Page 93
#define CMD 0x7E      // Page 65

class IMU
{
private:
    uint16_t x, y, z;
    uint16_t gyr_x, gyr_y, gyr_z;
    uint16_t temperature;
    float temperatureInDegree;

    void writeRegister16(uint16_t reg, uint16_t value);
    uint16_t readRegister16(uint8_t reg);
    void readAllAccel();

public:
    IMU();
    void begin();
    void read();
    void softReset();
};

#endif // _IMU_H_