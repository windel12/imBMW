using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using imBMW.Diagnostics;
using imBMW.iBus;

namespace OnBoardMonitorEmulator.DevicesEmulation
{
    public static class NavigationModuleEmulator
    {
        public static void Init() { }

        static NavigationModuleEmulator()
        {
            Manager.AddMessageReceiverForSourceAndDestinationDevice(DeviceAddress.Diagnostic, DeviceAddress.NavigationEurope, ProcessMessageToNavigationModule);
        }

        static void ProcessMessageToNavigationModule(Message m)
        {
            if (m.Data[0] == 0x0B) // 0x0B - get diag data
            {
                Random r = new Random();
                var randomVoltageValue = (byte)r.Next(0, 200);
                var navi_status_lesen_response = new Message(DeviceAddress.NavigationEurope, DeviceAddress.Diagnostic,
                    0xA0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x09, 0x87, 0x00, 0x13, 0x63, 0x00, 0x35, randomVoltageValue/*0x5B*/, 0x00, 0x04, 0xE3, 0x00, 0x00);
                Manager.EnqueueMessage(navi_status_lesen_response);
                //var navi_status_lesen_response = new DS2Message(DeviceAddress.Diagnostic,
                //    0xA0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x09, 0x87, 0x00, 0x13, 0x63, 0x00, 0x35, 0x5B, 0x00, 0x04, 0xE3/*, 0x00-dbusxor?*/);
                //Manager.EnqueueMessage(navi_status_lesen_response);
            }
        }
    }
}
