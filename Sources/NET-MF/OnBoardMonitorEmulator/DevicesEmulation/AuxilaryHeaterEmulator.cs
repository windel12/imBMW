using System;
using System.Threading;
using imBMW.iBus;
using imBMW.iBus.Devices.Real;
using imBMW.Tools;
using imBMW.Enums;

namespace OnBoardMonitorEmulator.DevicesEmulation
{
    public static class AuxilaryHeaterEmulator
    {
        private static int announceTimeout;

        public static void Init(int timeout = 10000)
        {
            announceTimeout = timeout;
        }

        static Thread announceThread;

        static AuxilaryHeaterEmulator()
        {
            //Manager.Instance.AddMessageReceiverForSourceAndDestinationDevice(DeviceAddress.Diagnostic, DeviceAddress.AuxilaryHeater, ProcessDiagnosticMessageToAuxilaryHeater);
            KBusManager.Instance.AddMessageReceiverForSourceAndDestinationDevice(DeviceAddress.IntegratedHeatingAndAirConditioning, DeviceAddress.AuxilaryHeater, ProcessMessageFromIHKA);
        }

        //static void ProcessDiagnosticMessageToAuxilaryHeater(Message m)
        //{
        //    if (m.Data.StartsWith(AuxilaryHeater.DiagnoseStart.Data) || 
        //        m.Data.StartsWith(AuxilaryHeater.SteuernZuheizerOn.Data) ||
        //        m.Data.StartsWith(AuxilaryHeater.SteuernZuheizerOff.Data))
        //    {
        //        Thread.Sleep(100);
        //        Manager.Instance.EnqueueMessage(AuxilaryHeater.DiagnoseOk_KBus);
        //    }
        //}

        static void ProcessMessageFromIHKA(Message message)
        {
            if (message.Data.StartsWith(MessageRegistry.DataPollRequest))
            {
                var auxilaryHeaterPollResponseMessage = new Message(DeviceAddress.AuxilaryHeater, DeviceAddress.IntegratedHeatingAndAirConditioning, MessageRegistry.DataPollResponse);
                KBusManager.Instance.EnqueueMessage(auxilaryHeaterPollResponseMessage);
            }

            if (message.Data.StartsWith(IntegratedHeatingAndAirConditioning.StartAuxilaryHeaterMessage.Data))
            {
                if (AuxilaryHeater.Status == AuxilaryHeaterStatus.Starting)
                {
                    Thread.Sleep(100);
                    KBusManager.Instance.EnqueueMessage(AuxilaryHeater.AuxilaryHeaterWorkingResponse);
                    announceThread = new Thread(announce);
                    announceThread.Start();
                }
            }

            if (message.Data.StartsWith(IntegratedHeatingAndAirConditioning.StopAuxilaryHeater1.Data))
            {
                Thread.Sleep(100);
                KBusManager.Instance.EnqueueMessage(AuxilaryHeater.AuxilaryHeaterStopped1);

                if (announceThread != null && announceThread.ThreadState != ThreadState.Suspended)
                    announceThread.Suspend();
            }
            if (message.Data.StartsWith(IntegratedHeatingAndAirConditioning.StopAuxilaryHeater2.Data))
            {
                Thread.Sleep(100);
                KBusManager.Instance.EnqueueMessage(AuxilaryHeater.AuxilaryHeaterStopped2);

                if (announceThread != null && announceThread.ThreadState != ThreadState.Suspended)
                    announceThread.Suspend();
            }
        }

        //public static void FirstMessageAfterWakeup()
        //{
        //    KBusManager.Instance.EnqueueMessage(AuxilaryHeater.AuxilaryHeaterStopped2);
        //}

        static void announce()
        {
            while (true)
            {
                Thread.Sleep(announceTimeout);
                KBusManager.Instance.EnqueueMessage(AuxilaryHeater.AuxilaryHeaterWorkingResponse);
            }
        }

        public static void Dispose()
        {
            try
            {
                announceThread.Abort();
            }
            catch (ThreadStateException)
            {
                announceThread.Resume();
            }
        }
    }
}
