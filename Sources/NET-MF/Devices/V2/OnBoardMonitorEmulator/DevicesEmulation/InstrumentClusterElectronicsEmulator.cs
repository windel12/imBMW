using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using imBMW.Diagnostics;
using imBMW.iBus;
using imBMW.Tools;

namespace OnBoardMonitorEmulator.DevicesEmulation
{
    public static class InstrumentClusterElectronicsEmulator
    {
        public static void Init() { }

        static InstrumentClusterElectronicsEmulator()
        {
            DbusManager.AddMessageReceiverForDestinationDevice(DeviceAddress.NavigationEurope, ProcessDiagnosticMessageToNavigationModule);
            //Manager.AddMessageReceiverForDestinationDevice(DeviceAddress.Diagnostic, ProcessDiagnosticMessage);
        }

        static void ProcessDiagnosticMessageToNavigationModule(Message m)
        {
            var message = new Message(DeviceAddress.Diagnostic, DeviceAddress.NavigationEurope, m.Data);
            Manager.EnqueueMessage(message);
        }

        //static void ProcessDiagnosticMessage(Message m)
        //{
        //    if (m.Data[3] == 0x0A) // 0xA0 - DIAG data
        //    {
        //        var message = new DS2Message(m.SourceDevice, m.Data);
        //    }
        //}
    }
}
