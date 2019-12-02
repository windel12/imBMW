using System;
using imBMW.Tools;
using System.Threading;
using imBMW.Enums;
using System.Collections;

namespace imBMW.iBus.Devices.Real
{
    public static class IntegratedHeatingAndAirConditioning
    {
        private static Timer delay;
        private static object _sync = new object();

        private static byte[] startOrContinueWorkingAuxilaryHeater = {0x92, 0x00, 0x22};

        /// <summary> IHKA > ZUH: 92 00 22 </summary>
        public static Message StartAuxilaryHeaterMessage = new Message(DeviceAddress.IntegratedHeatingAndAirConditioning, DeviceAddress.AuxilaryHeater, startOrContinueWorkingAuxilaryHeater);
        /// <summary> IHKA > ZUH: 92 00 22 </summary>
        public static Message ContinueWorkingAuxilaryHeater = new Message(DeviceAddress.IntegratedHeatingAndAirConditioning, DeviceAddress.AuxilaryHeater, startOrContinueWorkingAuxilaryHeater);
        /// <summary> IHKA > ZUH: 92 00 21 </summary>
        public static Message StopAuxilaryHeater1 = new Message(DeviceAddress.IntegratedHeatingAndAirConditioning, DeviceAddress.AuxilaryHeater, 0x92, 0x00, 0x21);
        /// <summary> IHKA > ZUH: 92 00 11 </summary>
        public static Message StopAuxilaryHeater2 = new Message(DeviceAddress.IntegratedHeatingAndAirConditioning, DeviceAddress.AuxilaryHeater, 0x92, 0x00, 0x11);

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

        public static void Init() { }

        static IntegratedHeatingAndAirConditioning()
        {
            KBusManager.Instance.AddMessageReceiverForSourceAndDestinationDevice(DeviceAddress.AuxilaryHeater, DeviceAddress.IntegratedHeatingAndAirConditioning, ProcessAuxilaryHeaterMessage);
            KBusManager.Instance.AddMessageReceiverForSourceAndDestinationDevice(DeviceAddress.IntegratedHeatingAndAirConditioning, DeviceAddress.InstrumentClusterElectronics, ProcessMessageToIKE);
            KBusManager.Instance.AddMessageReceiverForSourceAndDestinationDevice(DeviceAddress.IntegratedHeatingAndAirConditioning, DeviceAddress.Diagnostic, ProcessIHKAMessage);

            //InstrumentClusterElectronics.IgnitionStateChanged += (e) =>
            //{
            //    if (e.PreviousIgnitionState == IgnitionState.Acc && e.CurrentIgnitionState == IgnitionState.Off)
            //    {
            //        KBusManager.Instance.EnqueueMessage(StopAuxilaryHeater2);
            //    }
            //};
        }

        public static void ProcessAuxilaryHeaterMessage(Message m)
        {
            if(Settings.Instance.SuspendAuxilaryHeaterResponseEmulation)
            {
                m.ReceiverDescription = "Auxilary heater is working. Coolant Temperature: " + InstrumentClusterElectronics.TemperatureCoolant;
                return;
            }

            lock (_sync)
            {
                if (m.Data.StartsWith(MessageRegistry.DataPollResponse))
                {
                    Logger.Debug("Auxilary heater responded.");
                    AuxilaryHeater.Status = AuxilaryHeaterStatus.Present;
                    
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

                if (m.Data.StartsWith(AuxilaryHeater.AuxilaryHeaterWorkingResponse.Data))
                {
                    // this happens, if we restart imBMW during webasto working
                    if (AuxilaryHeater.Status == AuxilaryHeaterStatus.Unknown)
                    {
                        AuxilaryHeater.Status = AuxilaryHeaterStatus.Working;
                        Logger.Debug("Auxilary heater, previous state was restored.");
                        // without return!!! for answering
                    }

                    if (AuxilaryHeater.Status == AuxilaryHeaterStatus.StartPending)
                    {
                        Logger.Debug("Auxilary heater started");
                        AuxilaryHeater.Status = AuxilaryHeaterStatus.Started;
                        Manager.Instance.EnqueueMessage(FrontDisplay.AuxHeaterIndicatorBlinkingMessage);
                        return;
                    }
                    if (AuxilaryHeater.Status == AuxilaryHeaterStatus.Started || AuxilaryHeater.Status == AuxilaryHeaterStatus.Working)
                    {
                        if (InstrumentClusterElectronics.TemperatureCoolant >= 72)
                        {
                            Logger.Debug("Turning off Auxilary heater by reaching needed temperature.");
                            StopAuxilaryHeaterInternal();
                        }
                        else
                        {
                            AuxilaryHeater.Status = AuxilaryHeaterStatus.Working;
                            m.ReceiverDescription = "Auxilary heater is working. Coolant Temperature: " + InstrumentClusterElectronics.TemperatureCoolant;

                            var respondMessage = ContinueWorkingAuxilaryHeater;
                            respondMessage.ReceiverDescription = "Continue working.";
                            KBusManager.Instance.EnqueueMessage(respondMessage);
                            Manager.Instance.EnqueueMessage(FrontDisplay.AuxHeaterIndicatorBlinkingMessage);
                        }
                        return;
                    }

                    if (AuxilaryHeater.Status == AuxilaryHeaterStatus.Stopping)
                    {
                        Logger.Warning("Auxilary heater was requested to stop, but it still working. Trying to stop again.");
                        StopAuxilaryHeaterInternal();
                        return;
                    }
                }

                if (AuxilaryHeater.Status == AuxilaryHeaterStatus.Stopping && m.Data.StartsWith(AuxilaryHeater.AuxilaryHeaterStopped1.Data))
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

                if (m.Data.StartsWith(AuxilaryHeater.AuxilaryHeaterStopped2.Data))
                {
                    AuxilaryHeater.Status = AuxilaryHeaterStatus.Stopped;
                    Manager.Instance.EnqueueMessage(FrontDisplay.AuxHeaterIndicatorTurnOffMessage);
                }
            }
        }

        public static void ProcessMessageToIKE(Message m)
        {
            if (m.Data[0] == 0x83 && m.Data.Length == 3)
            {
                if (m.Data[1] == 0x80)
                {
                    AirConditioningCompressorStatus = AirConditioningCompressorStatus.Off;
                }
                else
                {
                    AirConditioningCompressorStatus = AirConditioningCompressorStatus.On;
                }

                AirConditioningCompressorStatus_FirstByte = m.Data[1];
                AirConditioningCompressorStatus_SecondByte = m.Data[2];

                var e = AirConditioningCompressorStatusChanged;
                if (e != null)
                {
                    e();
                }
            }
        }

        public static void ProcessIHKAMessage(Message m)
        {
            if (m.Data.StartsWith(0xA0) && m.Data.Length == 5) // Coding data
            {
                CodingData1 = m.Data[1];
                CodingData2 = m.Data[2];
                CodingData3 = m.Data[3];
                CodingData4 = m.Data[4];

                var e = CodingDataAcquired;
                if (e != null)
                {
                    e();
                }
            }
        }

        /// <summary> IHKA > ZUH: 01 </summary>
        private static void PollAuxilaryHeater()
        {
            Logger.Debug("Poll auxilary heater before start");
            var pollAuxilaryHeaterMessage = new Message(DeviceAddress.IntegratedHeatingAndAirConditioning, DeviceAddress.AuxilaryHeater, MessageRegistry.DataPollRequest);
            KBusManager.Instance.EnqueueMessage(pollAuxilaryHeaterMessage);
        }

        /// <summary> IHKA > ZUH: 92 00 22 </summary>
        private static void StartAuxilaryHeaterInternal()
        {
            lock (_sync)
            {
                Logger.Debug("Auxilary heater start pending: 92 00 22");
                KBusManager.Instance.EnqueueMessage(StartAuxilaryHeaterMessage);
                AuxilaryHeater.Status = AuxilaryHeaterStatus.StartPending;
            }
        }

        /// <summary> IHKA > ZUH:92 00 21 </summary>
        private static void StopAuxilaryHeaterInternal()
        {
            lock (_sync)
            {
                Logger.Debug("Manual stopping of auxilary heater");
                KBusManager.Instance.EnqueueMessage(StopAuxilaryHeater1);
                AuxilaryHeater.Status = AuxilaryHeaterStatus.Stopping;
            }
        }

        public static void StartAuxilaryHeater()
        {
            Logger.Debug("Manual start of auxilary heater");
            PollAuxilaryHeater();
            Manager.Instance.EnqueueMessage(FrontDisplay.AuxHeaterIndicatorTurnOnMessage);
        }

        public static void StopAuxilaryHeater()
        {
            if (AuxilaryHeater.Status == AuxilaryHeaterStatus.Started || AuxilaryHeater.Status == AuxilaryHeaterStatus.Working)
            {
                Logger.Debug("Manual stop of auxilary heater request");
                StopAuxilaryHeaterInternal();
                Manager.Instance.EnqueueMessage(FrontDisplay.AuxHeaterIndicatorTurnOnMessage);
            }
        }

        public static void ReadCodingData()
        {
            KBusManager.Instance.EnqueueMessage(new Message(DeviceAddress.Diagnostic, DeviceAddress.IntegratedHeatingAndAirConditioning, 
                0x08, 0x00, 0x00, 0x00, 0x00, 0x00));
        }

        public static void WriteCodingData()
        {
            KBusManager.Instance.EnqueueMessage(new Message(DeviceAddress.Diagnostic, DeviceAddress.IntegratedHeatingAndAirConditioning, 
                0x09, 0x00, 0x00, 0x00, 0x00, 0x00, CodingData1, CodingData2, CodingData3, CodingData4));
        }

        public static FlapPosition FlapPosition { get; set; }
        public static TemperatureUnit TemperatureUnit { get; set; }
        public static AuxilaryHeaterActivationMode AuxilaryHeaterActivationMode { get; set; }
        public static bool AuxilaryHeating { get; set; }
        public static bool CarKeyMemoryEnabled { get; set; }
        public static bool TemperatureAdjustmentRear { get; set; }
        public static bool ElectricFan { get; set; }
        public static bool AirConditioningBlowerRearSeats { get; set; }

        public static byte[] CodingData => new byte[] { CodingData1, CodingData2, CodingData3, CodingData4 };

        internal static byte CodingData1
        {
            get
            {
                int data = 0x00;
                data |= FlapPosition == FlapPosition.y_fahrer_beifahrer ? 0x01 : 0x00;
                data |= TemperatureUnit == TemperatureUnit.Fahrenheit ? 0x02 : 0x00;
                data |= AuxilaryHeaterActivationMode == AuxilaryHeaterActivationMode.Kbus ? 0x04 : 0x00;
                data |= AuxilaryHeating ? 0x08 : 0x00;
                data |= CarKeyMemoryEnabled ? 0x10 : 0x00;
                data |= TemperatureAdjustmentRear ? 0x20 : 0x00;
                data |= ElectricFan ? 0x40 : 0x00;
                data |= AirConditioningBlowerRearSeats ? 0x80 : 0x00;
                return (byte)data;
            }
            set
            {
                FlapPosition = value.HasBit(0) ? FlapPosition.y_fahrer_beifahrer : FlapPosition.y_fahrer;
                TemperatureUnit = value.HasBit(1) ? TemperatureUnit.Fahrenheit : TemperatureUnit.Celsius;
                AuxilaryHeaterActivationMode = value.HasBit(2) ? AuxilaryHeaterActivationMode.Kbus : AuxilaryHeaterActivationMode.Normal;
                AuxilaryHeating = value.HasBit(3);
                CarKeyMemoryEnabled = value.HasBit(4);
                TemperatureAdjustmentRear = value.HasBit(5);
                ElectricFan = value.HasBit(6);
                AirConditioningBlowerRearSeats = value.HasBit(7);
            }
        }

        internal static byte CodingData2 { get; set; }
        internal static byte CodingData3 { get; set; }
        internal static byte CodingData4 { get; set; }

        public delegate void AuxilaryHeaterWorkingRequestsCounterEventHandler(byte counter);

        public static event AuxilaryHeaterWorkingRequestsCounterEventHandler AuxilaryHeaterWorkingRequestsCounterChanged;
        public static event Action AirConditioningCompressorStatusChanged;
        public static event Action CodingDataAcquired;
    }
}
