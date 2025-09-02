#include "expander.h"

/*
 * Check for interrupts on the IO expander
 @return keypad of the returned interrupt
 @return -1 if no interrupt
 */
int checkExpanderInterrupt(Adafruit_MCP23X17 &mcp){
  if (!digitalRead(INT_A))
  {
    int x = mcp.getLastInterruptPin();
    mcp.clearInterrupts(); // clear
    return x; 
  }
  return -1;
}



bool setupExpander(Adafruit_MCP23X17 &mcp){
     if (!mcp.begin_I2C(IO_ADDRESS))
  {
    Serial.println("Error.");
    while (1)
      ;
  }

  Serial.println("IO Expander found!");


    pinMode(INT_A, INPUT);


    // IO Expander
  mcp.setupInterrupts(true, false, LOW);

  // configure button pin for input with pull up
  mcp.pinMode(0, INPUT_PULLUP);
  mcp.pinMode(1, INPUT_PULLUP);
  mcp.pinMode(2, INPUT_PULLUP);
  mcp.pinMode(3, INPUT_PULLUP);
  mcp.pinMode(4, INPUT_PULLUP);
  mcp.pinMode(5, INPUT_PULLUP);

  // enable interrupt on button_pin
  mcp.setupInterruptPin(0, LOW);
  mcp.setupInterruptPin(1, LOW);
  mcp.setupInterruptPin(2, LOW);
  mcp.setupInterruptPin(3, LOW);
  mcp.setupInterruptPin(4, LOW);
  mcp.setupInterruptPin(5, LOW);

  return true;

}