using GHI.IO.Storage;
using imBMW.Devices.V2.Hardware;
using imBMW.Enums;
using imBMW.Features;
using imBMW.Features.Localizations;
using imBMW.Features.Menu;
using imBMW.Features.Menu.Screens;
using imBMW.Features.Multimedia;
using imBMW.iBus;
using imBMW.iBus.Devices.Emulators;
using imBMW.iBus.Devices.Real;
using imBMW.Multimedia;
using imBMW.Tools;
using Microsoft.SPOT.Hardware;
using Microsoft.SPOT.IO;
using System;
using System.Diagnostics;
using System.IO.Ports;
using System.Threading;
//using GHI.Usb.Host;
using Debug = Microsoft.SPOT.Debug;
using Localization = imBMW.Features.Localizations.Localization;

namespace imBMW.Devices.V2
{
    public class Launcher
    {
        const string version = "FW1.0.12 HW2";

        static OutputPort blueLed;
        static OutputPort greenLed;
        static OutputPort orangeLed;
        static OutputPort redLed;

        internal static QueueThreadWorker LedBlinkingQueueThreadWorker;

        static OutputPort resetPin;

        static Settings settings;
        public static MediaEmulator Emulator { get; set; }
        public static IAudioPlayer Player { get; set; }

        static InterruptPort nextButton;
        static InterruptPort prevButton;

        private static ManualResetEvent _removableMediaInsertedSync;

        private static object lockObj = new object();

        private static bool _useWatchdog;

        private static GHI.Processor.Watchdog.ResetCause _resetCause;

        /// <summary> 30 sec </summary>
        static ushort watchDogTimeoutInMilliseconds = 30000;

        internal static SDCard _massStorage;

        internal static AppState State { get; set; }

        private static Timer requestIgnitionStateTimer;

        internal static int idleTime;

        internal static int requestIgnitionStateTimerPeriod;

        // TODO: revert to 30 seconds, instead of 20 minutes
        internal static int idleTimeout;

        internal static int sleepTimeout;

        public enum LaunchMode
        {
            MicroFramework,
            WPF
        }

        private static MassStorageMountState MassStorageMountState { get; set; }

        internal static int GetTimeoutInMilliseconds(byte minutes, byte seconds)
        {
            return minutes * 60 * 1000 + seconds * 1000;
        }

        private static TimeSpan GetTimeSpanFromMilliseconds(int milliseconds)
        {
            int overallSeconds = milliseconds / 1000;
            int minutes = overallSeconds / 60;
            int seconds = overallSeconds % 60;
            return new TimeSpan(0, minutes, seconds);
        }

        internal static void ResetBoard()
        {
            Logger.Warning("Board will reset in 2 seconds!!!");
            FrontDisplay.RefreshLEDs(LedType.RedBlinking, append: true);
            LedBlinkingQueueThreadWorker.Enqueue(new LedBlinkingItem(redLed, 6, 200));

            requestIgnitionStateTimer?.Dispose();

            UnmountMassStorage();
            FrontDisplay.RefreshLEDs(LedType.Empty);

#if OnBoardMonitorEmulator
            System.Windows.MessageBox.Show("Board was resetted");
#endif

#if DEBUG || DebugOnRealDeviceOverFTDI
            if (Debugger.IsAttached)
                return;
#endif

            if (resetPin == null)
            {
                resetPin = new OutputPort(Pin.ResetPin, false);
            }
        }

        static Launcher()
        {
#if (NETMF && RELEASE) || (OnBoardMonitorEmulator && !DebugOnRealDeviceOverFTDI)
            _useWatchdog = true;
            GHI.Processor.Watchdog.Enable(watchDogTimeoutInMilliseconds);
#endif

            LedBlinkingQueueThreadWorker = new QueueThreadWorker(LedBlinking, "ledBlibking");
            _removableMediaInsertedSync = new ManualResetEvent(false);
            requestIgnitionStateTimerPeriod = watchDogTimeoutInMilliseconds / 3;
            idleTimeout = GetTimeoutInMilliseconds(20, 00);

#if DEBUG
            sleepTimeout = GetTimeoutInMilliseconds(15, 20);
#else
            sleepTimeout = GetTimeoutInMilliseconds(15, 00);
#endif
    }

        public static void Launch(LaunchMode launchMode = LaunchMode.MicroFramework)
        {
            try
            {
                _resetCause = GHI.Processor.Watchdog.LastResetCause;

                blueLed = new OutputPort(FEZPandaIII.Gpio.Led1, false);
                greenLed = new OutputPort(FEZPandaIII.Gpio.Led2, false);
                orangeLed = new OutputPort(FEZPandaIII.Gpio.Led3, _resetCause == GHI.Processor.Watchdog.ResetCause.Watchdog);
                redLed = new OutputPort(FEZPandaIII.Gpio.Led4, false);

                Comfort.Init();
                IntegratedHeatingAndAirConditioning.Init();

                settings = Settings.Instance;

                FileLogger.Create();
                InitManagers();

                InstrumentClusterElectronics.DateTimeChanged += DateTimeChanged;

                Logger.Debug("Watchdog.ResetCause: " + (_resetCause == GHI.Processor.Watchdog.ResetCause.Normal ? "Normal" : "Watchdog"));
				if (_useWatchdog)
				{
					Logger.Debug("Watchdog enabled with timeout: " + watchDogTimeoutInMilliseconds);
				}

                //SettingsScreen.Instance.Status = version.Length > 11 ? version.Replace(" ", "") : version;
                //Localization.SetCurrent(RussianLocalization.SystemName); //Localization.SetCurrent(settings.Language);
                //Comfort.AutoLockDoors = settings.AutoLockDoors;
                //Comfort.AutoUnlockDoors = settings.AutoUnlockDoors;
                //Comfort.AutoCloseWindows = settings.AutoCloseWindows;
                //Comfort.AutoCloseSunroof = settings.AutoCloseSunroof;

                #region MassStorage
                //Controller.DeviceConnectFailed += (sss, eee) =>
                //{
                //    Logger.Error("DeviceConnectFailed!");
                //    LedBlinkingQueueThreadWorker.Enqueue(new LedBlinkingItem(redLed, 1, 100));

                //    ControllerState = UsbMountState.DeviceConnectFailed;
                //    _removableMediaInsertedSync.Set();
                //};
                //Controller.UnknownDeviceConnected += (ss, ee) =>
                //{
                //    Logger.Error("UnknownDeviceConnected!");
                //    LedBlinkingQueueThreadWorker.Enqueue(new LedBlinkingItem(redLed, 2, 100));

                //    ControllerState = UsbMountState.UnknownDeviceConnected;
                //    _removableMediaInsertedSync.Set();
                //};
                //Controller.MassStorageConnected += (sender, massStorage) =>
                //{
                //    Logger.Debug("Controller MassStorageConnected!");
                //    LedBlinkingQueueThreadWorker.Enqueue(new LedBlinkingItem(orangeLed, 2, 100));
                //    ControllerState = UsbMountState.MassStorageConnected;

                    RemovableMedia.Insert += (s, e) =>
                    {
                        LedBlinkingQueueThreadWorker.Enqueue(new LedBlinkingItem(orangeLed, 3, 100));

                        string rootDirectory = VolumeInfo.GetVolumes()[0].RootDirectory;
                        settings = Settings.Init(rootDirectory + "\\imBMW.ini");
                        FileLogger.Init(rootDirectory + "\\logs", () => VolumeInfo.GetVolumes()[0].FlushAll());
                        Logger.Debug("Logger initialized.");

                        MassStorageMountState = MassStorageMountState.Mounted;
                        _removableMediaInsertedSync.Set();
                    };

                    RemovableMedia.Eject += (s, e) =>
                    {
                        FileLogger.Eject();
                        Logger.Print("RemovableMedia Ejected!");
                        LedBlinkingQueueThreadWorker.Enqueue(new LedBlinkingItem(greenLed, 3, 100));
                        MassStorageMountState = MassStorageMountState.Unmounted;
                    };

                //_massStorage = massStorage;
                //};
                #endregion

                try
                {
                    _massStorage = new SDCard(SDCard.SDInterface.SPI);
                    _massStorage.Mount();

                    LedBlinkingQueueThreadWorker.Enqueue(new LedBlinkingItem(orangeLed, 1, 200));
                    bool isSignalled = _removableMediaInsertedSync.WaitOne(Debugger.IsAttached ? 5000 : 5000, true);
                    if (!isSignalled) // No Storage inserted
                    {
                        FrontDisplay.RefreshLEDs(LedType.Red, append: true);
                        LedBlinkingQueueThreadWorker.Enqueue(new LedBlinkingItem(redLed, 3, 100));
                    }
                    else
                    {
                        if (MassStorageMountState == MassStorageMountState.DeviceConnectFailed || MassStorageMountState == MassStorageMountState.UnknownDeviceConnected)
                        {
                            InstrumentClusterElectronics.ShowNormalTextWithGong(MassStorageMountState.ToStringValue());
                            FrontDisplay.RefreshLEDs(LedType.RedBlinking, append: true);
                            LedBlinkingQueueThreadWorker.Enqueue(new LedBlinkingItem(redLed, 4, 100));
                            ResetBoard();
                        }
                    }
                    Logger.Debug("MassStorage state: " + MassStorageMountState.ToStringValue());
                }
                catch (Exception ex)
                {
                    LedBlinkingQueueThreadWorker.Enqueue(new LedBlinkingItem(redLed, 1, 100));
                }
                finally
                {
                    InstrumentClusterElectronics.ShowNormalTextWithGong(MassStorageMountState.ToStringValue());
                }
                

                InstrumentClusterElectronics.RequestDateTime();

                Init();

                Logger.Debug("Started!");

                BordmonitorMenu.MenuButtonHold += () =>
                {
                    FrontDisplay.RefreshLEDs(LedType.Empty);

                    if (Emulator.IsEnabled)
                    {
                        Emulator.PlayerIsPlayingChanged += (s, isPlayingChangedValue) =>
                        {
                            if (!isPlayingChangedValue)
                            {
                                ResetBoard();
                            }
                        };
                        Radio.PressOnOffToggle();
                        //Emulator.IsEnabled = false;
                    }
                    else
                    {
                        ResetBoard();
                    }
                };
                BordmonitorMenu.SwitchScreenButtonHold += () =>
                {
                    UnmountMassStorage();
                    _massStorage = null;
                    Logger.Warning("UNMOUNTED!");
                };

                Manager.Instance.AddMessageReceiverForSourceDevice(DeviceAddress.InstrumentClusterElectronics, m =>
                {
                    if (m.Data[0] == 0x11 && m.Data.Length == 2) // Ignition status
                    {
                        GHI.Processor.Watchdog.ResetCounter();
                    }
                });
                requestIgnitionStateTimer = new Timer(RequestIgnitionStateTimerHandler, null, 0, requestIgnitionStateTimerPeriod);

                imBMWTest();

                Logger.Debug("Actions inited!");

                if (launchMode == LaunchMode.MicroFramework)
                    Thread.Sleep(Timeout.Infinite);
            }
            catch (Exception ex)
            {
                LedBlinking(new LedBlinkingItem(redLed, 5, 200));
                Thread.Sleep(200);
                Logger.Error(ex, "while modules initialization");
                ResetBoard();
            }
        }

        internal static void RequestIgnitionStateTimerHandler(object obj)
        {
            //if (InstrumentClusterElectronics.CurrentIgnitionState == IgnitionState.Off)
            //{
                Logger.Trace("idleTime: " + GetTimeSpanFromMilliseconds(idleTime));

                if (idleTime >= idleTimeout)
                {
                    lock (lockObj)
                    {
                        if (State != AppState.Idle)
                        {
                            IdleMode();
                        }
                    }
                }

                if (idleTime >= sleepTimeout)
                {
                    lock (lockObj)
                    {
                        if (State != AppState.Sleep)
                        {
                            SleepMode();
                        }
                    }
                }

                idleTime += requestIgnitionStateTimerPeriod;

                GHI.Processor.Watchdog.ResetCounter();
            //}
            //else
            //{
            //    InstrumentClusterElectronics.RequestIgnitionStatus();
            //}
        }

        private static void InitManagers()
        {
            var iBusBusy = Pin.TH3122SENSTA;
            var kBusBusy = Pin.K_BUS_TH3122SENSTA;
#if DEBUG || DebugOnRealDeviceOverFTDI
            iBusBusy = Cpu.Pin.GPIO_NONE;
            kBusBusy = Cpu.Pin.GPIO_NONE;
#endif

            string iBusComPort = Serial.COM1;
#if NETMF
            ISerialPort iBusPort = new SerialPortTH3122(iBusComPort, iBusBusy);
#endif
#if OnBoardMonitorEmulator
            ISerialPort iBusPort = new SerialPortTH3122(iBusComPort, iBusBusy, readBufferSize: ushort.MaxValue);
#endif
            Manager.Init(iBusPort);
            Logger.Debug("Manager inited");

            Manager.Instance.BeforeMessageReceived += Manager_BeforeMessageReceived;
            Manager.Instance.AfterMessageReceived += Manager_AfterMessageReceived;
            Manager.Instance.BeforeMessageSent += Manager_BeforeMessageSent;
            Manager.Instance.AfterMessageSent += Manager_AfterMessageSent;

#if NETMF || (OnBoardMonitorEmulator && (DEBUG || DebugOnRealDeviceOverFTDI))
            string kBusComPort = Serial.COM2;
            ISerialPort kBusPort = new SerialPortTH3122(kBusComPort, kBusBusy);
            KBusManager.Init(kBusPort, ThreadPriority.Normal);
            Logger.Debug("KBusManager inited");

            KBusManager.Instance.AfterMessageReceived += KBusManager_AfterMessageReceived;
            KBusManager.Instance.AfterMessageSent += KBusManager_AfterMessageSent;
#endif

#if NETMF || (OnBoardMonitorEmulator && DEBUG)
            //ISerialPort dBusPort = new SerialPortTH3122("COM4", Cpu.Pin.GPIO_NONE, writeBufferSize: 1); // d31, d33
            //dBusPort.AfterWriteDelay = 4;
            //DBusManager.Init(dBusPort, ThreadPriority.Highest);
            //Logger.Debug("DBusManager inited");

            //DBusManager.Instance.AfterMessageReceived += DBusManager_AfterMessageReceived;
            //DBusManager.Instance.AfterMessageSent += DBusManager_AfterMessageSent;
#endif


#if !NETMF && DebugOnRealDeviceOverFTDI
            if (!iBusPort.IsOpen)
                iBusPort.Open();
#endif
        }

        private static void DisposeManagers()
        {
            Manager.Instance.BeforeMessageReceived -= Manager_BeforeMessageReceived;
            Manager.Instance.AfterMessageReceived -= Manager_AfterMessageReceived;
            Manager.Instance.BeforeMessageSent -= Manager_BeforeMessageSent;
            Manager.Instance.AfterMessageSent -= Manager_AfterMessageSent;
            Manager.Instance.Dispose();

            KBusManager.Instance.AfterMessageReceived -= KBusManager_AfterMessageReceived;
            KBusManager.Instance.AfterMessageSent -= KBusManager_AfterMessageSent;
            KBusManager.Instance.Dispose();

            //DBusManager.Instance.AfterMessageReceived -= DBusManager_AfterMessageReceived;
            //DBusManager.Instance.AfterMessageSent -= DBusManager_AfterMessageSent;
            //DBusManager.Instance.Dispose();
        }

        public static void Init()
        {
            //player = new iPodViaHeadset(Cpu.Pin.GPIO_NONE);
            //player = new BluetoothOVC3860(Serial.COM2/*, sd != null ? sd + @"\contacts.vcf" : null*/);
            Logger.Debug("Creating VolumioRestApiPlayer.");
            Cpu.Pin chipSelect_RT = FEZPandaIII.Gpio.D27;
            Cpu.Pin externalInterrupt_WS = FEZPandaIII.Gpio.D24;
            Cpu.Pin reset_BR = FEZPandaIII.Gpio.D26;
            Player = new VolumioRestApiPlayer(chipSelect_RT, externalInterrupt_WS, reset_BR);
            Logger.Debug("VolumioRestApiPlayer created.");
            FrontDisplay.RefreshLEDs(LedType.GreenBlinking);
            if (settings.MenuMode != Tools.MenuMode.RadioCDC/* || Manager.FindDevice(DeviceAddress.OnBoardMonitor, 10000)*/)
            {
                //if (player is BluetoothWT32)
                //{
                //    ((BluetoothWT32)player).NowPlayingTagsSeparatedRows = true;
                //}
                if (settings.MenuMode == MenuMode.BordmonitorCDC)
                {
                    Emulator = new CDChanger(Player);
                    Logger.Debug("CDChanger media emulator created");
                    if (settings.NaviVersion == NaviVersion.MK2)
                    {
                        Localization.Current = new RadioLocalization();
                        SettingsScreen.Instance.CanChangeLanguage = false;
                    }
                    Bordmonitor.NaviVersion = settings.NaviVersion;
                    //BordmonitorMenu.FastMenuDrawing = settings.NaviVersion == NaviVersion.MK4;
                    BordmonitorMenu.Init(Emulator);
                    Logger.Debug("BordmonitorMenu inited");
                }
                else
                {
                    //emulator = new BordmonitorAUX(player);
                }
            }
            else
            {
                //Localization.Current = new RadioLocalization();
                //SettingsScreen.Instance.CanChangeLanguage = false;
                //MultiFunctionSteeringWheel.EmulatePhone = true;
                //Radio.HasMID = Manager.FindDevice(DeviceAddress.MultiInfoDisplay);
                //var menu = RadioMenu.Init(new CDChanger(player));
                //menu.TelephoneModeForNavigation = settings.MenuMFLControl;
                //Logger.Info("Radio menu inited" + (Radio.HasMID ? " with MID" : ""));
            }

            Thread.Sleep(200);
            FrontDisplay.RefreshLEDs(LedType.Green);

            greenLed.Write(true);

            //nextButton = new InterruptPort((Cpu.Pin)FEZPandaIII.Gpio.Ldr1, true, Port.ResistorMode.PullUp, Port.InterruptMode.InterruptEdgeHigh);
            //nextButton.OnInterrupt += (p, s, t) =>
            //{
            //    Emulator.Player.Next();
            //};
            //prevButton = new InterruptPort((Cpu.Pin)FEZPandaIII.Gpio.Ldr0, true, Port.ResistorMode.PullUp, Port.InterruptMode.InterruptEdgeHigh);
            //prevButton.OnInterrupt += (p, s, t) =>
            //{
            //    if (!Emulator.IsEnabled)
            //    {
            //        Emulator.IsEnabled = true;
            //        return;
            //    }

            //    if (Emulator.Player.IsPlaying)
            //    {
            //        Emulator.Pause();
            //    }
            //    else
            //    {
            //        Emulator.Play();
            //    }
            //    //UnmountMassStorage();
            //};
        }

        internal static void IdleMode()
        {
            Logger.Trace("Going to idle");
            UnmountMassStorage();

            State = AppState.Idle;
        }

        internal static void SleepMode()
        {
            Logger.Trace("Going to sleep");

            var waitHandle = new ManualResetEvent(false);
            VolumioRestApiPlayer.Shutdown(response =>
            {
                waitHandle.Set();
            }, exception =>
            {
                Logger.Trace("Exception while shuttig down before sleep. " + exception.Message);
            });
            bool result = waitHandle.WaitOne(5000, true);
            Logger.Trace("Shutdown waitHandle result: " + result);

            UnmountMassStorage();
            DisposeManagers();

            State = AppState.Sleep;
        }

        internal static void UnmountMassStorage()
        {
            Logger.Trace("Storage will be unmounted");
            if (_massStorage != null && _massStorage.Mounted)
            {
                FileLogger.Dispose();
                var waitHandle = new ManualResetEvent(false);

                var unmountThread = new Thread(() =>
                {
                    _massStorage.Unmount();
                    waitHandle.Set();
                });
                unmountThread.Start();

                waitHandle.WaitOne(3000, true);
                greenLed.Write(false);
            }
        }

        private static void DateTimeChanged(DateTimeEventArgs e)
        {
            Logger.Debug("Acquired dateTime from IKE: " + e.Value);
            Utility.SetLocalTime(e.Value);
            InstrumentClusterElectronics.DateTimeChanged -= DateTimeChanged;
        }

        // Log just needed message
        private static bool IBusLoggerPredicate(MessageEventArgs e)
        {
            bool isMusicPlayed = false;//Emulator != null && Emulator.IsEnabled;
            if (false
                || e.Message.Data[0] == 0x18    // rpm
                || e.Message.Data[0] == 0x19    // temp
                || e.Message.Data[0] == 0x21    // radio shortcuts
                || e.Message.Data[0] == 0x22    // text display confirmation
				|| e.Message.Data[0] == 0x23    // display text
                || e.Message.Data[0] == 0xA5    // display title
                )   
            {
                return false;
            }
            return true;
        }

        private static bool IBusReaderPredicate(MessageEventArgs e)
        {
            if (e.Message.SourceDevice == DeviceAddress.CDChanger
                || e.Message.SourceDevice == DeviceAddress.Telephone)
            {
                return false;
            }

            return true;
        }

        internal static void Manager_BeforeMessageReceived(MessageEventArgs e)
        {
            idleTime = 0;
        }

        private static void Manager_AfterMessageReceived(MessageEventArgs e)
        {
            if (IBusLoggerPredicate(e) && IBusReaderPredicate(e))
            {
                // Show only messages which are described
                if (e.Message.Describe() == null)
                {
                    return;
                }

                var logIco = "I < ";
                if (settings.LogMessageToASCII)
                {
                    Logger.Trace(e.Message.ToPrettyString(false, false), logIco);
                }
                else
                {
                    Logger.Trace(e.Message, logIco);
                }
            }
        }

        private static void Manager_BeforeMessageSent(MessageEventArgs e)
        {
            blueLed.Write(Busy(true, 2));
        }

        private static void Manager_AfterMessageSent(MessageEventArgs e)
        {
            blueLed.Write(Busy(false, 2));

            if (IBusLoggerPredicate(e))
            {
                var logIco = "I > ";
                if (settings.LogMessageToASCII)
                {
                    Logger.Trace(e.Message.ToPrettyString(false, false), logIco);
                }
                else
                {
                    Logger.Trace(e.Message, logIco);
                }
            }
        }

        // Log just needed message
        private static bool KBusLoggerPredicate(MessageEventArgs e)
        {
            if (false 
                || e.Message.Data[0] == 0x11    // Ignition status
                || e.Message.Data[0] == 0x13    // IKE Sensor status
                || e.Message.Data[0] == 0x15    // Country coding status
                || e.Message.Data[0] == 0x17    // Odometer
                || e.Message.Data[0] == 0x18    // Speed/RPM
                || e.Message.Data[0] == 0x19    // Temperature
                || e.Message.Data[0] == 0x83    // Air conditioning compressor status
                //|| e.Message.Data[0] == 0x92  // IHKA > ZUH: 92 00 22 (Command for auxilary heater)
                //|| e.Message.Data[0] == 0x93  // ZUH > IHKA: 93 00 22 (Auxilary heater status)
                ) 
            {
                return false;
            }

            return true;
        }

        private static void KBusManager_AfterMessageReceived(MessageEventArgs e)
        {
            idleTime = 0;

            if (KBusLoggerPredicate(e))
            {
                var logIco = "K < ";
                if (settings.LogMessageToASCII)
                {
                    Logger.Trace(e.Message.ToPrettyString(false, false), logIco);
                }
                else
                {
                    Logger.Trace(e.Message, logIco);
                }
            }
        }

        private static void KBusManager_AfterMessageSent(MessageEventArgs e)
        {
            if (KBusLoggerPredicate(e))
            {
                var logIco = "K > ";
                if (settings.LogMessageToASCII)
                {
                    Logger.Trace(e.Message.ToPrettyString(false, false), logIco);
                }
                else
                {
                    Logger.Trace(e.Message, logIco);
                }
            }
        }

        // Log just needed message
        //private static bool DBusLoggerPredicate(MessageEventArgs e)
        //{
        //    return true;
        //}

        //private static void DBusManager_AfterMessageReceived(MessageEventArgs e)
        //{
        //    if (DBusLoggerPredicate(e))
        //    {
        //        var logIco = "D < ";
        //        if (settings.LogMessageToASCII)
        //        {
        //            Logger.Trace(e.Message.ToPrettyString(false, false), logIco);
        //        }
        //        else
        //        {
        //            Logger.Trace(e.Message, logIco);
        //        }
        //    }
        //}

        //private static void DBusManager_AfterMessageSent(MessageEventArgs e)
        //{
        //    if (DBusLoggerPredicate(e))
        //    {
        //        var logIco = "D > ";
        //        if (settings.LogMessageToASCII)
        //        {
        //            Logger.Trace(e.Message.ToPrettyString(false, false), logIco);
        //        }
        //        else
        //        {
        //            Logger.Trace(e.Message, logIco);
        //        }
        //    }
        //}

        static void LedBlinking(object item)
        {
            var ledBlibkingItem = (LedBlinkingItem)item;
            for (byte i = 0; i < ledBlibkingItem.BlinkingCount; i++)
            {
                ledBlibkingItem.Led.Write(true);
                Thread.Sleep(ledBlibkingItem.Interval);
                ledBlibkingItem.Led.Write(false);
                Thread.Sleep(ledBlibkingItem.Interval);
            }

            Thread.Sleep(500);
        }

        static byte busy = 0;
        static bool Busy(bool busy, byte type)
        {
            if (busy)
            {
                Launcher.busy = Launcher.busy.AddBit(type);
            }
            else
            {
                Launcher.busy = Launcher.busy.RemoveBit(type);
            }
            return Launcher.busy > 0;
        }

        private static void imBMWTest()
        {
            Manager.Instance.AddMessageReceiverForSourceDevice(DeviceAddress.imBMWTest, m =>
            {
                if (m.Data[0] == 0x02)
                {
                    SleepMode();
                }
            });
        }
    }
}
