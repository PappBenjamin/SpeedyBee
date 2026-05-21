#include "display.h"

Display::Display()
    : display(SCREEN_WIDTH, SCREEN_HEIGHT, &Wire, OLED_RESET)
{
}

bool Display::setup()
{
    if (!display.begin(SSD1306_SWITCHCAPVCC, SCREEN_ADDRESS))
    {
        Serial.println("SSD1306 initialization failed!");
        return false;
    }
    display.clearDisplay();
    display.setTextColor(SSD1306_WHITE);
    display.display();
    return true;
}

void Display::clear()
{
    display.clearDisplay();
    display.display();
}

void Display::drawLoadingScreen(const char *status)
{
    display.clearDisplay();
    display.setTextSize(2);
    display.setCursor(10, 20);
    display.print("SpeedyBee");

    display.setTextSize(1);
    display.setCursor(10, 45);
    display.print(status);
    display.display();
}

void Display::drawPidTuningScreen(double Kp, double Kd, double baseSpeed, double maxTurnSpeed, int selectedOption)
{
    display.clearDisplay();
    display.setTextSize(1);

    // Title
    display.setCursor(0, 0);
    display.print("PID TUNING");

    const int startY = 16;
    const int lineHeight = 12;

    // 0 = Kp
    display.setCursor(10, startY + 0 * lineHeight);
    if (selectedOption == 0)
        display.print("> ");
    display.print("Kp: ");
    display.print(Kp, 3);

    // 1 = Kd
    display.setCursor(10, startY + 1 * lineHeight);
    if (selectedOption == 1)
        display.print("> ");
    display.print("Kd: ");
    display.print(Kd, 3);

    // 2 = Base Speed
    display.setCursor(10, startY + 2 * lineHeight);
    if (selectedOption == 2)
        display.print("> ");
    display.print("Base: ");
    display.print(baseSpeed, 0);

    // 3 = Max Turn Speed
    display.setCursor(10, startY + 3 * lineHeight);
    if (selectedOption == 3)
        display.print("> ");
    display.print("Turn: ");
    display.print(maxTurnSpeed, 0);

    display.display();
}

void Display::drawSensorScreen(uint16_t *qtrValues, int sensorCount, int positionError)
{
    display.clearDisplay();
    display.setTextSize(1);

    // Title
    display.setCursor(0, 0);
    display.print("SENSOR VIEW");

    // Draw bar graph for sensors
    int barWidth = SCREEN_WIDTH / sensorCount;
    if (barWidth < 2)
        barWidth = 2;

    // Assuming values are 0-1000 or similar
    for (int i = 0; i < sensorCount; i++)
    {
        // scale 0-1000 to max 30 pixels height
        int h = map(qtrValues[i], 0, 1000, 0, 30);
        h = constrain(h, 0, 30);
        int x = i * barWidth + (barWidth / 2) - 5;
        int y = 50 - h;
        display.fillRect(x, y, 10, h, SSD1306_WHITE);
    }

    display.setCursor(0, 54);
    display.print("Err: ");
    display.print(positionError);

    display.display();
}

void Display::drawEdfScreen(bool enabled, int pwm, int selectedOption)
{
    display.clearDisplay();
    display.setTextSize(1);

    display.setCursor(0, 0);
    display.print("EDF SETUP");

    display.setCursor(10, 20);
    if (selectedOption == 0)
        display.print("> ");
    display.print("Status: ");
    display.print(enabled ? "ON" : "OFF");

    display.setCursor(10, 40);
    if (selectedOption == 1)
        display.print("> ");
    display.print("PWM: ");
    display.print(pwm);

    display.display();
}