#include "xline.h"

namespace
{
    const uint8_t XLineSelectPins[] = {XLINE_S0_PIN, XLINE_S1_PIN, XLINE_S2_PIN, XLINE_S3_PIN};
    const int16_t XLineSensorPositions[XLINE_SENSOR_COUNT] = {
        0,
        1000,
        2000,
        3000,
        4000,
        5000,
        6000,
        7000,
        8000,
        9000,
        10000,
        11000,
        12000,
        13000,
        14000,
        7000,
    };
    uint16_t xlineMinValues[XLINE_SENSOR_COUNT];
    uint16_t xlineMaxValues[XLINE_SENSOR_COUNT];
    uint16_t xlineLastPosition = XLINE_POSITION_CENTER;

    void setChannel(uint8_t index)
    {
        digitalWrite(XLineSelectPins[0], (index & 0x01) ? HIGH : LOW);
        digitalWrite(XLineSelectPins[1], (index & 0x02) ? HIGH : LOW);
        digitalWrite(XLineSelectPins[2], (index & 0x04) ? HIGH : LOW);
        digitalWrite(XLineSelectPins[3], (index & 0x08) ? HIGH : LOW);
    }

    uint16_t readRawSensor(uint8_t index)
    {
        setChannel(index);
        delayMicroseconds(100);

#if XLINE_USE_ANALOG_READ
        return analogRead(XLINE_ANALOG_PIN);
#else
        int state = digitalRead(XLINE_IR_OUT_PIN);
        bool active = XLINE_DIGITAL_ACTIVE_LOW ? (state == LOW) : (state == HIGH);
        return active ? 1000 : 0;
#endif
    }

    uint16_t normalizeAnalogValue(uint16_t raw, uint8_t index)
    {
        uint16_t minimum = xlineMinValues[index];
        uint16_t maximum = xlineMaxValues[index];

        if (maximum <= minimum)
        {
            return 0;
        }

        long mapped = map(raw, minimum, maximum, 0, 1000);
        mapped = constrain(mapped, 0, 1000);

#if XLINE_ANALOG_ACTIVE_LOW
        mapped = 1000 - mapped;
#endif

        return static_cast<uint16_t>(mapped);
    }
} // namespace

void xlineCalibrate()
{
    Serial.println("Calibrating XLine sensors...");

    for (uint8_t index = 0; index < XLINE_SENSOR_COUNT; index++)
    {
        xlineMinValues[index] = 0xFFFF;
        xlineMaxValues[index] = 0;
    }

    for (uint8_t index = 0; index < 4; index++)
    {
        pinMode(XLineSelectPins[index], OUTPUT);
        digitalWrite(XLineSelectPins[index], LOW);
    }

    pinMode(XLINE_IR_OUT_PIN, INPUT);
    pinMode(XLINE_ANALOG_PIN, INPUT);

#if XLINE_USE_ANALOG_READ
    for (int sample = 0; sample < 150; sample++)
    {
        for (uint8_t index = 0; index < XLINE_SENSOR_COUNT; index++)
        {
            uint16_t raw = readRawSensor(index);
            if (raw < xlineMinValues[index])
            {
                xlineMinValues[index] = raw;
            }
            if (raw > xlineMaxValues[index])
            {
                xlineMaxValues[index] = raw;
            }
        }
    }

    for (uint8_t index = 0; index < XLINE_SENSOR_COUNT; index++)
    {
        if (xlineMaxValues[index] <= xlineMinValues[index])
        {
            xlineMinValues[index] = 0;
            xlineMaxValues[index] = 1000;
        }
    }
#endif
}

void readXLineSensors(uint16_t *values)
{
    for (uint8_t index = 0; index < XLINE_SENSOR_COUNT; index++)
    {
#if XLINE_USE_ANALOG_READ
        uint16_t raw = readRawSensor(index);
        values[index] = normalizeAnalogValue(raw, index);
#else
        values[index] = readRawSensor(index);
#endif
    }
}

int16_t xlineReadLinePosition(const uint16_t *values)
{
    uint32_t weightedSum = 0;
    uint32_t total = 0;

    for (uint8_t index = 0; index < XLINE_SENSOR_COUNT; index++)
    {
        uint32_t value = values[index];
        weightedSum += value * static_cast<uint32_t>(XLineSensorPositions[index]);
        total += value;
    }

    if (total == 0)
    {
        return static_cast<int16_t>(xlineLastPosition);
    }

    xlineLastPosition = static_cast<uint16_t>(weightedSum / total);
    return static_cast<int16_t>(xlineLastPosition);
}

void printXLineSensorValues(const uint16_t *values)
{
    Serial.print("XLine Sensor ");
    for (uint8_t index = 0; index < XLINE_SENSOR_COUNT; index++)
    {
        Serial.print(values[index]);
        Serial.print(" ");
    }
    Serial.println();
}