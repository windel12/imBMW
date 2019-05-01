using System;
using System.Collections;
using imBMW.iBus.Devices.Real;
using imBMW.iBus;
using imBMW.Tools;
using imBMW.Features.Menu.Screens;
using imBMW.iBus.Devices.Emulators;

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
            // TODO: Refactor this!!!
            BordcomputerScreen.Instance.MediaEmulator = mediaEmulator;
            MusicListScreen.Instance.MediaEmulator = mediaEmulator;

            CurrentScreen = BordcomputerScreen.Instance;

            byte titleStartIndex = 0;
            byte statusStartIndex = 0;
            var trackInfo = mediaEmulator.Player.CurrentTrack;

            // TODO: Refactor this!!!
            mediaEmulator.Player.IsPlayingChanged += (s, e) =>
            {
                if (s.IsPlaying)
                {
                    /*DDEScreen.Instance.TitleCallback = */BordcomputerScreen.Instance.TitleCallback = x =>
                    {
                        //if (trackInfo.Title != null && trackInfo.Title != "")
                        //{
                            return TrimTextToLength(trackInfo.Title, ref titleStartIndex, 10);
                        //}
                        //return TrimTextToLength(trackInfo.FileName, ref titleStartIndex, 10);
                    };
                    /*DDEScreen.Instance.StatusCallback = */BordcomputerScreen.Instance.StatusCallback = x =>
                    {
                        if (trackInfo.Artist != null && trackInfo.Artist != "")
                        {
                            return TrimTextToLength(trackInfo.Artist, ref statusStartIndex, 10);
                        }
                        return string.Empty;
                    };
                    Radio.DisplayText(trackInfo.Title);
                }
            };
            mediaEmulator.Player.TrackChanged += (s, e) =>
            {
                titleStartIndex = 0;
                statusStartIndex = 0;
                trackInfo = mediaEmulator.Player.CurrentTrack;
                //UpdateScreenWitDelay(500);
                UpdateHeaderWithDelay(500);
                Radio.DisplayText(trackInfo.Title);
            };
            //mediaEmulator.IsEnabledChanged += mediaEmulator_IsEnabledChanged;
            //Radio.OnOffChanged += Radio_OnOffChanged;
            Manager.AddMessageReceiverForDestinationDevice(DeviceAddress.Radio, ProcessToRadioMessage);
        }

        public static string TrimTextToLength(string text, ref byte startIndex, int length)
        {
            if (text.Length <= length)
            {
                return text;
            }
            if (startIndex + length > text.Length)
            {
                startIndex = 0;
            }
            string result = text.Substring(startIndex++, length);
            return result;
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

        protected override byte StatusTextMaxlen { get { return 11; } }

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
                        m.ReceiverDescription = "Screen switch by nav";
                        break;
                    case 0x02:
                        m.ReceiverDescription = "Screen switch by rad";
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
            // BM buttons
            if (m.Data[0] == 0x48 && m.Data.Length == 2)
            {
                switch (m.Data[1])
                {
                    case 0x87: // 'AuxilaryHeater/Clock' button released
                        IntegratedHeatingAndAirConditioning.StartAuxilaryHeater();
                        break;
                    case 0x94: // '<>' button released
                        IntegratedHeatingAndAirConditioning.StopAuxilaryHeater();
                        break;
                }
            }

            if (mediaEmulator.IsEnabled)
            {
                // BM buttons
                if (m.Data[0] == 0x48 && m.Data.Length == 2)
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
                        case 0x74: // Menu hold >1s
                            OnResetButtonPressed();
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
            }

            if (IsEnabled)
            {
                // item click
                if (m.Data.Length == 4 && m.Data.StartsWith(Bordmonitor.DataItemClicked) && m.Data[3] <= 9)
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
                if (m.Data[0] == 0x48 && m.Data.Length == 2)
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
                            m.ReceiverDescription = "BM button Select";
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
                }
            }
        }

        bool isHeaderDrawing;
        bool isBodyDrawing;
        Message lastTitle;

        protected override void DrawHeader()
        {
            if (isHeaderDrawing || !mediaEmulator.IsEnabled)
            {
                return; // TODO test
            }
            lock (drawLock)
            {
                isHeaderDrawing = true;
                base.DrawHeader();

                var messages = new ArrayList();
                var n = 0;

                // TODO: refactor
                string title = CurrentScreen.Title;
                string status = CurrentScreen.Status;

                if (!StringHelpers.IsNullOrEmpty(title))
                    messages.Add(Bordmonitor.ShowText(title, BordmonitorFields.Title, 0, false, false));
                if (!StringHelpers.IsNullOrEmpty(CurrentScreen.T1Field))
                    messages.Add(Bordmonitor.ShowText(CurrentScreen.T1Field, BordmonitorFields.T1, 1, false, false));
                if (!StringHelpers.IsNullOrEmpty(CurrentScreen.T2Field))                                           
                    messages.Add(Bordmonitor.ShowText(CurrentScreen.T2Field, BordmonitorFields.T2, 2, false, false));
                if (!StringHelpers.IsNullOrEmpty(CurrentScreen.T3Field))                                           
                    messages.Add(Bordmonitor.ShowText(CurrentScreen.T3Field, BordmonitorFields.T3, 3, false, false));
                if (!StringHelpers.IsNullOrEmpty(CurrentScreen.T4Field))                                           
                    messages.Add(Bordmonitor.ShowText(CurrentScreen.T4Field, BordmonitorFields.T4, 4, false, false));
                if (!StringHelpers.IsNullOrEmpty(CurrentScreen.T5Field))
                    messages.Add(Bordmonitor.ShowText(CurrentScreen.T5Field, BordmonitorFields.T5, 5, false, false));
                if (!StringHelpers.IsNullOrEmpty(status))
                    messages.Add(Bordmonitor.ShowText(status, BordmonitorFields.Status, 6, false, false));

                Manager.EnqueueMessage((Message[])messages.ToArray(typeof(Message)));
                isHeaderDrawing = false;
            }
        }

        protected override void DrawBody()
        {
            if (isBodyDrawing || !mediaEmulator.IsEnabled)
            {
                return; // TODO test
            }
            lock (drawLock)
            {
                isBodyDrawing = true;
                base.DrawBody();

                var messages = new Message[FastMenuDrawing ? 2 : 11];
                var n = 0;

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
                        if (item == null && n > 2) // !!!!!
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

                Manager.EnqueueMessage(messages);
                isBodyDrawing = false;
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

        static void OnResetButtonPressed()
        {
            var e = ResetButtonPressed;
            if (e != null)
            {
                e();
            }
        }

        public delegate void ButtonPressedHanlder();
        public static event ButtonPressedHanlder ResetButtonPressed;
    }
}
