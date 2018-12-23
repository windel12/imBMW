using System;
using System.IO.Ports;
using imBMW.Diagnostics;
using imBMW.Features.Localizations;
using imBMW.iBus;
using imBMW.iBus.Devices.Real;
using Microsoft.SPOT.Hardware;

namespace imBMW.Features.Menu.Screens
{
    public class ActivateScreen : MenuScreen
    {
        protected static ActivateScreen instance;

        private string MotorTemperatur = "MotorTemperatur";

        protected ActivateScreen()
        {
            TitleCallback = s => Localization.Current.Activate;
            //StatusCallback = s => AuxilaryHeater.Status.ToString();

            DBusManager.Instance.AddMessageReceiverForSourceAndDestinationDevice(DeviceAddress.DDE, DeviceAddress.OBD, ProcessFromDDEMessage);

            AuxilaryHeater.Init();

            DBusMessage motor_temperatur = new DBusMessage(DeviceAddress.OBD, DeviceAddress.DDE, 0x2C, 0x10, 0x0F, 0x00);

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

                DBusManager.Instance.EnqueueMessage(motor_temperatur);
            }, MenuItemType.Button, MenuItemAction.Refresh));

            AddItem(new MenuItem(i => Localization.Current.Voltage + ": " + (NavigationModule.BatteryVoltage > 0 ? NavigationModule.BatteryVoltage.ToString("F2") : "-") + " " + Localization.Current.VoltageShort, x =>
            {
                DBusManager.Port.WriteBufferSize = 0;
                DS2Message status_lesen = new DS2Message(DeviceAddress.NavigationEurope, 0x0B);
                DBusManager.Instance.EnqueueMessage(status_lesen);
            }, MenuItemType.Button, MenuItemAction.None));

            this.AddBackButton();

            NavigationModule.BatteryVoltageChanged += (voltage) => OnUpdateBody(MenuScreenUpdateReason.Refresh);
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

        void ProcessFromDDEMessage(Message m)
        {
            if (m.Data[0] == 0x6C && m.Data[1] == 0x10)
            {
                MotorTemperatur = BitConverter.ToInt16(new byte[2] { m.Data[2], m.Data[3] }, 0).ToString();
                this.Refresh();
            }
        }
    }
}
