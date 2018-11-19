using System;
using Microsoft.SPOT;
using imBMW.Features.Localizations;

namespace imBMW.Features.Menu.Screens
{
    public class HomeScreen : MenuScreen
    {
        protected static HomeScreen instance;

        protected MenuItem itemBC;
        protected MenuItem itemSettings;
        protected MenuItem auxilaryHeaterItem;
        protected MenuItem ddeItem;
        protected MenuItem bluetoothItem;
        protected MenuItem activateItem;

        protected HomeScreen()
        {
            Title = "imBMW";

            itemBC = new MenuItem(i => Localization.Current.Bordcomputer, MenuItemType.Button, MenuItemAction.GoToScreen)
            {
                GoToScreen = BordcomputerScreen.Instance
            };
            itemSettings = new MenuItem(i => Localization.Current.Settings, MenuItemType.Button, MenuItemAction.GoToScreen)
            {
                GoToScreen = SettingsScreen.Instance
            };
            auxilaryHeaterItem = new MenuItem(i => Localization.Current.AuxilaryHeater, MenuItemType.Button, MenuItemAction.GoToScreen)
            {
                GoToScreen = AuxilaryHeaterScreen.Instance
            };
            ddeItem = new MenuItem(i => "DDE", MenuItemType.Button, MenuItemAction.GoToScreen)
            {
                GoToScreen = DDEScreen.Instance
            };
            bluetoothItem = new MenuItem(i => "Bluetooth", MenuItemType.Button, MenuItemAction.GoToScreen)
            {
                GoToScreen = BluetoothScreen.Instance
            };
            activateItem = new MenuItem(i => Localization.Current.Activate, MenuItemType.Button, MenuItemAction.GoToScreen)
            {
                GoToScreen = ActivateScreen.Instance
            };

            SetItems();
        }

        protected virtual void SetItems()
        {
            ClearItems();

            AddItem(itemBC);
            AddItem(itemSettings);
            AddItem(auxilaryHeaterItem);
            AddItem(ddeItem);
            AddItem(bluetoothItem);
            AddItem(activateItem);
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
