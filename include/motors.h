#ifndef MOTORS_H
#define MOTORS_H

#include <Arduino.h>
#include "defines.h"

class Motor
{
public:
    Motor();

    // Initialize motor pins and DRV8243 driver
    void setup();

    // Set speeds for both motors. Positive values for forward, negative for reverse.
    void forward(int speedA, int speedB);

    // Stop both motors (brake)
    void stop();

private:
    void initDRV8243();
};

#endif // MOTORS_H