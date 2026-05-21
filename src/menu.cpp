#include "menu.h"

// External references to main tuning variables
extern double Kp;
extern double Kd;
extern double BaseSpeed;
extern double MaxTurnSpeed;

Menu::Menu()
{
    currentScreen = SCREEN_PID_TUNING;
    selectedOption = 0;
    isEditing = false;
    edfEnabled = false;
    edfPwm = 150;
}

void Menu::update(int keypadNum)
{
    if (keypadNum == -1)
        return; // No key pressed

    // Assuming Key mappings based on IO Expander enum from defines.h:
    // Key1 = 4 (Up / Increase)
    // Key2 = 5 (Down / Decrease)
    // BTN0 = 0 (Select / Toggle Edit)
    // BTN1 = 1 (Next Screen)

    if (keypadNum == Key4)
    {
        if (!isEditing)
        {
            // Cycle screen
            int nextScreen = (static_cast<int>(currentScreen) + 1) % SCREEN_COUNT;
            currentScreen = static_cast<MenuScreen>(nextScreen);
            selectedOption = 0;
        }
        return;
    }

    if (keypadNum == Key3)
    {
        // Toggle Edit Mode in applicable screens
        if (currentScreen == SCREEN_PID_TUNING || currentScreen == SCREEN_EDF)
        {
            isEditing = !isEditing;
        }
        return;
    }

    if (!isEditing)
    {
        // Navigation (Up/Down) within current screen options
        if (keypadNum == Key1)
        { // Up
            selectedOption--;
        }
        else if (keypadNum == Key2)
        { // Down
            selectedOption++;
        }

        // Wrap-around logic per screen
        if (currentScreen == SCREEN_PID_TUNING)
        {
            if (selectedOption < 0)
                selectedOption = 3;
            if (selectedOption > 3)
                selectedOption = 0;
        }
        else if (currentScreen == SCREEN_EDF)
        {
            if (selectedOption < 0)
                selectedOption = 1;
            if (selectedOption > 1)
                selectedOption = 0;
        }
    }
    else
    {
        // Editing values
        if (currentScreen == SCREEN_PID_TUNING)
        {
            double tweakDir = (keypadNum == Key1) ? 1.0 : (keypadNum == Key2 ? -1.0 : 0.0);

            if (selectedOption == 0)
                Kp += tweakDir * 0.05;
            else if (selectedOption == 1)
                Kd += tweakDir * 0.005;
            else if (selectedOption == 2)
                BaseSpeed += tweakDir * 5.0;
            else if (selectedOption == 3)
                MaxTurnSpeed += tweakDir * 5.0;

            if (Kp < 0)
                Kp = 0;
            if (Kd < 0)
                Kd = 0;
            if (BaseSpeed < 0)
                BaseSpeed = 0;
            if (MaxTurnSpeed < 0)
                MaxTurnSpeed = 0;
        }
        else if (currentScreen == SCREEN_EDF)
        {
            if (selectedOption == 0)
            {
                // Toggle mode (any button)
                if (keypadNum == Key1 || keypadNum == Key2)
                {
                    edfEnabled = !edfEnabled;
                }
            }
            else if (selectedOption == 1)
            {
                int tweakDir = (keypadNum == Key1) ? 10 : (keypadNum == Key2 ? -10 : 0);
                edfPwm += tweakDir;
                if (edfPwm < 0)
                    edfPwm = 0;
                if (edfPwm > 255)
                    edfPwm = 255; // Assuming 8-bit pwm max
            }
        }
    }
}

void Menu::render(Display &disp, uint16_t *currentQtrValues, int currentError)
{
    switch (currentScreen)
    {
    case SCREEN_PID_TUNING:
        disp.drawPidTuningScreen(Kp, Kd, BaseSpeed, MaxTurnSpeed, selectedOption);
        // Optional: Draw a blinking cursor or indicator if 'isEditing' is true
        break;

    case SCREEN_SENSORS:
        disp.drawSensorScreen(currentQtrValues, QTRSensorCount, currentError);
        break;

    case SCREEN_EDF:
        disp.drawEdfScreen(edfEnabled, edfPwm, selectedOption);
        break;

    default:
        break;
    }
}