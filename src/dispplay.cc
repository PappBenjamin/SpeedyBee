#include "display.h"

void displayInit()
{
  if (!display.begin(SSD1306_SWITCHCAPVCC, SCREEN_ADDRESS))
  {
    Serial1.println("SSD1306 initialization failed!");
    while (true)
      ;
  }
}

void displayClear()
{
  display.clearDisplay();
  display.display();
}

void displayPrint(const char *text)
{
  display.clearDisplay();
  display.setTextSize(2.5);
  display.setTextColor(SSD1306_WHITE);
  display.setCursor(0, 29.5);
  display.println(text);
  display.display();
}

void display_IR(u16_t *irValues)
{
  display.clearDisplay();
  uint8_t bar_width = SCREEN_WIDTH / QTRSensorCount;
  if (bar_width < 2)
    bar_width = 2; // minimum width for visibility

  for (uint8_t i = 0; i < QTRSensorCount; i++)
  {
    uint16_t ir = irValues[i];
    if (ir > 2500)
      ir = 2500;
    uint8_t bar_height = (ir * SCREEN_HEIGHT) / 1000;
    uint8_t x = i * bar_width;
    uint8_t y = SCREEN_HEIGHT - bar_height;
    display.fillRect(x, y, bar_width - 1, bar_height, SSD1306_WHITE);
  }
  display.display();
}
