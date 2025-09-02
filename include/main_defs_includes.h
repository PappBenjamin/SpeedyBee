#ifndef MAIN_DEFS_INCLUDES_H
#define MAIN_DEFS_INCLUDES_H

#include <Arduino.h>


// Buzzer
#include "pitches.h"

// QTR Sensors
#include <QTRSensors.h>


// display
#include <Adafruit_GFX.h>
#include <Adafruit_SSD1306.h>

#define SCREEN_WIDTH 128
#define SCREEN_HEIGHT 64
#define OLED_RESET -1
#define SCREEN_ADDRESS 0x3C



// IO Expander
#include <Adafruit_MCP23X17.h>
#define IO_ADDRESS 0x22
#define INT_A 26

// Motor pins
#define AIN1 6
#define AIN2 7
#define PWMA 8

#define BIN1 20
#define BIN2 21
#define PWMB 22

#define BUZZER 3

#define SCL 5
#define SDA 4



#endif // MAIN_DEFS_INCLUDES_H