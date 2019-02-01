using System;
using imBMW.Tools;
using System.Collections;
using System.Text;
using imBMW.iBus.Devices.Real;

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
            "Ignition status request",
            "Ignition status",
            "IKE sensor status request",
            "IKE sensor status",        // "0x13"
            "Country coding status request",
            "Country coding status",
            "Odometer request",
            "Odometer",
            "Speed/RPM",
            "Temperature",
            "IKE text display/Gong",
            "IKE text status",
            "Gong",
            "Temperature request",
            "", // "0x1E",
            "UTC time and date",
            "LOC: Display status", // "0x20",
            "Radio Short cuts",
            "Text display confirmation",
            "Display Title",
            "Update ANZV",
            "", // "0x25",
            "", // "0x26",
            "MID display request", // "0x27",
            "", // "0x28",
            "", // "0x29",
            "On-Board Computer State Update",
            "Phone LEDs",
            "Phone symbol", // "0x2C",
            "", // "0x2D",
            "", // "0x2E",
            "", // "0x2F",
            "", // "0x30",
            "Select screen item", // "0x31",
            "MFL volume buttons",
            "", // "0x33",
            "DSP Equalizer Button",
            "", // "0x35",
            "Audio_control", // "0x36",
            "GT: Select Menu", // "0x37",
            "CD status request",
            "CD status",
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
            "BMBT buttons",
            "BMBT buttons",
            "KNOB button",
            "Monitor CD/Tape control",
            "Monitor CD/Tape status",
            "", // "0x4C",
            "", // "0x4D",
            "Audio source selection", // "0x4E",
            "Monitor Control",
            "", // "0x50",
            "Check control messages", // "0x51",
            "?Some message to CheckControlModule?", // "0x52",
            "Vehicle data request",
            "Vehicle data status",
            "LCM Service Interval Display ", // "0x55",
            "", // "0x56",
            "Check Control button", // "0x57", 01 - Check Control button CHECK_pressed; 02 - Check Control button BC_pressed; 41 - Check Control button CHECK_released; 43 - Check Control button Button 3_released
            "Headlight wipe interval", // "0x58",
            "", // "0x59",
            "Lamp status request",
            "Lamp status",
            "Instrument cluster lighting status",
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
            "Rain sensor status request",
            "Remote Key buttons",
            "EWS Status request", // "0x73",
            "EWS key status",
            "", // "0x75",
            "External lights", // "0x76",
            "Wiper status", // "0x77",
            "Seat Memory", // "0x78",
            "Doors/windows status request",
            "Doors/windows status",
            "", // "0x7B",
            "SHD status",
            "SHD control", // "0x7D",
            "", // "0x7E",
            "", // "0x7F",
            "", // "0x80",
            "", // "0x81",
            "Air conditioning on/off status", 
            "Air conditioning compressor status", // "0x83", (00 00 - Off; 80 00 - On?; 80 08 - On?)
            "", // "0x84",
            "", // "0x85",
            "?IHKA tell something to NavigationEurope?", // "0x86", (86 00   or   81 01)
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
            "Current position and time",
            "", // "0xA3",
            "Current location",
            "Header fields T1-T6",
            "ANZV: Special indicators", // "0xA6",
            "TMC status request",
            "TMC data", // "0xA8",
            "Telephone data", // "0xA9",
            "Navigation voice Control",
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
            messageDescriptions.Add(Radio.DataModePressed.ToHex(' '), "BMBT Mode Pressed");
            messageDescriptions.Add(Radio.DataModeReleased.ToHex(' '), "BMBT Mode Released");
            //messageDescriptions.Add(Radio.DataNaviKnobPressed.ToHex(' '), "Navi knob Pressed");
            //messageDescriptions.Add(Radio.DataNaviKnobReleased.ToHex(' '), "Navi knob Released");
            messageDescriptions.Add(Radio.DataNextPressed.ToHex(' '), "BMBT Next Pressed");
            messageDescriptions.Add(Radio.DataNextReleased.ToHex(' '), "BMBT Next Released");
            messageDescriptions.Add(Radio.DataPrevPressed.ToHex(' '), "BMBT Prev Pressed");
            messageDescriptions.Add(Radio.DataPrevReleased.ToHex(' '), "BMBT Prev Released");
            messageDescriptions.Add(Radio.DataSwitchPressed.ToHex(' '), "BMBT Switch Pressed");
            messageDescriptions.Add(Radio.DataSwitchReleased.ToHex(' '), "BMBT Switch Released");

            messageDescriptions.Add(new byte[] { 0x38, 0x0A, 00 }.ToHex(' '), "CD changer next track");
            messageDescriptions.Add(new byte[] { 0x38, 0x0A, 01 }.ToHex(' '), "CD changer prev track");

            messageDescriptions.Add(new byte[] { 0x82, 0x83 }.ToHex(' '), "IHKA message after engine started, or after EngineRunning > Acc > Ign"); //  maybe for auxilary heater starting?
            messageDescriptions.Add(new byte[] { 0x82, 0x05 }.ToHex(' '), "IHKA turned on by webasto activation"); // after manual starting auxilary heater(by receiving command 92 00 22 from auxilary heater(key in ACC))
            messageDescriptions.Add(new byte[] { 0x82, 0x03 }.ToHex(' '), "IHKA turned off"); //    after changing Ign > Acc
                                                                                              // or after manual stopping auxilary heater(by receiving command 92 00 11 from auxilary heater(key in ACC))
        }

        #endregion

        public static string ToPrettyString(this Message message, bool withPerformanceInfo = false, bool withBytesAsAscii = false)
        {
            #if !MF_FRAMEWORK_VERSION_V4_1
            if (message is InternalMessage)
            {
                var m = (InternalMessage)message;
                return m.Device.ToStringValue() + ": " + (m.ReceiverDescription ?? m.DataDump);
            }
            #endif

            string description = message.Describe();
            if (description == null)
            {
                description = message.DataDump;
            }
            description = message.SourceDevice.ToStringValue() + " > " + message.DestinationDevice.ToStringValue() + ": " + description;
            if (withBytesAsAscii)
            {
                description += " (" + ASCIIEncoding.GetString(message.Data) + ")";
            }
            if (withPerformanceInfo)
            {
                description += " (" + message.PerformanceInfo.ToString() + ")";
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
                return message.ReceiverDescription;
            }

            if (messageDescriptions.Contains(message.DataDump))
            {
                return message.DataDump + " [" + (string)messageDescriptions[message.DataDump] + " ]";
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
