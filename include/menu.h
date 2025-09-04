#ifndef MENU_H
#define MENU_H

#include <Arduino.h>
#include "defines.h"
#include "display.h"
// Define menu states
enum MenuState
{
    MAIN_MENU,
    SETTINGS_MENU,
    CALIBRATION_MENU,
    IR_SENSOR_MENU,
    IMU_MENU,
    BATTERY_MENU,
    RACE_MODE_MENU
};

class Menu
{
public:
    Menu();
    void begin();
    void displayMainMenu();
    void displaySettingsMenu();
    void displayCalibrationMenu();
    void displayIRSensorMenu();
    void displayIMUMenu();
    void displayBatteryMenu();
    void handleMenuButtonPress(Keypad button);
    void showCurrentMenu();
};

#endif