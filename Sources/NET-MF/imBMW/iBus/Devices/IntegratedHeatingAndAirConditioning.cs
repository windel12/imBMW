using System;
using imBMW.Tools;
using System.Threading;

namespace imBMW.iBus.Devices.Real
{
    public enum AirConditioningCompressorStatus
    {
        Off,
        On
    }

    public static class IntegratedHeatingAndAirConditioning
    {
        private static Timer delay;
        private static object _sync = new object();

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

        public static AirConditioningCompressorStatus AirConditioningCompressorStatus = AirConditioningCompressorStatus.Off;

        public static byte AirConditioningCompressorStatus_FirstByte = 0x00;
        public static byte AirConditioningCompressorStatus_SecondByte = 0x00;

        static IntegratedHeatingAndAirConditioning()
        {
            KBusManager.Instance.AddMessageReceiverForSourceAndDestinationDevice(DeviceAddress.AuxilaryHeater, DeviceAddress.IntegratedHeatingAndAirConditioning, ProcessAuxilaryHeaterMessage);
            KBusManager.Instance.AddMessageReceiverForSourceAndDestinationDevice(DeviceAddress.IntegratedHeatingAndAirConditioning, DeviceAddress.InstrumentClusterElectronics, ProcessMessageToIKE);
        }

        public static void ProcessAuxilaryHeaterMessage(Message message)
        {
            lock (_sync)
            {
                if (message.Data.StartsWith(MessageRegistry.DataPollResponse))
                {
                    Logger.Trace("Auxilary heater responded.");
                    AuxilaryHeaterStatus = AuxilaryHeaterStatus.Present;
                    delay = new Timer(delegate
                    {
                        StartAuxilaryHeaterInternal();

                        if (delay != null)
                        {
                            delay.Dispose();
                            delay = null;
                        }
                    }, null, 2000, 0);
                    return;
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
                        Manager.Instance.EnqueueMessage(FrontDisplay.AuxHeaterIndicatorBlinkingMessage);
                        return;
                    }
                    if (AuxilaryHeaterStatus == AuxilaryHeaterStatus.Started)
                    {
                        KBusManager.Instance.EnqueueMessage(ContinueWorkingAuxilaryHeater);
                        Logger.Trace("Coolant Temperature: " + InstrumentClusterElectronics.TemperatureCoolant);
                        Manager.Instance.EnqueueMessage(FrontDisplay.AuxHeaterIndicatorBlinkingMessage);

                        bool stoppingByReachingNeededTemperature = InstrumentClusterElectronics.TemperatureCoolant >= 75;
                        if (stoppingByReachingNeededTemperature)
                        {
                            Logger.Trace("Turning off Auxilary heater by reaching needed temperature.");
                            StopAuxilaryHeaterInternal();
                        }
                        return;
                    }
                }

                if (message.Data.StartsWith(AuxilaryHeater.AuxilaryHeaterStopped1.Data))
                {
                    delay = new Timer(delegate
                    {
                        KBusManager.Instance.EnqueueMessage(StopAuxilaryHeater2);

                        if (delay != null)
                        {
                            delay.Dispose();
                            delay = null;
                        }
                    }, null, 1000, 0);
                }
                if (message.Data.StartsWith(AuxilaryHeater.AuxilaryHeaterStopped2.Data))
                {
                    AuxilaryHeaterStatus = AuxilaryHeaterStatus.Stopped;
                }
            }
        }

        public static void ProcessMessageToIKE(Message message)
        {
            if (message.Data[0] == 0x83 && message.Data.Length == 3)
            {
                if (message.Data[1] == 0x80)
                {
                    AirConditioningCompressorStatus = AirConditioningCompressorStatus.Off;
                }
                else
                {
                    AirConditioningCompressorStatus = AirConditioningCompressorStatus.On;
                }

                AirConditioningCompressorStatus_FirstByte = message.Data[1];
                AirConditioningCompressorStatus_SecondByte = message.Data[2];

                var e = AirConditioningCompressorStatusChanged;
                if (e != null)
                {
                    e();
                }
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
            lock (_sync)
            {
                Logger.Trace("Auxilary heater start pending: 92 00 22");
                KBusManager.Instance.EnqueueMessage(StartAuxilaryHeaterMessage);
                AuxilaryHeaterStatus = AuxilaryHeaterStatus.StartPending;
            }
        }

        /// <summary> IntegratedHeatingAndAirConditioning > AuxilaryHeater: 92 00 21 </summary>
        private static void StopAuxilaryHeaterInternal()
        {
            lock (_sync)
            {
                Logger.Trace("Manual stopping of auxilary heater");
                KBusManager.Instance.EnqueueMessage(StopAuxilaryHeater1);
                AuxilaryHeaterStatus = AuxilaryHeaterStatus.Stopping;
                Manager.Instance.EnqueueMessage(FrontDisplay.AuxHeaterIndicatorTurnOffMessage);
            }
        }

        public static void StartAuxilaryHeater()
        {
            Logger.Trace("Manual start of auxilary heater");
            PollAuxilaryHeater();
            Manager.Instance.EnqueueMessage(FrontDisplay.AuxHeaterIndicatorTurnOnMessage);
            //AuxilaryHeaterStatus = AuxilaryHeaterStatus.Unknown;
        }

        public static void StopAuxilaryHeater()
        {
            if (AuxilaryHeaterStatus == AuxilaryHeaterStatus.Started)
            {
                Logger.Trace("Manual stop of auxilary heater request");
                StopAuxilaryHeaterInternal();
            }
        }

        public static void Init() { }

        public static event AuxilaryHeater.AuxilaryHeaterStatusEventHandler AuxilaryHeaterStatusChanged;

        public delegate void AuxilaryHeaterWorkingRequestsCounterEventHandler(byte counter);
        public static event AuxilaryHeaterWorkingRequestsCounterEventHandler AuxilaryHeaterWorkingRequestsCounterChanged;

        public static event Action AirConditioningCompressorStatusChanged;
    }
}
