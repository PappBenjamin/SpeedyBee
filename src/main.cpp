#include "includes.h"
#include "defines.h"

// IO Expander
Adafruit_MCP23X17 mcp;

// Display
Adafruit_SSD1306 display(SCREEN_WIDTH, SCREEN_HEIGHT, &Wire, OLED_RESET);

// QTR Sensors
QTRSensors qtr;

void setup()
{
  Serial.begin(115200);
  Wire.begin();

  delay(200);

  pinMode(AIN1, OUTPUT);
  pinMode(AIN2, OUTPUT);
  pinMode(PWMA, OUTPUT);

  pinMode(BIN1, OUTPUT);
  pinMode(BIN2, OUTPUT);
  pinMode(PWMB, OUTPUT);

  pinMode(BUZZER, OUTPUT);

  // Buzzer
  playStartSong();

  // IO Expander
  setupExpander();

  // // display

  displayInit();
  displayPrint("SpeedyBee!");

  // QTR Sensors
  qtrCalibrate();
}

void loop()
{
  int KeypadNum = checkExpanderInterrupt();
  if (KeypadNum != -1)
  {
    // TODO: handle interrupt
    Serial.print("Interrupt on pin: ");
    Serial.println(KeypadNum);
  }

  u16_t QTRSensorValues[5];
  readQTRSensors(QTRSensorValues);
  printQTRSensorValues(QTRSensorValues);

  delay(100); // Delay for readability
}
