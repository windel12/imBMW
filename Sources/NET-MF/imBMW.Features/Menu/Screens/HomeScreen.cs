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
        protected MenuItem bluetoothItem;
        protected MenuItem activateItem;
        protected MenuItem musicListItem;
        protected MenuItem test1Item;
        protected MenuItem test2Item;
        protected MenuItem test3Item;

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
            bluetoothItem = new MenuItem(i => "Bluetooth", MenuItemType.Button, MenuItemAction.GoToScreen)
            {
                GoToScreenCallback = () => BluetoothScreen.Instance
            };
            activateItem = new MenuItem(i => Localization.Current.Activate, MenuItemType.Button, MenuItemAction.GoToScreen)
            {
                GoToScreenCallback = () => ActivateScreen.Instance
            };
            musicListItem = new MenuItem(i => "Music list", MenuItemType.Button, MenuItemAction.GoToScreen)
            {
                GoToScreenCallback = () => MusicListScreen.Instance
            };
            test1Item = new MenuItem(i => "-", MenuItemType.Button, MenuItemAction.Refresh)
            {
            };
            test2Item = new MenuItem(i => "-", MenuItemType.Button, MenuItemAction.Refresh)
            {
            };
            test3Item = new MenuItem(i => "-", MenuItemType.Button, MenuItemAction.Refresh)
            {
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
            AddItem(musicListItem);
            AddItem(test1Item);
            AddItem(test2Item);
            AddItem(test3Item);
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
