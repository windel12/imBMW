#ifndef KLINE_H
#define KLINE_H

#include <Arduino.h>

//Most of this table taken from OBDUINO project 
//(http://opengauge.googlecode.com)
//Kudos to them!
#define PID_SUPPORT00 0x00
#define MIL_CODE      0x01
#define FREEZE_DTC    0x02
#define FUEL_STATUS   0x03
#define LOAD_VALUE    0x04
#define COOLANT_TEMP  0x05
#define STFT_BANK1    0x06
#define LTFT_BANK1    0x07
#define STFT_BANK2    0x08
#define LTFT_BANK2    0x09
#define FUEL_PRESSURE 0x0A
#define MAN_PRESSURE  0x0B
#define ENGINE_RPM    0x0C
#define VEHICLE_SPEED 0x0D
#define TIMING_ADV    0x0E
#define INT_AIR_TEMP  0x0F
#define MAF_AIR_FLOW  0x10
#define THROTTLE_POS  0x11
#define SEC_AIR_STAT  0x12
#define OXY_SENSORS1  0x13
#define B1S1_O2_V     0x14
#define B1S2_O2_V     0x15
#define B1S3_O2_V     0x16
#define B1S4_O2_V     0x17
#define B2S1_O2_V     0x18
#define B2S2_O2_V     0x19
#define B2S3_O2_V     0x1A
#define B2S4_O2_V     0x1B
#define OBD_STD       0x1C
#define OXY_SENSORS2  0x1D
#define AUX_INPUT     0x1E
#define RUNTIME_START 0x1F
#define PID_SUPPORT20 0x20
#define DIST_MIL_ON   0x21
#define FUEL_RAIL_P   0x22
#define FUEL_RAIL_DIESEL 0x23
#define O2S1_WR_V     0x24
#define O2S2_WR_V     0x25
#define O2S3_WR_V     0x26
#define O2S4_WR_V     0x27
#define O2S5_WR_V     0x28
#define O2S6_WR_V     0x29
#define O2S7_WR_V     0x2A
#define O2S8_WR_V     0x2B
#define EGR           0x2C
#define EGR_ERROR     0x2D
#define EVAP_PURGE    0x2E
#define FUEL_LEVEL    0x2F
#define WARM_UPS      0x30
#define DIST_MIL_CLR  0x31
#define EVAP_PRESSURE 0x32
#define BARO_PRESSURE 0x33
#define O2S1_WR_C     0x34
#define O2S2_WR_C     0x35
#define O2S3_WR_C     0x36
#define O2S4_WR_C     0x37
#define O2S5_WR_C     0x38
#define O2S6_WR_C     0x39
#define O2S7_WR_C     0x3A
#define O2S8_WR_C     0x3B
#define CAT_TEMP_B1S1 0x3C
#define CAT_TEMP_B2S1 0x3D
#define CAT_TEMP_B1S2 0x3E
#define CAT_TEMP_B2S2 0x3F
#define PID_SUPPORT40 0x40
#define MONITOR_STAT  0x41//This one will be spared, sorry!
#define CTRL_MOD_V    0x42
#define ABS_LOAD_VAL  0x43
#define CMD_EQUIV_R   0x44
#define REL_THR_POS   0x45
#define AMBIENT_TEMP  0x46
#define ABS_THR_POS_B 0x47
#define ABS_THR_POS_C 0x48
#define ACCEL_PEDAL_D 0x49
#define ACCEL_PEDAL_E 0x4A
#define ACCEL_PEDAL_F 0x4B
#define CMD_THR_ACTU  0x4C
#define TIME_MIL_ON   0x4D
#define TIME_MIL_CLR  0x4E
#define OIL_TEMP      0x5C
#define PID_SUPPORT60 0x60
#define DD_TORQUE     0x61
#define ACTUAL_TORQUE 0x62
#define REF_TORQUE    0x63
#define MAF_SENSOR    0x66
#define COOLANT_TEMP2 0x67
#define INTAKE_AIR_T  0x68
#define EGR2          0x69
#define EGR_TEMP      0x6B
#define TURBO_INLET_P 0x6F
#define BOOST_PRESS_C 0x70
#define EXHAUST_PRESS 0x73
#define TURBO_RPM     0x74
#define TURBO_TEMP1   0x75
#define TURBO_TEMP2   0x76
#define ENGINE_RUNT   0x7F//Engine run time
#define PID_SUPPORT80 0x80
#define MAN_SURF_TMP  0x84//Manifold surface temp
#define INL_ABS_PRESS 0x87//Inlet Absolute pressure
//End of the PID defs
class KLINE 
{
  public:
          KLINE();
		  void begin(byte* adr, byte* tst);
          boolean securityAccess(long Key, byte accmod);
		  boolean securityAccess(long Key, long Key2, byte accmod, byte accparam);
		  byte readECUID(byte opt, byte buffer[]);
		  byte read(byte buffer[]);
		  boolean write(byte data[], byte len);
		  boolean write(byte data[], byte len1, byte data2[], byte len2);
          boolean startComms(byte buffer[]); 
		  boolean stopComms();
		  long getPIDSupport(byte PID);
		  byte getPID(byte PID, byte buffer[]);
		  boolean getEmissionsDTCs(byte buffer[]);
		  byte getDTCs(byte buffer[]);
		  boolean accessTiming(byte buffer[],byte cmd,byte p2min, byte p2max, byte p3min, byte p3max, byte p4min);
          boolean startDiagSession(byte sub);
		  boolean stopDiagSession();
		  boolean stopFlashSession();
          boolean fastInit();
		  byte requestDownload(byte memaddrh, byte memaddrm, byte memaddrl, byte dataformID, byte ucmsh, byte ucmsm, byte ucmsl);
		  boolean requestUpload(byte memaddrh, byte memaddrm, byte memaddrl, byte dataformID, byte ucmsh, byte ucmsm, byte ucmsl);
		  boolean transferData(long addr, byte buffer[], byte len);
		  boolean transferData24(long addr, byte buffer[], byte len);
		  boolean requestTransferData(byte buffer[]);
		  boolean testerPresent();
		  boolean clearEmissionsDTCs();
		  boolean clearGeneralDTCs();
		  boolean reset(byte type, byte buffer[]);
		  boolean startRoutine(byte buffer[], byte len);
		  boolean requestRoutineResults(byte ID, byte buffer[]);
		  boolean requestTransferExit();
		  boolean sendDownloadData(byte data[], byte len);
		  byte getUploadData(byte data[]);
		  byte slowInit(byte skip);
		  boolean dynDefDataID(byte buffer[], byte len);
		  byte readDataByLocalID(byte ID, byte buffer[]);
		  boolean writeDataByLocalID(byte ID, byte buffer[], byte len);
		  byte readMemByAddr(byte hiMem, byte midMem, byte lowMem, byte length, byte buffer[]);
		  boolean writeMemByAddr(byte hiMem, byte midMem, byte lowMem, byte length, byte buffer[]);
		  byte readKW1281(byte cnt, byte buf[]);
		  byte readKW1281(byte buf[]);
		  void writeKW1281(byte buf[], byte len);
		  void closeKW1281Session();
		  
		  
  private:
		  void send_delay(byte del);
          void serial_tx_off();
          void serial_rx_off();
		  boolean PIDwrite(byte data[], byte len);
		  byte PIDread(byte buffer[]);
          boolean SendKey(int Key1, int Key2, byte inc, byte buffer[]);
		  boolean SendKey(long Key, byte inc);
		  boolean LVL41Auth(long Key, long Key3, byte accmod, byte accparam);
		  boolean LVL1Auth(long Key, byte accmod);
		  boolean LVL3Auth(long Key, byte accmod);
		  boolean LVL67Auth(long Key, byte accmod);
		  boolean LVL30Auth(long Key, byte accmod);
          boolean RequestSeed(byte accmod, byte accparam,byte buffer[]);
		  boolean RequestSeed(byte accmod, byte buffer[]);
		  byte getIDData(byte data[]);
		  byte iso_checksum(byte *data, long len);
		  byte setspeed;
		  byte block;
		  byte* address;
		  byte* tester;
		  boolean readUploadData(byte buffer[]);
		  boolean writeByte(byte b);
		  void bitBang();

};

#endif
