using System;
using Microsoft.SPOT;
using imBMW.Features.Localizations;

namespace imBMW.Features.Menu
{
    public static class MenuHelpers
    {
        public static void AddBackButton(this MenuScreen screen, SByte index = -1)
        {
            screen.AddItem(new MenuItem(i => "« " + Localization.Current.Back, MenuItemType.Button, MenuItemAction.GoBackScreen), index);
        }

        public static void AddDummyButton(this MenuScreen screen)
        {
            screen.AddItem(new MenuItem(i => "-", MenuItemType.Button, MenuItemAction.Refresh));
        }
    }
}
