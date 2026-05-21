#include "includes.h"
#include "defines.h"

#define ARDUINOTRACE_ENABLE 1
#include <ArduinoTrace.h>

#include "motors.h"
#include "menu.h"
#include "display.h"

// Hardware instances
Adafruit_MCP23X17 mcp;
Display uiDisplay;
QTRSensors qtr;
IMU imu;
Motor motor;
Menu robotMenu;

// Global Sensor State
uint16_t QTRSensorValues[QTRSensorCount];
int currentError = 0;
double filteredError = 0;
int lastError = 0;

// Core Tuning Variables (Modified by Menu/Serial)
double Kp = 1.525;
double Kd = 0.001;
double BaseSpeed = 80.0;
double MaxTurnSpeed = 90.0;

// Forward declarations
void readSensorDataAndControl();
void readSerialDataAndControl();

void setup()
{
  Serial.begin(115200);
  Wire.begin();

  TRACE();

  delay(1000);

  // Initialize motors
  motor.setup();

  // Buzzer init
  pinMode(BUZZER, OUTPUT);
  analogWrite(BUZZER, 255);
  delay(100);
  analogWrite(BUZZER, 0);

  // Display init
  uiDisplay.setup();
  uiDisplay.drawLoadingScreen("Display Init");

  // IO Expander init
  uiDisplay.drawLoadingScreen("Expander Init");
  setupExpander(); // Provided elsewhere in the project

  // QTR Sensors
  uiDisplay.drawLoadingScreen("QTR calibration");
  qtrCalibrate(); // Provided elsewhere in the project

  // IMU
  uiDisplay.drawLoadingScreen("IMU init");
  if (!imu.begin())
  {
    Serial.println("BMI323 failed to initialize!");
    uiDisplay.drawLoadingScreen("IMU FAIL!");
    while (1)
      ;
  }

  uiDisplay.drawLoadingScreen("Setup done!");
  Serial.println("Setup done!");
  delay(1000);

  analogWrite(BUZZER, 255);
  delay(500);
  analogWrite(BUZZER, 0);
}

void loop()
{
  // 1. Process Menu Input
  int keypadNum = checkExpanderInterrupt();
  if (keypadNum != -1)
  {
    Serial.print("Keypad Pressed: ");
    Serial.println(keypadNum);
    robotMenu.update(keypadNum);
  }

  // 2. Read Serial overrides (optional)
  readSerialDataAndControl();

  // 3. Sensor & Control Core Update
  readSensorDataAndControl();

  // 4. Auxiliary Sensor update
  imu.read();
  // imu.printData(); // Optional debug

  // 5. Update UI (throttled inside or called directly)
  robotMenu.render(uiDisplay, QTRSensorValues, currentError);

  // Note: EDF control not yet implemented hardware-wise,
  // but logic is prepared via robotMenu.isEdfEnabled()
  // and robotMenu.getEdfPwm() if an ESC is wired.

  delay(10); // Loop stability
}

void readSerialDataAndControl()
{
  if (Serial.available() > 0)
  {
    String input = Serial.readStringUntil('\n');
    input.trim();

    int commaIndex = input.indexOf(',');
    if (commaIndex != -1)
    {
      String firstValue = input.substring(0, commaIndex);
      String secondValue = input.substring(commaIndex + 1);
      // More robust parting should be used for full replacement, simplified given context
      Kp = firstValue.toDouble();
      Kd = secondValue.toDouble();
    }
  }
}

void readSensorDataAndControl()
{
  // Assume generic reads and display hook defined in separate files
  readQTRSensors(QTRSensorValues);

  // Position is white-line targeted
  int position = qtr.readLineWhite(QTRSensorValues);

  // Update globals for menu viewing
  currentError = position - 2000;

  // Low-pass filter
  double alpha = 0.25;
  filteredError = alpha * filteredError + (1 - alpha) * currentError;

  // Smoothing non-linear mapping
  double tanhError = tanh(filteredError / 1000.0);

  // PD control calculation
  double speedCorrection = (Kp * tanhError) + (Kd * (filteredError - lastError));

  // Apply correction
  int leftSpeed = BaseSpeed - (int)(speedCorrection * MaxTurnSpeed);
  int rightSpeed = BaseSpeed + (int)(speedCorrection * MaxTurnSpeed);

  // Limit motor speed map strictly for driver limits
  leftSpeed = constrain(leftSpeed, -200, 200);
  rightSpeed = constrain(rightSpeed, -200, 200);

  // Drive new motor interface
  motor.forward(leftSpeed, rightSpeed);

  lastError = filteredError;
}