//****************hud stuff****************
#include <Wire.h> 
#include <LiquidCrystal_I2C.h>

//includes for K-line and ECU hud
#include <tool.h> 
#include <kline.h>

KLINE Ecu;
TOOL hud;

byte adr=0x12;//Address for target ECU
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
  hud.flp("Renault EDC15C2 Demo");
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
  if (!Ecu.fastInit())
  {
    hud.flp("No response");
    delay(2000);
    return;
  }
    byte buf[255];
   if (!Ecu.read(buf))
   {
     hud.flp("CRC Error");
     delay(2000);
     return;
   }
   if (!Ecu.startDiagSession(0x85))
   {
     hud.flp("Diag rejected");
     delay(1000);
       return;
   }
   else
   {
     hud.flp("Diag accepted");
     hud.setCursor(0,1);
     if (!Ecu.securityAccess(0x7F272A4F,0x67))//key for renault EDC15C2
     {
       hud.flp("Security Error");
       delay(1000);
       return;
     }
     else
     {
       hud.flp("Security bypassed!");
     }
     hud.setCursor(0,2);
     if (!Ecu.requestDownload(0x40,0xE0,0x00,0x00,0x00,0x04,0x20))
     {
       hud.flp("Download rejected");
       delay(1000);
       return;
     }
     else
     {
       hud.flp("Download Accepted");
     }
     hud.setCursor(0,3);
     if (!myFile.open("EDC15C2.FLD", O_READ))
     {
       hud.flp("Loader not found");
       delay(1000);
       return;
     }
     int lsize=myFile.fileSize();
     int cnt=0;
     while (cnt<lsize)
     {
       int tmp=lsize-cnt;
       byte buffersize=0;
       
       if (tmp > 0xFE)
       {
         buffersize=0xFE;
         cnt=cnt+0xFE;
       }
       else
       {
         buffersize=tmp;
         cnt=cnt+buffersize;
       }
       byte crip[buffersize];
       myFile.read(crip,buffersize);
       Ecu.sendDownloadData(crip,buffersize);
     }
     myFile.close();//loader sent
     for (byte pp=0;pp<1;pp++)//send some config params
     {
       byte rr[]={0x36,0xC3,0xC3,0x89,0xC5,0xA6,0xFE,0x1B,0xC8};
       Ecu.write(rr,sizeof(rr));
       Ecu.read(rr);
       if (rr[0]!= 0x76)
       {
         hud.flp("Loader Error");
         delay(1000);
       return;
       }
       byte r2[]={0x31,0x02,0x08,0x00,0x00,0x0F,0xBF,0xFF};
       Ecu.write(r2,sizeof(r2));
       Ecu.read(r2);
     }
     if (myFile.open("dump.bin", O_WRITE))
     {
       myFile.remove();
       myFile.close();
     }
     if (!myFile.open("dump.bin", O_CREAT | O_WRITE))
     {
       hud.flp("SD error");
       delay(1000);
       return;
     }
     long addr=0x23080000;//Start of external flash
     byte before=0;
     while (addr < 0x230FFFFF)//End of 512kB external flash
     {
       byte request[]={addr>>24,addr>>16,addr>>8,addr,0x00};
       if (addr < 0x230FFF03)
       {
         request[4]=0xFE;
       }
       else
       {
         request[4]=0x2310000-addr;
       }
       addr=addr+request[4];
       Ecu.write(request,sizeof(request));
       byte len=Ecu.getUploadData(buf);
       if (len==0)
       {
         hud.setCursor(0,3);
         hud.flp("Download Error");
         delay(1000);
       return;
       }
       myFile.write(buf,len);
       byte percent=((addr-0x23080000)*100)/0x7FFFF;
       if (percent != before)
       {
         hud.setCursor(0,3);
         hud.print(percent);
         hud.flp("%");
         before=percent;
       }
     }
     myFile.close();
     Ecu.stopFlashSession();
   }
   return;
}
   

void loop(){run();}