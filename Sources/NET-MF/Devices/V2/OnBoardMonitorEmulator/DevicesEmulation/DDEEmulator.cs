using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using imBMW.Diagnostics;
using imBMW.iBus;

namespace OnBoardMonitorEmulator.DevicesEmulation
{
    public static class DDEEmulator
    {
        public static void Init() { }

        static DDEEmulator()
        {
            DbusManager.AddMessageReceiverForSourceAndDestinationDevice(DeviceAddress.OBD, DeviceAddress.DDE, ProcessToDDEMessage);
        }

        static void ProcessToDDEMessage(Message m)
        {
            if (m.Data[0] == 0x2C && m.Data[1] == 0x10)
            {
                Random r = new Random();
                var motor_temperatur_positive_response = new DBusMessage(DeviceAddress.DDE, DeviceAddress.OBD, 0x6C, 0x10, 0x00, (byte)r.Next(0, 255));
                DbusManager.EnqueueMessage(motor_temperatur_positive_response);
            }
        }
    }
}
