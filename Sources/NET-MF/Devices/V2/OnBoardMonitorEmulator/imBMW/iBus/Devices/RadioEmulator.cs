using imBMW.iBus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnBoardMonitorEmulator.imBMW.iBus.Devices
{
    public static class RadioEmulator
    {
        static RadioEmulator()
        {
            Manager.AddMessageReceiverForDestinationDevice(DeviceAddress.Radio, ProcessToRadioMessage);
        }

        static void ProcessToRadioMessage(Message m)
        {
            // BM buttons
            if (m.Data[0] == 0x48 && m.Data.Length == 2)
            {
                switch (m.Data[1])
                {
                    case 0x06:
                        m.ReceiverDescription = "BM button Phone - draw bordmonitor menu";
                        //IsEnabled = true;
                        break;
                    case 0x34: // Menu
                        m.ReceiverDescription = "BM button Menu";
                        //IsEnabled = false;
                        break;
                    case 0x74: // Menu hold >1s
                        //OnResetButtonPressed();
                        break;
                    case 0x30: // Radio menu
                        m.ReceiverDescription = "BM button Switch Screen";
                        //IsEnabled = !IsEnabled;
                        //if (screenSwitched)
                        //{
                        //    UpdateScreen();
                        //}
                        break;
                }
            }
        }
    }
}
