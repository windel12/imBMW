#ifndef TOOL_H
#define TOOL_H

#include <Arduino.h>
#include <Print.h>

//LCD stuff
#define flp(string) flashprint(PSTR(string));//save strings in flash, for LCD
#define sflp(string) sflashprint(PSTR(string));//save strings in flash, for serial port



//buttons stuff
#define OKbuttonPin 6
#define BackbuttonPin 7
#define LeftbuttonPin 8
#define RightbuttonPin 9 
#define OKbutton 1
#define Backbutton 2
#define Leftbutton 3
#define Rightbutton 4
#define KLINE_CS 5  




class TOOL : public Print
{
  public:

          TOOL();
		  byte CheckButtonPressed();
		  void clear();
		  void init();
		  void setCursor(uint8_t a, uint8_t b);
		  bool exists(const char* name);
		  void flashprint (const char p[]);
		  void sflashprint (const char p[]);
          virtual size_t write(uint8_t);
		  //using Print::write;
		  
  private:
		 uint8_t lcdCol;
		 uint8_t lcdRow;

};


#endif
