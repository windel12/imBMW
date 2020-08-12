using System;
using imBMW.iBus;

namespace OnBoardMonitorEmulator.DevicesEmulation
{
    public static class LightControlModuleEmulator
    {
        public static void Init() { }

        static LightControlModuleEmulator()
        {
            Manager.Instance.AddMessageReceiverForSourceAndDestinationDevice(DeviceAddress.Diagnostic, DeviceAddress.LightControlModule, ProcessMessageToLightControlModuleEmulator);
        }

        static void ProcessMessageToLightControlModuleEmulator(Message m)
        {
            if (m.Data[0] == 0x0B) // 0x0B - get diag data
            {
                Random r = new Random();
                var randomHeatingValue = (byte)r.Next(0, 255);
                var randomCoolingValue = (byte)r.Next(0, 255);

                var lcm_status_lesen_response = new Message(DeviceAddress.LightControlModule, DeviceAddress.Diagnostic,
                    0xA0, 0xC1, 0xC0, 0x00, 0x20, 0x00, 0x00, 0x02, 0x00, 0x00, 0xB4, 0x00, 0x00, 0x00, 0x04, 0x00, 0x78, 0xFF, 0x00, 0x00,
                    randomHeatingValue, 0x01,
                    randomCoolingValue, 0x13, 
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFE);
                Manager.Instance.EnqueueMessage(lcm_status_lesen_response);
                //var navi_status_lesen_response = new DS2Message(DeviceAddress.Diagnostic,
                //    0xA0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x09, 0x87, 0x00, 0x13, 0x63, 0x00, 0x35, 0x5B, 0x00, 0x04, 0xE3/*, 0x00-dbusxor?*/);
                //Manager.Instance.EnqueueMessage(navi_status_lesen_response);
            }
        }
    }
}
