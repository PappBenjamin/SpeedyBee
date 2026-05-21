#ifndef DISPLAY_H
#define DISPLAY_H

#include <Arduino.h>
#include <Wire.h>
#include <Adafruit_GFX.h>
#include <Adafruit_SSD1306.h>
#include "defines.h"

class Display
{
public:
    Display();

    // Initialize display
    bool setup();

    // Clear display
    void clear();

    // Draw loading screen
    void drawLoadingScreen(const char *status = "Loading...");

    // Draw the PID Tuning screen
    // selectedOption: 0=Kp, 1=Kd, 2=Base, 3=MaxTurn
    void drawPidTuningScreen(double Kp, double Kd, double baseSpeed, double maxTurnSpeed, int selectedOption);

    // Draw the Sensor View screen
    void drawSensorScreen(uint16_t *qtrValues, int sensorCount, int positionError);

    // Draw EDF Settings screen
    // selectedOption: 0=Enable/Disable, 1=PWM
    void drawEdfScreen(bool enabled, int pwm, int selectedOption);

private:
    Adafruit_SSD1306 display;
};

#endif // DISPLAY_H