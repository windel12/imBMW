using System;
using Microsoft.SPOT;
using imBMW.Features.Localizations;
using imBMW.iBus;

namespace imBMW.Features.Menu.Screens
{
    public class AuxilaryHeaterScreen : MenuScreen
    {
        public static Message MessageStartAuxHeater = new Message(DeviceAddress.GraphicsNavigationDriver, DeviceAddress.InstrumentClusterElectronics, 0x41, 0x12);
        public static Message MessageStopAuxHeater = new Message(DeviceAddress.GraphicsNavigationDriver, DeviceAddress.InstrumentClusterElectronics, 0x41, 0x11);

        protected static AuxilaryHeaterScreen instance;

        protected AuxilaryHeaterScreen()
        {
            TitleCallback = s => Localization.Current.AuxilaryHeater;

            SetItems();
        }

        protected virtual void SetItems()
        {
            ClearItems();
            AddItem(new MenuItem(i => i.IsChecked ? Localization.Current.TurnOff : Localization.Current.TurnOn,
                i =>
                {
                    if (i.IsChecked)
                    {
                        Manager.EnqueueMessage(MessageStopAuxHeater);
                    }
                    else
                    {
                        Manager.EnqueueMessage(MessageStartAuxHeater);
                    }
                }, MenuItemType.Checkbox));

            this.AddBackButton();
        }

        public static AuxilaryHeaterScreen Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new AuxilaryHeaterScreen();
                }
                return instance;
            }
        }
    }
}
