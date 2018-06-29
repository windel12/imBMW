//****************hud stuff****************
#include <Wire.h> 
#include <LiquidCrystal_I2C.h>

//includes for K-line and ECU hud
#include <tool.h> 
#include <kline.h>

KLINE Ecu;
TOOL hud;

byte adr=0x11;//Address for target ECU
byte tst=0xF1;//Address for tester

//SD stuff
#include <SdFat.h>
#include <SdFatUtil.h>
SdFat SD;
Sd2Card card; 
SdVolume volume; 
const uint8_t SD_CHIP_SELECT = 10;
SdFile myFile;
// store error strings in flash to save RAM 
#define error(s) error_P(PSTR(s))


void setup()                    
{
  Ecu.begin(&adr,&tst);
  hud.init();
  pinMode(10, OUTPUT);//CS for SD
  hud.flp("VAG ME7 EEPROM Demo");
  hud.setCursor(0,1);
  hud.flp("Checking SD...");
  //SD card INIT
  if (!SD.begin(SD_CHIP_SELECT, SPI_FULL_SPEED))
  {
    hud.setCursor(0,3);
    hud.flp("SD Error...");
    while (1){}
  }
  hud.setCursor(14,1);
  hud.flp("Done!");
  delay(1000);
  hud.clear();
}

void run()
{
  hud.clear();
  hud.setCursor(0,0);
  hud.flp("Press any button");
  while (hud.CheckButtonPressed()==0){}
  hud.clear();
  hud.setCursor(0,0);
  if (!start())
  {
    return;
  }
  if (!sendLoader())
  {
    hud.flp("Error");
    delay(2000);
    return;
  }
  if (!downloadEEPROM())
  {
    hud.flp("Error");
    delay(2000);
    return;
  }
}
   
boolean start()
{
  hud.flp("Connecting..");
  boolean poop=Ecu.slowInit(0);
  if (!poop)
  {
    hud.clear();
    hud.setCursor(0,0);
    hud.flp("No response");
    delay(2000);
    return 0;
  }
    byte buf[20];
   adr=10;//we change the address to 10
   poop=Ecu.startDiagSession(0x81);
   if (!poop)
   {
     hud.clear();
     hud.setCursor(0,0);
     hud.flp("Diag rejected");
     delay(1000);
     return 0;
   }
   else
   {
     hud.flp("Done!");
     hud.setCursor(0,1);
     return 1;
   }
}

boolean sendLoader()
{
  hud.flp("Send loader..");
  byte buf[]={0x3E,0x28,0x63,0x29,0x20,0x32,0x30,0x30,0x63,0x20,0x16,0x72,0x31,0x6E,0x6B,0x22,0x24,0x25,0x64,0x62,0x62,0x20,0x20};
  if(!Ecu.write(buf,sizeof(buf)))
  {
    return 0;
  }
  Ecu.read(buf);
  if (buf[0]!=0x7E)
  {
    return 0;
  }
  byte buf2[]={0x3E,0x56,0x69,0x15,0x1C,0x20,0x12,0x19,0x15,0x12,0x1C,0x20,0x17,0x15,0x1E,0x19,0x17,0x20,0x1B,0x15,0x6D,0x6D,0x65,0x72};
  if(!Ecu.write(buf2,sizeof(buf2)))
  {
    return 0;
  }
  Ecu.read(buf);
  if (buf[0]!=0x7E)
  {
    return 0;
  }
  byte buf3[]={0x3E,0x50,0x72,0x6F,0x73,0x74,0x20,0x56,0x6F,0x6E,0x20,0x52,0x65,0x76,0x6F};//to store replies
  if(!Ecu.write(buf3,sizeof(buf3)))
  {
    return 0;
  }
  Ecu.read(buf);
  if (buf[0]!=0x7E)
  {
    return 0;
  }
  byte buf4[]={0xF0,4};
  if(!Ecu.dynDefDataID(buf4,sizeof(buf4)))
  {
    return 0;
  }
  byte buf5[]={0xF0,3,1,8,0,0Xe2,0x10};
  if(!Ecu.dynDefDataID(buf5,sizeof(buf5)))
  {
    return 0;
  }
  byte buf6[]={0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF};
  if(!Ecu.writeDataByLocalID(0xF0,buf6,sizeof(buf6)))
  {
    return 0;
  }
  
  hud.flp("Done!");
  hud.setCursor(0,2);
  return 1; 
}
  
  
  

boolean downloadEEPROM()
{
  hud.setCursor(0,2);
  hud.flp("Reading EEPROM");
     if (myFile.open("eeprom.bin", O_WRITE))
     {
       myFile.remove();
       myFile.close();
     }
     if (!myFile.open("eeprom.bin", O_CREAT | O_WRITE))
     {
       hud.flp("SD error");
       delay(1000);
       return 0;
     }
     long addr=0x383D0E;//Start of EEPROM
     byte buf[128];
     Ecu.readMemByAddr(addr>>16,addr>>8,addr,0x80,buf);
     byte i=buf[0];
     long cnt=0;
     while (i!=0x05&&cnt<0x7F)//search for beginning of eeprom
     {
       cnt++;
       i=buf[cnt];
     }
     if (cnt==0x80)
     {
       return 0;//need to add dynamic search, not just an error
     }
     addr=addr+cnt;
     byte before=0;
     cnt=0;
     while (cnt<0x200)//End of 512kB external flash in RAM
     {
       byte buf[80];
       byte len=Ecu.readMemByAddr(addr>>16,addr>>8,addr,0x80,buf);
       myFile.write(buf,len);
       byte percent=(cnt*100)/0x1FFF;
       if (percent != before)
       {
         hud.setCursor(0,3);
         hud.print(percent);
         hud.flp("%");
         before=percent;
       }
       cnt=cnt+len;
       addr=addr+0x80;
     }
     myFile.close();
     Ecu.stopFlashSession();
     hud.setCursor(0,3);
     hud.flp("Done");
     delay(2000);
     return 1;
    
}








void loop()
{
  run();
  
}