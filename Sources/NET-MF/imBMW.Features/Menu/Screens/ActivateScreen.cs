using imBMW.Enums;
using imBMW.Features.Localizations;
using imBMW.iBus;
using imBMW.iBus.Devices.Real;

namespace imBMW.Features.Menu.Screens
{
    public class ActivateScreen : MenuScreen
    {
        protected static ActivateScreen instance;

        //DS2Message navigation_module_status_lesen = new DS2Message(DeviceAddress.NavigationEurope, 0x0B);

        protected ActivateScreen()
        {
            FastMenuDrawing = false;

            TitleCallback = s => Radio.AudioSource.ToStringValue();
            //StatusCallback = s => AuxilaryHeater.Status.ToStringValue();

            AuxilaryHeater.Init();
        }

        protected virtual void SetItems()
        {
            AddItem(new MenuItem(i => "OFF: 36 AF", x =>
            {
                DigitalSignalProcessingAudioAmplifier.ChangeSource(AudioSource.Off);
            }, MenuItemType.Button, MenuItemAction.Refresh));

            AddItem(new MenuItem(i => "CD: 36 A0", x =>
            {
                DigitalSignalProcessingAudioAmplifier.ChangeSource(AudioSource.CD);
            }, MenuItemType.Button, MenuItemAction.Refresh));

            AddItem(new MenuItem(i => "Tuner/Tape: 36 A1", x =>
            {
                DigitalSignalProcessingAudioAmplifier.ChangeSource(AudioSource.TunerTape);
            }, MenuItemType.Button, MenuItemAction.Refresh));

            AddItem(new MenuItem(i => "DSP > LOC: 02 01", x =>
            {
                Manager.Instance.EnqueueMessage(new Message(DeviceAddress.DigitalSignalProcessingAudioAmplifier, DeviceAddress.LocalBroadcastAddress, 0x02, 0x01));
            }, MenuItemType.Button, MenuItemAction.Refresh));

            AddItem(new MenuItem(i => "LCD Clear: 46 01", x =>
            {
                Manager.Instance.EnqueueMessage(new Message(DeviceAddress.Radio, DeviceAddress.GraphicsNavigationDriver, 0x46, 0x01));
            }, MenuItemType.Button, MenuItemAction.Refresh));

            AddItem(new MenuItem(i => "LCD Clear: 46 02", x =>
            {
                Manager.Instance.EnqueueMessage(new Message(DeviceAddress.Radio, DeviceAddress.GraphicsNavigationDriver, 0x46, 0x02));
            }, MenuItemType.Button, MenuItemAction.Refresh));

            //AddItem(new MenuItem(i => "Increase delay: " + DBusManager.Port.AfterWriteDelay, x =>
            //{
            //    DBusManager.Port.AfterWriteDelay += 1;
            //}, MenuItemType.Button, MenuItemAction.Refresh));
            //AddItem(new MenuItem(i => "Decrease delay: " + DBusManager.Port.AfterWriteDelay, x =>
            //{
            //    if (DBusManager.Port.AfterWriteDelay > 0)
            //        DBusManager.Port.AfterWriteDelay -= 1;
            //}, MenuItemType.Button, MenuItemAction.Refresh));

            AddItem(new MenuItem(i => "Temp: " + DigitalDieselElectronics.PresupplyPressure.ToString("F2"), i =>
            {
                // DBusManager.Port.WriteBufferSize = 1;
                DBusManager.Instance.EnqueueMessage(DigitalDieselElectronics.status_motortemperatur);
            }));

            AddItem(new MenuItem(i => "Pre-supply: " + DigitalDieselElectronics.PresupplyPressure.ToString("F2"), i =>
            {
               // DBusManager.Port.WriteBufferSize = 1;
                DBusManager.Instance.EnqueueMessage(DigitalDieselElectronics.status_vorfoederdruck);
            }));

            AddItem(new MenuItem(i => "Eluefter: " + DigitalDieselElectronics.EluefterFrequency, x =>
            {
                byte value = DigitalDieselElectronics.EluefterFrequency;

                if (DigitalDieselElectronics.EluefterFrequency < 30)
                    value = 30;
                else if (DigitalDieselElectronics.EluefterFrequency >= 30 && DigitalDieselElectronics.EluefterFrequency < 60)
                    value = 60;
                else if (DigitalDieselElectronics.EluefterFrequency >= 60 && DigitalDieselElectronics.EluefterFrequency < 90)
                    value = 90;
                else
                    value = 0;

                DBusManager.Instance.EnqueueMessage(DigitalDieselElectronics.SteuernEluefter(value));
            }));

            //AddItem(new MenuItem(i => Localization.Current.Voltage + ": " + (NavigationModule.BatteryVoltage > 0 ? NavigationModule.BatteryVoltage.ToString("F2") : "-") + " " + Localization.Current.VoltageShort, x =>
            //{
            //    DBusManager.Port.WriteBufferSize = 0;
            //    DBusManager.Instance.EnqueueMessage(navigation_module_status_lesen);
            //}));

            //AddItem(new MenuItem(i => "DisableWatchdog", x =>
            //{
            //    OnDisableWatchdogCounterReset();
            //}, MenuItemType.Button, MenuItemAction.None));


            //AddItem(new MenuItem(i => "First+: " + IntegratedHeatingAndAirConditioning.AirConditioningCompressorStatus_FirstByte.ToString("X") + " " + IntegratedHeatingAndAirConditioning.AirConditioningCompressorStatus_SecondByte.ToString("X"), x =>
            //{
            //    if (IntegratedHeatingAndAirConditioning.AirConditioningCompressorStatus_FirstByte != byte.MaxValue)
            //    {
            //        IntegratedHeatingAndAirConditioning.AirConditioningCompressorStatus_FirstByte += 0x02;
            //    }

            //    var messageForTurningLuefter = new Message(DeviceAddress.IntegratedHeatingAndAirConditioning, DeviceAddress.InstrumentClusterElectronics,
            //        0x83, IntegratedHeatingAndAirConditioning.AirConditioningCompressorStatus_FirstByte, IntegratedHeatingAndAirConditioning.AirConditioningCompressorStatus_SecondByte);
            //    KBusManager.Instance.EnqueueMessage(messageForTurningLuefter);
            //}));
            //AddItem(new MenuItem(i => "First-: " + IntegratedHeatingAndAirConditioning.AirConditioningCompressorStatus_FirstByte.ToString("X") + " " + IntegratedHeatingAndAirConditioning.AirConditioningCompressorStatus_SecondByte.ToString("X"), x =>
            //{
            //    if (IntegratedHeatingAndAirConditioning.AirConditioningCompressorStatus_FirstByte != byte.MinValue)
            //    {
            //        IntegratedHeatingAndAirConditioning.AirConditioningCompressorStatus_FirstByte -= 0x02;
            //    }

            //    var messageForTurningLuefter = new Message(DeviceAddress.IntegratedHeatingAndAirConditioning, DeviceAddress.InstrumentClusterElectronics,
            //        0x83, IntegratedHeatingAndAirConditioning.AirConditioningCompressorStatus_FirstByte, IntegratedHeatingAndAirConditioning.AirConditioningCompressorStatus_SecondByte);
            //    KBusManager.Instance.EnqueueMessage(messageForTurningLuefter);
            //}));
            //AddItem(new MenuItem(i => "Second+" + IntegratedHeatingAndAirConditioning.AirConditioningCompressorStatus_FirstByte.ToString("X") + " " + IntegratedHeatingAndAirConditioning.AirConditioningCompressorStatus_SecondByte.ToString("X"), x =>
            //{
            //    if (IntegratedHeatingAndAirConditioning.AirConditioningCompressorStatus_SecondByte != byte.MaxValue)
            //    {
            //        IntegratedHeatingAndAirConditioning.AirConditioningCompressorStatus_SecondByte += 0x08;
            //    }

            //    var messageForTurningLuefter = new Message(DeviceAddress.IntegratedHeatingAndAirConditioning, DeviceAddress.InstrumentClusterElectronics,
            //        0x83, IntegratedHeatingAndAirConditioning.AirConditioningCompressorStatus_FirstByte, IntegratedHeatingAndAirConditioning.AirConditioningCompressorStatus_SecondByte);
            //    KBusManager.Instance.EnqueueMessage(messageForTurningLuefter);
            //}));
            //AddItem(new MenuItem(i => "Second-" + IntegratedHeatingAndAirConditioning.AirConditioningCompressorStatus_FirstByte.ToString("X") + " " + IntegratedHeatingAndAirConditioning.AirConditioningCompressorStatus_SecondByte.ToString("X"), x =>
            //{
            //    if (IntegratedHeatingAndAirConditioning.AirConditioningCompressorStatus_SecondByte != byte.MinValue)
            //    {
            //        IntegratedHeatingAndAirConditioning.AirConditioningCompressorStatus_SecondByte -= 0x08;
            //    }

            //    var messageForTurningLuefter = new Message(DeviceAddress.IntegratedHeatingAndAirConditioning, DeviceAddress.InstrumentClusterElectronics,
            //        0x83, IntegratedHeatingAndAirConditioning.AirConditioningCompressorStatus_FirstByte, IntegratedHeatingAndAirConditioning.AirConditioningCompressorStatus_SecondByte);
            //    KBusManager.Instance.EnqueueMessage(messageForTurningLuefter);
            //}));


            //AddItem(new MenuItem(i => "IDENT", x =>
            //{
            //    Logger.Trace("Manual: HeadlightVerticalAimControl.IDENT");
            //    HeadlightVerticalAimControl.IDENT();
            //}, MenuItemType.Button, MenuItemAction.None));
            //AddItem(new MenuItem(
            //    i => "Status: " + HeadlightVerticalAimControl.FrontSensorVoltage.ToString("F2") + "V " + HeadlightVerticalAimControl.RearSensorVoltage.ToString("F2") + "V", x =>
            //    {
            //        HeadlightVerticalAimControl.STATUS_LESSEN();
            //    }, MenuItemType.Button, MenuItemAction.None));
            //AddItem(new MenuItem(
            //    i => "Sensors: " + HeadlightVerticalAimControl.FrontSensorVoltage.ToString("F2") + "V " + HeadlightVerticalAimControl.RearSensorVoltage.ToString("F2") + "V", x =>
            //    {
            //        HeadlightVerticalAimControl.STATUS_SENSOR_LESSEN();
            //    }, MenuItemType.Button, MenuItemAction.None));
            //AddItem(new MenuItem(i => "LWR-STEUERN_ANTRIEBE", x =>
            //{
            //    HeadlightVerticalAimControl.STEUERN_ANTRIEBE();
            //}, MenuItemType.Button, MenuItemAction.None));
            //AddItem(new MenuItem(i => "LWR-DIAGNOSE_ENDE", x =>
            //{
            //    HeadlightVerticalAimControl.DIAGNOSE_ENDE();
            //}, MenuItemType.Button, MenuItemAction.None));

            this.AddBackButton();
        }

        public override bool OnNavigatedTo(MenuBase menu)
        {
            if (base.OnNavigatedTo(menu))
            {
                SetItems();

                NavigationModule.BatteryVoltageChanged += NavigationModule_BatteryVoltageChanged;
                IntegratedHeatingAndAirConditioning.AirConditioningCompressorStatusChanged += IntegratedHeatingAndAirConditioning_AirConditioningCompressorStatusChanged;

                //HeadlightVerticalAimControl.FrontSensorVoltageChanged += HeadlightVerticalAimControl_SensorsVoltageChanged;
                //HeadlightVerticalAimControl.RearSensorVoltageChanged += HeadlightVerticalAimControl_SensorsVoltageChanged;
                //DigitalDieselElectronics.MessageReceived += DigitalDieselElectronics_MessageReceived;
                return true;
            }
            return false;
        }

        public override bool OnNavigatedFrom(MenuBase menu)
        {
            if (base.OnNavigatedFrom(menu))
            {
                ClearItems();

                NavigationModule.BatteryVoltageChanged -= NavigationModule_BatteryVoltageChanged;
                IntegratedHeatingAndAirConditioning.AirConditioningCompressorStatusChanged -= IntegratedHeatingAndAirConditioning_AirConditioningCompressorStatusChanged;

                //HeadlightVerticalAimControl.FrontSensorVoltageChanged -= HeadlightVerticalAimControl_SensorsVoltageChanged;
                //HeadlightVerticalAimControl.RearSensorVoltageChanged -= HeadlightVerticalAimControl_SensorsVoltageChanged;
                //DigitalDieselElectronics.MessageReceived -= DigitalDieselElectronics_MessageReceived;
                return true;
            }
            return false;
        }

        private void NavigationModule_BatteryVoltageChanged(double voltage)
        {
            OnUpdateBody(MenuScreenUpdateReason.Refresh);
        }

        private void IntegratedHeatingAndAirConditioning_AirConditioningCompressorStatusChanged()
        {
            OnUpdateBody(MenuScreenUpdateReason.Refresh);
        }

        private void HeadlightVerticalAimControl_SensorsVoltageChanged(double voltage)
        {
            OnUpdateBody(MenuScreenUpdateReason.Refresh);
        }

        private void DigitalDieselElectronics_MessageReceived()
        {
            Refresh();
        }

        public static ActivateScreen Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new ActivateScreen();
                }
                return instance;
            }
        }

        //void OnDisableWatchdogCounterReset()
        //{
        //    var e = DisableWatchdogCounterReset;
        //    if (e != null)
        //    {
        //        Logger.Trace("OnDisableWatchdogCounterReset was called");
        //        e();
        //    }
        //}

        //public delegate void DisableWatchdogCounterResetHanlder();
        //public static event DisableWatchdogCounterResetHanlder DisableWatchdogCounterReset;
    }
}
