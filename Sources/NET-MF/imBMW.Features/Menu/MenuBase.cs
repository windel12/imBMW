using System;
using Microsoft.SPOT;
using System.Collections;
using imBMW.Features.Menu.Screens;
using imBMW.Tools;
using imBMW.iBus.Devices.Emulators;
using imBMW.Multimedia;
using imBMW.Features.Localizations;
using System.Threading;
using imBMW.iBus;
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
            //CurrentScreen = homeScreen;

            this.mediaEmulator = mediaEmulator;
            mediaEmulator.IsEnabledChanged += mediaEmulator_IsEnabledChanged;
            //mediaEmulator.PlayerIsPlayingChanged += ShowPlayerStatus;
            //mediaEmulator.PlayerStatusChanged += ShowPlayerStatus;
            //mediaEmulator.PlayerChanged += mediaEmulator_PlayerChanged;
            //mediaEmulator_PlayerChanged(mediaEmulator.Player);

            //Manager.AddMessageReceiverForSourceDevice(DeviceAddress.Radio, ProcessRadioMessage);
        }

        #region Radio members

        protected virtual void ProcessRadioMessage(Message m)
        {
            //if (!IsEnabled)
            //{
            //    return;
            //}

            //if (m.Data.Length == 3 && m.Data[0] == 0x38 && m.Data[1] == 0x0A)
            //{
            //    switch (m.Data[2])
            //    {
            //        case 0x00:
            //            //mediaEmulator.Player.Next();
            //            m.ReceiverDescription = "Next track";
            //            break;
            //        case 0x01:
            //            //mediaEmulator.Player.Prev();
            //            m.ReceiverDescription = "Prev track";
            //            break;
            //    }
            //}
        }

        #endregion

        #region MediaEmulator members

        protected Timer displayStatusDelayTimer;
        protected const ushort displayStatusDelay = 900; // TODO make abstract
        protected Timer delayTimeout;

        protected abstract byte StatusTextMaxlen { get; }

        //protected abstract void ShowPlayerStatus(IAudioPlayer player, string status, PlayerEvent playerEvent);

        //protected abstract void ShowPlayerStatus(IAudioPlayer player, bool isPlaying);

        //protected void ShowPlayerStatus(IAudioPlayer player)
        //{
        //    // TODO move to player interface
        //    #if !MF_FRAMEWORK_VERSION_V4_1
        //    if (player is BluetoothWT32 && !((BluetoothWT32)player).IsConnected)
        //    {
        //        ShowPlayerStatus(player, Localization.Current.Disconnected, PlayerEvent.Wireless);
        //    }
        //    else
        //    #endif
        //    {
        //        ShowPlayerStatus(player, player.IsPlaying);
        //    }
        //}

        //protected void ShowPlayerStatus(IAudioPlayer player, string status)
        //{
            //if (!IsEnabled)
            //{
            //    //return;
            //    Logger.Warning("Why shouldn't I set player status when menu is disabled?!");
            //}
            //if (displayStatusDelayTimer != null)
            //{
            //    displayStatusDelayTimer.Dispose();
            //    displayStatusDelayTimer = null;
            //}

            //player.Menu.Status = status;
        //}

        protected void ShowPlayerStatusWithDelay(IAudioPlayer player)
        {
            if (displayStatusDelayTimer != null)
            {
                displayStatusDelayTimer.Dispose();
                displayStatusDelayTimer = null;
            }

            byte startIndex = 6; // remove path to SD card TODO: refactor this later.Length)
            displayStatusDelayTimer = new Timer(delegate
            {
                //if (displayStatusDelayTimer == null)
                //{
                //    return;
                //}
                //string trimmedFileName = "";
                //if (startIndex + 11 >= player.FileName.Length - 3)
                //{
                //    startIndex = 6;
                //}
                //trimmedFileName = player.FileName.Substring(startIndex++, 11);
                //Bordmonitor.ShowText(trimmedFileName, BordmonitorFields.Title);
            }, null, 400, displayStatusDelay);
        }

        protected string TextWithIcon(string icon, string text = null)
        {
            if (StringHelpers.IsNullOrEmpty(text))
            {
                return icon;
            }
            if (icon.Length + text.Length < StatusTextMaxlen)
            {
                return icon + " " + text;
            }
            return icon + text;
        }

        protected string TextWithIcon(char icon, string text = null)
        {
            if (StringHelpers.IsNullOrEmpty(text))
            {
                return icon + "";
            }
            if (text.Length + 1 < StatusTextMaxlen)
            {
                return icon + " " + text;
            }
            return icon + text;
        }

        void mediaEmulator_PlayerChanged(IAudioPlayer player)
        {
            //HomeScreen.Instance.PlayerScreen = player.Menu;
        }

        void mediaEmulator_IsEnabledChanged(MediaEmulator emulator, bool isEnabled)
        {
            IsEnabled = isEnabled;
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

        public virtual void UpdateHeader(MenuScreenUpdateEventArgs args = null)
        {
            if (!IsEnabled)
            {
                return;
            }
            DrawHeader(/*args*/);
        }

        public virtual void UpdateBody(/*MenuScreenUpdateEventArgs args*/)
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

        public virtual void UpdateBodyWithDelay(ushort delayTime = 1000)
        {
            delayTimeout = new Timer(delegate
            {
                UpdateBody();
                if (delayTimeout != null)
                {
                    delayTimeout.Dispose();
                    delayTimeout = null;
                }
            }, null, delayTime, 0);
        }

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

        void currentScreen_UpdateHeader(MenuScreen screen, MenuScreenUpdateEventArgs args)
        {
            if (args.Reason == MenuScreenUpdateReason.RefreshWithDelay && args.Item != null)
            {
                UpdateHeaderWithDelay((ushort) args.Item);
            }
            else
            {
                UpdateHeader();
            }
        }

        void currentScreen_UpdateBody(MenuScreen screen, MenuScreenUpdateEventArgs args)
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
                Logger.TryError("Navigation to null screen");
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
