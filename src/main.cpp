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

// Menu
Menu menu;
// Menu state and handler
MenuState currentMenuState = MAIN_MENU;

// PID Constants     speed correction
double Kp = 90;   //0.03   // Increase Proportional control slightly for better response
double Kd = 0.35;  //0.009    // Increase Derivative for stability in curves

int currentError = 0;
double filteredError = 0;  // Use for low-pass filtering error
int lastError = 0;

int currentspeedL = 10;
int currentspeedR = 10;

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

  // // display

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
  // }

  u16_t QTRSensorValues[5];
  readQTRSensors(QTRSensorValues);
  printQTRSensorValues(QTRSensorValues);
  display_IR(QTRSensorValues);

  int position = qtr.readLineBlack(QTRSensorValues);
   // Calculate error: assume center of line is 2500
  currentError = position - 2000;

  Serial.print("Error: ");
  Serial.println(currentError);

  // Low-pass filter on error to smooth rapid changes
  double alpha = 0.82;  // Smoothing factor
  filteredError = alpha * filteredError + (1 - alpha) * currentError;

  // Proportional and Derivative calculations
  double cError = pow(filteredError / 600, 3) / (1 + 1 * abs(pow(filteredError / 600, 3))); 

  int speedCorrection = (Kp * cError) + (Kd * (filteredError - lastError));
  // int speedCorrection = (Kp * currentError) + (Kd * (lastError));

  Serial.print("Speed Correction: ");
  Serial.println(speedCorrection);

  // Dynamic speed adjustment to reduce drift
  double beta = 0.15 - sqrt(abs((filteredError / 300) / (1 + abs(filteredError / 300))));

  int baseSpeed = 60;  // Base speed, slightly higher to maintain line-following momentum
  int speedAdjust = 20; // Additional speed for turns


  currentspeedL = baseSpeed + speedAdjust * beta - speedCorrection;

  Serial.print("Left Speed: ");
  Serial.println(currentspeedL);

  currentspeedR = baseSpeed + speedAdjust * beta + speedCorrection;

  Serial.print("Right Speed: ");
  Serial.println(currentspeedR);

  // currentspeedL = baseSpeed - speedCorrection;
  // currentspeedR = baseSpeed + speedCorrection;

  // Speed cap
  currentspeedL = constrain(currentspeedL, - 200, 200); 
  currentspeedR = constrain(currentspeedR, - 200, 200);


  // Drive motors
  forward(currentspeedL, currentspeedR);
  lastError = filteredError;

  // imu.read();
  // imu.printData();
  delay(5); // Delay for readability
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