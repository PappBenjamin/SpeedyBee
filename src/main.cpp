#include "includes.h"
#include "defines.h"

#define ARDUINOTRACE_ENABLE 1
#include <ArduinoTrace.h>

// IO Expander
Adafruit_MCP23X17 mcp;

// Display
Adafruit_SSD1306 display(SCREEN_WIDTH, SCREEN_HEIGHT, &Wire, OLED_RESET);

// QTR Sensors
QTRSensors qtr;

// IMU
IMU imu;

void settingPinsModes();

void setup()
{
  Serial.begin(115200);
  Wire.begin();

  TRACE();

  delay(2000);

  // Set pin modes
  settingPinsModes();

  // Buzzer
  playStartSong();

  // IO Expander
  setupExpander();

  // // display

  displayInit();
  displayPrint("SpeedyBee!");

  // QTR Sensors
  qtrCalibrate();

  // IMU
  imu.begin();
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
  display_IR(QTRSensorValues);

  // imu.read();
  delay(10); // Delay for readability
}

void settingPinsModes()
{
  pinMode(AIN1, OUTPUT);
  pinMode(AIN2, OUTPUT);
  pinMode(PWMA, OUTPUT);

  pinMode(BIN1, OUTPUT);
  pinMode(BIN2, OUTPUT);
  pinMode(PWMB, OUTPUT);

  pinMode(BUZZER, OUTPUT);
}