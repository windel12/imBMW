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

    [Flags]
    public enum Lights
    {
        Off = 0x00,

        RearRightBlinker = 0x02,
        RearLeftFogLamp =  0x04,
        RearRightInnerStandingLight = 0x08,
        RearRightStandingLight = 0x10,

        FrontLeftBlinker = 0x40,



        RightLicensePlate = 0x0400,
        RearLeftStandingLight = 0x0800,
        ThirdBrakeLight = 0x1000,
        FrontRightStandingLight = 0x2000,
        FrontRightBlinker = 0x4000,
        FrontLeftStandingLight = 0x010000,
        RearLeftInnerStandingLight = 0x020000,
        FrontLeftFogLamp = 0x040000,

        LeftHighBeam = 0x100000,
        RightHighBeam = 0x200000,
        FrontRightFogLamp = 0x400000,
        RearRightFogLamp = 0x800000,


        LeftLicensePlate = 0x04000000,
        LeftBrakeLight = 0x08000000,
        RightBrakeLight = 0x10000000,
        RightLowBeam = 0x20000000,
        LeftLowBeam = 0x40000000,
    }

    public delegate void LightStatusEventHandler(Message message, LightStatusEventArgs args);

    #endregion

    public static class LightControlModule
    {
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
            data[5] = mask[3];
            data[6] = mask[2];
            data[7] = mask[1];
            data[8] = mask[0];

            var message = new Message(DeviceAddress.Diagnostic, DeviceAddress.LightControlModule, "Turn lamps", data);
            Manager.Instance.EnqueueMessage(message);
        }

        public static event LightStatusEventHandler LightStatusReceived;
    }
}
