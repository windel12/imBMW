using System;
using imBMW.iBus;

namespace OnBoardMonitorEmulator.DevicesEmulation
{
    public static class DigitalDieselElectronicsEmulator
    {
        public static void Init() { }

        static DigitalDieselElectronicsEmulator()
        {
            DBusManager.Instance.AddMessageReceiverForSourceAndDestinationDevice(DeviceAddress.OBD, DeviceAddress.DDE, ProcessToDDEMessage);
        }

        static void ProcessToDDEMessage(Message m)
        {
            if (m.Data[0] == 0x2C && m.Data[1] == 0x10)
            {
                Random r = new Random();
                var response = new DBusMessage(DeviceAddress.DDE, DeviceAddress.OBD, 0x6C, 0x10, 
                    0x01, (byte)r.Next(0, 255),
                    0x03, (byte)r.Next(0, 255),
                    0x05, (byte)r.Next(0, 255),
                    0x07, (byte)r.Next(0, 255),
                    0x09, (byte)r.Next(0, 255),
                    0x0B, (byte)r.Next(0, 255),
                    0x0D, (byte)r.Next(0, 255),
                    0x0F, (byte)r.Next(0, 255),
                    0x11, (byte)r.Next(0, 255));
                DBusManager.Instance.EnqueueMessage(response);
            }

            if (m.Data[0] == 0x30 && m.Data[1] == 0xC7)
            {
                var response = new DBusMessage(DeviceAddress.DDE, DeviceAddress.OBD, 0x70, 0xC7, 0x07, m.Data[3]);
                DBusManager.Instance.EnqueueMessage(response);
            }
        }
    }
}
