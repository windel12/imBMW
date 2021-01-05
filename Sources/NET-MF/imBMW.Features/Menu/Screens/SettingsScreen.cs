using System;
using Microsoft.SPOT;
using imBMW.Features.Localizations;
using imBMW.Tools;

namespace imBMW.Features.Menu.Screens
{
    public class SettingsScreen : MenuScreen
    {
        protected static SettingsScreen instance;

        private bool canChangeLanguage = true;

        protected SettingsScreen()
        {
            TitleCallback = s => Localization.Current.Settings;

            FastMenuDrawing = true;

            SetItems();

            Logger.Debug("protected SettingsScreen()");
        }

        protected virtual void SetItems()
        {
            ClearItems();
            if (CanChangeLanguage)
            {
                AddItem(new MenuItem(i => Localization.Current.Language + ": " + Localization.Current.LanguageName, i => SwitchLanguage(), MenuItemType.Button, MenuItemAction.Refresh));
            }
            else
            {
                this.AddDummyButton();
            }
            AddItem(new MenuItem(i => Localization.Current.ComfortWindows, i => Comfort.AutoCloseWindows = i.IsChecked, MenuItemType.Checkbox)
            {
                IsChecked = Comfort.AutoCloseWindows
            });
            AddItem(new MenuItem(i => Localization.Current.ComfortSunroof, i => Comfort.AutoCloseSunroof = i.IsChecked, MenuItemType.Checkbox)
            {
                IsChecked = Comfort.AutoCloseSunroof
            });
            AddItem(new MenuItem(i => Localization.Current.AutoLock, i => Comfort.AutoLockDoors = i.IsChecked, MenuItemType.Checkbox)
            {
                IsChecked = Comfort.AutoLockDoors
            });
            AddItem(new MenuItem(i => Localization.Current.AutoUnlock, i => Comfort.AutoUnlockDoors = i.IsChecked, MenuItemType.Checkbox)
            {
                IsChecked = Comfort.AutoUnlockDoors
            });
            AddItem(new MenuItem(i => "WatchdogIKE", i => Settings.Instance.WatchdogResetOnIKEResponse = i.IsChecked, MenuItemType.Checkbox)
            {
                IsChecked = Settings.Instance.WatchdogResetOnIKEResponse
            });
            AddItem(new MenuItem(i => nameof(Settings.Instance.LowBeamInWelcomeLight), i => Settings.Instance.LowBeamInWelcomeLight = i.IsChecked, MenuItemType.Checkbox)
            {
                IsChecked = Settings.Instance.LowBeamInWelcomeLight
            });
            AddItem(new MenuItem(i => "Suspend CDC", i => Settings.Instance.SuspendCDChangerResponseEmulation = i.IsChecked, MenuItemType.Checkbox)
            {
                IsChecked = Settings.Instance.SuspendCDChangerResponseEmulation
            });
            AddItem(new MenuItem(i => "Suspend ZUH", i => Settings.Instance.SuspendAuxilaryHeaterResponseEmulation = i.IsChecked, MenuItemType.Checkbox)
            {
                IsChecked = Settings.Instance.SuspendAuxilaryHeaterResponseEmulation
            });
            this.AddBackButton();
        }

        public bool CanChangeLanguage
        {
            get { return canChangeLanguage; }
            set
            {
                if (canChangeLanguage == value)
                {
                    return;
                }
                canChangeLanguage = value;
                SetItems();
            }
        }

        void SwitchLanguage()
        {
            if (Localization.Current is EnglishLocalization)
            {
                Localization.Current = new RussianLocalization();
            }
            else
            {
                Localization.Current = new EnglishLocalization();
            }
        }

        public static SettingsScreen Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new SettingsScreen();
                }
                return instance;
            }
        }
    }
}
