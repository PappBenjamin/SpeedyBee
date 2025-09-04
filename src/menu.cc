#include "menu.h"

extern MenuState currentMenuState;
extern Menu menu;

Menu::Menu()
{
}

void Menu::displayMainMenu()
{
    display.clearDisplay();
    display.setCursor(0, 0);
    display.setTextSize(1);
    display.setTextColor(SSD1306_WHITE);
    display.println("Main Menu");
    display.println("1. Settings");
    display.println("2. Calibration");
    display.println("3. IR Sensor");
    display.println("4. IMU");
    display.println("5. Battery");
    display.display();
}

void Menu::displaySettingsMenu()
{
    display.clearDisplay();
    display.setCursor(0, 0);
    display.setTextSize(1);
    display.setTextColor(SSD1306_WHITE);
    display.println("Settings Menu");
    display.display();
}

void Menu::displayCalibrationMenu()
{
    display.clearDisplay();
    display.setCursor(0, 0);
    display.setTextSize(1);
    display.setTextColor(SSD1306_WHITE);
    display.println("Calibration Menu");
    display.display();
}

void Menu::displayIRSensorMenu()
{
    display.clearDisplay();
    display.setCursor(0, 0);
    display.setTextSize(1);
    display.setTextColor(SSD1306_WHITE);
    display.println("IR Sensor Menu");
    display.display();
}

void Menu::displayIMUMenu()
{
    display.clearDisplay();
    display.setCursor(0, 0);
    display.setTextSize(1);
    display.setTextColor(SSD1306_WHITE);
    display.println("IMU Menu");
    display.display();
}

void Menu::displayBatteryMenu()
{
    display.clearDisplay();
    display.setCursor(0, 0);
    display.setTextSize(1);
    display.setTextColor(SSD1306_WHITE);
    display.println("Battery Menu");
    display.display();
}

void Menu::handleMenuButtonPress(Keypad button)
{
    // Cycle through menu states on button press
    switch (button)
    {
    case Key1:
        currentMenuState = SETTINGS_MENU;
        break;
    case Key2:
        currentMenuState = CALIBRATION_MENU;
        break;
    case Key3:
        currentMenuState = IR_SENSOR_MENU;
        break;
    case Key4:
        currentMenuState = IMU_MENU;
        break;
    case BTN0:
        currentMenuState = BATTERY_MENU;
        break;
    case BTN1:
        currentMenuState = MAIN_MENU;
        break;
    default:
        currentMenuState = MAIN_MENU;
        break;
    }

    switch (currentMenuState)
    {
    case MAIN_MENU:
        currentMenuState = SETTINGS_MENU;
        break;
    case SETTINGS_MENU:
        currentMenuState = CALIBRATION_MENU;
        break;
    case CALIBRATION_MENU:
        currentMenuState = IR_SENSOR_MENU;
        break;
    case IR_SENSOR_MENU:
        currentMenuState = IMU_MENU;
        break;
    case IMU_MENU:
        currentMenuState = BATTERY_MENU;
        break;
    case BATTERY_MENU:
        currentMenuState = MAIN_MENU;
        break;
    default:
        currentMenuState = MAIN_MENU;
        break;
    }
}

void Menu::showCurrentMenu()
{
    switch (currentMenuState)
    {
    case MAIN_MENU:
        menu.displayMainMenu();
        break;
    case SETTINGS_MENU:
        menu.displaySettingsMenu();
        break;
    case CALIBRATION_MENU:
        menu.displayCalibrationMenu();
        break;
    case IR_SENSOR_MENU:
        menu.displayIRSensorMenu();
        break;
    case IMU_MENU:
        menu.displayIMUMenu();
        break;
    case BATTERY_MENU:
        menu.displayBatteryMenu();
        break;
    default:
        menu.displayMainMenu();
        break;
    }
}
