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
IMU imu;
Motor motor;
Menu robotMenu;

// Global Sensor State
uint16_t XLineSensorValues[XLINE_SENSOR_COUNT];
int currentError = 0;
double filteredError = 0;
int lastError = 0;
int currentPwmL = 0;
int currentPwmR = 0;

// Core Tuning Variables (Modified by Menu/Serial)
double Kp = 4.825;
double Kd = 0.001;
double BaseSpeed = 60.0;
double MaxTurnSpeed = 5.0;

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

  // XLine Sensors
  uiDisplay.drawLoadingScreen("XLine calibration");
  xlineCalibrate();

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
  robotMenu.render(uiDisplay, XLineSensorValues, currentError, currentPwmL, currentPwmR);

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
  readXLineSensors(XLineSensorValues);

  int position = xlineReadLinePosition(XLineSensorValues);

  // Update globals for menu viewing
  currentError = position - XLINE_POSITION_CENTER;

  // Low-pass filter
  double alpha = 0.25;
  filteredError = alpha * filteredError + (1 - alpha) * currentError;

  // Smoothing non-linear mapping
  double tanhError = tanh(filteredError / 1000.0);

  // PD control calculation
  double speedCorrection = (Kp * tanhError) + (Kd * (filteredError - lastError));

  // Apply correction
  int leftSpeed = BaseSpeed + (int)(speedCorrection * MaxTurnSpeed);
  int rightSpeed = BaseSpeed - (int)(speedCorrection * MaxTurnSpeed);

  // Limit motor speed map strictly for driver limits
  leftSpeed = constrain(leftSpeed, -150, 150);
  rightSpeed = constrain(rightSpeed, -150, 150);

  // Drive new motor interface
  currentPwmL = leftSpeed;
  currentPwmR = rightSpeed;

  motor.forward(leftSpeed, rightSpeed);

  lastError = filteredError;
}