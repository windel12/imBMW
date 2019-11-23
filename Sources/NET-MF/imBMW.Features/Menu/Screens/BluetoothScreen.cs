using System;
using GHI.Pins;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using imBMW.Tools;
using imBMW.Enums;

namespace imBMW.Features.Menu.Screens
{
    public class BluetoothScreen : MenuScreen
    {
        protected static BluetoothScreen instance;

        static OutputPort xiaomiBluetoothTransceiverPowerOutput = new OutputPort(FEZPandaIII.Gpio.D8, true); // K1 relay
        static OutputPort leftAudioChannelRelay = new OutputPort(FEZPandaIII.Gpio.D9, true); // K2 relay
        static OutputPort groundAudioRelay = new OutputPort(FEZPandaIII.Gpio.D10, true); // K3 relay
        static OutputPort rightAudioChannelRelay = new OutputPort(FEZPandaIII.Gpio.D11, true); // K4 relay

        public BluetoothScreen()
        {
            ClearItems();

            AddItem(new MenuItem(i => "Source: " + AudioSource.ToStringValue(), i =>
            {
                AudioSource = AudioSource == AudioSource.SDCard ? AudioSource.Bluetooth : AudioSource.SDCard;
            }, MenuItemType.Button, MenuItemAction.Refresh));

            AddItem(new MenuItem(i => "Power", i => BluetoothChargingState = i.IsChecked, MenuItemType.Checkbox)
            {
                IsChecked = BluetoothChargingState
            });

            this.AddBackButton();
        }

        public static AudioSource AudioSource
        {
            get { return leftAudioChannelRelay.Read() ? AudioSource.SDCard : AudioSource.Bluetooth; }
            set
            {
                if (value == AudioSource.SDCard)
                {
                    leftAudioChannelRelay.Write(true);
                    groundAudioRelay.Write(true);
                    rightAudioChannelRelay.Write(true);
                }
                else
                {
                    leftAudioChannelRelay.Write(false);
                    groundAudioRelay.Write(false);
                    rightAudioChannelRelay.Write(false);
                }
            }
        }

        public static bool BluetoothChargingState
        {
            get { return !xiaomiBluetoothTransceiverPowerOutput.Read(); }
            set { xiaomiBluetoothTransceiverPowerOutput.Write(!value); }
        }

        public static BluetoothScreen Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new BluetoothScreen();
                }
                return instance;
            }
        }

        public static void Init()
        {
            // do nothing, just call static constructor
        }
    }
}
