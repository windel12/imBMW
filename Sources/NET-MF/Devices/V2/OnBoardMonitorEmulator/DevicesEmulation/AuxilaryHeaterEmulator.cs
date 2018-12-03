using System;
using System.Threading;
using imBMW.iBus;
using imBMW.iBus.Devices.Real;
using imBMW.Tools;

namespace OnBoardMonitorEmulator.DevicesEmulation
{
    public static class AuxilaryHeaterEmulator
    {
        public static void Init() { }

        static AuxilaryHeaterEmulator()
        {
            Manager.AddMessageReceiverForSourceAndDestinationDevice(DeviceAddress.Diagnostic, DeviceAddress.AuxilaryHeater, ProcessDiagnosticMessageToAuxilaryHeater);
        }

        static void ProcessDiagnosticMessageToAuxilaryHeater(Message m)
        {
            if (m.Data.StartsWith(AuxilaryHeater.SteuernZuheizerOn1.Data) || 
                m.Data.StartsWith(AuxilaryHeater.SteuernZuheizerOn2.Data) ||
                m.Data.StartsWith(AuxilaryHeater.SteuernZuheizerOff.Data))
            {
                Thread.Sleep(100);
                Manager.EnqueueMessage(AuxilaryHeater.ZuheizerStatusOk_KBus);
            }
        }
    }
}
