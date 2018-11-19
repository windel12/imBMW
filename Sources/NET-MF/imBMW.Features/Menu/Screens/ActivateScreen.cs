using System;
using System.IO.Ports;
using System.Text;
using imBMW.Diagnostics;
using Microsoft.SPOT;
using imBMW.Features.Localizations;
using imBMW.iBus;
using imBMW.iBus.Devices;
using Microsoft.SPOT.Hardware;

namespace imBMW.Features.Menu.Screens
{
    public class ActivateScreen : MenuScreen
    {
        protected static ActivateScreen instance;

        private string MotorTemperatur = "MotorTemperatur";
        private string SteuernZuheizer = "SteuernZuheizer";
        private string NaviStatusLesen = "NaviStausLessen";

        protected ActivateScreen()
        {
            TitleCallback = s => Localization.Current.Activate;

            DbusManager.AddMessageReceiverForSourceAndDestinationDevice(DeviceAddress.DDE, DeviceAddress.OBD, ProcessFromDDEMessage);

            ISerialPort dBusPort = new SerialPortTH3122("COM4", Cpu.Pin.GPIO_NONE, writeBufferSize: 1); // d31, d33
            dBusPort.AfterWriteDelay = 3;
            DbusManager.Init(dBusPort);
#if !NETMF
            if (!dBusPort.IsOpen)
            {
                //dBusPort.Open();
            }
#endif

            bool firstHalf = true;
            DBusMessage motor_temperatur = new DBusMessage(DeviceAddress.OBD, DeviceAddress.DDE, 0x2C, 0x10, 0x0F, 0x00);

            AddItem(new MenuItem(i => "Increase delay: " + dBusPort.AfterWriteDelay, x =>
            {
                dBusPort.AfterWriteDelay += 1;
            }, MenuItemType.Button, MenuItemAction.Refresh));
            AddItem(new MenuItem(i => "Decrease delay: " + dBusPort.AfterWriteDelay, x =>
            {
                if (dBusPort.AfterWriteDelay > 0)
                    dBusPort.AfterWriteDelay -= 1;
            }, MenuItemType.Button, MenuItemAction.Refresh));

            AddItem(new MenuItem(i => MotorTemperatur, i =>
            {
                dBusPort.WriteBufferSize = 1;

                DbusManager.EnqueueMessage(motor_temperatur);
            }, MenuItemType.Button, MenuItemAction.Refresh));

            AddItem(new MenuItem(i => SteuernZuheizer, x =>
            {
                dBusPort.WriteBufferSize = 0;

                DS2Message steuern_suheizer = null;
                if (x.IsChecked)
                {
                    var steuern_suheizer_on = new DS2Message(DeviceAddress.AuxilaryHeater, 0x9E);
                    steuern_suheizer = steuern_suheizer_on;
                }
                else
                {
                    var steuern_suheizer_off = new DS2Message(DeviceAddress.AuxilaryHeater, 0x0C, 0x00);
                    steuern_suheizer = steuern_suheizer_off;
                }
                DbusManager.EnqueueMessage(steuern_suheizer);
            }, MenuItemType.Checkbox, MenuItemAction.Refresh));

            AddItem(new MenuItem(i => Localization.Current.Voltage + ": " + (NavigationModule.BatteryVoltage > 0 ? NavigationModule.BatteryVoltage.ToString("F2") : "-") + " " + Localization.Current.VoltageShort, x =>
            {
                dBusPort.WriteBufferSize = 0;
                DS2Message status_lesen = new DS2Message(DeviceAddress.NavigationEurope, 0x0B);
                DbusManager.EnqueueMessage(status_lesen);
            }, MenuItemType.Button, MenuItemAction.None));

            this.AddBackButton();

            NavigationModule.BatteryVoltageChanged += (voltage) => Refresh();
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
