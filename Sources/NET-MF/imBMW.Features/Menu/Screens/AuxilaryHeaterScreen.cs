using System;
using imBMW.Features.Localizations;
using imBMW.iBus;
using imBMW.iBus.Devices.Real;

namespace imBMW.Features.Menu.Screens
{
    public class AuxilaryHeaterScreen : MenuScreen
    {
        private string SteuernZuheizerOn = "SteuernZuheizerOn";
        private string SteuernZuheizerOff = "SteuernZuheizerOff";

        public static Message MessageStartAuxilaryHeater = new Message(DeviceAddress.GraphicsNavigationDriver, DeviceAddress.InstrumentClusterElectronics, 0x41, 0x12);
        public static Message MessageStopAuxilaryHeater = new Message(DeviceAddress.GraphicsNavigationDriver, DeviceAddress.InstrumentClusterElectronics, 0x41, 0x11);

        protected AuxilaryHeaterScreen()
        {
            TitleCallback = s => Localization.Current.AuxilaryHeater;
            StatusCallback = s => IntegratedHeatingAndAirConditioning.AuxilaryHeaterStatus.ToStringValue();

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
                        Manager.EnqueueMessage(MessageStopAuxilaryHeater);
                    }
                    else
                    {
                        Manager.EnqueueMessage(MessageStartAuxilaryHeater);
                    }
                }, MenuItemType.Checkbox));

            AddItem(new MenuItem(i => SteuernZuheizerOn, i =>
            {
                DBusManager.Port.WriteBufferSize = 0;

                AuxilaryHeater.StartAuxilaryHeaterOverDBus();
            }, MenuItemType.Button, MenuItemAction.None));

            AddItem(new MenuItem(i => SteuernZuheizerOff, i =>
            {
                DBusManager.Port.WriteBufferSize = 0;

                AuxilaryHeater.StopAuxilaryHeaterOverDBus();
            }, MenuItemType.Button, MenuItemAction.None));

            AddItem(new MenuItem(i => "StartAdditionalHeater", i =>
            {
                IntegratedHeatingAndAirConditioning.StartAuxilaryHeater();
            }, MenuItemType.Button, MenuItemAction.None));

            AddItem(new MenuItem(i => "StopAuxilaryHeater", i =>
            {
                IntegratedHeatingAndAirConditioning.StopAuxilaryHeater();
            }, MenuItemType.Button, MenuItemAction.None));

            this.AddBackButton();
        }

        public override bool OnNavigatedTo(MenuBase menu)
        {
            if (base.OnNavigatedTo(menu))
            {
                IntegratedHeatingAndAirConditioning.AuxilaryHeaterStatusChanged += IntegratedHeatingAndAirConditioning_AuxilaryHeaterStatusChanged;
                IntegratedHeatingAndAirConditioning.AuxilaryHeaterWorkingRequestsCounterChanged += IntegratedHeatingAndAirConditioning_AuxilaryHeaterWorkingRequestsCounterChanged;
                return true;
            }
            return false;
        }

        public override bool OnNavigatedFrom(MenuBase menu)
        {
            if (base.OnNavigatedFrom(menu))
            {
                IntegratedHeatingAndAirConditioning.AuxilaryHeaterStatusChanged -= IntegratedHeatingAndAirConditioning_AuxilaryHeaterStatusChanged;
                IntegratedHeatingAndAirConditioning.AuxilaryHeaterWorkingRequestsCounterChanged -= IntegratedHeatingAndAirConditioning_AuxilaryHeaterWorkingRequestsCounterChanged;
                return true;
            }
            return false;
        }

        private void IntegratedHeatingAndAirConditioning_AuxilaryHeaterStatusChanged(AuxilaryHeaterStatus status)
        {
            OnUpdateHeader(MenuScreenUpdateReason.Refresh);
        }

        private void IntegratedHeatingAndAirConditioning_AuxilaryHeaterWorkingRequestsCounterChanged(byte counter)
        {
            OnUpdateHeader(MenuScreenUpdateReason.Refresh);
        }

        protected static AuxilaryHeaterScreen instance;
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
