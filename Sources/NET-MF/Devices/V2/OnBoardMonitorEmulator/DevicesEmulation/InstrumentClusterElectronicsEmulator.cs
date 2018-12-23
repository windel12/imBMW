using System;
using System.Threading;
using imBMW.iBus;
using imBMW.iBus.Devices.Real;
using imBMW.Tools;

namespace OnBoardMonitorEmulator.DevicesEmulation
{
    public static class InstrumentClusterElectronicsEmulator
    {
        public static void Init() { }

        static InstrumentClusterElectronicsEmulator()
        {
            Manager.AddMessageReceiverForSourceAndDestinationDevice(DeviceAddress.GraphicsNavigationDriver, DeviceAddress.InstrumentClusterElectronics, ProcessMessageFromGraphicNavigationDriver);

            DBusManager.Instance.AddMessageReceiverForSourceDevice(DeviceAddress.OBD, ProcessDS2MessageAndForwardToIBus);
            Manager.AddMessageReceiverForDestinationDevice(DeviceAddress.Diagnostic, ProcessDiagnosticMessageFromIBusAndForwardToDBus);
        }

        static void ProcessMessageFromGraphicNavigationDriver(Message m)
        {
            if (m.Data.Compare(InstrumentClusterElectronics.MessageRequestTime.Data))
            {
                // 16:58
                Manager.EnqueueMessage(new Message(DeviceAddress.InstrumentClusterElectronics, DeviceAddress.FrontDisplay, 0x24, 0x01, 0x00, 0x31, 0x36, 0x3A, 0x34, 0x38, 0x20, 0x20));
            }
            if (m.Data.Compare(InstrumentClusterElectronics.MessageRequestDate.Data))
            {
                // 06/26/2016"
                Manager.EnqueueMessage(new Message(DeviceAddress.InstrumentClusterElectronics, DeviceAddress.FrontDisplay, 0x24, 0x02, 0x00, 0x30, 0x36, 0x2F, 0x32, 0x36, 0x2F, 0x32, 0x30, 0x31, 0x36));
            }
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
                DBusManager.Instance.EnqueueMessage(message);
            }
        }
    }
}
