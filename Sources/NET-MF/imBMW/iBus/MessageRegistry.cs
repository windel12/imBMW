using System.Collections;
using System.Text;
using imBMW.Tools;
using imBMW.Enums;
using imBMW.Enums.Volumio;

namespace imBMW.iBus
{
    public static class MessageRegistry
    {
        #region Registries

        static string[] messageTypeDescriptions = {
            #if !MF_FRAMEWORK_VERSION_V4_1
            "DIAG Read identity",       // "0x00",
            "Device status request",    // "0x01",
             "Device status ready",      // "0x02",
            "Bus status request",       // "0x03",
            "Bus status",               // "0x04",
            "DIAG Clear fault memory",  // "0x05",
            "DIAG read memory",         // "0x06",
            "DIAG write memory",        // "0x07",
            "DIAG read coding data",    // "0x08",
            "DIAG write coding data",   // "0x09",
            "",                         // "0x0A",
            "DIAG Read IO status",      // "0x0B",
            "Vehicle control",          // "0x0C",
            "DIAG ",                    // "0x0D",
            "DIAG PRUEFSTEMPEL_LESEN",  // "0x0E",
            "DIAG PRUEFSTEMPEL_SCHREIBEN", // "0x0F",
            "Ignition status request", // "0x10
            "Ignition status", // "0x11
            "IKE sensor status request",// "0x12
            "IKE sensor status",        // "0x13"
            "Country coding status request", // "0x14
            "Country coding status",// "0x15
            "Odometer request",// "0x16
            "Odometer",// "0x17
            "Speed/RPM",// "0x18
            "Temperature",// "0x19
            "IKE text display/Gong",// "0x1A
            "IKE text status",// "0x1B
            "Gong",// "0x1C
            "Temperature request",// "0x1D
            "", // "0x1E",
            "UTC time and date",// "0x1F
            "LOC: Display status", // "0x20",
            "Navi menu items",
            "Text display confirmation",
            "Display Title", // "0x23"
            "Update ANZV", // "0x24"
            "", // "0x25",
            "", // "0x26",
            "MID display request", // "0x27",
            "", // "0x28",
            "", // "0x29",
            "On-Board Computer State Update", // "0x2A"
            "Phone LEDs", // "0x2B"
            "Phone status", // "0x2C",  C8 04 50 2C xx chk - Bit6 0 = telephone adapter not installed, 1 = telephone adapter installed, example 1: C8 04 E7 2C 10 17 see byte 5 hex 10 is binary 0001 0000 which means: handsfree off \, telephone off
            "", // "0x2D",
            "", // "0x2E",
            "", // "0x2F",
            "", // "0x30",
            "Select screen item", // "0x31",
            "MFL volume buttons",
            "", // "0x33",
            "DSP Equalizer Button", // "0x34"
            "GT Car memory response", // "0x35",
            "AudioControl", // "0x36",              RAD > DSP: 36 68  ;  RAD > DSP: 36 C4
            "GT: Select Menu", // "0x37",
            "CD status request", // "0x38",
            "CD status", // "0x39",
            "Recirculating air control", // "0x3A",
            "MFL media buttons",
            "", // "0x3C",
            "SDRS status request",
            "SDRS status",
            "", // "0x3F",
            "Set On-Board Computer Data",
            "On-Board Computer Data Request",
            "On-Board Computer Scroll", // "0x42",
            "", // "0x43",
            "", // "0x44",
            "Radio status request", // "0x45",
            "LCD Clear",
            "BM SELECT/INFO buttons",               // "0x47"
            "BM buttons",   // "0x48"
            "Navi Knob button rotation",              // "0x49"
            "Monitor CD/Tape control",                 // "0x4A"
            "Monitor CD/Tape status",                   // 0x4B
            "", // "0x4C",
            "", // "0x4D",
            "???Audio source selection???", // "0x4E",
            "Monitor Control", // "0x4F"
            "", // "0x50",
            "Check control messages", // "0x51",
            "IKE sending error to CCM", // "0x52",
            "Vehicle data request",
            "Vehicle data status",
            "LCM Service Interval Display ", // "0x55",
            "", // "0x56",
            "Check Control button", // "0x57", 01 - Check Control button CHECK_pressed; 02 - Check Control button BC_pressed; 41 - Check Control button CHECK_released; 43 - Check Control button Button 3_released
            "Headlight wipe interval", // "0x58",
            "", // "0x59",
            "Lamp status request", // "0x5A",
            "Lamp status", // "0x5B",
            "Instrument cluster lighting status", // "0x5C",
            "Instrument cluster lighting status request", // "0x5D",
            "", // "0x5E",
            "", // "0x5F",
            "", // "0x60",
            "Suspension control", // "0x61",
            "", // "0x62",
            "", // "0x63",
            "", // "0x64",
            "", // "0x65",
            "", // "0x66",
            "", // "0x67",
            "", // "0x68",
            "Read ZCS/FA", // "0x69",
            "", // "0x6A",
            "", // "0x6B",
            "", // "0x6C",
            "Sideview Mirror", // "0x6D",
            "", // "0x6E",
            "FBZV", // "0x6F",
            "Remote control central locking status", // "0x70",
            "Rain sensor status request", // "0x71"
            "Remote Key buttons", // "0x72"
            "EWS Status request", // "0x73",
            "EWS key status", // "0x74"
            "Wiper status request", // "0x75",
            "External lights(Crash Alarm)", // "0x76",  Bit 1 = hazard lights, Bit 2 = low beam, Bit 3 = flash. The bits can be combined. e.g. low beam and hazard lights = 0 0 0 0 0 1 1 0 = $ 06 (hex)
            "Wiper status", // "0x77",
            "Seat Memory", // "0x78",
            "Doors/windows status request", // "0x79"
            "Doors/windows status", // "0x7A"
            "", // "0x7B",
            "SHD status", // "0x7C"
            "SHD control", // "0x7D",
            "", // "0x7E",
            "", // "0x7F",
            "", // "0x80",
            "", // "0x81",
            "Air conditioning on/off status", // 0x82
            "Air conditioning compressor status", // "0x83", (00 00 - Off; 80 00 - On?; 80 08 - On?)
            "", // "0x84",
            "", // "0x85",
            "Rear defroster", // "0x86", (86 00   or   81 01)
            "", // "0x87",
            "", // "0x88",
            "", // "0x89",
            "", // "0x8A",
            "", // "0x8B",
            "", // "0x8C",
            "", // "0x8D",
            "", // "0x8E",
            "", // "0x8F",
            "Some diag request from AirBagModule1", // "0x90", (FF, FF)
            "", // "0x91",
            "Command for auxilary heater", // "0x92",
            "Auxilary heater status", // "0x93",
            "", // "0x94",
            "Some diag request from AirBagModule2", // "0x95", (09 03 24 37)
            "", // "0x96",
            "", // "0x97",
            "", // "0x98",
            "", // "0x99",
            "", // "0x9A",
            "", // "0x9B",
            "", // "0x9C",
            "?SLEEP MODE?", // "0x9D",
            "DIAG BEGIN", // "0x9E",
            "DIAG END", // "0x9F",
            "DIAG OKAY", // "0xA0"
            "DIAG BUSY", // "0xA1",
            "Current position and time", // "A2"
            "", // "0xA3",
            "Current location", // "0xA4"
            "Header fields T1-T6",  // "0xA5"
            "ANZV: Special indicators", // "0xA6",
            "TMC status request", // "0xA7"
            "TMC data", // "0xA8",
            "Telephone data", // "0xA9",
            "Navigation voice Control", // "0xAA"
            "", // "0xAB",
            "", // "0xAC",
            "", // "0xAD",
            "", // "0xAE",
            "", // "0xAF",
            "ERROR_ECU_PARAMETER", // "0xB0",
            "ERROR_ECU_FUNCTION", // "0xB1",
            "ERROR_ECU_NUMBER", // "0xB2",
            "", // "0xB3",
            "", // "0xB4",
            "", // "0xB5",
            "", // "0xB6",
            "", // "0xB7",
            "", // "0xB8",
            "", // "0xB9",
            "", // "0xBA",
            "", // "0xBB",
            "", // "0xBC",
            "", // "0xBD",
            "", // "0xBE",
            "", // "0xBF",
            "", // "0xC0",
            "", // "0xC1",
            "", // "0xC2",
            "", // "0xC3",
            "", // "0xC4",
            "", // "0xC5",
            "", // "0xC6",
            "", // "0xC7",
            "", // "0xC8",
            "", // "0xC9",
            "", // "0xCA",
            "", // "0xCB",
            "", // "0xCC",
            "", // "0xCD",
            "", // "0xCE",
            "", // "0xCF",
            "", // "0xD0",
            "", // "0xD1",
            "", // "0xD2",
            "", // "0xD3",
            "RDS channel list",
            "", // "0xD5",
            "", // "0xD6",
            "", // "0xD7",
            "", // "0xD8",
            "", // "0xD9",
            "", // "0xDA",
            "", // "0xDB",
            "", // "0xDC",
            "", // "0xDD",
            "", // "0xDE",
            "", // "0xDF",
            "", // "0xE0",
            "", // "0xE1",
            "", // "0xE2",
            "", // "0xE3",
            "", // "0xE4",
            "", // "0xE5",
            "", // "0xE6",
            "", // "0xE7",
            "", // "0xE8",
            "", // "0xE9",
            "", // "0xEA",
            "", // "0xEB",
            "", // "0xEC",
            "", // "0xED",
            "imBMWTest", // "0xEE",
            "", // "0xEF",
            "", // "0xF0",
            "", // "0xF1",
            "", // "0xF2",
            "", // "0xF3",
            "", // "0xF4",
            "", // "0xF5",
            "", // "0xF6",
            "", // "0xF7",
            "", // "0xF8",
            "", // "0xF9",
            "", // "0xFA",
            "", // "0xFB",
            "", // "0xFC",
            "", // "0xFD",
            "Diagnostic command not acknowledged", // "0xFE",
            "ERROR_ECU_NACK", // "0xFF"
            #endif
        };

        /// <summary> 01 </summary>
        public static byte[] DataPollRequest = new byte[] { 0x01 };
        /// <summary> 02 00 </summary>
        public static byte[] DataPollResponse = new byte[] { 0x02, 0x00 };
        /// <summary> 02 01 </summary>
        public static byte[] DataAnnounce = new byte[] { 0x02, 0x01 };
        /// <summary> 07 01 </summary>
        public static byte[] SteuernSomething = new byte[] {0x07, 0x1};

        static Hashtable messageDescriptions;

        static MessageRegistry()
        {
            messageDescriptions = new Hashtable();
            messageDescriptions.Add(DataPollRequest.ToHex(' '), "Poll request");
            messageDescriptions.Add(DataPollResponse.ToHex(' '), "Poll response");
            messageDescriptions.Add(DataAnnounce.ToHex(' '), "Announce");

            //messageDescriptions.Add(Radio.DataAMPressed.ToHex(' '), "BMBT AM Pressed");
            //messageDescriptions.Add(Radio.DataAMReleased.ToHex(' '), "BMBT AM Released");
            //messageDescriptions.Add(Radio.DataFMPressed.ToHex(' '), "BMBT FM Pressed");
            //messageDescriptions.Add(Radio.DataFMReleased.ToHex(' '), "BMBT FM Released");
            //messageDescriptions.Add(Radio.DataModePressed.ToHex(' '), "BMBT Mode Pressed");
            //messageDescriptions.Add(Radio.DataModeReleased.ToHex(' '), "BMBT Mode Released");
            //messageDescriptions.Add(Radio.DataNaviKnobPressed.ToHex(' '), "Navi knob Pressed");
            //messageDescriptions.Add(Radio.DataNaviKnobReleased.ToHex(' '), "Navi knob Released");
            //messageDescriptions.Add(Radio.DataNextPressed.ToHex(' '), "BMBT Next Pressed");
            //messageDescriptions.Add(Radio.DataNextReleased.ToHex(' '), "BMBT Next Released");
            //messageDescriptions.Add(Radio.DataPrevPressed.ToHex(' '), "BMBT Prev Pressed");
            //messageDescriptions.Add(Radio.DataPrevReleased.ToHex(' '), "BMBT Prev Released");
            //messageDescriptions.Add(Radio.DataSwitchPressed.ToHex(' '), "BMBT Switch Pressed");
            //messageDescriptions.Add(Radio.DataSwitchReleased.ToHex(' '), "BMBT Switch Released");

            messageDescriptions.Add(new byte[] { 0x22, 0x00, 0x00 }.ToHex(' '), "Text display confirmation for T1-T6");
            messageDescriptions.Add(new byte[] { 0x22, 0x10, 0xFF }.ToHex(' '), "Text display confirmation for shortcuts");

            messageDescriptions.Add(new byte[] { 0x36, 0xA0 }.ToHex(' '), "AudioControl Source=CD");
            messageDescriptions.Add(new byte[] { 0x36, 0xA1 }.ToHex(' '), "AudioControl Source=Tuner/Tape");
            messageDescriptions.Add(new byte[] { 0x36, 0xAF }.ToHex(' '), "AudioControl Source=Off");

            messageDescriptions.Add(new byte[] { (byte)VolumioCommands.Common, (byte)CommonCommands.Init }.ToHex(' '), "Init");
            messageDescriptions.Add(new byte[] { (byte)VolumioCommands.Playback, (byte)PlaybackState.Stop }.ToHex(' '), "Stop");
            messageDescriptions.Add(new byte[] { (byte)VolumioCommands.Playback, (byte)PlaybackState.Pause }.ToHex(' '), "Pause");
            messageDescriptions.Add(new byte[] { (byte)VolumioCommands.Playback, (byte)PlaybackState.Play }.ToHex(' '), "Play");
            messageDescriptions.Add(new byte[] { (byte)VolumioCommands.Playback, (byte)PlaybackState.Prev }.ToHex(' '), "Prev");
            messageDescriptions.Add(new byte[] { (byte)VolumioCommands.Playback, (byte)PlaybackState.Next }.ToHex(' '), "Next");
            messageDescriptions.Add(new byte[] { (byte)VolumioCommands.System, (byte)SystemCommands.Reboot }.ToHex(' '), "Reboot");
            messageDescriptions.Add(new byte[] { (byte)VolumioCommands.System, (byte)SystemCommands.Shutdown }.ToHex(' '), "Shutdown");

            //messageDescriptions.Add(new byte[] { 0x38, 0x00, 00 }.ToHex(' '), "CD StatusRequest");
            messageDescriptions.Add(new byte[] { 0x38, 0x01, 00 }.ToHex(' '), "CD Stop");
            messageDescriptions.Add(new byte[] { 0x38, 0x02, 00 }.ToHex(' '), "CD Pause");
            messageDescriptions.Add(new byte[] { 0x38, 0x03, 00 }.ToHex(' '), "CD Play");
            messageDescriptions.Add(new byte[] { 0x38, 0x04, 00 }.ToHex(' '), "CD Rewind");
            messageDescriptions.Add(new byte[] { 0x38, 0x04, 01 }.ToHex(' '), "CD Fast Forward");
            messageDescriptions.Add(new byte[] { 0x38, 0x07, 00 }.ToHex(' '), "CD Scan off");
            messageDescriptions.Add(new byte[] { 0x38, 0x07, 01 }.ToHex(' '), "CD Scan on");
            messageDescriptions.Add(new byte[] { 0x38, 0x08, 00 }.ToHex(' '), "CD Random off");
            messageDescriptions.Add(new byte[] { 0x38, 0x08, 01 }.ToHex(' '), "CD Random on");
            messageDescriptions.Add(new byte[] { 0x38, 0x0A, 00 }.ToHex(' '), "CD Next");
            messageDescriptions.Add(new byte[] { 0x38, 0x0A, 01 }.ToHex(' '), "CD Prev");

            messageDescriptions.Add(new byte[] { 0x82, 0x83 }.ToHex(' '), "IHKA message after engine started, or after EngineRunning > Acc > Ign"); //  maybe for auxilary heater starting?
            messageDescriptions.Add(new byte[] { 0x82, 0x05 }.ToHex(' '), "IHKA turned on by webasto activation"); // after manual starting auxilary heater(by receiving command 92 00 22 from auxilary heater(key in ACC))
            messageDescriptions.Add(new byte[] { 0x82, 0x03 }.ToHex(' '), "IHKA turned off"); //    after changing Ign > Acc
                                                                                              // or after manual stopping auxilary heater(by receiving command 92 00 11 from auxilary heater(key in ACC))

            messageDescriptions.Add(new byte[] { 0x0C, 0x10 }.ToHex(' '), "Get DDE params");
            /*messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x0F, 0x60 }.ToHex(' '), "Get anmPWG");
            messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x0F, 0x65 }.ToHex(' '), "Get anmUBT");
            messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x1F, 0x06 }.ToHex(' '), "Get anmVDF");
            messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x0F, 0x00 }.ToHex(' '), "Get anmWTF");

            messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x0F, 0x30 }.ToHex(' '), "Get armM_List");
            messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x0F, 0x32 }.ToHex(' '), "Get armM_Lsoll");
            messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x0F, 0x10 }.ToHex(' '), "Get dzmNmit");
            messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x0F, 0x40 }.ToHex(' '), "Get ldmP_Llin");
            messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x0F, 0x42 }.ToHex(' '), "Get ldmP_Lsoll");
            messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x1F, 0x5D }.ToHex(' '), "Get zumP_RAIL");
            messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x1F, 0x5E }.ToHex(' '), "Get zumPQsoll");*/

            //messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x20, 0x41 }.ToHex(' '), "Get admADF");
            //messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x2F, 0xFE }.ToHex(' '), "Get admIDV");
            //messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x2F, 0xFC }.ToHex(' '), "Get admKDF");
            //messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x20, 0x42 }.ToHex(' '), "Get admLDF");
            //messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x20, 0x61 }.ToHex(' '), "Get admLMM");
            //messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x20, 0x01 }.ToHex(' '), "Get admLTF");
            //messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x2F, 0xFA }.ToHex(' '), "Get admPGS");
            //messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x20, 0x60 }.ToHex(' '), "Get admPWG");
            //messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x20, 0x65 }.ToHex(' '), "Get admUBT");
            //messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x20, 0x06 }.ToHex(' '), "Get admVDF");
            //messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x20, 0x00 }.ToHex(' '), "Get admWTF");

            /*messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x0F, 0x63 }.ToHex(' '), "Get anmADF");
            messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x0F, 0xFE }.ToHex(' '), "Get anmIDV");
            messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x0F, 0xFC }.ToHex(' '), "Get anmKDF");
            messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x0F, 0x62 }.ToHex(' '), "Get anmLDF");
            messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x0F, 0x61 }.ToHex(' '), "Get anmLMM");
            messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x0F, 0x01 }.ToHex(' '), "Get anmLTF");
            messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x0F, 0xFA }.ToHex(' '), "Get anmPGS");*/

            //messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x30, 0x11 }.ToHex(' '), "Get anoU_ADF");
            //messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x30, 0x0C }.ToHex(' '), "Get anoU_IDV");
            //messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x30, 0x0A }.ToHex(' '), "Get anoU_KDF");
            //messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x30, 0x04 }.ToHex(' '), "Get anoU_LDF");
            //messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x0F, 0x34 }.ToHex(' '), "Get anoU_LMM");
            //messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x30, 0x0E }.ToHex(' '), "Get anoU_LTF");
            //messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x30, 0x06 }.ToHex(' '), "Get anoU_PGS");
            //messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x0F, 0x35 }.ToHex(' '), "Get anoU_PWG");
            //messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x30, 0x13 }.ToHex(' '), "Get anoU_UBT");
            //messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x30, 0x1A }.ToHex(' '), "Get anoU_VDF");
            //messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x30, 0x02 }.ToHex(' '), "Get anoU_WTF");

            //messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x0E, 0x91 }.ToHex(' '), "Get ehmFKLI");
            //messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x0E, 0x80 }.ToHex(' '), "Get ehmFARS");
            //messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x0E, 0xA6 }.ToHex(' '), "Get ehmFEKP");
            //messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x0E, 0x9A }.ToHex(' '), "Get ehmFDRA");
            //messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x0E, 0x87 }.ToHex(' '), "Get ehmFGRS");
            //messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x0E, 0x9B }.ToHex(' '), "Get ehmFKWH");
            //messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x0E, 0xA5 }.ToHex(' '), "Get ehmFKDR");
            //messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x0E, 0x81 }.ToHex(' '), "Get ehmFLDS");
            //messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x0E, 0xA2 }.ToHex(' '), "Get ehmFMLS");
            //messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x0E, 0x9F }.ToHex(' '), "Get ehmFMML");

            //messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x00, 0x10 }.ToHex(' '), "Get aroIST_4");
            //messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0xDF, 0x0E }.ToHex(' '), "Get aroIST_5");
            //messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x0E, 0xE0 }.ToHex(' '), "Get aroREG_2");
            //messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x0F, 0x31 }.ToHex(' '), "Get aroSOLL_5");

            //messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x0F, 0x19 }.ToHex(' '), "Get dzmzMk1");
            //messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x0F, 0x1A }.ToHex(' '), "Get dzmzMk2");
            //messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x0F, 0x1B }.ToHex(' '), "Get dzmzMk3");
            //messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x0F, 0x1C }.ToHex(' '), "Get dzmzMk4");
            //messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x0F, 0x1D }.ToHex(' '), "Get dzmzMk5");
            //messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x0F, 0x1E }.ToHex(' '), "Get dzmzMk6");

            //messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x0F, 0x13 }.ToHex(' '), "Get dzmzN1");
            //messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x0F, 0x14 }.ToHex(' '), "Get dzmzN2");
            //messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x0F, 0x15 }.ToHex(' '), "Get dzmzN3");
            //messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x0F, 0x16 }.ToHex(' '), "Get dzmzN4");
            //messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x0F, 0x17 }.ToHex(' '), "Get dzmzN5");
            //messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x0F, 0x18 }.ToHex(' '), "Get dzmzN6");

            //messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x0F, 0x80 }.ToHex(' '), "Get mrmM_EAKT");

            //messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x1F, 0x51 }.ToHex(' '), "Get zuoAB_VE1");
            //messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x1F, 0x5A }.ToHex(' '), "Get zumAB_HE");
            //messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x1F, 0x60 }.ToHex(' '), "Get zumAB_NE");

            //messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x1F, 0x52 }.ToHex(' '), "Get zuoAD_VE1");
            //messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x1F, 0x5B }.ToHex(' '), "Get zuoAD_HE");
            //messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x1F, 0x61 }.ToHex(' '), "Get zumAD_NE");

            //messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x1F, 0x53 }.ToHex(' '), "Get zuoME_VE");
            //messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x1F, 0x5C }.ToHex(' '), "Get zuoMEHE");

            //messageDescriptions.Add(new byte[] { 0x0C, 0x10, 0x1F, 0x57 }.ToHex(' '), "Get zuoMEVGW"); // GW-Kennfeld Menge Voreinspritzung
        }

        #endregion

        public static string ToPrettyString(this Message message, bool withPerformanceInfo = false, bool withBytesAsAscii = false)
        {
            //#if !MF_FRAMEWORK_VERSION_V4_1
            //if (message is InternalMessage)
            //{
            //    var m = (InternalMessage)message;
            //    return m.Device.ToStringValue() + ": " + (m.ReceiverDescription ?? m.DataDump);
            //}
            //#endif
            bool isIkeCcmMessage = message.Data[0] == 0x1A || message.Data[0] == 0x52;
            bool isVolumioMessage = message.SourceDevice == DeviceAddress.Volumio && message.DestinationDevice == DeviceAddress.imBMW;
            bool isDisplayTitleMessage = message.Data[0] == 0x23;

            string description = message.Describe();
            if (description == null)
            {
                description = message.DataDump;
            }
            description = message.SourceDevice.ToStringValue() + " > " + message.DestinationDevice.ToStringValue() + ": " + description;

            if (withPerformanceInfo)
            {
                description += " (" + message.PerformanceInfo.ToString() + ")";
            }
            if (withBytesAsAscii)
            {
                description += " (" + ASCIIEncoding.GetString(message.Data) + ")";
            }
            if (isIkeCcmMessage && message.ReceiverDescription == null && message.Data.Length > 3)
            {
                description += " - \"" + ASCIIEncoding.GetString(message.Data.Skip(3)) + "\"";
            }
            if (isVolumioMessage && message.Data.Length > 2)
            {
                description += " - \"" + ASCIIEncoding.GetString(message.Data.Skip(2)) + "\"";
            }
            if (isDisplayTitleMessage && message.Data.Length > 3)
            {
                description += " - \"" + ASCIIEncoding.GetString(message.Data.Skip(3)) + "\"";
            }
            return description;
        }

        public static string Describe(this Message message)
        {
            if (message.Data.Length == 0)
            {
                return "";
            }

            if (message.ReceiverDescription != null)
            {
                return message.DataDump + " {" + message.ReceiverDescription + "}";
            }

            if (messageDescriptions.Contains(message.DataDump))
            {
                return message.DataDump + " [" + (string)messageDescriptions[message.DataDump] + "]";
            }

            byte firstByte = message.Data[0];
            if (firstByte >= messageTypeDescriptions.Length || messageTypeDescriptions[firstByte] == "")
            {
                return null;
            }

            return message.DataDump + " (" + messageTypeDescriptions[firstByte] + ')';
        }

        public static bool IsInternal(this byte device)
        {
            return ((DeviceAddress)device).IsInternal();
        }

        public static bool IsInternal(this DeviceAddress device)
        {
            return device == DeviceAddress.imBMWLogger || device == DeviceAddress.imBMWPlayer;
        }
    }
}
