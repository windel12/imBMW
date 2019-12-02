using System;
using imBMW.Features.Localizations;
using imBMW.iBus.Devices.Real;
using imBMW.Tools;
using imBMW.Enums;

namespace imBMW.Features.Menu.Screens
{
    public class IntegratedHeatingAndAirConditioningScreen : MenuScreen
    {
        protected static IntegratedHeatingAndAirConditioningScreen instance;

        private static bool IsCodingDataReaded { get; set; }

        private MenuItem item1;
        private MenuItem item2;
        private MenuItem item3;
        private MenuItem item4;

        private byte[] readedCodingData = new byte[4];

        protected IntegratedHeatingAndAirConditioningScreen()
        {
            TitleCallback = s => Localization.Current.AirConditioning;
        }

        protected virtual void SetItems()
        {
            item1 = new MenuItem(i => "Read: " + readedCodingData.ToHex(' '), item =>
            {
                IntegratedHeatingAndAirConditioning.ReadCodingData();
                IsCodingDataReaded = true;
            }, MenuItemType.Button, MenuItemAction.None);

            item2 = new MenuItem(i => "Write: " + IntegratedHeatingAndAirConditioning.CodingData.ToHex(' '), item =>
            {
                IntegratedHeatingAndAirConditioning.WriteCodingData();
                IsCodingDataReaded = false;
            }, MenuItemType.Text, MenuItemAction.None);

            item3 = new MenuItem(i => "Aux Heater: " + IntegratedHeatingAndAirConditioning.AuxilaryHeaterActivationMode.ToStringValue(), item =>
            {
                IntegratedHeatingAndAirConditioning.AuxilaryHeaterActivationMode = 
                    IntegratedHeatingAndAirConditioning.AuxilaryHeaterActivationMode == AuxilaryHeaterActivationMode.Normal 
                    ? AuxilaryHeaterActivationMode.Kbus: AuxilaryHeaterActivationMode.Normal;
            }, MenuItemType.Button, MenuItemAction.Refresh);

            item4 = new MenuItem(i => "Aux Heating ", item =>
            {
                IntegratedHeatingAndAirConditioning.AuxilaryHeating = item.IsChecked;
            }, MenuItemType.Checkbox, MenuItemAction.Refresh);

            AddItem(item1);
            AddItem(item2);
            AddItem(item3);
            AddItem(item4);

            this.AddBackButton();
        }

        public override bool OnNavigatedTo(MenuBase menu)
        {
            if (base.OnNavigatedTo(menu))
            {
                SetItems();

                IntegratedHeatingAndAirConditioning.CodingDataAcquired += IntegratedHeatingAndAirConditioning_CodingDataAcquired;

                return true;
            }
            return false;
        }

        public override bool OnNavigatedFrom(MenuBase menu)
        {
            if (base.OnNavigatedFrom(menu))
            {
                ClearItems();

                IntegratedHeatingAndAirConditioning.CodingDataAcquired -= IntegratedHeatingAndAirConditioning_CodingDataAcquired;

                return true;
            }
            return false;
        }

        private void IntegratedHeatingAndAirConditioning_CodingDataAcquired()
        {
            readedCodingData = IntegratedHeatingAndAirConditioning.CodingData;

            item4.IsChecked = IntegratedHeatingAndAirConditioning.AuxilaryHeating;
            this.Refresh();
        }

        public static IntegratedHeatingAndAirConditioningScreen Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new IntegratedHeatingAndAirConditioningScreen();
                }
                return instance;
            }
        }
    }
}
