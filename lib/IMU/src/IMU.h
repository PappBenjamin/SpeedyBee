#ifndef _IMU_H_
#define _IMU_H_

#include <Wire.h>
#include <Arduino.h>

// I2C address and register definitions
#define BMI323_I2C_ADDR_1 0x68 // Default I2C address (SDO = GND)
#define CHIP_ID_REG 0x00
#define ERR_REG 0x01
#define STATUS_REG 0x02
#define ACC_CONF_REG 0x20
#define GYR_CONF_REG 0x21
#define ACC_DATA_X_REG 0x03
#define ACC_DATA_Y_REG 0x04
#define ACC_DATA_Z_REG 0x05
#define GYR_DATA_X_REG 0x06
#define GYR_DATA_Y_REG 0x07
#define GYR_DATA_Z_REG 0x08
#define TEMP_DATA_REG 0x09
#define CMD_REG 0x7E
#define FEATURE_IO0_REG 0x10
#define FEATURE_IO1_REG 0x11
#define FEATURE_IO2_REG 0x12
#define FEATURE_IO_STATUS_REG 0x14
#define FEATURE_CTRL_REG 0x40

// Configuration constants
#define OUTPUT_RATE_HZ 20                    // 20Hz data output
#define ACC_RANGE_LSB_PER_G 4096.0           // +-8g range
#define GYR_RANGE_LSB_PER_DPS 16.384         // +-2000 deg/s range
#define SOFT_RESET_CMD 0xDEAF                // Soft reset command
#define ACC_CONF_NORMAL_100HZ_8G 0x4028      // Accel: 100Hz, +-8g
#define GYR_CONF_NORMAL_100HZ_2000DPS 0x4048 // Gyro: 100Hz, +-2000 deg/s

/**
 * @class IMU
 * @brief Class for interfacing with the BMI323 6-axis IMU over I2C.
 */
class IMU
{
private:
    float ax;   /**< Accelerometer X-axis in g */
    float ay;   /**< Accelerometer Y-axis in g */
    float az;   /**< Accelerometer Z-axis in g */
    float gx;   /**< Gyroscope X-axis in deg/s */
    float gy;   /**< Gyroscope Y-axis in deg/s */
    float gz;   /**< Gyroscope Z-axis in deg/s */
    float temp; /**< Temperature in C */

    // Global I2C address
    uint8_t bmi323_i2c_addr;

    /**
     * @brief Initializes the BMI323 IMU.
     * @return True if initialization successful, false otherwise.
     */
    bool initBMI323();

    /**
     * @brief Initializes the feature engine on the BMI323.
     * @return True if successful, false otherwise.
     */
    bool initializeFeatureEngine();

    /**
     * @brief Reads a 16-bit value from a register on the IMU.
     * @param reg The register address.
     * @param addr I2C address (default to bmi323_i2c_addr).
     * @return The 16-bit value read from the register.
     */
    uint16_t readRegister16(uint8_t reg, uint8_t addr = 0); // addr will be set to bmi323_i2c_addr

    /**
     * @brief Writes a 16-bit value to a register on the IMU.
     * @param reg The register address.
     * @param data The 16-bit value to write.
     */
    void writeRegister16(uint8_t reg, uint16_t data);

    /**
     * @brief Converts accelerometer raw data to g units.
     * @param rawData The raw 16-bit accelerometer data.
     * @return Value in g, or NAN if invalid.
     */
    float convertAccelData(uint16_t rawData);

    /**
     * @brief Converts gyroscope raw data to deg/s units.
     * @param rawData The raw 16-bit gyroscope data.
     * @return Value in deg/s, or NAN if invalid.
     */
    float convertGyroData(uint16_t rawData);

    /**
     * @brief Converts temperature raw data to Celsius.
     * @param rawData The raw 16-bit temperature data.
     * @return Value in C, or NAN if invalid.
     */
    float convertTempData(uint16_t rawData);

public:
    /**
     * @brief Constructs an IMU object with default I2C address.
     */
    IMU();

    /**
     * @brief Initializes the IMU and configures accelerometer/gyroscope.
     * @return True if initialization successful, false otherwise.
     */
    bool begin();

    /**
     * @brief Reads all sensor data and converts to physical units.
     */
    void read();

    /**
     * @brief Prints the current sensor data to serial in CSV format.
     * Output format: ax,ay,az,gx,gy,gz,temp
     * Units: g,g,g,deg/s,deg/s,deg/s,C
     */
    void printData();
};

#endif // _IMU_H_
