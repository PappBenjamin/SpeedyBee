#ifndef _IMU_H_
#define _IMU_H_

#include <Wire.h>
#include <Arduino.h>

#define INC_ADDRESS 0x68
#define ACC_CONF 0x20 // Page 91
#define GYR_CONF 0x21 // Page 93
#define CMD 0x7E      // Page 65

/**
 * @class IMU
 * @brief Class for interfacing with an Inertial Measurement Unit (IMU) sensor.
 */
class IMU
{
private:
    uint16_t x;                /**< Accelerometer X-axis value */
    uint16_t y;                /**< Accelerometer Y-axis value */
    uint16_t z;                /**< Accelerometer Z-axis value */
    uint16_t gyr_x;            /**< Gyroscope X-axis value */
    uint16_t gyr_y;            /**< Gyroscope Y-axis value */
    uint16_t gyr_z;            /**< Gyroscope Z-axis value */
    uint16_t temperature;      /**< Raw temperature value */
    float temperatureInDegree; /**< Temperature in degrees Celsius */

    /**
     * @brief Writes a 16-bit value to a register on the IMU.
     * @param reg The register address.
     * @param value The 16-bit value to write.
     */
    void writeRegister16(uint16_t reg, uint16_t value);

    /**
     * @brief Reads a 16-bit value from a register on the IMU.
     * @param reg The register address.
     * @return The 16-bit value read from the register.
     */
    uint16_t readRegister16(uint8_t reg);

    /**
     * @brief Reads all accelerometer, gyroscope, and temperature data from the IMU.
     *        Updates the corresponding member variables.
     */
    void readAllAccel();

public:
    /**
     * @brief Constructs an IMU object and initializes sensor values to zero.
     */
    IMU();

    /**
     * @brief Initializes the IMU by performing a soft reset and configuring the accelerometer and gyroscope.
     */
    void begin();

    /**
     * @brief Reads all sensor data from the IMU and updates member variables.
     */
    void read();

    /**
     * @brief Performs a software reset of the IMU.
     */
    void softReset();

    /**
     * @brief Prints the current sensor data to the serial output in CSV format.
     */
    void printData();
};

#endif // _IMU_H_