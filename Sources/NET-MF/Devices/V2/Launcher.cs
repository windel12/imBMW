using GHI.Pins;
using GHI.Usb.Host;
using imBMW.Devices.V2.Hardware;
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
using System.IO;
using System.IO.Ports;
using System.Threading;
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

        internal static QueueThreadWorker LedBlinkingQueueThreadWorker = new QueueThreadWorker(LedBlinking, "ledBlibking");

        static OutputPort resetPin;

        static Settings settings;
        public static MediaEmulator emulator;
        public static IAudioPlayer player;

        //static InterruptPort nextButton;
        //static InterruptPort prevButton;

        private static ManualResetEvent _removableMediaInsertedSync = new ManualResetEvent(false);

        private static object lockObj = new object();

        private static bool _useWatchdog;

        private static GHI.Processor.Watchdog.ResetCause _resetCause;

        /// <summary> 30 sec </summary>
        static ushort watchDogTimeoutInMilliseconds = 30000;

        internal static MassStorage _massStorage;

        internal static AppState State { get; set; }

        private static Timer requestIgnitionStateTimer;

        internal static int idleTime = 0;

        internal static int requestIgnitionStateTimerPeriod = watchDogTimeoutInMilliseconds / 3;

        // TODO: revert to 30 seconds, instead of 20 minutes
        internal static int idleTimeout = GetTimeoutInMilliseconds(20, 00);

#if DEBUG
        internal static int sleepTimeout = GetTimeoutInMilliseconds(0, 30);
#else
        internal static int sleepTimeout = GetTimeoutInMilliseconds(15, 20);
#endif

        public enum LaunchMode
        {
            MicroFramework,
            WPF
        }

        private static UsbMountState ControllerState { get; set; }

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
            Logger.Trace("Board will reset in 2 seconds!!!");
            FrontDisplay.RefreshLEDs(LedType.RedBlinking, append: true);
            LedBlinkingQueueThreadWorker.Enqueue(new LedBlinkingItem(orangeLed, 2, 200));

            requestIgnitionStateTimer.Dispose();

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

        public static void Launch(LaunchMode launchMode = LaunchMode.MicroFramework)
        {
            try
            {
                BluetoothScreen.BluetoothChargingState = true;
                BluetoothScreen.AudioSource = AudioSource.Bluetooth;

                Comfort.Init();

                _resetCause = GHI.Processor.Watchdog.LastResetCause;

                blueLed = new OutputPort(Pin.LED1, false);
                greenLed = new OutputPort(Pin.LED2, false);
                orangeLed = new OutputPort(Pin.LED3, _resetCause == GHI.Processor.Watchdog.ResetCause.Watchdog);
                redLed = new OutputPort(Pin.LED4, false);

#if (NETMF && RELEASE) || (OnBoardMonitorEmulator && !DebugOnRealDeviceOverFTDI)
                _useWatchdog = true;
#endif
                if (_useWatchdog)
                    GHI.Processor.Watchdog.Enable(watchDogTimeoutInMilliseconds);

                settings = Settings.Instance;

                FileLogger.Create();
                InitManagers();

                InstrumentClusterElectronics.DateTimeChanged += DateTimeChanged;

                Logger.Debug("Watchdog.ResetCause: " + _resetCause.ToStringValue());

                //SettingsScreen.Instance.Status = version.Length > 11 ? version.Replace(" ", "") : version;
                //Localization.SetCurrent(RussianLocalization.SystemName); //Localization.SetCurrent(settings.Language);
                //Comfort.AutoLockDoors = settings.AutoLockDoors;
                //Comfort.AutoUnlockDoors = settings.AutoUnlockDoors;
                //Comfort.AutoCloseWindows = settings.AutoCloseWindows;
                //Comfort.AutoCloseSunroof = settings.AutoCloseSunroof;

                #region UsbMassStorage
                Controller.DeviceConnectFailed += (sss, eee) =>
                {
                    Logger.Error("DeviceConnectFailed!");
                    LedBlinkingQueueThreadWorker.Enqueue(new LedBlinkingItem(redLed, 1, 100));

                    ControllerState = UsbMountState.DeviceConnectFailed;
                    _removableMediaInsertedSync.Set();
                };
                Controller.UnknownDeviceConnected += (ss, ee) =>
                {
                    Logger.Error("UnknownDeviceConnected!");
                    LedBlinkingQueueThreadWorker.Enqueue(new LedBlinkingItem(redLed, 2, 100));

                    ControllerState = UsbMountState.UnknownDeviceConnected;
                    _removableMediaInsertedSync.Set();
                };
                Controller.MassStorageConnected += (sender, massStorage) =>
                {
                    Logger.Debug("Controller MassStorageConnected!");
                    LedBlinkingQueueThreadWorker.Enqueue(new LedBlinkingItem(orangeLed, 2, 100));
                    ControllerState = UsbMountState.MassStorageConnected;

                    RemovableMedia.Insert += (s, e) =>
                    {
                        LedBlinkingQueueThreadWorker.Enqueue(new LedBlinkingItem(orangeLed, 3, 100));
 
                        string rootDirectory = VolumeInfo.GetVolumes()[0].RootDirectory;
                        settings = Settings.Init(rootDirectory + "\\imBMW.ini");
                        FileLogger.Init(rootDirectory + "\\logs", () => VolumeInfo.GetVolumes()[0].FlushAll());
                        Logger.Debug("Logger initialized.");

                        ControllerState = UsbMountState.Mounted;
                        _removableMediaInsertedSync.Set();
                    };

                    RemovableMedia.Eject += (s, e) =>
                    {
                        FileLogger.Eject();
                        Logger.Print("RemovableMedia Ejected!");
                        LedBlinkingQueueThreadWorker.Enqueue(new LedBlinkingItem(greenLed, 3, 100));
                        ControllerState = UsbMountState.Unmounted;
                    };

                    _massStorage = massStorage;
                    massStorage.Mount();
                };
#if RELEASE
                Controller.Start();
#else

#if NETMF
                // WARNING! Be aware, without this line you can get 'Controller -> DeviceConnectFailed' each time when you start debugging...
                if (Debugger.IsAttached)
#endif
                {
                    Controller.Start();
                }
#endif
                #endregion

                LedBlinkingQueueThreadWorker.Enqueue(new LedBlinkingItem(orangeLed, 1, 100));
                bool isSignalled = _removableMediaInsertedSync.WaitOne(Debugger.IsAttached ? 10000 : 10000, true);
                if (!isSignalled) // No USB inserted
                {
                    InstrumentClusterElectronics.ShowTextWithGong("USB:" + ControllerState.ToStringValue());
                    FrontDisplay.RefreshLEDs(LedType.RedBlinking, append: true);
                    LedBlinkingQueueThreadWorker.Enqueue(new LedBlinkingItem(redLed, 3, 100));
                }
                else
                {
                    if (ControllerState == UsbMountState.DeviceConnectFailed || ControllerState == UsbMountState.UnknownDeviceConnected)
                    {
                        InstrumentClusterElectronics.ShowTextWithGong("USB:" + ControllerState.ToStringValue());
                        FrontDisplay.RefreshLEDs(LedType.Red, append: true);
                        LedBlinkingQueueThreadWorker.Enqueue(new LedBlinkingItem(redLed, 4, 100));
                        ResetBoard();
                    }
                }
                Logger.Debug("Controller state: " + ControllerState.ToStringValue());

                InstrumentClusterElectronics.RequestDateTime();

                Init();

                Logger.Debug("Started!");

                BordmonitorMenu.MenuButtonHold += () =>
                {
                    FrontDisplay.RefreshLEDs(LedType.Empty);

                    if (emulator.IsEnabled)
                    {
                        emulator.PlayerIsPlayingChanged += (s, isPlayingChangedValue) =>
                        {
                            if (!isPlayingChangedValue)
                            {
                                ResetBoard();
                            }
                        };
                        Radio.PressOnOffToggle();
                        emulator.IsEnabled = false;
                    }
                    else
                    {
                        ResetBoard();
                    }
                };
                BordmonitorMenu.PhoneButtonHold += () =>
                {
                    Logger.Trace("PhoneButtonHold");
                    UnmountMassStorage();
                    _massStorage = null;
                    InstrumentClusterElectronics.ShowNormalText("Unmounted");
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
                LedBlinking(new LedBlinkingItem(redLed, 5, 100));
                Thread.Sleep(200);
                redLed.Write(true);
                Logger.Error(ex, "while modules initialization");
                ResetBoard();
            }
        }

        internal static void RequestIgnitionStateTimerHandler(object obj)
        {
            if (InstrumentClusterElectronics.CurrentIgnitionState == IgnitionState.Off)
            {
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
            }
            else
            {
                InstrumentClusterElectronics.RequestIgnitionStatus();
            }
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

            KBusManager.Instance.BeforeMessageReceived += KBusManager_BeforeMessageReceived;
            KBusManager.Instance.BeforeMessageSent += KBusManager_BeforeMessageSent;
#endif

#if NETMF || (OnBoardMonitorEmulator && DEBUG)
            ISerialPort dBusPort = new SerialPortTH3122("COM4", Cpu.Pin.GPIO_NONE, writeBufferSize: 1); // d31, d33
            dBusPort.AfterWriteDelay = 4;
            DBusManager.Init(dBusPort, ThreadPriority.Highest);
            Logger.Debug("DBusManager inited");

            DBusManager.Instance.BeforeMessageReceived += DBusManager_BeforeMessageReceived;
            DBusManager.Instance.BeforeMessageSent += DBusManager_BeforeMessageSent;
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

            KBusManager.Instance.BeforeMessageReceived -= KBusManager_BeforeMessageReceived;
            KBusManager.Instance.BeforeMessageSent -= KBusManager_BeforeMessageSent;
            KBusManager.Instance.Dispose();

            DBusManager.Instance.BeforeMessageReceived -= DBusManager_BeforeMessageReceived;
            DBusManager.Instance.BeforeMessageSent -= DBusManager_BeforeMessageSent;
            DBusManager.Instance.Dispose();
        }

        public static void Init()
        {
            //player = new iPodViaHeadset(Cpu.Pin.GPIO_NONE);
            //player = new BluetoothOVC3860(Serial.COM2/*, sd != null ? sd + @"\contacts.vcf" : null*/);
            Logger.Debug("Prepare creating VS1003Player");
            player = new VS1003Player(FEZPandaIII.Gpio.D25, FEZPandaIII.Gpio.D27, FEZPandaIII.Gpio.D24, FEZPandaIII.Gpio.D26);
            Logger.Debug("VS1003Player created");
            FrontDisplay.RefreshLEDs(LedType.GreenBlinking);
            if (settings.MenuMode != Tools.MenuMode.RadioCDC/* || Manager.FindDevice(DeviceAddress.OnBoardMonitor, 10000)*/)
            {
                //if (player is BluetoothWT32)
                //{
                //    ((BluetoothWT32)player).NowPlayingTagsSeparatedRows = true;
                //}
                if (settings.MenuMode == MenuMode.BordmonitorCDC)
                {
                    emulator = new CDChanger(player);
                    Logger.Debug("CDChanger media emulator created");
                    if (settings.NaviVersion == NaviVersion.MK2)
                    {
                        Localization.Current = new RadioLocalization();
                        SettingsScreen.Instance.CanChangeLanguage = false;
                    }
                    Bordmonitor.NaviVersion = settings.NaviVersion;
                    //BordmonitorMenu.FastMenuDrawing = settings.NaviVersion == NaviVersion.MK4;
                    BordmonitorMenu.Init(emulator);
                    Logger.Debug("BordmonitorMenu inited");
                    BluetoothScreen.Init();
                    Logger.Debug("BluetoothScreen inited");
                }
                else
                {
                    //emulator = new BordmonitorAUX(player);
                }

                //Bordmonitor.NaviVersion = settings.NaviVersion;
                //BordmonitorMenu.FastMenuDrawing = settings.NaviVersion == NaviVersion.MK4;
                //BordmonitorMenu.Init(emulator);

                Logger.Debug("Bordmonitor menu inited");
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

            greenLed.Write(true);

            short getDateTimeTimeout = 1500;
#if OnBoardMonitorEmulator
            getDateTimeTimeout = 0;
#endif
            FrontDisplay.RefreshLEDs(LedType.Green);
            FrontDisplay.RefreshLEDs(LedType.Green);

            //nextButton = new InterruptPort((Cpu.Pin)FEZPandaIII.Gpio.Ldr1, true, Port.ResistorMode.PullUp, Port.InterruptMode.InterruptEdgeHigh);
            //nextButton.OnInterrupt += (p, s, t) =>
            //{
            //    if (!emulator.IsEnabled) { emulator.IsEnabled = true; }
            //    else { emulator.Player.Next(); }
            //};
            //prevButton = new InterruptPort((Cpu.Pin)FEZPandaIII.Gpio.Ldr0, true, Port.ResistorMode.PullUp, Port.InterruptMode.InterruptEdgeHigh);
            //prevButton.OnInterrupt += (p, s, t) => { };
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
            UnmountMassStorage();
            DisposeManagers();

            State = AppState.Sleep;
        }


        internal static void WakeUp()
        {
            if (_massStorage != null && !_massStorage.Mounted)
            {
                FileLogger.Create();
                Logger.Trace("Waking up!");
                _massStorage.Mount();
            }
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

                bool waitResult = waitHandle.WaitOne(3000, true);
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
            bool isMusicPlayed = emulator != null && emulator.IsEnabled;
            return
                //e.Message.SourceDevice == DeviceAddress.Radio && e.Message.DestinationDevice == DeviceAddress.GraphicsNavigationDriver && e.Message.Data.StartsWith(0x21, 0x60, 0x00)
                // ||
                //e.Message.SourceDevice == DeviceAddress.Radio && e.Message.Data.StartsWith(0x02, 0x00) // Radio poll response
                //||
                //e.Message.SourceDevice == DeviceAddress.Radio &&
                //e.Message.DestinationDevice == DeviceAddress.InstrumentClusterElectronics
                //||
                //e.Message.SourceDevice == DeviceAddress.InstrumentClusterElectronics &&
                //e.Message.DestinationDevice == DeviceAddress.FrontDisplay 
                //||
                //e.Message.SourceDevice == DeviceAddress.InstrumentClusterElectronics &&
                //e.Message.DestinationDevice == DeviceAddress.GlobalBroadcastAddress &&
                //e.Message.Data[0] == 0x19
                //||
                //e.Message.SourceDevice == DeviceAddress.MultiFunctionSteeringWheel &&
                //e.Message.DestinationDevice == DeviceAddress.Radio
                //||
                InstrumentClusterElectronics.CurrentIgnitionState == IgnitionState.Acc
                ||
                InstrumentClusterElectronics.CurrentIgnitionState == IgnitionState.Off
                ||
                settings.ForceMessageLog
                ||
                !isMusicPlayed
#if DEBUG
                || true
#endif
                ;
        }

        internal static void Manager_BeforeMessageReceived(MessageEventArgs e)
        {
            //blueLed.Write(Busy(true, 1));
            idleTime = 0;

            WakeUp();
        }

        private static void Manager_AfterMessageReceived(MessageEventArgs e)
        {
            //blueLed.Write(Busy(false, 1));

            if (IBusLoggerPredicate(e))
            {
                // Show only messages which are described
                if (e.Message.Describe() == null)
                {
                    return;
                }

                var logIco = "I < ";
                if (settings.LogMessageToASCII)
                {
                    //object[] messages = StringHelpers.WholeChunks(e.Message.ToPrettyString(false, false), 10).ToArray();
                    //foreach (string message in messages)
                    //{
                    //    Logger.Trace(message, logIco);
                    //}
                    //Logger.Trace("/n");

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
            if (
                e.Message.SourceDevice == DeviceAddress.IntegratedHeatingAndAirConditioning && e.Message.Data[0] == 0x83 // Air conditioning compressor status
                || 
                //e.Message.SourceDevice == DeviceAddress.IntegratedHeatingAndAirConditioning && e.Message.Data[0] == 0x86 // Some info for NavigationEurope
                //||
                e.Message.SourceDevice == DeviceAddress.InstrumentClusterElectronics && e.Message.Data[0] == 0x02 // Poll response
                ||
                e.Message.SourceDevice == DeviceAddress.InstrumentClusterElectronics && e.Message.Data[0] == 0x11 // Ignition status
                ||
                e.Message.SourceDevice == DeviceAddress.InstrumentClusterElectronics && e.Message.Data[0] == 0x13 // IKE Sensor status
                ||
                e.Message.SourceDevice == DeviceAddress.InstrumentClusterElectronics && e.Message.Data[0] == 0x15 // Country coding status
                ||
                e.Message.SourceDevice == DeviceAddress.InstrumentClusterElectronics && e.Message.Data[0] == 0x17 // Odometer
                ||
                e.Message.SourceDevice == DeviceAddress.InstrumentClusterElectronics && e.Message.Data[0] == 0x18 // Speed/RPM
                ||
                e.Message.SourceDevice == DeviceAddress.InstrumentClusterElectronics && e.Message.Data[0] == 0x19 // Temperature
                //||
                //e.Message.SourceDevice == DeviceAddress.IntegratedHeatingAndAirConditioning && e.Message.Data.StartsWith(0x92, 0x00, 0x22) // IntegratedHeatingAndAirConditioning > AuxilaryHeater: 92 00 22 (Command for auxilary heater)
                //||
                //e.Message.SourceDevice == DeviceAddress.AuxilaryHeater && e.Message.Data.StartsWith(0x93, 0x00, 0x22) // AuxilaryHeater > IntegratedHeatingAndAirConditioning: 93 00 22 (Auxilary heater status)
                ) 
            {
                return false;
            }

            return
                   e.Message.SourceDevice == DeviceAddress.InstrumentClusterElectronics ||

                   e.Message.SourceDevice == DeviceAddress.AuxilaryHeater ||
                   e.Message.DestinationDevice == DeviceAddress.AuxilaryHeater ||

                   e.Message.SourceDevice == DeviceAddress.IntegratedHeatingAndAirConditioning ||
                   e.Message.DestinationDevice == DeviceAddress.IntegratedHeatingAndAirConditioning ||

                   e.Message.SourceDevice == DeviceAddress.HeadlightVerticalAimControl ||
                   e.Message.DestinationDevice == DeviceAddress.HeadlightVerticalAimControl ||

                   e.Message.SourceDevice == DeviceAddress.Diagnostic ||
                   e.Message.DestinationDevice == DeviceAddress.Diagnostic || 

                   InstrumentClusterElectronics.CurrentIgnitionState == IgnitionState.Off; 
        }

        private static void KBusManager_BeforeMessageReceived(MessageEventArgs e)
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

        private static void KBusManager_BeforeMessageSent(MessageEventArgs e)
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
        private static bool DBusLoggerPredicate(MessageEventArgs e)
        {
            return true;
        }

        private static void DBusManager_BeforeMessageReceived(MessageEventArgs e)
        {
            if (DBusLoggerPredicate(e))
            {
                var logIco = "D < ";
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

        private static void DBusManager_BeforeMessageSent(MessageEventArgs e)
        {
            if (DBusLoggerPredicate(e))
            {
                var logIco = "D > ";
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
