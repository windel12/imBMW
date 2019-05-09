using System;
using imBMW.Devices.V2;
using imBMW.iBus;
using imBMW.iBus.Devices.Real;

namespace OnBoardMonitorEmulator.DevicesEmulation
{
    public static class FrontDisplayEmulator
    {
        public delegate void LedTypeEventHandler(LedType ledType);
        public static event LedTypeEventHandler LedChanged;

        public static void Init() { }

        static FrontDisplayEmulator()
        {
            Manager.Instance.AddMessageReceiverForSourceAndDestinationDevice(DeviceAddress.Telephone, DeviceAddress.FrontDisplay, ProcessMessageToFrontDisplay);
        }

        static void ProcessMessageToFrontDisplay(Message m)
        {
            if (m.Data.Length == 2 && m.Data[0] == 0x2B)
            {
                var ledType = (LedType)m.Data[1];
                var e = LedChanged;
                if (e != null)
                {
                    e(ledType);
                }
            }
        }
    }
}
