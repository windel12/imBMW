using System;
using imBMW.Tools;

namespace imBMW.iBus.Devices.Real
{
    #region Enums, delegales and event args

    public class LightStatusEventArgs
    {
        public bool ParkingLightsOn;
        public bool LowBeamOn;

        public byte ErrorCode;
        public bool ErrorFrontLeftLights;
        public bool ErrorFrontRightsLights;
    }

    #region Tool32Table
    // "STEUER_I_O", "BYTE", "BITWERT"
    // "Kl30A", "0", "0x01"
    // "LOESCHAN", "0", "0x02"
    // "VGLESP", "0", "0x04"
    // "CARB", "0", "0x10"
    // "KlR", "0", "0x40"
    // "Kl30B", "0", "0x80"
    // "ZSK", "1", "0x01"
    // "GKFA", "1", "0x02"
    // "S_LH", "1", "0x04"
    // "WBL", "1", "0x10"
    // "KFN", "1", "0x20"
    // "PANZTUE", "1", "0x40"
    // "BRFN", "1", "0x80"
    // "S_BLS", "2", "0x01"
    // "S_FL", "2", "0x02"
    // "S_NSW", "2", "0x04"
    // "S_NSL", "2", "0x10"
    // "S_SL", "2", "0x20"
    // "S_BLK_R", "2", "0x40"
    // "S_BLK_L", "2", "0x80"
    // "LUFTAN", "3", "0x01"
    // "ALARM", "3", "0x02"
    // "WWN", "3", "0x04"
    // "S2_AL", "3", "0x08"
    // "S1_AL", "3", "0x10"
    // "Kl15", "3", "0x20"
    // "MOTNOT", "3", "0x40"
    // "REIF_DEF", "3", "0x80"
    // 
    // "KZL_L", "4", "0x04"
    // "BL_L", "4", "0x08"
    // "BL_R", "4", "0x10"
    // "FL_R", "4", "0x20"
    // "FL_L", "4", "0x40"
    // 
    // "SL_LV", "5", "0x01"
    // "SL_LHI", "5", "0x02"
    // "NSW_L", "5", "0x04"
    // "RFS_L", "5", "0x08"
    // "AL_L", "5", "0x10"
    // "AL_R", "5", "0x20"
    // "NSW_R", "5", "0x40"
    // "NSL_R", "5", "0x80"
    // 
    // "LWR", "6", "0x02"
    // "KZL_R", "6", "0x04"
    // "SL_LH", "6", "0x08"
    // "BL_M", "6", "0x10"
    // "SL_RV", "6", "0x20"
    // "BLK_RV", "6", "0x40"
    // "BLK_LH", "6", "0x80"
    // 
    // "BLK_RH", "7", "0x02"
    // "NSL_L", "7", "0x04"
    // "SL_RHI", "7", "0x08"
    // "SL_RH", "7", "0x10"
    // "BLK_LV", "7", "0x40"
    // "RFS_R", "7", "0x80"
    // 
    // "NOTAKTIV", "8", "0x01"
    // "KL58_EIN", "8", "0x02"
    // "WBLSUCH_EIN", "8", "0x04"
    // "LSSUCH_EIN", "8", "0x08"
    // "NSL_AH_EIN", "8", "0x10"
    // "RFS_AH_EIN", "8", "0x20"
    // "SLEEPMODE", "8", "0x40"
    #endregion

    [Flags]
    public enum Lights : uint
    {
        Off = 0x00000000,
        // byte #7 in LCM_III.prg -> Table:STEUERN
        RearRightBlinker = 0x02000000,                  // BLK_RH   - Blinker
        RearLeftFogLamp =  0x04000000,                  // NSL_L    - Nebelschlussleuchte
        RearRightInnerStandingLight = 0x08000000,       // SL_RHI   - Stehendes Licht
        RearRightStandingLight = 0x10000000,            // SL_RH    - Stehendes Licht
        FrontLeftBlinker = 0x40000000,                  // BLK_LV   - Blinker
        RightReverseLight = 0x80000000,                 // RFS_R    - Rückfahrscheinwerfer

        // byte #6 in LCM_III.prg -> Table:STEUERN
        LWR = 0x00020000,
        RightLicensePlate = 0x00040000,                 // KZL_R    - Kennzeichen
        RearLeftStandingLight = 0x00080000,             // SL_LH    - Stehendes Licht
        ThirdBrakeLight = 0x00100000,                   // BL_M     - Blinker Mittel
        FrontRightStandingLight = 0x00200000,           // SL_RV    - Stehendes Licht
        FrontRightBlinker = 0x00400000,                 // BLK_RV   - Blinker rechts Vorne
        RearLeftBlinker = 0x00800000,                   // BLK_LH   - Blinker links Hinten

        // byte #5 in LCM_III.prg -> Table:STEUERN
        FrontLeftStandingLight = 0x00000100,            // SL_LV    - Stehendes Licht
        RearLeftInnerStandingLight = 0x00000200,        // SL_LHI   - Stehendes Licht
        FrontLeftFogLamp = 0x00000400,                  // NSW_L    - Nebelscheinwerfer
        LeftReverseLight = 0x00000800,                  // RFS_L    - Rückfahrscheinwerfer
        LeftLowBeam = 0x00001000,                       // AL_L     - Abblendlicht
        RightLowBeam = 0x00002000,                      // AL_R     - Abblendlicht
        FrontRightFogLamp = 0x00004000,                 // NSW_R    - NebelScheinWerfer
        RearRightFogLamp = 0x00008000,                  // NSL_R    - Nebelschlussleuchte

        // byte #4 in LCM_III.prg -> Table:STEUERN
        LeftLicensePlate = 0x00000004,                  // KZL_L    - Kennzeichen
        LeftBrakeLight = 0x00000008,                    // BL_L     - Bremslicht
        RightBrakeLight = 0x00000010,                   // BL_R     - Bremslicht
        RightHighBeam = 0x00000020,                     // FL_R     - Fernlicht
        LeftHighBeam = 0x00000040,                      // FL_L     - Fernlicht
    }   

    public delegate void LightStatusEventHandler(Message message, LightStatusEventArgs args);

    #endregion

    public static class LightControlModule
    {
        static readonly Message StatusLessen = new Message(DeviceAddress.Diagnostic, DeviceAddress.LightControlModule, "Status lessen", 0x0B);

        private static double _heatingTime;
        private static double _coolingTime;

        public static double HeatingTime
        {
            get { return _heatingTime; }
            set
            {
                if (_heatingTime != value)
                {
                    _heatingTime = value;
                    HeatingTimeChanged?.Invoke(_heatingTime);
                }
            }
        }

        public static double CoolingTime
        {
            get { return _coolingTime; }
            set
            {
                if (_coolingTime != value)
                {
                    _coolingTime = value;
                    CoolingTimeChanged?.Invoke(_coolingTime);
                }
            }
        }

        public static event ActionDouble HeatingTimeChanged;
        public static event ActionDouble CoolingTimeChanged;

        static LightControlModule()
        {
            Manager.Instance.AddMessageReceiverForSourceDevice(DeviceAddress.LightControlModule, ProcessLCMMessage);
        }

        /// <summary>
        /// Does nothing. Just to call static constructor.
        /// </summary>
        public static void Init() { }

        static void ProcessLCMMessage(Message m)
        {
            if (m.Data.Length == 5 && m.Data[0] == 0x5B)
            {
                OnLightStatusReceived(m);
            }

            if (m.Data[0] == 0xA0 && m.Data.Length > 30)
            {
                HeatingTime = (m.Data[21] * 255 + m.Data[20]) * 0.00005;
                CoolingTime = (m.Data[23] * 255 + m.Data[22]) * 0.00005;
                m.ReceiverDescription = "LCM Status - HeatingTime: " + HeatingTime.ToString("F5") + "; CoolingTime" + CoolingTime.ToString("F5");
            }
        }

        static void OnLightStatusReceived(Message m)
        {
            // TODO Hack Data[3] and other bits meaning
            var on = m.Data[1];
            var error = m.Data[2];
            var errorUnk = m.Data[3];
            var errorReason = m.Data[4];

            string description = "";
            var args = new LightStatusEventArgs();
            if (on == 0)
            {
                description = "Lights Off ";
            }
            else
            {
                if (on.HasBit(0))
                {
                    args.ParkingLightsOn = true;
                    on = on.RemoveBit(0);
                    description += "Park ";
                }
                if (on.HasBit(1))
                {
                    args.LowBeamOn = true;
                    on = on.RemoveBit(1);
                    description += "LowBeam ";
                }
                if (on != 0)
                {
                    description += "Unknown=" + on.ToHex() + " ";
                }
            }
            if (error != 0 || errorReason != 0)
            {
                description += "| Errors";
                if (error != 0x01)
                {
                    description += "=" + error.ToHex();
                }
                description += ": ";
            }
            args.ErrorCode = error;
            if (errorReason == 0)
            {
                if (error != 0)
                {
                    description += "Unknown=" + errorUnk.ToHex() + "00";
                }
                else
                {
                    description += "| Lights OK";
                }
            }
            else
            {
                if (errorReason.HasBit(4))
                {
                    args.ErrorFrontRightsLights = true;
                    errorReason = errorReason.RemoveBit(4);
                    description += "FrontRight ";
                }
                if (errorReason.HasBit(5))
                {
                    args.ErrorFrontLeftLights = true;
                    errorReason = errorReason.RemoveBit(5);
                    description += "FrontLeft ";
                }
                if (errorReason != 0)
                {
                    description += "Unknown=" + errorReason.ToHex();
                }
            }
            m.ReceiverDescription = description;

            var e = LightStatusReceived;
            if (e != null)
            {
                e(m, args);
            }
        }

        public static void TurnOnLamps(Lights lights)
        {
            var mask = BitConverter.GetBytes((int)lights);
            byte[] data = new byte[13];
            data[0] = 0x0C;
            data[5] = mask[0];
            data[6] = mask[1];
            data[7] = mask[2];
            data[8] = mask[3];

            var message = new Message(DeviceAddress.Diagnostic, DeviceAddress.LightControlModule, "Turn lamps", data);
            Manager.Instance.EnqueueMessage(message);
        }

        public static void UpdateThermalOilLevelSensorValues()
        {
            Manager.Instance.EnqueueMessage(StatusLessen);
        }

        public static event LightStatusEventHandler LightStatusReceived;
    }
}
