#ifndef DEFINES_H
#define DEFINES_H

#define SCREEN_WIDTH 128
#define SCREEN_HEIGHT 64
#define OLED_RESET -1
#define SCREEN_ADDRESS 0x3C

#define XLINE_SENSOR_COUNT 16
#define XLINE_USE_ANALOG_READ 0
#define XLINE_DIGITAL_ACTIVE_LOW 0
#define XLINE_ANALOG_ACTIVE_LOW 1

#define XLINE_IR_OUT_PIN 11
#define XLINE_S0_PIN 12
#define XLINE_S1_PIN 13
#define XLINE_S2_PIN 14
#define XLINE_S3_PIN 15
#define XLINE_ANALOG_PIN 28

#define XLINE_POSITION_SCALE 1000
#define XLINE_LAST_SENSOR_SHIFT (XLINE_POSITION_SCALE)
#define XLINE_POSITION_CENTER ((((XLINE_SENSOR_COUNT - 1) * XLINE_POSITION_SCALE) - XLINE_LAST_SENSOR_SHIFT) / 2)

// IO Expander
#define IO_ADDRESS 0x22
#define INT_A 26

// Motor pins
#define PWM_A1 6
#define PWM_A2 7
#define PWM_B1 8
#define PWM_B2 9
#define N_SLEEP 22

#define EDF_PWM_PIN 10
#define BUZZER 3

enum Keypad
{
    // Key mappings from IO expander pins
    // Values reflect mcp.getLastInterruptPin() results
    Key1 = 7, // Up / Increase
    Key2 = 6, // Down / Decrease
    Key3 = 5, // Next Screen
    Key4 = 4, // Toggle Edit / Select
};

#endif // DEFINES_H