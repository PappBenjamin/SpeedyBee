#ifndef MENU_H
#define MENU_H

#include "defines.h"
#include "display.h"

enum MenuScreen
{
    SCREEN_PID_TUNING = 0,
    SCREEN_SENSORS = 1,
    SCREEN_EDF = 2,
    SCREEN_COUNT
};

class Menu
{
public:
    Menu();

    // Updates internal state machine based on button presses
    // pass -1 if no button pressed
    void update(int keypadNum);

    // Renders the current menu state
    void render(Display &disp, uint16_t *currentQtrValues, int currentError);

    MenuScreen getCurrentScreen() const { return currentScreen; }

    bool isEdfEnabled() const { return edfEnabled; }
    int getEdfPwm() const { return edfPwm; }

private:
    MenuScreen currentScreen;
    int selectedOption;

    // Mode state for PID editing
    bool isEditing;

    bool edfEnabled;
    int edfPwm;
};

#endif // MENU_H