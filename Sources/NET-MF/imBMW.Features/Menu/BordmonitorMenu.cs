using System;
using Microsoft.SPOT;
using imBMW.iBus.Devices.Real;
using imBMW.iBus;
using imBMW.Tools;
using System.Threading;
using imBMW.Features.Menu.Screens;
using imBMW.iBus.Devices.Emulators;
using imBMW.Multimedia;
using imBMW.Features.Localizations;

namespace imBMW.Features.Menu
{
    public class BordmonitorMenu : MenuBase
    {
        static BordmonitorMenu instance;

        bool skipRefreshScreen;
        bool skipClearScreen;
        bool skipClearTillRefresh;
        bool disableRadioMenu;
        bool isScreenSwitched;
        object drawLock = new object();

        private BordmonitorMenu(MediaEmulator mediaEmulator)
            : base(mediaEmulator)
        {
            //CurrentScreen = HomeScreen.Instance;
            CurrentScreen = BordcomputerScreen.Instance;

            //mediaEmulator.Player.TrackChanged += (s, e) =>
            //{
            //    ShowPlayerStatusWithDelay(mediaEmulator.Player);
            //};
            int startIndex = 6; // remove path to SD card TODO: refactor this later.Length)
            mediaEmulator.Player.IsPlayingChanged += (s, e) =>
            {
                if (s.IsPlaying)
                {
                    BordcomputerScreen.Instance.TitleCallback = x =>
                    {
                        if(mediaEmulator.Player.FileName == null)
                        {
                            return "";
                        }
                        string trimmedFileName = "";
                        if (startIndex + 11 >= mediaEmulator.Player.FileName.Length - 3)
                        {
                            startIndex = 6;
                        }
                        return mediaEmulator.Player.FileName.Substring(startIndex++, 11);
                    };
                }
            };
            mediaEmulator.Player.TrackChanged += (s, e) =>
            {
                startIndex = 6;
                UpdateScreenWitDelay(500);
            };
            //mediaEmulator.IsEnabledChanged += mediaEmulator_IsEnabledChanged;
            //Radio.OnOffChanged += Radio_OnOffChanged;
            Manager.AddMessageReceiverForDestinationDevice(DeviceAddress.Radio, ProcessToRadioMessage);
        }

        public static BordmonitorMenu Init(MediaEmulator mediaEmulator)
        {
            if (instance != null)
            {
                // TODO implement hot switch of emulators
                throw new Exception("Already inited");
            }
            instance = new BordmonitorMenu(mediaEmulator);
            return instance;
        }

        #region Player items

        protected override int StatusTextMaxlen { get { return 11; } }

        //protected override void ShowPlayerStatus(IAudioPlayer player, bool isPlaying)
        //{
        //    string s = isPlaying ? Localization.Current.Playing : Localization.Current.Paused;
        //    ShowPlayerStatus(player, s);
        //}

        //protected override void ShowPlayerStatus(IAudioPlayer player, string status, PlayerEvent playerEvent)
        //{
        //    if (!IsEnabled)
        //    {
        //        return;
        //    }
            //bool showAfterWithDelay = false;
            //switch (playerEvent)
            //{
            //    case PlayerEvent.Next:
            //        status = Localization.Current.Next;
            //        showAfterWithDelay = true;
            //        break;
            //    case PlayerEvent.Prev:
            //        status = Localization.Current.Previous;
            //        showAfterWithDelay = true;
            //        break;
            //    case PlayerEvent.Playing:
            //        status = TextWithIcon(">", status);
            //        break;
            //    case PlayerEvent.Current:
            //        status = TextWithIcon("\x07", status);
            //        break;
            //    case PlayerEvent.Voice:
            //        status = TextWithIcon("*", status);
            //        break;
            //    case PlayerEvent.Settings:
            //        status = TextWithIcon("*", status);
            //        showAfterWithDelay = true;
            //        break;
            //}
            //ShowPlayerStatus(player, status);
            //if (showAfterWithDelay)
            //{
            //    ShowPlayerStatusWithDelay(player);
            //}
        //}

        void mediaEmulator_IsEnabledChanged(MediaEmulator emulator, bool isEnabled)
        {
            //if (!isEnabled)
            //{
            //    Bordmonitor.EnableRadioMenu();
            //}
        }

        #endregion

        #region Screen items

        public bool FastMenuDrawing
        {
            get { return CurrentScreen.FastMenuDrawing; }
        }

        protected override void ScreenWakeup()
        {
            base.ScreenWakeup();

            //disableRadioMenu = true;
        }

        public override void UpdateScreen(/*MenuScreenUpdateEventArgs args*/)
        {
            if (IsScreenSwitched)
            {
                return;
            }

            base.UpdateScreen(/*args*/);
        }

        void Radio_OnOffChanged(bool turnedOn)
        {
            if (turnedOn)
            {
                //Bordmonitor.EnableRadioMenu(); // fixes disabled radio menu to update screen
            }
        }

        protected override void ProcessRadioMessage(Message m)
        {
            //base.ProcessRadioMessage(m);

            if (!IsEnabled)
            {
                return;
            }

            var isRefresh = m.Data.Compare(Bordmonitor.MessageRefreshScreen.Data);
            if (isRefresh)
            {
                //m.ReceiverDescription = "Screen refresh";
                //skipClearTillRefresh = false;
                //if (skipRefreshScreen)
                //{
                //    skipRefreshScreen = false;
                //    return;
                //}
            }
            var isClear = m.Data.Compare(Bordmonitor.MessageClearScreen.Data);
            if (isClear)
            {
                //m.ReceiverDescription = "Screen clear";
                //if (skipClearScreen || skipClearTillRefresh)
                //{
                //    skipClearScreen = false;
                //    return;
                //}
            }
            if (isClear || isRefresh)
            {
                //if (IsScreenSwitched)
                //{
                //    IsScreenSwitched = false;
                //}

                //if (disableRadioMenu || isClear)
                //{
                //    disableRadioMenu = false;
                //    Bordmonitor.DisableRadioMenu();
                //    return;
                //}

                // TODO test "INFO" button
                //UpdateScreen(MenuScreenUpdateReason.Refresh);
                return;
            }

            // Screen switch
            // 0x46 0x01 - switched by nav, after 0x45 0x91 from nav (eg. "menu" button)
            // 0x46 0x02 - switched by radio ("switch" button). 
            if (m.Data.Length == 2 && m.Data[0] == 0x46 && (m.Data[1] == 0x01 || m.Data[1] == 0x02))
            {
                switch (m.Data[1])
                {
                    case 0x01:
                        m.ReceiverDescription = "Screen SW by nav";
                        break;
                    case 0x02:
                        m.ReceiverDescription = "Screen SW by rad";
                        skipClearScreen = true; // to prevent on "clear screen" update on switch to BC/nav
                        break;
                }
                IsScreenSwitched = true;
                return;
            }

            //if (m.Data.Compare(Bordmonitor.DataAUX))
            //{
            //    IsScreenSwitched = false;
            //    UpdateScreen(); // TODO prevent flickering
            //    return;
            //}

            if (m.Data.StartsWith(Bordmonitor.DataShowTitle) && (lastTitle == null || !lastTitle.Data.Compare(m.Data)))
            {
                IsScreenSwitched = false;
                //disableRadioMenu = true;
                //UpdateScreen(MenuScreenUpdateReason.Refresh);
                return;
            }
        }

        protected void ProcessToRadioMessage(Message m)
        {
            if (!mediaEmulator.IsEnabled)
            {
                return;
            }

            // BM buttons
            if (m.Data.Length == 2 && m.Data[0] == 0x48)
            {
                switch (m.Data[1])
                {
                    case 0x08: // phone
                        m.ReceiverDescription = "BM button Phone - draw bordmonitor menu";
                        IsEnabled = true;
                        break;
                    case 0x34: // Menu
                        m.ReceiverDescription = "BM button Menu";
                        IsEnabled = false;
                        break;
                    case 0x30: // Radio menu
                        m.ReceiverDescription = "BM button Switch Screen";
                        IsEnabled = !IsEnabled;
                        //if (screenSwitched)
                        //{
                        //    UpdateScreen();
                        //}
                        break;
                }
            }

            if (!IsEnabled)
            {
                return;
            }

            // item click
            if (m.Data.Length == 4 && m.Data.StartsWith(0x31, 0x60, 0x00) && m.Data[3] <= 9)
            {
                var index = GetItemIndex(m.Data[3], true);
                m.ReceiverDescription = "Screen item click #" + index;
                var item = CurrentScreen.GetItem(index);
                if (item != null)
                {
                    item.Click();
                }
                return;
            }

            // BM buttons
            if (m.Data.Length == 2 && m.Data[0] == 0x48)
            {
                switch (m.Data[1])
                {
                    case 0x14: // <>
                        m.ReceiverDescription = "BM button <> - navigate home";
                        //NavigateHome();
                        break;
                    case 0x07:
                        m.ReceiverDescription = "BM button Clock - navigate BC";
                        //NavigateAfterHome(BordcomputerScreen.Instance);
                        break;
                    case 0x20: // Select
                        m.ReceiverDescription = "BM button Sel"; // - navigate player";
                        IsEnabled = false;
                        // TODO fix in cdc mode
                        //NavigateAfterHome(HomeScreen.Instance.PlayerScreen);
                        break;
                    case 0x04:
                        m.ReceiverDescription = "BM button Tone";
                        IsEnabled = false;
                        //Bordmonitor.EnableRadioMenu(); // TODO test [and remove]
                        break;
                    case 0x23: // Mode
                        m.ReceiverDescription = "BM button Mode";
                        //IsEnabled = false;
                        //Bordmonitor.EnableRadioMenu(); // TODO test [and remove]
                        break;
                }
                return;
            }
        }

        bool isDrawing;
        Message lastTitle;

        protected override void DrawScreen(/*MenuScreenUpdateEventArgs args*/)
        {
            if (isDrawing || !mediaEmulator.IsEnabled)
            {
                return; // TODO test
            }
            lock (drawLock)
            {
                isDrawing = true;
                skipRefreshScreen = true;
                skipClearTillRefresh = true; // TODO test no screen items lost
                base.DrawScreen(/*args*/);

                var messages = new Message[FastMenuDrawing ? 4 : 13];
                var n = 0;
                messages[n++] = Bordmonitor.ShowText(CurrentScreen.Status ?? String.Empty, BordmonitorFields.Status, 0, false, false);
                lastTitle = Bordmonitor.ShowText(CurrentScreen.Title ?? String.Empty, BordmonitorFields.Title, 0, false, false);
                messages[n++] = lastTitle;
                byte[] itemsBytes = null;
                for (byte i = 0; i < 10; i++)
                {
                    var index = GetItemIndex(i, true);
                    var item = CurrentScreen.GetItem(index);
                    if (FastMenuDrawing)
                    {
                        if (item == null && itemsBytes != null)
                        {
                            itemsBytes = itemsBytes.Combine(0x06);
                            continue;
                        }
                        var m = DrawItem(item, i);
                        if (itemsBytes == null)
                        {
                            itemsBytes = m.Data;
                        }
                        else
                        {
                            var d = m.Data.Skip(3);
                            d[0] = 0x06;
                            itemsBytes = itemsBytes.Combine(d);
                        }
                    }
                    else
                    {
                        if (item == null && n > 2)
                        {
                            var prevMess = messages[n-1];
                            messages[n - 1] = new Message(prevMess.SourceDevice, prevMess.DestinationDevice, prevMess.ReceiverDescription, prevMess.Data.Combine(0x06));
                        }
                        else
                        {
                            messages[n++] = DrawItem(item, i);
                        }
                        messages[n - 1].AfterSendDelay = 40;
                    }
                }
                if (FastMenuDrawing)
                {
                    itemsBytes = itemsBytes.Combine(0x06);
                    messages[n++] = new Message(DeviceAddress.Radio, DeviceAddress.GraphicsNavigationDriver, "Fill screen items", itemsBytes);
                }
                messages[n++] = Bordmonitor.MessageRefreshScreen;
                skipRefreshScreen = true;
                skipClearTillRefresh = true;
                Manager.EnqueueMessage(messages);
                isDrawing = false;
            }
        }

        Message DrawItem(MenuItem item, byte index)
        {
            var s = item != null ? item.Text : "";
            return Bordmonitor.ShowText(s ?? String.Empty, BordmonitorFields.Item, index, item != null && item.IsChecked, false);
        }

        byte GetItemIndex(byte index, bool back = false)
        {
            return Bordmonitor.GetItemIndex(CurrentScreen.ItemsCount, index, back);
        }

        public bool IsScreenSwitched
        {
            get { return isScreenSwitched; }
            set
            {
                if (isScreenSwitched == value)
                {
                    return;
                }
                isScreenSwitched = value;
                if (value)
                {
                    ScreenSuspend();
                }
                else
                {
                    Logger.Info("Screen switched back to radio", "BM");
                    ScreenWakeup();
                }
            }
        }

        #endregion

        public static BordmonitorMenu Instance
        {
            get
            {
                if (instance == null)
                {
                    //instance = new BordmonitorMenu();
                    throw new Exception("Not inited BM menu");
                }
                return instance;
            }
        }
    }
}
