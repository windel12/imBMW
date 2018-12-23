using System;
using imBMW.Tools;
using System.Threading;

namespace imBMW.iBus.Devices.Real
{
    public static class IntegratedHeatingAndAirConditioning
    {
        private static Timer startDelay;

        private static byte[] startOrContinueWorkingAdditionalHeater = {0x92, 0x00, 0x22};

        /// <summary> 5B 05 6B 92 00 22 ?? </summary>
        public static Message StartAdditionalHeater = new Message(DeviceAddress.IntegratedHeatingAndAirConditioning, DeviceAddress.AuxilaryHeater, startOrContinueWorkingAdditionalHeater);
        /// <summary> 5B 05 6B 92 00 22 ?? </summary>
        public static Message ContinueWorkingAdditionalHeater = new Message(DeviceAddress.IntegratedHeatingAndAirConditioning, DeviceAddress.AuxilaryHeater, startOrContinueWorkingAdditionalHeater);
        /// <summary> 5B 05 6B 92 00 21 ?? </summary>
        public static Message StopAdditionalHeater1 = new Message(DeviceAddress.IntegratedHeatingAndAirConditioning, DeviceAddress.AuxilaryHeater, 0x92, 0x00, 0x21);
        /// <summary> 5B 05 6B 92 00 11 ?? </summary>
        public static Message StopAdditionalHeater2 = new Message(DeviceAddress.IntegratedHeatingAndAirConditioning, DeviceAddress.AuxilaryHeater, 0x92, 0x00, 0x11);

        private static AuxilaryHeaterStatus _auxilaryHeaterStatus;
        public static AuxilaryHeaterStatus AuxilaryHeaterStatus
        {
            get { return _auxilaryHeaterStatus; }
            private set
            {
                _auxilaryHeaterStatus = value;
                var e = AuxilaryHeaterStatusChanged;
                if (e != null)
                {
                    e(value);
                }
            }
        }

        private static byte _auxilaryHeaterWorkingRequestsCounter = 0;
        public static byte AuxilaryHeaterWorkingRequestsCounter
        {
            get { return _auxilaryHeaterWorkingRequestsCounter; }
            set
            {
                _auxilaryHeaterWorkingRequestsCounter = value;
                var e = AuxilaryHeaterWorkingRequestsCounterChanged;
                if (e != null)
                {
                    e(value);
                }
            }
        }

        static IntegratedHeatingAndAirConditioning()
        {
            KBusManager.Instance.AddMessageReceiverForSourceAndDestinationDevice(DeviceAddress.AuxilaryHeater, DeviceAddress.IntegratedHeatingAndAirConditioning, ProcessAuxilaryHeaterMessage);
            InstrumentClusterElectronics.IgnitionStateChanged += InstrumentClusterElectronics_IgnitionStateChanged;
        }

        private static void InstrumentClusterElectronics_IgnitionStateChanged(IgnitionEventArgs e)
        {
            Logger.Trace("Ignition was changed: " + e.PreviousIgnitionState.ToStringValue() + " > " + e.CurrentIgnitionState.ToStringValue());
        }

        public static void ProcessAuxilaryHeaterMessage(Message message)
        {
            if (message.Data.StartsWith(MessageRegistry.DataPollResponse))
            {
                AuxilaryHeaterStatus = AuxilaryHeaterStatus.Present;
                startDelay = new Timer(delegate
                {
                    StartAuxilaryHeaterInternal();

                    if (startDelay != null)
                    {
                        startDelay.Dispose();
                        startDelay = null;
                    }
                }, null, 2000, 0);
            }

            if (message.Data.StartsWith(AuxilaryHeater.AdditionalHeaterWorkingResponse.Data))
            {
                // this happens, if we restart imBMW during webasto working
                if (AuxilaryHeaterStatus == AuxilaryHeaterStatus.Unknown)
                {
                    AuxilaryHeaterStatus = AuxilaryHeaterStatus.Started;
                }
                if (AuxilaryHeaterStatus == AuxilaryHeaterStatus.StartPending)
                {
                    AuxilaryHeaterStatus = AuxilaryHeaterStatus.Started;
                    return;
                }
                if (AuxilaryHeaterStatus == AuxilaryHeaterStatus.Started)
                {
                    ++AuxilaryHeaterWorkingRequestsCounter;
                    KBusManager.Instance.EnqueueMessage(ContinueWorkingAdditionalHeater);
                    return;
                }
            }

            if (message.Data.StartsWith(AuxilaryHeater.AdditionalHeaterStopped1.Data))
            {
                startDelay = new Timer(delegate
                {
                    AuxilaryHeaterStatus = AuxilaryHeaterStatus.StopPending;
                    KBusManager.Instance.EnqueueMessage(StopAdditionalHeater2);

                    if (startDelay != null)
                    {
                        startDelay.Dispose();
                        startDelay = null;
                    }
                }, null, 1000, 0);
            }
            if (message.Data.StartsWith(AuxilaryHeater.AdditionalHeaterStopped2.Data))
            {
                AuxilaryHeaterStatus = AuxilaryHeaterStatus.Stopped;
            }
        }

        private static void PollAuxilaryHeater()
        {
            var pollAuxilaryHeaterMessage = new Message(DeviceAddress.IntegratedHeatingAndAirConditioning, DeviceAddress.AuxilaryHeater, MessageRegistry.DataPollRequest);
            KBusManager.Instance.EnqueueMessage(pollAuxilaryHeaterMessage);
        }

        private static void StartAuxilaryHeaterInternal()
        {
            KBusManager.Instance.EnqueueMessage(StartAdditionalHeater);
            AuxilaryHeaterStatus = AuxilaryHeaterStatus.StartPending;
        }

        public static void StartAuxilaryHeater()
        {
            Logger.Trace("Manual start of auxilary heater");
            PollAuxilaryHeater();
        }

        public static void StopAuxilaryHeater()
        {
            Logger.Trace("Manual stop of auxilary heater");
            KBusManager.Instance.EnqueueMessage(StopAdditionalHeater1);
        }

        public static void Init() { }

        public static event AuxilaryHeater.AuxilaryHeaterStatusEventHandler AuxilaryHeaterStatusChanged;

        public delegate void AuxilaryHeaterWorkingRequestsCounterEventHandler(byte counter);
        public static event AuxilaryHeaterWorkingRequestsCounterEventHandler AuxilaryHeaterWorkingRequestsCounterChanged;
    }
}
