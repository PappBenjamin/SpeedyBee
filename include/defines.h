#ifndef DEFINES_H
#define DEFINES_H

#define SCREEN_WIDTH 128
#define SCREEN_HEIGHT 64
#define OLED_RESET -1
#define SCREEN_ADDRESS 0x3C

#define QTRSensorCount 5

// IO Expander
#define IO_ADDRESS 0x22
#define INT_A 26

// Motor pins
#define PWM_A1 6
#define PWM_A2 7
#define PWM_B1 8
#define PWM_B2 9
#define N_SLEEP 22

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