#include "motors.h"

Motor::Motor()
{
}

void Motor::setup()
{
    // Configure motor control pins as outputs
    pinMode(PWM_A1, OUTPUT);
    pinMode(PWM_A2, OUTPUT);
    pinMode(PWM_B1, OUTPUT);
    pinMode(PWM_B2, OUTPUT);
    pinMode(N_SLEEP, OUTPUT);

    // Set all motor pins to LOW initially (brake mode)
    digitalWrite(PWM_A1, LOW);
    digitalWrite(PWM_A2, LOW);
    digitalWrite(PWM_B1, LOW);
    digitalWrite(PWM_B2, LOW);

    // Initialize DRV8243 driver
    initDRV8243();
}

void Motor::initDRV8243()
{
    // Wake up driver
    digitalWrite(N_SLEEP, HIGH);
    delay(2); // Wait 2ms for internal charge pumps to power up and stabilize

    // Pulse nSLEEP LOW for 30 microseconds to clear any latched faults
    digitalWrite(N_SLEEP, LOW);
    delayMicroseconds(30);
    digitalWrite(N_SLEEP, HIGH);

    delay(2);
}

void Motor::forward(int speedA, int speedB)
{
    // Handling Motor A (IN/IN mode)
    if (speedA >= 0)
    {
        analogWrite(PWM_A1, speedA);
        digitalWrite(PWM_A2, LOW);
    }
    else
    {
        digitalWrite(PWM_A1, LOW);
        analogWrite(PWM_A2, -speedA);
    }

    // Handling Motor B (IN/IN mode)
    if (speedB >= 0)
    {
        analogWrite(PWM_B1, speedB);
        digitalWrite(PWM_B2, LOW);
    }
    else
    {
        digitalWrite(PWM_B1, LOW);
        analogWrite(PWM_B2, -speedB);
    }
}

void Motor::stop()
{
    digitalWrite(PWM_A1, LOW);
    digitalWrite(PWM_A2, LOW);
    digitalWrite(PWM_B1, LOW);
    digitalWrite(PWM_B2, LOW);
}