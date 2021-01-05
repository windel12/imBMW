using System;
using System.Threading;
using System.Collections;
using imBMW.Tools;
using imBMW.Features.Multimedia;
using imBMW.Features.Menu.Screens;
using imBMW.iBus.Devices.Real;

namespace imBMW.Features.Menu
{
    public abstract class MenuBase
    {
        bool isEnabled;
        MenuScreen homeScreen;
        MenuScreen currentScreen;
        Stack navigationStack = new Stack();

        protected MediaEmulator mediaEmulator;

        public MenuBase(MediaEmulator mediaEmulator)
        {
            homeScreen = HomeScreen.Instance;

            this.mediaEmulator = mediaEmulator;
            mediaEmulator.IsEnabledChanged += mediaEmulator_IsEnabledChanged;
            InstrumentClusterElectronics.IgnitionStateChanged += InstrumentClusterElectronics_IgnitionStateChanged;
        }

        #region MediaEmulator members

        //protected Timer displayStatusDelayTimer;
        //protected const ushort displayStatusDelay = 900; // TODO make abstract
        protected Timer delayTimeout;

        void mediaEmulator_IsEnabledChanged(MediaEmulator emulator, bool isEnabled)
        {
            IsEnabled = isEnabled;
        }

        void InstrumentClusterElectronics_IgnitionStateChanged(IgnitionEventArgs e)
        {
            if (e.PreviousIgnitionState == IgnitionState.Acc && e.CurrentIgnitionState == IgnitionState.Ign)
            {
                IsEnabled = true;
            }
        }

        #endregion

        #region Drawing members

        protected virtual void DrawScreen( /*MenuScreenUpdateEventArgs args*/)
        {
            DrawHeader();
            DrawBody();
        }

        protected virtual void DrawHeader() { }

        protected virtual void DrawBody() { }

        protected virtual void ScreenSuspend()
        {
            ScreenNavigatedFrom(CurrentScreen);
        }

        protected virtual void ScreenWakeup()
        {
            ScreenNavigatedTo(CurrentScreen);
        }

        public virtual void UpdateHeader()
        {
            if (!IsEnabled)
            {
                return;
            }
            DrawHeader();
        }

        public virtual void UpdateBody()
        {
            if (!IsEnabled)
            {
                return;
            }
            DrawBody(/*args*/);
        }

        public virtual void UpdateScreen(ushort headerDrawDelay = 500)
        {
            UpdateBody();
            UpdateHeaderWithDelay(headerDrawDelay);
        }

        public virtual void UpdateHeaderWithDelay(ushort delayTime = 1000)
        {
            delayTimeout = new Timer(delegate
            {
                UpdateHeader();
                if (delayTimeout != null)
                {
                    delayTimeout.Dispose();
                    delayTimeout = null;
                }
            }, null, delayTime, 0);
        }

        //public virtual void UpdateBodyWithDelay(ushort delayTime = 1000)
        //{
        //    delayTimeout = new Timer(delegate
        //    {
        //        UpdateBody();
        //        if (delayTimeout != null)
        //        {
        //            delayTimeout.Dispose();
        //            delayTimeout = null;
        //        }
        //    }, null, delayTime, 0);
        //}

        public virtual void UpdateScreenWitDelay(ushort delayTime = 1000)
        {
            delayTimeout = new Timer(delegate
            {
                UpdateHeaderWithDelay(500);
                UpdateBody();
                if (delayTimeout != null)
                {
                    delayTimeout.Dispose();
                    delayTimeout = null;
                }
            }, null, delayTime, 0);
        }

        void currentScreen_UpdateHeader(MenuScreen screen)
        {
            UpdateHeader();
        }

        void currentScreen_UpdateBody(MenuScreen screen)
        {
            UpdateBody();
        }

        #endregion

        #region Navigation members

        public bool IsEnabled
        {
            get { return isEnabled; }
            set
            {
                if (isEnabled == value)
                {
                    if (value)
                    {
                        UpdateScreen();
                    }
                    return;
                }
                isEnabled = value;
                if (value)
                {
                    ScreenWakeup();
                    UpdateScreenWitDelay();
                }
                else
                {
                    ScreenSuspend();
                }
            }
        }

        public void Navigate(MenuScreen screen)
        {
            if (screen == null)
            {
                Logger.Error("Navigation to null screen");
                return;
            }
            if (CurrentScreen == screen)
            {
                return;
            }
            navigationStack.Push(CurrentScreen);
            CurrentScreen = screen;
        }

        public void NavigateBack()
        {
            if (navigationStack.Count > 0)
            {
                CurrentScreen = (MenuScreen)navigationStack.Pop();
            }
            else
            {
                NavigateHome();
            }
        }

        public void NavigateHome()
        {
            CurrentScreen = homeScreen;
            navigationStack.Clear();
        }

        public void NavigateAfterHome(MenuScreen screen)
        {
            navigationStack.Clear();
            navigationStack.Push(homeScreen);
            CurrentScreen = screen;
        }

        public MenuScreen CurrentScreen
        {
            get
            {
                return currentScreen;
            }
            set
            {
                if (currentScreen == value || value == null)
                {
                    return;
                }
                ScreenNavigatedFrom(currentScreen);
                currentScreen = value;
                ScreenNavigatedTo(currentScreen);
                UpdateScreen();
            }
        }

        protected virtual void ScreenNavigatedTo(MenuScreen screen)
        {
            if (screen == null || !screen.OnNavigatedTo(this))
            {
                return;
            }

            screen.ItemClicked += currentScreen_ItemClicked;
            screen.UpdateHeader += currentScreen_UpdateHeader;
            screen.UpdateBody += currentScreen_UpdateBody;
        }

        protected virtual void ScreenNavigatedFrom(MenuScreen screen)
        {
            if (screen == null)
            {
                return;
            }

            screen.OnNavigatedFrom(this);

            screen.ItemClicked -= currentScreen_ItemClicked;
            screen.UpdateHeader -= currentScreen_UpdateHeader;
            screen.UpdateBody -= currentScreen_UpdateBody;
        }

        void currentScreen_ItemClicked(MenuScreen screen, MenuItem item)
        {
            switch (item.Action)
            {
                case MenuItemAction.GoToScreen:
                    Navigate(item.GoToScreenCallback());
                    break;
                case MenuItemAction.GoBackScreen:
                    NavigateBack();
                    break;
                case MenuItemAction.GoHomeScreen:
                    NavigateHome();
                    break;
                case MenuItemAction.Refresh:
                    UpdateScreen();
                    break;
            }
        }

        #endregion
    }
}
