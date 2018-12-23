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

        static Thread announceThread;

        static AuxilaryHeaterEmulator()
        {
            Manager.AddMessageReceiverForSourceAndDestinationDevice(DeviceAddress.Diagnostic, DeviceAddress.AuxilaryHeater, ProcessDiagnosticMessageToAuxilaryHeater);
            KBusManager.Instance.AddMessageReceiverForSourceAndDestinationDevice(DeviceAddress.IntegratedHeatingAndAirConditioning, DeviceAddress.AuxilaryHeater, ProcessMessageFromIHKA);
        }

        static void ProcessDiagnosticMessageToAuxilaryHeater(Message m)
        {
            if (m.Data.StartsWith(AuxilaryHeater.DiagnoseStart.Data) || 
                m.Data.StartsWith(AuxilaryHeater.SteuernZuheizerOn.Data) ||
                m.Data.StartsWith(AuxilaryHeater.SteuernZuheizerOff.Data))
            {
                Thread.Sleep(100);
                Manager.EnqueueMessage(AuxilaryHeater.DiagnoseOk_KBus);
            }
        }

        static void ProcessMessageFromIHKA(Message message)
        {
            if (message.Data.StartsWith(MessageRegistry.DataPollRequest))
            {
                var auxilaryHeaterPollResponseMessage = new Message(DeviceAddress.AuxilaryHeater, DeviceAddress.IntegratedHeatingAndAirConditioning, MessageRegistry.DataPollResponse);
                KBusManager.Instance.EnqueueMessage(auxilaryHeaterPollResponseMessage);
            }

            if (message.Data.StartsWith(IntegratedHeatingAndAirConditioning.StartAdditionalHeater.Data))
            {
                if (IntegratedHeatingAndAirConditioning.AuxilaryHeaterStatus == AuxilaryHeaterStatus.StartPending)
                {
                    Thread.Sleep(100);
                    KBusManager.Instance.EnqueueMessage(AuxilaryHeater.AdditionalHeaterWorkingResponse);
                    announceThread = new Thread(announce);
                    announceThread.Start();
                }
            }

            if (message.Data.StartsWith(IntegratedHeatingAndAirConditioning.StopAdditionalHeater1.Data))
            {
                Thread.Sleep(100);
                KBusManager.Instance.EnqueueMessage(AuxilaryHeater.AdditionalHeaterStopped1);

                if (announceThread.ThreadState != ThreadState.Suspended)
                    announceThread.Suspend();
            }
            if (message.Data.StartsWith(IntegratedHeatingAndAirConditioning.StopAdditionalHeater2.Data))
            {
                Thread.Sleep(100);
                KBusManager.Instance.EnqueueMessage(AuxilaryHeater.AdditionalHeaterStopped2);
            }
        }

        static void announce()
        {
            while (true)
            {
                Thread.Sleep(10000);
                KBusManager.Instance.EnqueueMessage(AuxilaryHeater.AdditionalHeaterWorkingResponse);
            }
        }
    }
}
