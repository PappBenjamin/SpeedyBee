#include "IMU.h"

/**
 * @brief Constructs an IMU object and initializes sensor values to zero.
 */
IMU::IMU()
    : x(0), y(0), z(0), gyr_x(0), gyr_y(0), gyr_z(0), temperature(0), temperatureInDegree(0.f)
{
}

/**
 * @brief Writes a 16-bit value to a register on the IMU.
 * @param reg The register address.
 * @param value The 16-bit value to write.
 */
void IMU::writeRegister16(uint16_t reg, uint16_t value)
{
    Wire.beginTransmission(INC_ADDRESS);
    Wire.write(reg);
    // Low
    Wire.write((uint16_t)value & 0xff);
    // High
    Wire.write((uint16_t)value >> 8);
    Wire.endTransmission();
}

/**
 * @brief Reads a 16-bit value from a register on the IMU.
 * @param reg The register address.
 * @return The 16-bit value read from the register.
 */
uint16_t IMU::readRegister16(uint8_t reg)
{
    Wire.beginTransmission(INC_ADDRESS);
    Wire.write(reg);
    Wire.endTransmission(false);
    int n = Wire.requestFrom(INC_ADDRESS, 4);
    uint16_t data[4] = {0};
    int i = 0;
    while (Wire.available() && i < 4)
    {
        data[i] = Wire.read();
        i++;
    }
    return (data[3] | data[2] << 8);
}

/**
 * @brief Reads all accelerometer, gyroscope, and temperature data from the IMU.
 *        Updates the corresponding member variables.
 */
void IMU::readAllAccel()
{
    Wire.beginTransmission(INC_ADDRESS);
    Wire.write(0x03);
    Wire.endTransmission();
    Wire.requestFrom(INC_ADDRESS, 20);
    uint16_t data[20] = {0};
    int i = 0;
    while (Wire.available() && i < 20)
    {
        data[i] = Wire.read();
        i++;
    }

    // Offset = 2 because the 2 first bytes are dummy (useless)
    int offset = 2;
    x = (data[offset + 0] | (uint16_t)data[offset + 1] << 8);             // 0x03
    y = (data[offset + 2] | (uint16_t)data[offset + 3] << 8);             // 0x04
    z = (data[offset + 4] | (uint16_t)data[offset + 5] << 8);             // 0x05
    gyr_x = (data[offset + 6] | (uint16_t)data[offset + 7] << 8);         // 0x06
    gyr_y = (data[offset + 8] | (uint16_t)data[offset + 9] << 8);         // 0x07
    gyr_z = (data[offset + 10] | (uint16_t)data[offset + 11] << 8);       // 0x08
    temperature = (data[offset + 12] | (uint16_t)data[offset + 13] << 8); // 0x09
    temperatureInDegree = (temperature / 512.f) + 23.0f;
}

/**
 * @brief Performs a software reset of the IMU.
 */
void IMU::softReset()
{
    writeRegister16(CMD, 0xDEAF);
    delay(50);
}

/**
 * @brief Initializes the IMU by performing a soft reset and configuring the accelerometer and gyroscope.
 */
void IMU::begin()
{
    softReset();
    /*
     * Acc_Conf P.91
     * mode:        0x7000  -> High
     * average:     0x0000  -> No
     * filtering:   0x0080  -> ODR/4
     * range:       0x0000  -> 2G
     * ODR:         0x000B  -> 800Hz
     * Total:       0x708B
     */
    writeRegister16(ACC_CONF, 0x708B); // Setting accelerometer
    /*
     * Gyr_Conf P.93
     * mode:        0x7000  -> High
     * average:     0x0000  -> No
     * filtering:   0x0080  -> ODR/4
     * range:       0x0000  -> 125kdps
     * ODR:         0x000B  -> 800Hz
     * Total:       0x708B
     */
    writeRegister16(GYR_CONF, 0x708B); // Setting gyroscope
    delay(50);
}

/**
 * @brief Reads all sensor data from the IMU and updates member variables.
 */
void IMU::read()
{
    // readRegister16(0x02);
    // if (readRegister16(0x02) == 0x00)
    // {
    // Read ChipID
    // Serial.print("ChipID:");
    // Serial.print(readRegister16(0x00));
    readAllAccel(); // read all accelerometer/gyroscope/temperature data

    // }
    // else
    // {
    //     Serial.println("No Data");
    // }
}

/**
 * @brief Prints the current sensor data to the serial output in CSV format.
 */
void IMU::printData()
{
    // Serial.print(" \tx:");
    Serial.print(x);
    // Serial.print(" \ty:");
    Serial.print(",");

    Serial.print(y);
    // Serial.print(" \tz:");
    Serial.print(",");
    Serial.print(z);
    // Serial.print(" \tgyr_x:");
    Serial.print(",");
    Serial.print(gyr_x);
    // Serial.print(" \tgyr_y:");
    Serial.print(",");
    Serial.print(gyr_y);
    // Serial.print(" \tgyr_z:");
    Serial.print(",");
    Serial.println(gyr_z);
    // Serial.print(" \ttemp:");
    // Serial.print("\t");
    // Serial.println(temperatureInDegree);
}