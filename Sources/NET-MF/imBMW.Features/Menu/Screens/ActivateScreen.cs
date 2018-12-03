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
        private string SteuernZuheizerOn = "SteuernZuheizerOn";
        private string SteuernZuheizerOff = "SteuernZuheizerOff";

        protected ActivateScreen()
        {
            TitleCallback = s => Localization.Current.Activate;
            StatusCallback = s => AuxilaryHeater.Status.ToString();

            DbusManager.AddMessageReceiverForSourceAndDestinationDevice(DeviceAddress.DDE, DeviceAddress.OBD, ProcessFromDDEMessage);

            ISerialPort dBusPort = new SerialPortTH3122("COM4", Cpu.Pin.GPIO_NONE, writeBufferSize: 0); // d31, d33
            dBusPort.AfterWriteDelay = 4;
            DbusManager.Init(dBusPort);

            AuxilaryHeater.Init();
#if !NETMF
            if (!dBusPort.IsOpen)
            {
                //dBusPort.Open();
            }
#endif

            DBusMessage motor_temperatur = new DBusMessage(DeviceAddress.OBD, DeviceAddress.DDE, 0x2C, 0x10, 0x0F, 0x00);

            //AddItem(new MenuItem(i => "Increase delay: " + dBusPort.AfterWriteDelay, x =>
            //{
            //    dBusPort.AfterWriteDelay += 1;
            //}, MenuItemType.Button, MenuItemAction.Refresh));
            //AddItem(new MenuItem(i => "Decrease delay: " + dBusPort.AfterWriteDelay, x =>
            //{
            //    if (dBusPort.AfterWriteDelay > 0)
            //        dBusPort.AfterWriteDelay -= 1;
            //}, MenuItemType.Button, MenuItemAction.Refresh));

            AddItem(new MenuItem(i => MotorTemperatur, i =>
            {
                dBusPort.WriteBufferSize = 1;

                DbusManager.EnqueueMessage(motor_temperatur);
            }, MenuItemType.Button, MenuItemAction.Refresh));

            AddItem(new MenuItem(i => SteuernZuheizerOn, i =>
            {
                dBusPort.WriteBufferSize = 0;

                AuxilaryHeater.StartAuxilaryHeater();
            }, MenuItemType.Button, MenuItemAction.None));

            AddItem(new MenuItem(i => SteuernZuheizerOff, i =>
            {
                dBusPort.WriteBufferSize = 0;

                AuxilaryHeater.StopAuxilaryHeater();
            }, MenuItemType.Button, MenuItemAction.None));

            AddItem(new MenuItem(i => Localization.Current.Voltage + ": " + (NavigationModule.BatteryVoltage > 0 ? NavigationModule.BatteryVoltage.ToString("F2") : "-") + " " + Localization.Current.VoltageShort, x =>
            {
                dBusPort.WriteBufferSize = 0;
                DS2Message status_lesen = new DS2Message(DeviceAddress.NavigationEurope, 0x0B);
                DbusManager.EnqueueMessage(status_lesen);
            }, MenuItemType.Button, MenuItemAction.None));

            this.AddBackButton();

            NavigationModule.BatteryVoltageChanged += (voltage) => OnUpdateBody(MenuScreenUpdateReason.Refresh);
            AuxilaryHeater.AuxilaryHeaterStatusChanged += (status) => OnUpdateHeader(MenuScreenUpdateReason.Refresh);
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
