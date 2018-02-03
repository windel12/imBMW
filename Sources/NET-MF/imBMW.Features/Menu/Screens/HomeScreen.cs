using System;
using Microsoft.SPOT;
using imBMW.Features.Localizations;

namespace imBMW.Features.Menu.Screens
{
    public class HomeScreen : MenuScreen
    {
        protected static HomeScreen instance;

        protected MenuItem itemPlayer;
        protected MenuItem itemPhone;
        protected MenuItem itemFav;
        protected MenuItem itemBC;
        protected MenuItem itemSettings;
        protected MenuItem auxilaryHeaterItem;

        protected HomeScreen()
        {
            Title = "imBMW";

            itemPlayer = new MenuItem(i => Localization.Current.Player, MenuItemType.Button, MenuItemAction.GoToScreen);
            itemPhone = new MenuItem(i => Localization.Current.Phone, MenuItemType.Button, MenuItemAction.GoToScreen);
            //itemFav = new MenuItem(i => Localization.Current.QuickAccess, MenuItemType.Button, MenuItemAction.GoToScreen)
            //{
            //    GoToScreen = null // TODO fav screen
            //};

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
            SetItems();
        }

        protected virtual void SetItems()
        {
            ClearItems();
            if (itemPlayer.GoToScreen != null)
            {
                AddItem(itemPlayer);
            }
            if (itemPhone.GoToScreen != null)
            {
                AddItem(itemPhone);
            }
            //AddItem(itemFav);
            AddItem(itemBC);
            AddItem(itemSettings);
            AddItem(auxilaryHeaterItem);
        }

        public MenuScreen PlayerScreen
        {
            get
            {
                return itemPlayer.GoToScreen;
            }
            set
            {
                // TODO check it is shown now and renavigate
                itemPlayer.GoToScreen = value;
                SetItems();
            }
        }

        public MenuScreen PhoneScreen
        {
            get
            {
                return itemPhone.GoToScreen;
            }
            set
            {
                itemPhone.GoToScreen = value;
                SetItems();
            }
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
