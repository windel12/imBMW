using System;
using imBMW.Diagnostics;
using imBMW.Features.Localizations;
using imBMW.iBus;
using imBMW.iBus.Devices.Real;
using imBMW.Tools;

namespace imBMW.Features.Menu.Screens
{
    public class ActivateScreen : MenuScreen
    {
        protected static ActivateScreen instance;

        private string MotorTemperatur = "MotorTemperatur";

        protected ActivateScreen()
        {
            FastMenuDrawing = true;

            TitleCallback = s => Localization.Current.Activate;
            //StatusCallback = s => AuxilaryHeater.Status.ToString();

            AuxilaryHeater.Init();

            DBusMessage motor_temperatur = new DBusMessage(DeviceAddress.OBD, DeviceAddress.DDE, 0x2C, 0x10, 0x0F, 0x00);
            DS2Message navigation_module_status_lesen = new DS2Message(DeviceAddress.NavigationEurope, 0x0B);

            AddItem(new MenuItem(i => "Increase delay: " + DBusManager.Port.AfterWriteDelay, x =>
            {
                DBusManager.Port.AfterWriteDelay += 1;
            }, MenuItemType.Button, MenuItemAction.Refresh));
            AddItem(new MenuItem(i => "Decrease delay: " + DBusManager.Port.AfterWriteDelay, x =>
            {
                if (DBusManager.Port.AfterWriteDelay > 0)
                    DBusManager.Port.AfterWriteDelay -= 1;
            }, MenuItemType.Button, MenuItemAction.Refresh));

            AddItem(new MenuItem(i => MotorTemperatur, i =>
            {
                DBusManager.Port.WriteBufferSize = 1;
                Logger.Trace("Sending diagnostic message to DDE, for acquiring motor temperature.");
                DBusManager.Instance.EnqueueMessage(motor_temperatur);
            }, MenuItemType.Button, MenuItemAction.Refresh));

            AddItem(new MenuItem(i => Localization.Current.Voltage + ": " + (NavigationModule.BatteryVoltage > 0 ? NavigationModule.BatteryVoltage.ToString("F2") : "-") + " " + Localization.Current.VoltageShort, x =>
            {
                DBusManager.Port.WriteBufferSize = 0;
                Logger.Trace("Sending diagnostic message to navigation module, for acquiring voltage.");
                DBusManager.Instance.EnqueueMessage(navigation_module_status_lesen);
            }, MenuItemType.Button, MenuItemAction.None));

            AddItem(new MenuItem(i => "DisableWatchdog", x =>
            {
                OnDisableWatchdogCounterReset();
            }, MenuItemType.Button, MenuItemAction.None));

            AddItem(new MenuItem(i => "IDENT", x =>
            {
                Logger.Trace("Manual: HeadlightVerticalAimControl.IDENT");
                HeadlightVerticalAimControl.IDENT();
            }, MenuItemType.Button, MenuItemAction.None));
            AddItem(new MenuItem(
                i => "Status: " + HeadlightVerticalAimControl.FrontSensorVoltage.ToString("F2") + "V " + HeadlightVerticalAimControl.RearSensorVoltage.ToString("F2") + "V", x =>
                {
                    HeadlightVerticalAimControl.STATUS_LESSEN();
                }, MenuItemType.Button, MenuItemAction.None));
            AddItem(new MenuItem(
                i => "Sensors: " + HeadlightVerticalAimControl.FrontSensorVoltage.ToString("F2") + "V " + HeadlightVerticalAimControl.RearSensorVoltage.ToString("F2") + "V", x =>
                {
                    HeadlightVerticalAimControl.STATUS_SENSOR_LESSEN();
                }, MenuItemType.Button, MenuItemAction.None));
            //AddItem(new MenuItem(i => "LWR-STEUERN_ANTRIEBE", x =>
            //{
            //    HeadlightVerticalAimControl.STEUERN_ANTRIEBE();
            //}, MenuItemType.Button, MenuItemAction.None));
            AddItem(new MenuItem(i => "LWR-DIAGNOSE_ENDE", x =>
            {
                HeadlightVerticalAimControl.DIAGNOSE_ENDE();
            }, MenuItemType.Button, MenuItemAction.None));

            this.AddBackButton();

            NavigationModule.BatteryVoltageChanged += (voltage) => OnUpdateBody(MenuScreenUpdateReason.Refresh);
        }

        public override bool OnNavigatedTo(MenuBase menu)
        {
            if (base.OnNavigatedTo(menu))
            {
                HeadlightVerticalAimControl.FrontSensorVoltageChanged += HeadlightVerticalAimControl_SensorsVoltageChanged;
                HeadlightVerticalAimControl.RearSensorVoltageChanged += HeadlightVerticalAimControl_SensorsVoltageChanged;
                return true;
            }
            return false;
        }

        public override bool OnNavigatedFrom(MenuBase menu)
        {
            if (base.OnNavigatedFrom(menu))
            {
                HeadlightVerticalAimControl.FrontSensorVoltageChanged -= HeadlightVerticalAimControl_SensorsVoltageChanged;
                HeadlightVerticalAimControl.RearSensorVoltageChanged -= HeadlightVerticalAimControl_SensorsVoltageChanged;
                return true;
            }
            return false;
        }

        private void HeadlightVerticalAimControl_SensorsVoltageChanged(double voltage)
        {
            OnUpdateBody(MenuScreenUpdateReason.Refresh);
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

        void OnDisableWatchdogCounterReset()
        {
            var e = DisableWatchdogCounterReset;
            if (e != null)
            {
                Logger.Trace("OnDisableWatchdogCounterReset was called");
                e();
            }
        }

        public delegate void DisableWatchdogCounterResetHanlder();
        public static event DisableWatchdogCounterResetHanlder DisableWatchdogCounterReset;
    }
}
