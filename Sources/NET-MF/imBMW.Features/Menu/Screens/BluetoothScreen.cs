using System;
using GHI.Pins;
using imBMW.Features.Localizations;
using imBMW.iBus;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;

namespace imBMW.Features.Menu.Screens
{
    public enum AudioSource
    {
        SDCard,
        Bluetooth
    }

    public class BluetoothScreen : MenuScreen
    {
        protected static BluetoothScreen instance;

        static OutputPort xiaomiBluetoothTransceiverPowerOutput;
        static OutputPort leftAudioChannelRelay;
        static OutputPort groundAudioRelay;
        static OutputPort rightAudioChannelRelay;

        public BluetoothScreen()
        {
            xiaomiBluetoothTransceiverPowerOutput = new OutputPort(FEZPandaIII.Gpio.D8, true); // K1 relay
            leftAudioChannelRelay = new OutputPort(FEZPandaIII.Gpio.D9, true); // K2 relay
            groundAudioRelay = new OutputPort(FEZPandaIII.Gpio.D10, true); // K3 relay
            rightAudioChannelRelay = new OutputPort(FEZPandaIII.Gpio.D11, true); // K4 relay

            FastMenuDrawing = false;

            ClearItems();

            AddItem(new MenuItem(i => "Источник: " + AudioSource.ToString(), i =>
            {
                AudioSource = AudioSource == AudioSource.SDCard ? AudioSource.Bluetooth : AudioSource.SDCard;
            }, MenuItemType.Button, MenuItemAction.Refresh));

            AddItem(new MenuItem(i => "Зарядка", i => BluetoothChargingState = i.IsChecked, MenuItemType.Checkbox)
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
    }
}
