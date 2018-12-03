using System;
using System.Threading;
using imBMW.iBus;

namespace OnBoardMonitorEmulator.DevicesEmulation
{
    public static class InstrumentClusterElectronicsEmulator
    {
        public static void Init() { }

        static InstrumentClusterElectronicsEmulator()
        {
            DbusManager.AddMessageReceiverForSourceDevice(DeviceAddress.OBD, ProcessDS2MessageAndForwardToIBus);
            Manager.AddMessageReceiverForDestinationDevice(DeviceAddress.Diagnostic, ProcessDiagnosticMessageFromIBusAndForwardToDBus);
        }

        static void ProcessDS2MessageAndForwardToIBus(Message m)
        {
            // do not forward responses from modules
            if (m.Data[0] == 0xA0)
            {
                return;
            }

            Thread.Sleep(100);
            var message = new Message(DeviceAddress.Diagnostic, m.DestinationDevice, m.Data);
            Manager.EnqueueMessage(message);
        }

        static void ProcessDiagnosticMessageFromIBusAndForwardToDBus(Message m)
        {
            if (m.Data[0] == 0xA0) // 0xA0 - DIAG OKAY
            {
                Thread.Sleep(100);
                var message = new DS2Message(m.SourceDevice, m.Data);
                DbusManager.EnqueueMessage(message);
            }
        }
    }
}
