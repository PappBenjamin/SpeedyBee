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

// Menu2
Menu menu;
// Menu state and handler
MenuState currentMenuState = MAIN_MENU;

// PID Constants        speed correction
double Kp = 1.525;   /*       1.525          Increase Proportional control slightly for better response */
double Kd = 0.0017;  /*       0.0015         Increase Derivative for stability in curves */

int currentError = 0; // Current position error
double filteredError = 0;  // Use for low-pass filtering error
int lastError = 0;

void forward(int speedA, int speedB);




void settingPinsModes();

void setup()
{
  Serial.begin(115200);
  Wire.begin();

  TRACE();

  delay(1000);

  // Set pin modes
  settingPinsModes();

  analogWrite(BUZZER, 255);
  delay(100);
  analogWrite(BUZZER, 0);

  // display

  displayInit();
  displayPrint("SpeedyBee!");

  // Buzzer
  // playStartSong();

  //IO Expander
  // setupExpander();
  displayPrint("Expander init");

  // QTR Sensors
  displayPrint("QTR calibration ...");
  qtrCalibrate();

  // IMU
  // displayPrint("IMU init");
  // imu.begin();

  displayPrint("Setup done!");

  Serial.println("Setup done!");
  delay(1000);

  analogWrite(BUZZER, 255);
  delay(500);
  analogWrite(BUZZER, 0);
}

void loop()
{
  // int KeypadNum = checkExpanderInterrupt();
  // if (KeypadNum != -1)
  // {
  //   // TODO: handle menu
  //   Serial.print("Keypad: ");
  //   Serial.println(KeypadNum);
  //   displayPrint("Keypad: ");
  //   displayPrint(String(KeypadNum).c_str());
  //   delay(200);
  // }

  u16_t QTRSensorValues[5];
  readQTRSensors(QTRSensorValues);
  printQTRSensorValues(QTRSensorValues);
  display_IR(QTRSensorValues);

  int position = qtr.readLineWhite(QTRSensorValues);
  currentError = position - 2000; // Calculate error: assume center of line is 2000

  Serial.print("Error: ");
  Serial.println(currentError);

  // Low-pass filter on error
  double alpha = 0.25;  // if alpha is closer to 1, less responsive but smoother
  filteredError = alpha * filteredError + (1 - alpha) * currentError;

  Serial.print("Filtered Error: ");
  Serial.println(filteredError);

  // Cubic function for speed correction
  double tanhError = tanh(filteredError / 1000.0);

  Serial.print(" T Error: ");
  Serial.println(tanhError);

// --- PD control calculation ---
double speedCorrection = (Kp * tanhError) + (Kd * (filteredError - lastError));

// --- Base speeds ---
int baseSpeed = 100;        // Normal forward speed
int maxTurnSpeed = 110;     // Max extra speed added/subtracted for turning

// --- Apply correction symmetrically ---
int leftSpeed  = baseSpeed - (int)(speedCorrection * maxTurnSpeed);
int rightSpeed = baseSpeed + (int)(speedCorrection * maxTurnSpeed);

// --- Limit motor speed ---
leftSpeed  = constrain(leftSpeed, -200, 200);
rightSpeed = constrain(rightSpeed, -200, 200);

// --- Debug ---
Serial.print("Speed Correction: "); Serial.println(speedCorrection);
Serial.print("Left Speed: "); Serial.println(leftSpeed);
Serial.print("Right Speed: "); Serial.println(rightSpeed);


  // Drive motors
  forward(leftSpeed, rightSpeed);
  lastError = filteredError;

  // imu.read();
  // imu.printData();

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

void forward(int speedA, int speedB)
{
  if (speedA >= 0)
  {
    digitalWrite(AIN1, LOW);
    digitalWrite(AIN2, HIGH);
    analogWrite(PWMA, speedA);
  }
  else
  {
    digitalWrite(AIN1, HIGH);
    digitalWrite(AIN2, LOW);
    analogWrite(PWMA, -speedA);
  }

  if (speedB >= 0)
  {
    digitalWrite(BIN1, LOW);
    digitalWrite(BIN2, HIGH);
    analogWrite(PWMB, speedB);
  }
  else
  {
    digitalWrite(BIN1, HIGH);
    digitalWrite(BIN2, LOW);
    analogWrite(PWMB, -speedB);
  }
}