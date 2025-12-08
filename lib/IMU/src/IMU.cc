#include "IMU.h"

/**
 * @brief Constructs an IMU object and initializes sensor values to zero.
 */
IMU::IMU()
    : x(0), y(0), z(0), gyr_x(0), gyr_y(0), gyr_z(0), temperature(0), temperatureInDegree(0.f),
      sat_x(false), sat_y(false), sat_z(false), sat_gyr_x(false), sat_gyr_y(false), sat_gyr_z(false),
      prev_gyr_x(0), prev_gyr_y(0), prev_gyr_z(0),
      filtering_enabled(false), filter_alpha(0.3f), filtered_gyr_x(0.0f), filtered_gyr_y(0.0f), filtered_gyr_z(0.0f)
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

    // Read saturation flags
    readSatFlags();
}

/**
 * @brief Reads saturation flags from the IMU.
 */
void IMU::readSatFlags()
{
    uint16_t flags = readRegister16(SAT_FLAGS);
    sat_x = flags & 0x01;
    sat_y = flags & 0x02;
    sat_z = flags & 0x04;
    sat_gyr_x = flags & 0x08;
    sat_gyr_y = flags & 0x10;
    sat_gyr_z = flags & 0x20;
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
     * average:     0x0600  -> No
     * filtering:   0x0080  -> ODR/4
     * range:       0x0000  -> 2G
     * ODR:         0x000B  -> 800Hz
     * Total:       0x768B
     */
    writeRegister16(ACC_CONF, 0x768B); // Setting accelerometer
    /*
     * Gyr_Conf P.93
     * mode:        0x7000  -> High
     * average:     0x0600  -> No
     * filtering:   0x0020  -> ODR/2 (improved filtering for reduced jumps)
     * range:       0x0040  -> ±2000dps
     * ODR:         0x000B  -> 800Hz
     * Total:       0x762B
     */
    writeRegister16(GYR_CONF, 0x762B); // Setting gyroscope (improved filtering)
    delay(50);
}

/**
 * @brief Reads all sensor data from the IMU and updates member variables.
 */
void IMU::read()
{

    // if (readRegister16(0x02) == 0x00)
    // {
    // Read ChipID
    // Serial.print("ChipID:");
    // Serial.print(readRegister16(0x00));
    readAllAccel(); // read all accelerometer/gyroscope/temperature data

    // Apply saturation clamping to prevent gyro jumps during fast rotations
    if (sat_gyr_x)
        gyr_x = prev_gyr_x;
    else
        prev_gyr_x = gyr_x;
    if (sat_gyr_y)
        gyr_y = prev_gyr_y;
    else
        prev_gyr_y = gyr_y;
    if (sat_gyr_z)
        gyr_z = prev_gyr_z;
    else
        prev_gyr_z = gyr_z;

    // Apply exponential smoothing filter if enabled
    if (filtering_enabled)
    {
        filtered_gyr_x = filter_alpha * (float)gyr_x + (1.0f - filter_alpha) * filtered_gyr_x;
        filtered_gyr_y = filter_alpha * (float)gyr_y + (1.0f - filter_alpha) * filtered_gyr_y;
        filtered_gyr_z = filter_alpha * (float)gyr_z + (1.0f - filter_alpha) * filtered_gyr_z;

        // Update gyr values with filtered results
        gyr_x = (uint16_t)filtered_gyr_x;
        gyr_y = (uint16_t)filtered_gyr_y;
        gyr_z = (uint16_t)filtered_gyr_z;
    }

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

/**
 * @brief Returns true if accelerometer X-axis is saturated.
 */
bool IMU::isAccelSatX()
{
    return sat_x;
}

/**
 * @brief Returns true if gyroscope X-axis is saturated.
 */
bool IMU::isGyroSatX()
{
    return sat_gyr_x;
}

/**
 * @brief Returns true if gyroscope Y-axis is saturated.
 */
bool IMU::isGyroSatY()
{
    return sat_gyr_y;
}

/**
 * @brief Returns true if gyroscope Z-axis is saturated.
 */
bool IMU::isGyroSatZ()
{
    return sat_gyr_z;
}

/**
 * @brief Enables or disables exponential smoothing filter for gyroscope data.
 * @param enable Enable filtering if true, disable if false.
 * @param alpha Smoothing factor (0.0-1.0), lower = more smoothing, higher = less smoothing. Default 0.3.
 */
void IMU::enableFiltering(bool enable, float alpha)
{
    filtering_enabled = enable;
    filter_alpha = alpha;

    // Reset filtered values to current raw values when enabling filtering
    if (filtering_enabled)
    {
        filtered_gyr_x = (float)gyr_x;
        filtered_gyr_y = (float)gyr_y;
        filtered_gyr_z = (float)gyr_z;
    }
}
