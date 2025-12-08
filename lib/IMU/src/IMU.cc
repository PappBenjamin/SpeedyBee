#include "IMU.h"

/**
 * @brief Constructs an IMU object with default I2C address.
 */
IMU::IMU() : ax(0.0f), ay(0.0f), az(0.0f), gx(0.0f), gy(0.0f), gz(0.0f), temp(0.0f), bmi323_i2c_addr(BMI323_I2C_ADDR_1) {}

/**
 * @brief Initializes the IMU and configures accelerometer/gyroscope.
 * @return True if initialization successful, false otherwise.
 */
bool IMU::begin()
{
    return initBMI323();
}

/**
 * @brief Initializes the BMI323 IMU.
 * @return True if initialization successful, false otherwise.
 */
bool IMU::initBMI323()
{
    // Try both possible I2C addresses
    Serial.print("Testing I2C address 0x");
    Serial.println(BMI323_I2C_ADDR_1, HEX);
    uint16_t chipID = readRegister16(CHIP_ID_REG, BMI323_I2C_ADDR_1);
    if ((chipID & 0xFF) == 0x43 || (chipID & 0xFF) == 0x41)
    {
        bmi323_i2c_addr = BMI323_I2C_ADDR_1;
        Serial.println("Found BMI323 at address 0x68");
    }
    else
    {
        Serial.print("Invalid chip ID: 0x");
        Serial.println(chipID, HEX);
        return false;
    }

    // Reset sensor
    Serial.println("Performing soft reset...");
    writeRegister16(CMD_REG, SOFT_RESET_CMD);
    delay(5); // Wait for reset (datasheet: ~2ms)

    // Set up feature engine
    Serial.println("Initializing feature engine...");
    if (!initializeFeatureEngine())
    {
        Serial.println("Feature engine setup failed");
        return false;
    }

    // Configure accelerometer and gyroscope
    Serial.println("Configuring accelerometer and gyroscope...");
    writeRegister16(ACC_CONF_REG, ACC_CONF_NORMAL_100HZ_8G);
    writeRegister16(GYR_CONF_REG, GYR_CONF_NORMAL_100HZ_2000DPS);
    delay(50); // Wait for configuration to settle

    return true;
}

/**
 * @brief Initializes the feature engine on the BMI323.
 * @return True if successful, false otherwise.
 */
bool IMU::initializeFeatureEngine()
{
    // Clear feature config
    writeRegister16(FEATURE_IO0_REG, 0x0000);
    delay(1);

    // Set startup config
    writeRegister16(FEATURE_IO2_REG, 0x012C);
    delay(1);

    // Trigger startup
    writeRegister16(FEATURE_IO_STATUS_REG, 0x0001);
    delay(1);

    // Enable feature engine
    writeRegister16(FEATURE_CTRL_REG, 0x0001);
    delay(10);

    // Check feature engine status
    int timeout = 0;
    uint16_t featureIO1Status;
    do
    {
        delay(10);
        featureIO1Status = readRegister16(FEATURE_IO1_REG);
        uint8_t errorStatus = featureIO1Status & 0x0F;
        if (errorStatus == 0x01)
        {
            Serial.println("Feature engine initialized successfully");
            return true;
        }
        if (errorStatus == 0x03)
        {
            Serial.println("Feature engine error");
            return false;
        }
        timeout++;
    } while ((featureIO1Status & 0x0F) == 0x00 && timeout < 50);

    Serial.println("Feature engine timeout");
    return false;
}

/**
 * @brief Reads a 16-bit value from a register on the IMU.
 * @param reg The register address.
 * @param addr I2C address (uses bmi323_i2c_addr if 0).
 * @return The 16-bit value read from the register.
 */
uint16_t IMU::readRegister16(uint8_t reg, uint8_t addr)
{
    if (addr == 0)
        addr = bmi323_i2c_addr;
    Wire.beginTransmission(addr);
    Wire.write(reg);
    int error = Wire.endTransmission(false); // Restart
    if (error != 0)
    {
        Serial.print("I2C write error: ");
        Serial.println(error);
        return 0xFFFF;
    }

    // Request 4 bytes: 2 dummy bytes + 2 data bytes (LSB, MSB)
    Wire.requestFrom(addr, (uint8_t)4);
    if (Wire.available() < 4)
    {
        Serial.println("I2C read error: Not enough bytes");
        return 0xFFFF;
    }

    // Discard dummy bytes
    Wire.read(); // Dummy byte 1
    Wire.read(); // Dummy byte 2
    uint8_t lsb = Wire.read();
    uint8_t msb = Wire.read();
    return (msb << 8) | lsb;
}

/**
 * @brief Writes a 16-bit value to a register on the IMU.
 * @param reg The register address.
 * @param data The 16-bit value to write.
 */
void IMU::writeRegister16(uint8_t reg, uint16_t data)
{
    Wire.beginTransmission(bmi323_i2c_addr);
    Wire.write(reg);
    Wire.write(data & 0xFF);        // LSB
    Wire.write((data >> 8) & 0xFF); // MSB
    int error = Wire.endTransmission();
    if (error != 0)
    {
        Serial.print("I2C write error: ");
        Serial.println(error);
    }
}

/**
 * @brief Converts accelerometer raw data to g units.
 * @param rawData The raw 16-bit accelerometer data.
 * @return Value in g, or NAN if invalid.
 */
float IMU::convertAccelData(uint16_t rawData)
{
    int16_t signedData = (int16_t)rawData;
    if (signedData == -32768)
        return NAN;
    return signedData / ACC_RANGE_LSB_PER_G;
}

/**
 * @brief Converts gyroscope raw data to deg/s units.
 * @param rawData The raw 16-bit gyroscope data.
 * @return Value in deg/s, or NAN if invalid.
 */
float IMU::convertGyroData(uint16_t rawData)
{
    int16_t signedData = (int16_t)rawData;
    if (signedData == -32768)
        return NAN;
    return signedData / GYR_RANGE_LSB_PER_DPS;
}

/**
 * @brief Converts temperature raw data to Celsius.
 * @param rawData The raw 16-bit temperature data.
 * @return Value in C, or NAN if invalid.
 */
float IMU::convertTempData(uint16_t rawData)
{
    int16_t signedData = (int16_t)rawData;
    if (signedData == -32768)
        return NAN;
    return (signedData / 512.0) + 23.0;
}

/**
 * @brief Reads all sensor data and converts to physical units.
 */
void IMU::read()
{
    // Read sensor data
    uint16_t accX = readRegister16(ACC_DATA_X_REG);
    uint16_t accY = readRegister16(ACC_DATA_Y_REG);
    uint16_t accZ = readRegister16(ACC_DATA_Z_REG);
    uint16_t gyrX = readRegister16(GYR_DATA_X_REG);
    uint16_t gyrY = readRegister16(GYR_DATA_Y_REG);
    uint16_t gyrZ = readRegister16(GYR_DATA_Z_REG);
    uint16_t tempRaw = readRegister16(TEMP_DATA_REG);

    // Convert to physical units
    ax = convertAccelData(accX);
    ay = convertAccelData(accY);
    az = convertAccelData(accZ);
    gx = convertGyroData(gyrX);
    gy = convertGyroData(gyrY);
    gz = convertGyroData(gyrZ);
    temp = convertTempData(tempRaw);
}

/**
 * @brief Prints the current sensor data to serial in CSV format.
 * Output format: ax,ay,az,gx,gy,gz,temp
 * Units: g,g,g,deg/s,deg/s,deg/s,C
 */
void IMU::printData()
{
    // Print valid data in CSV format
    if (!isnan(ax) && !isnan(ay) && !isnan(az) &&
        !isnan(gx) && !isnan(gy) && !isnan(gz))
    {
        Serial.print(ax, 3);
        Serial.print(",");
        Serial.print(ay, 3);
        Serial.print(",");
        Serial.print(az, 3);
        Serial.print(",");
        Serial.print(gx, 2);
        Serial.print(",");
        Serial.print(gy, 2);
        Serial.print(",");
        Serial.print(gz, 2);
        Serial.print(",");
        Serial.println(isnan(temp) ? "NAN" : String(temp, 1));
    }
}
