using System;
using System.Collections;
using imBMW.iBus.Devices.Real;
using imBMW.iBus;
using imBMW.Tools;
using imBMW.Features.Menu.Screens;
using System.Threading;
using imBMW.Features.Multimedia;

namespace imBMW.Features.Menu
{
    public class BordmonitorMenu : MenuBase
    {
        static BordmonitorMenu instance;

        //bool skipRefreshScreen;
        //bool skipClearScreen;
        //bool skipClearTillRefresh;
        //bool disableRadioMenu;
        bool isScreenSwitched;
        object drawLock = new object();

        private BordmonitorMenu(MediaEmulator mediaEmulator)
            : base(mediaEmulator)
        {
            //CurrentScreen = HomeScreen.Instance;
            // TODO: Refactor this!!!
            BordcomputerScreen.Instance.MediaEmulator = mediaEmulator;
            //MusicListScreen.GetMediaEmulator = () => mediaEmulator;

            CurrentScreen = BordcomputerScreen.Instance;

            byte titleStartIndex = 0;
            byte statusStartIndex = 0;
            //var trackInfo = mediaEmulator.Player.CurrentTrack;

            // TODO: Refactor this!!!
            //mediaEmulator.Player.IsPlayingChanged += (s, e) =>
            //{
            //    if (s.IsPlaying)
            //    {
            //        /*DDEScreen.Instance.TitleCallback = */BordcomputerScreen.Instance.TitleCallback = x =>
            //        {
            //            //if (trackInfo.Title != null && trackInfo.Title != "")
            //            //{
            //                return TrimTextToLength(trackInfo.Title, ref titleStartIndex, 10);
            //            //}
            //            //return TrimTextToLength(trackInfo.FileName, ref titleStartIndex, 10);
            //        };
            //        /*DDEScreen.Instance.StatusCallback = */BordcomputerScreen.Instance.StatusCallback = x =>
            //        {
            //            if (trackInfo.Artist != null && trackInfo.Artist != "")
            //            {
            //                return TrimTextToLength(trackInfo.Artist, ref statusStartIndex, 10);
            //            }
            //            return string.Empty;
            //        };
            //        InstrumentClusterElectronics.ShowNormalTextWithoutGong(trackInfo.Title);
            //    }
            //};

            mediaEmulator.Player.TrackChanged += (audioPlayer, trackName) =>
            {
                //titleStartIndex = 0;
                //statusStartIndex = 0;
                InstrumentClusterElectronics.ShowNormalTextWithoutGong(trackName, timeout: 5000);
                if (IsEnabled)
                {
                    Thread.Sleep(Settings.Instance.Delay2);
                    Bordmonitor.ShowText(trackName, BordmonitorFields.Title);
                }
            };

            //mediaEmulator.IsEnabledChanged += mediaEmulator_IsEnabledChanged;
            //Radio.OnOffChanged += Radio_OnOffChanged;
            Manager.Instance.AddMessageReceiverForDestinationDevice(DeviceAddress.Radio, ProcessToRadioMessage);
            Manager.Instance.AddMessageReceiverForSourceDevice(DeviceAddress.Radio, ProcessRadioMessage);
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

        #region Screen items

        public bool FastMenuDrawing
        {
            get { return CurrentScreen.FastMenuDrawing; }
        }

        //protected override void ProcessRadioMessage(Message m)
        //{
            //base.ProcessRadioMessage(m);

            //if (!IsEnabled)
            //{
            //    return;
            //}

            //var isRefresh = m.Data.Compare(Bordmonitor.MessageRefreshScreen.Data);
            //if (isRefresh)
            //{
            //    m.ReceiverDescription = "Screen refresh";
            //    skipClearTillRefresh = false;
            //    if (skipRefreshScreen)
            //    {
            //        skipRefreshScreen = false;
            //        return;
            //    }
            //}
            //var isClear = m.Data.Compare(Bordmonitor.MessageClearScreen.Data);
            //if (isClear)
            //{
            //    m.ReceiverDescription = "Screen clear";
            //    if (skipClearScreen || skipClearTillRefresh)
            //    {
            //        skipClearScreen = false;
            //        return;
            //    }
            //}
            //if (isClear || isRefresh)
            //{
            //    if (IsScreenSwitched)
            //    {
            //        IsScreenSwitched = false;
            //    }

            //    if (disableRadioMenu || isClear)
            //    {
            //        disableRadioMenu = false;
            //        Bordmonitor.DisableRadioMenu();
            //        return;
            //    }

            //    TODO test "INFO" button
            //    UpdateScreen(MenuScreenUpdateReason.Refresh);
            //    return;
            //}

            //Screen switch

            //0x46 0x01 - switched by nav, after 0x45 0x91 from nav (eg. "menu" button)
            // 0x46 0x02 - switched by radio ("switch" button). 
            //if (m.Data.Length == 2 && m.Data[0] == 0x46 && (m.Data[1] == 0x01 || m.Data[1] == 0x02))
            //{
            //    switch (m.Data[1])
            //    {
            //        case 0x01:
            //            m.ReceiverDescription = "Screen switch by nav";
            //            break;
            //        case 0x02:
            //            m.ReceiverDescription = "Screen switch by rad";
            //            skipClearScreen = true; // to prevent on "clear screen" update on switch to BC/nav
            //            break;
            //    }
            //    IsScreenSwitched = true;
            //    return;
            //}

            //if (m.Data.Compare(Bordmonitor.DataAUX))
            //{
            //    IsScreenSwitched = false;
            //    UpdateScreen(); // TODO prevent flickering
            //    return;
            //}

            //if (m.Data.StartsWith(Bordmonitor.DataShowTitle) /*&& (lastTitle == null || !lastTitle.Data.Compare(m.Data))*/)
            //{
            //    IsScreenSwitched = false;
            //    disableRadioMenu = true;
            //    UpdateScreen(MenuScreenUpdateReason.Refresh);
            //    return;
            //}
        //}

        protected void ProcessToRadioMessage(Message m)
        {
            // BM buttons
            if (m.Data[0] == 0x48 && m.Data.Length == 2)
            {
                switch (m.Data[1])
                {
                    // 'Phone' button
                    case 0x08:
                        m.ReceiverDescription = "Phone press";
                        break;
                    case 0x48:
                        m.ReceiverDescription = "Phone hold";
                        BodyModule.SleepMode(5);
                        break;
                    case 0x88:
                        IsEnabled = true;
                        m.ReceiverDescription = "Phone release";
                        break;

                    // 'AuxilaryHeater/Clock' button
                    case 0x07:
                        m.ReceiverDescription = "Clock press";
                        break;
                    case 0x47:
                        m.ReceiverDescription = "Clock hold";
                        break;
                    case 0x87:
                        m.ReceiverDescription = "Clock release";
                        IntegratedHeatingAndAirConditioning.StartAuxilaryHeater();
                        break;

                    // '<>' button
                    case 0x14:
                    case 0x54:
                        break;
                    case 0x94:
                        IntegratedHeatingAndAirConditioning.StopAuxilaryHeater();
                        break;

                    // 'Tone' button
                    case 0x04:
                        IsEnabled = false;
                        m.ReceiverDescription = "TONE press";
                        break;
                    case 0x44:
                        DigitalSignalProcessingAudioAmplifier.Reset();
                        m.ReceiverDescription = "TONE hold";
                        break;
                    case 0x84:
                        m.ReceiverDescription = "TONE release";
                        break;

                    // 'Select' button
                    case 0x20: 
                        IsEnabled = false;
                        m.ReceiverDescription = "SELECT press";
                        break;
                    case 0x60:
                        DigitalSignalProcessingAudioAmplifier.SelfTest();
                        m.ReceiverDescription = "SELECT hold";
                        break;
                    case 0xA0:
                        m.ReceiverDescription = "SELECT release";
                        break;

                    // 'Next' button
                    case 0x00:
                        m.ReceiverDescription = "Next press";
                        break;
                    case 0x40:
                        m.ReceiverDescription = "Next hold";
                        break;
                    case 0x80:
                        m.ReceiverDescription = "Next release";
                        break;

                    case 0x10:
                        m.ReceiverDescription = "Prev press";
                        break;
                    case 0x50:
                        m.ReceiverDescription = "Prev hold";
                        break;
                    case 0x90:
                        m.ReceiverDescription = "Prev release";
                        break;

                    // 'Mode' button
                    case 0x23:
                        m.ReceiverDescription = "MODE press";
                        break;
                    case 0x63:
                        m.ReceiverDescription = "MODE hold";
                        ActionString volumioUartPlayerShuttedDown = null;
                        volumioUartPlayerShuttedDown = message =>
                        {
                            Thread.Sleep(500);
                            if (InstrumentClusterElectronics.CurrentIgnitionState == IgnitionState.Ign || InstrumentClusterElectronics.CurrentIgnitionState == IgnitionState.Acc)
                                Logger.Warning(message);

                            Thread.Sleep(500);
                            OnSwitchScreenButtonHold();

                            VolumioUartPlayer.ShuttedDown -= volumioUartPlayerShuttedDown;
                        };
                        VolumioUartPlayer.ShuttedDown += volumioUartPlayerShuttedDown;
                        VolumioUartPlayer.Shutdown();
                        Logger.Warning("Shutdown request sent.");
                        break;
                    case 0xA3:
                        m.ReceiverDescription = "MODE released.";
                        break;

                    // 'Eject' button
                    case 0x24:
                        m.ReceiverDescription = "Eject press";
                        break;
                    case 0x64:
                        m.ReceiverDescription = "Eject hold";
                        VolumioUartPlayer.Reboot();
                        Logger.Warning("Reboot request sent.");
                        break;
                    case 0xA4:
                        m.ReceiverDescription = "Eject release";
                        break;

                    // 'Menu' button
                    case 0x34:
                        m.ReceiverDescription = "MENU press";
                        break;
                    case 0x74:
                        OnMenuButtonHold();
                        m.ReceiverDescription = "MENU hold";
                        break;
                    case 0xB4:
                        m.ReceiverDescription = "MENU release";
                        IsEnabled = false;
                        break;

                    // switch screen
                    case 0x30:
                        m.ReceiverDescription = "SwitchScreen press";
                        break;
                    case 0x70:
                        OnSwitchScreenButtonHold();
                        m.ReceiverDescription = "SwitchScreen hold";
                        break;
                    case 0xB0:
                        m.ReceiverDescription = "SwitchScreen release";
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
                        // switch screen
                        case 0x30: // pressed
                            IsEnabled = !IsEnabled;
                            //if (screenSwitched) { UpdateScreen(); }
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
            }
        }

        protected void ProcessRadioMessage(Message m)
        {
            if (IsEnabled && m.DestinationDevice == DeviceAddress.GraphicsNavigationDriver && !StringHelpers.IsNullOrEmpty(mediaEmulator.Player.CurrentTrackTitle) &&
                m.Data.StartsWith(new byte[] { 0x23, 0x62, 0x30, 0x20, 0x20, 0x07, 0x20, 0x20, 0x20, 0x20, 0x20, 0x08, 0x43, 0x44 })) // CD X-XX title
            {
                Thread.Sleep(Settings.Instance.Delay2);
                Bordmonitor.ShowText(mediaEmulator.Player.CurrentTrackTitle, BordmonitorFields.Title);
            }
        }

        bool isHeaderDrawing;
        bool isBodyDrawing;
        //Message lastTitle;

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
                //var n = 0;

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

                Manager.Instance.EnqueueMessage((Message[])messages.ToArray(typeof(Message)));
                isHeaderDrawing = false;
            }
        }

        protected override void DrawBody()
        {
            if (isBodyDrawing)
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

                Manager.Instance.EnqueueMessage(messages);
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
                    Logger.Debug("Screen switched back to radio", "BM");
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

        static void OnMenuButtonHold()
        {
            var e = MenuButtonHold;
            if (e != null)
            {
                e();
            }
        }

        static void OnSwitchScreenButtonHold()
        {
            var e = SwitchScreenButtonHold;
            if (e != null)
            {
                e();
            }
        }

        public delegate void ButtonPressedHanlder();
        public static event ButtonPressedHanlder MenuButtonHold;
        public static event ButtonPressedHanlder SwitchScreenButtonHold;
    }
}
