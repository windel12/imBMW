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

        private static DateTime _auxilaryHeaterWorkingLastResponseTime;

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

        private static byte TrunkLidButtonPressedCount = 0;

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

            BodyModule.RemoteKeyButtonPressed += BodyModule_RemoteKeyButtonPressed;
        }

        private static void BodyModule_RemoteKeyButtonPressed(RemoteKeyEventArgs e)
        {
            if (e.Button == RemoteKeyButton.Trunk)
            {
                TrunkLidButtonPressedCount++;
                switch (TrunkLidButtonPressedCount)
                {
                    case 1:
                        LightControlModule.TurnOnLamps(Lights.FrontLeftStandingLight & Lights.FrontRightStandingLight);
                        break;
                    case 2:
                        LightControlModule.TurnOnLamps(Lights.FrontLeftStandingLight & Lights.FrontRightStandingLight &
                                                       Lights.FrontLeftBlinker & Lights.FrontRightBlinker);
                        break;
                    case 3:
                        LightControlModule.TurnOnLamps(Lights.FrontLeftStandingLight & Lights.FrontRightStandingLight &
                                                       Lights.FrontLeftBlinker & Lights.FrontRightBlinker &
                                                       Lights.FrontLeftFogLamp & Lights.FrontRightFogLamp);
                        StartAuxilaryHeater();
                        break;
                }
            }
            else
            {
                TrunkLidButtonPressedCount = 0;
            }
        }

        public static void ProcessAuxilaryHeaterMessage(Message m)
        {
            m.ReceiverDescription = "Coolant Temperature: " + InstrumentClusterElectronics.TemperatureCoolant;

            if (Settings.Instance.SuspendAuxilaryHeaterResponseEmulation)
            {
                /*
                05-12 19:06:47.727 [DEBUG] Manual start of auxilary heater
                05-12 19:06:47.729 [DEBUG] Auxilary heater start pending: 92 00 22
                05-12 19:06:47.739 [K > ] IHKA > ZUH: 92 00 22 (Command for auxilary heater)
                05-12 19:06:47.743 [I > ] IKE > ANZV: 2A 00 04 (On-Board Computer State Update)
                05-12 19:06:47.781 [K < ] ZUH > IHKA: 93 00 22 {Coolant Temperature: 15}

                05-12 19:06:48.168 [K < ] IHKA > ZUH: 92 00 22 (Command for auxilary heater)
                05-12 19:06:48.187 [K < ] IHKA > GLO: 82 05 [IHKA turned on by webasto activation]
                05-12 19:06:48.214 [I < ] IHKA > GLO: 82 05 [IHKA turned on by webasto activation]
                05-12 19:06:48.219 [K < ] ZUH > IHKA: 93 00 22 {Coolant Temperature: 15}

                -- after 30 sec
                05-12 19:07:16.513 [K < ] ZUH > IHKA: 93 00 22 {Coolant Temperature: 16}
                05-12 19:07:16.966 [K < ] IHKA > ZUH: 92 00 22 (Command for auxilary heater)
                 */

                if (m.Data.StartsWith(AuxilaryHeater.AuxilaryHeaterWorkingResponse.Data))
                {
                    // turning on lights, if webasto working
                    if (InstrumentClusterElectronics.CurrentIgnitionState == IgnitionState.Off)
                    {
                        LightControlModule.TurnOnLamps(Lights.FrontLeftBlinker & Lights.FrontRightBlinker & Lights.RearLeftStandingLight & Lights.RearRightStandingLight);
                    }

                    // skipping first reply
                    if (AuxilaryHeater.Status == AuxilaryHeaterStatus.Starting)
                    {
                        AuxilaryHeater.Status = AuxilaryHeaterStatus.Started;
                        return;
                    }

                    // WORKAROUND: when IHKA replied, but webasto not acquired response, and sending message again, but IHKA isn't replying, because thinking that already replied
                    Logger.Trace("_auxilaryHeaterWorkingLastResponseTime: " + _auxilaryHeaterWorkingLastResponseTime, "WEBASTO");
                    Logger.Trace("DateTime.Now: " + DateTime.Now, "WEBASTO");
                    if (_auxilaryHeaterWorkingLastResponseTime > DateTime.Now.AddSeconds(-15) && _auxilaryHeaterWorkingLastResponseTime < DateTime.Now)
                    {
                        Logger.Error("Webasto thinking that K-Bus connection is lost and trying to fire K-Bus breakdown error. ", "WEBASTO");
                        // TODO: Wait for 1 sec, and if there is no real reply from IHKA - reply manually. see 2020.03.27\traceLog45.
                        var respondMessage = ContinueWorkingAuxilaryHeater;
                        respondMessage.ReceiverDescription = "Replying instead of IHKA.";
                        KBusManager.Instance.EnqueueMessage(respondMessage);
                    }
                    _auxilaryHeaterWorkingLastResponseTime = DateTime.Now;
                }
                return;
            }

            lock (_sync)
            {
                if (m.Data.StartsWith(MessageRegistry.DataPollResponse))
                {
                    Logger.Debug("SuspendAuxilaryHeaterResponseEmulation:" + Settings.Instance.SuspendAuxilaryHeaterResponseEmulation);
                    if (AuxilaryHeater.Status == AuxilaryHeaterStatus.Unknown)
                    {
                        AuxilaryHeater.Status = AuxilaryHeaterStatus.Present;
                    }
                }

                if (m.Data.StartsWith(AuxilaryHeater.AuxilaryHeaterWorkingResponse.Data))
                {
                    // this happens, if we restart imBMW during webasto working
                    if (AuxilaryHeater.Status == AuxilaryHeaterStatus.Unknown)
                    {
                        AuxilaryHeater.Status = AuxilaryHeaterStatus.Working;
                        Logger.Debug("Auxilary heater, previous state was restored.");
                    }

                    if (AuxilaryHeater.Status == AuxilaryHeaterStatus.Starting)
                    {
                        Logger.Debug("Auxilary heater started.");
                        AuxilaryHeater.Status = AuxilaryHeaterStatus.Started;
                        Manager.Instance.EnqueueMessage(FrontDisplay.AuxHeaterIndicatorBlinkingMessage);
                        return;
                    }
                    if (AuxilaryHeater.Status == AuxilaryHeaterStatus.Started)
                    {
                        Logger.Debug("First working response acquired successfully.");
                        AuxilaryHeater.Status = AuxilaryHeaterStatus.Working;
                        return;
                    }

                    if (AuxilaryHeater.Status == AuxilaryHeaterStatus.Working)
                    {
                        if (InstrumentClusterElectronics.TemperatureCoolant >= 72)
                        {
                            Logger.Debug("Turning off Auxilary heater by reaching needed temperature.");
                            StopAuxilaryHeaterInternal();
                        }
                        else
                        {
                            AuxilaryHeater.Status = AuxilaryHeaterStatus.Working;
                            m.ReceiverDescription = "Auxilary heater is working." + m.ReceiverDescription;

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

                if (m.Data.StartsWith(AuxilaryHeater.AuxilaryHeaterStopped1.Data))
                {
                    AuxilaryHeater.Status = AuxilaryHeaterStatus.WorkPending;
                    m.ReceiverDescription = "WorkPending? " + m.ReceiverDescription;
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
        public static void PollAuxilaryHeater()
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
                AuxilaryHeater.Status = AuxilaryHeaterStatus.Starting;
            }
        }

        /// <summary> IHKA > ZUH:92 00 11 </summary>
        private static void StopAuxilaryHeaterInternal()
        {
            lock (_sync)
            {
                Logger.Debug("Manual stopping of auxilary heater");
                KBusManager.Instance.EnqueueMessage(StopAuxilaryHeater2);
                AuxilaryHeater.Status = AuxilaryHeaterStatus.Stopping;
            }
        }

        public static void StartAuxilaryHeater()
        {
            Logger.Debug("Manual start of auxilary heater");
            StartAuxilaryHeaterInternal();
            Manager.Instance.EnqueueMessage(FrontDisplay.AuxHeaterIndicatorTurnOnMessage);
        }

        public static void StopAuxilaryHeater()
        {
            Logger.Debug("Manual stop of auxilary heater request");
            StopAuxilaryHeaterInternal();
            Manager.Instance.EnqueueMessage(FrontDisplay.AuxHeaterIndicatorTurnOnMessage);
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
