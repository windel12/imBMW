using System;
using Microsoft.SPOT;
using imBMW.Features.Localizations;
using imBMW.Tools;

namespace imBMW.Features.Menu.Screens
{
    public class HomeScreen : MenuScreen
    {
        protected static HomeScreen instance;

        protected MenuItem itemBC;
        protected MenuItem itemSettings;
        protected MenuItem auxilaryHeaterItem;
        protected MenuItem ddeItem;
        //protected MenuItem bluetoothItem;
        protected MenuItem activateItem;
        //protected MenuItem musicListItem;
        protected MenuItem integratedHeatingAndAirConditioningItem;
        protected MenuItem delayItem;

        protected HomeScreen()
        {
            Title = "imBMW";

            FastMenuDrawing = true;

            itemBC = new MenuItem(i => Localization.Current.Bordcomputer, MenuItemType.Button, MenuItemAction.GoToScreen)
            {
                GoToScreenCallback = () => BordcomputerScreen.Instance
            };
            itemSettings = new MenuItem(i => Localization.Current.Settings, MenuItemType.Button, MenuItemAction.GoToScreen)
            {
                GoToScreenCallback = () => SettingsScreen.Instance
            };
            auxilaryHeaterItem = new MenuItem(i => Localization.Current.AuxilaryHeater, MenuItemType.Button, MenuItemAction.GoToScreen)
            {
                GoToScreenCallback = () => AuxilaryHeaterScreen.Instance
            };
            ddeItem = new MenuItem(i => "DDE", MenuItemType.Button, MenuItemAction.GoToScreen)
            {
                GoToScreenCallback = () => DDEScreen.Instance
            };
            //bluetoothItem = new MenuItem(i => "Bluetooth", MenuItemType.Button, MenuItemAction.GoToScreen)
            //{
            //    GoToScreenCallback = () => BluetoothScreen.Instance
            //};
            activateItem = new MenuItem(i => Localization.Current.Activate, MenuItemType.Button, MenuItemAction.GoToScreen)
            {
                GoToScreenCallback = () => ActivateScreen.Instance
            };
            //musicListItem = new MenuItem(i => Localization.Current.MusicList, MenuItemType.Button, MenuItemAction.GoToScreen)
            //{
            //    GoToScreenCallback = () => MusicListScreen.Instance
            //};
            integratedHeatingAndAirConditioningItem = new MenuItem(i => Localization.Current.AirConditioning, MenuItemType.Button, MenuItemAction.GoToScreen)
            {
                GoToScreenCallback = () => IntegratedHeatingAndAirConditioningScreen.Instance
            };
            delayItem = new MenuItem(i => "Delay", MenuItemType.Button, MenuItemAction.GoToScreen)
            {
                GoToScreenCallback = () => DelayScreen.Instance
            };

            SetItems();

            Logger.Debug("protected HomeScreen()");
        }

        protected virtual void SetItems()
        {
            ClearItems();

            this.AddItem(itemBC);
            this.AddItem(itemSettings);
            this.AddItem(auxilaryHeaterItem);
            this.AddItem(activateItem);
            this.AddItem(integratedHeatingAndAirConditioningItem);
            this.AddItem(delayItem);
            AddItem(ddeItem);
            this.AddDummyButton();//AddItem(bluetoothItem);
            this.AddDummyButton();//AddItem(musicListItem);
            this.AddDummyButton();
        }

        public static HomeScreen Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new HomeScreen();
                }
                return instance;
            }
        }
    }
}
