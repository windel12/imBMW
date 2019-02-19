using System;
using imBMW.Tools;
using System.Threading;

namespace imBMW.iBus.Devices.Real
{
    public static class IntegratedHeatingAndAirConditioning
    {
        private static Timer startDelay;

        private static byte[] startOrContinueWorkingAuxilaryHeater = {0x92, 0x00, 0x22};

        /// <summary> 5B 05 6B 92 00 22 ?? </summary>
        public static Message StartAuxilaryHeaterMessage = new Message(DeviceAddress.IntegratedHeatingAndAirConditioning, DeviceAddress.AuxilaryHeater, startOrContinueWorkingAuxilaryHeater);
        /// <summary> 5B 05 6B 92 00 22 ?? </summary>
        public static Message ContinueWorkingAuxilaryHeater = new Message(DeviceAddress.IntegratedHeatingAndAirConditioning, DeviceAddress.AuxilaryHeater, startOrContinueWorkingAuxilaryHeater);
        /// <summary> 5B 05 6B 92 00 21 ?? </summary>
        public static Message StopAuxilaryHeater1 = new Message(DeviceAddress.IntegratedHeatingAndAirConditioning, DeviceAddress.AuxilaryHeater, 0x92, 0x00, 0x21);
        /// <summary> 5B 05 6B 92 00 11 ?? </summary>
        public static Message StopAuxilaryHeater2 = new Message(DeviceAddress.IntegratedHeatingAndAirConditioning, DeviceAddress.AuxilaryHeater, 0x92, 0x00, 0x11);

        private static AuxilaryHeaterStatus _auxilaryHeaterStatus;
        public static AuxilaryHeaterStatus AuxilaryHeaterStatus
        {
            get { return _auxilaryHeaterStatus; }
            private set
            {
                var previousStatus = _auxilaryHeaterStatus;
                _auxilaryHeaterStatus = value;

                if (value != previousStatus)
                {
                    var e = AuxilaryHeaterStatusChanged;
                    if (e != null)
                    {
                        e(value);
                    }
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
                Logger.Trace("Auxilary heater responded.");
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

            if (message.Data.StartsWith(AuxilaryHeater.AuxilaryHeaterWorkingResponse.Data))
            {
                // this happens, if we restart imBMW during webasto working
                if (AuxilaryHeaterStatus == AuxilaryHeaterStatus.Unknown)
                {
                    AuxilaryHeaterStatus = AuxilaryHeaterStatus.Started;
                    Logger.Trace("Auxilary Heater, previous state was restored.");
                    // without return!!! for answering
                }

                if (AuxilaryHeaterStatus == AuxilaryHeaterStatus.StartPending)
                {
                    Logger.Trace("Auxilary heater started");
                    AuxilaryHeaterStatus = AuxilaryHeaterStatus.Started;
                    Manager.EnqueueMessage(new Message(DeviceAddress.InstrumentClusterElectronics, DeviceAddress.FrontDisplay, 0x2A, 0x00, 0x08)); // Aux_Heating_LED = On_blink
                    return;
                }
                if (AuxilaryHeaterStatus == AuxilaryHeaterStatus.Started || AuxilaryHeaterStatus == AuxilaryHeaterStatus.StopPending || AuxilaryHeaterStatus == AuxilaryHeaterStatus.Stopping)
                {
                    KBusManager.Instance.EnqueueMessage(ContinueWorkingAuxilaryHeater);
                    Logger.Trace("Coolant Temperature: " + InstrumentClusterElectronics.TemperatureCoolant);
                    bool stoppingByReachingNeededTemperature = InstrumentClusterElectronics.TemperatureCoolant >= 75;
                    if (AuxilaryHeaterStatus == AuxilaryHeaterStatus.StopPending || AuxilaryHeaterStatus == AuxilaryHeaterStatus.Stopping || stoppingByReachingNeededTemperature)
                    {
                        if (stoppingByReachingNeededTemperature)
                        {
                            Logger.Trace("Turning off Auxilary heater by reaching needed temperature.");
                        }
                        StopAuxilaryHeaterInternal();
                    }
                    return;
                }
            }

            if (message.Data.StartsWith(AuxilaryHeater.AuxilaryHeaterStopped1.Data))
            {
                startDelay = new Timer(delegate
                {
                    KBusManager.Instance.EnqueueMessage(StopAuxilaryHeater2);

                    if (startDelay != null)
                    {
                        startDelay.Dispose();
                        startDelay = null;
                    }
                }, null, 1000, 0);
            }
            if (message.Data.StartsWith(AuxilaryHeater.AuxilaryHeaterStopped2.Data))
            {
                AuxilaryHeaterStatus = AuxilaryHeaterStatus.Stopped;
            }
        }

        /// <summary> IntegratedHeatingAndAirConditioning > AuxilaryHeater: 01 </summary>
        private static void PollAuxilaryHeater()
        {
            Logger.Trace("Poll auxilary heater before start");
            var pollAuxilaryHeaterMessage = new Message(DeviceAddress.IntegratedHeatingAndAirConditioning, DeviceAddress.AuxilaryHeater, MessageRegistry.DataPollRequest);
            KBusManager.Instance.EnqueueMessage(pollAuxilaryHeaterMessage);
        }

        /// <summary> IntegratedHeatingAndAirConditioning > AuxilaryHeater: 92 00 22 </summary>
        private static void StartAuxilaryHeaterInternal()
        {
            Logger.Trace("Auxilary heater start pending: 92 00 22");
            KBusManager.Instance.EnqueueMessage(StartAuxilaryHeaterMessage);
            AuxilaryHeaterStatus = AuxilaryHeaterStatus.StartPending;
        }

        /// <summary> IntegratedHeatingAndAirConditioning > AuxilaryHeater: 92 00 21 </summary>
        private static void StopAuxilaryHeaterInternal()
        {
            Logger.Trace("Manual stopping of auxilary heater");
            KBusManager.Instance.EnqueueMessage(StopAuxilaryHeater1);
            AuxilaryHeaterStatus = AuxilaryHeaterStatus.Stopping;
            Manager.EnqueueMessage(new Message(DeviceAddress.InstrumentClusterElectronics, DeviceAddress.FrontDisplay, 0x2A, 0x00, 0x04)); // Aux_Heating_LED = On
        }

        public static void StartAuxilaryHeater()
        {
            Logger.Trace("Manual start of auxilary heater");
            PollAuxilaryHeater();
            Manager.EnqueueMessage(new Message(DeviceAddress.InstrumentClusterElectronics, DeviceAddress.FrontDisplay, 0x2A, 0x00, 0x04)); // Aux_Heating_LED = On
            //AuxilaryHeaterStatus = AuxilaryHeaterStatus.Unknown;
        }

        public static void StopAuxilaryHeater()
        {
            if (AuxilaryHeaterStatus == AuxilaryHeaterStatus.Started)
            {
                Logger.Trace("Manual stop of auxilary heater request");
                AuxilaryHeaterStatus = AuxilaryHeaterStatus.StopPending;
            }
        }

        public static void Init() { }

        public static event AuxilaryHeater.AuxilaryHeaterStatusEventHandler AuxilaryHeaterStatusChanged;

        public delegate void AuxilaryHeaterWorkingRequestsCounterEventHandler(byte counter);
        public static event AuxilaryHeaterWorkingRequestsCounterEventHandler AuxilaryHeaterWorkingRequestsCounterChanged;
    }
}
