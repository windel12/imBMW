using GHI.Pins;
using GHI.Usb.Host;
using imBMW.Devices.V2.Hardware;
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

        static OutputPort iBusMessageSendReceiveBlinker_BlueLed;
        static OutputPort successInited_GreenLed;
        static OutputPort orangeLed;
        static OutputPort error_RedLed;

        static OutputPort resetPin;

        static Settings settings;
        public static MediaEmulator emulator;
        public static IAudioPlayer player;

        //static InterruptPort nextButton;
        //static InterruptPort prevButton;

        //public static SDCard sd_card;
        private static string rootDirectory;

        private static ManualResetEvent _removableMediaInsertedSync = new ManualResetEvent(false);

        private static bool _useWatchdog = false;

        private static GHI.Processor.Watchdog.ResetCause _resetCause;

        /// <summary> 30 sec </summary>
        static ushort watchDogTimeoutInMilliseconds = 30000;
        private static Timer RequestIgnitionStateTimer;

        private static MassStorage _massStorage;

        private static int sleepTimeInMilliseconds = 0;

        public enum LaunchMode
        {
            MicroFramework,
            WPF
        }

        public enum UsbMountState
        {
            NotInitialized,
            DeviceConnectFailed,
            UnknownDeviceConnected,
            MassStorageConnected,
            Mounted
        }

        private static UsbMountState ControllerState { get; set; }

        private static void ResetBoard()
        {
            Logger.TryTrace("Board will reset in 2 seconds!!!");
            FrontDisplay.RefreshLEDs(LedType.RedBlinking, append: true);
            LedBlinking(orangeLed, 2, 200);
            
            RequestIgnitionStateTimer.Dispose();

            UnmountMassStorage(null, false);         

#if OnBoardMonitorEmulator
            System.Windows.MessageBox.Show("Board was resetted");
#endif

#if DEBUG || DebugOnRealDeviceOverFTDI
            if(Debugger.IsAttached)
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
#if DEBUG
                if (!Debugger.IsAttached)
                {
                    Thread.Sleep(5000);
                }
#endif

                BluetoothScreen.BluetoothChargingState = true;
                BluetoothScreen.AudioSource = AudioSource.Bluetooth;

                _resetCause = GHI.Processor.Watchdog.LastResetCause;

                iBusMessageSendReceiveBlinker_BlueLed = new OutputPort(Pin.LED1, false);
                successInited_GreenLed = new OutputPort(Pin.LED2, false);
                orangeLed = new OutputPort(Pin.LED3, _resetCause == GHI.Processor.Watchdog.ResetCause.Watchdog);
                error_RedLed = new OutputPort(Pin.LED4, false);

#if (NETMF && RELEASE) || (OnBoardMonitorEmulator && DEBUG)
                _useWatchdog = true;
#endif
                if (_useWatchdog)
                    GHI.Processor.Watchdog.Enable(watchDogTimeoutInMilliseconds);

                settings = Settings.Instance;

                FileLogger.BaseInit();
                InitManagers();

                InstrumentClusterElectronics.DateTimeChanged += DateTimeChanged;

                Logger.Debug("Watchdog.ResetCause: " + _resetCause.ToStringValue());


                //SettingsScreen.Instance.Status = version.Length > 11 ? version.Replace(" ", "") : version;
                //Localization.SetCurrent(RussianLocalization.SystemName); //Localization.SetCurrent(settings.Language);
                //Features.Comfort.AutoLockDoors = settings.AutoLockDoors;
                //Features.Comfort.AutoUnlockDoors = settings.AutoUnlockDoors;
                //Features.Comfort.AutoCloseWindows = settings.AutoCloseWindows;
                //Features.Comfort.AutoCloseSunroof = settings.AutoCloseSunroof;

                //RemovableMedia.Insert += (a, b) =>
                //{
                //    _removableMediaInsertedSync.Set();
                //};

                //sd_card = new SDCard(SDCard.SDInterface.MCI);
                //sd_card.Mount();


                #region UsbMassStorage
                Controller.DeviceConnectFailed += (sss, eee) =>
                {
                    Logger.Print("Controller DeviceConnectFailed!");
                    Radio.DisplayText("DeviceConnectFailed");
                    LedBlinking(error_RedLed, 1, 100);

                    ControllerState = UsbMountState.DeviceConnectFailed;
                    _removableMediaInsertedSync.Set();
                };
                Controller.UnknownDeviceConnected += (ss, ee) =>
                {
                    Logger.Print("Controller UnknownDeviceConnected!");
                    Radio.DisplayText("UnknownDeviceConnected");
                    LedBlinking(error_RedLed, 2, 100);

                    ControllerState = UsbMountState.UnknownDeviceConnected;
                    _removableMediaInsertedSync.Set();
                };
                Controller.MassStorageConnected += (sender, massStorage) =>
                {
                    Logger.Print("Controller MassStorageConnected!");
                    LedBlinking(orangeLed, 1, 100);
                    ControllerState = UsbMountState.MassStorageConnected;

                    RemovableMedia.Insert += (s, e) =>
                    {
                        Logger.Print("RemovableMedia Inserted!");
                        LedBlinking(orangeLed, 2, 100);

                        ControllerState = UsbMountState.Mounted;
                        _removableMediaInsertedSync.Set();
                    };

                    _massStorage = massStorage;
                    Logger.Print("Mass Storage created!");
                    massStorage.Mount();
                    Logger.Print("Mass Storage mounted!");
                };
#if RELEASE
                Controller.Start();
#else
                // WARNING! Be aware, without this lines you can get 'Controller -> DeviceConnectFailed' each time when you start debugging...
                //if (Debugger.IsAttached)
                {
                    Controller.Start();
                }
#endif
                #endregion

                //FrontDisplay.RefreshLEDs(LedType.OrangeBlinking);
                LedBlinking(orangeLed, 1, 100);
                bool isSignalled = !error && _removableMediaInsertedSync.WaitOne(Debugger.IsAttached ? 30000 : 10000, true);
                if (!isSignalled)
                {
                    Radio.DisplayText("!isSignalled");
                    LedBlinking(error_RedLed, 3, 100);
                    ResetBoard();
                }
                else
                {
                    Logger.Debug("Controller state: " + ControllerState.ToString());
                    //FrontDisplay.RefreshLEDs(LedType.Orange, append:true);
                    LedBlinking(orangeLed, 3, 100);
                }

                if (!VolumeInfo.GetVolumes()[0].IsFormatted)
                {
                    Radio.DisplayText("!VolumeInfo.IsFormatted");
                    //FrontDisplay.RefreshLEDs(LedType.Orange | LedType.Red | LedType.Green);
                    rootDirectory = "Unknown";
                    FileLogger.BaseDispose();
                }
                else
                {
                    rootDirectory = VolumeInfo.GetVolumes()[0].RootDirectory;
                    settings = Settings.Init(rootDirectory + "\\imBMW.ini");
                    FileLogger.Init(rootDirectory, () => VolumeInfo.GetVolumes()[0].FlushAll());
                }

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

                int requestIgnitionStateTimerPeriod = watchDogTimeoutInMilliseconds / 4;
#if DEBUG
                int sleepTimeout = 1 * 30 * 1000;
#else
                int sleepTimeout = 15 * 60 * 1000 + 50;
#endif
                RequestIgnitionStateTimer = new Timer(obj =>
                {
                    if (InstrumentClusterElectronics.CurrentIgnitionState == IgnitionState.Off)
                    {
                        if (sleepTimeInMilliseconds < sleepTimeout)
                        {
                            Logger.Trace("sleepTimeInMilliseconds: " + sleepTimeInMilliseconds);
                        }
                        sleepTimeInMilliseconds += requestIgnitionStateTimerPeriod;
                        if (sleepTimeInMilliseconds == sleepTimeout)
                        {
                            Logger.Trace("Storage will be unmounted");
                            UnmountMassStorage();
                        }
                        GHI.Processor.Watchdog.ResetCounter();
                    }
                    else
                    {
                        InstrumentClusterElectronics.RequestIgnitionStatus();
                    }
                }, null, 0, requestIgnitionStateTimerPeriod);

                Manager.Instance.AddMessageReceiverForSourceDevice(DeviceAddress.InstrumentClusterElectronics, m =>
                {
                    if (m.Data[0] == 0x11 && m.Data.Length == 2) // Ignition status
                    {
                        GHI.Processor.Watchdog.ResetCounter();
                    }
                });

                InstrumentClusterElectronics.IgnitionStateChanged += (args) =>
                {
                    if (Settings.Instance.UnmountMassStorageOnChangingIgnitionToAcc && args.CurrentIgnitionState == IgnitionState.Acc && args.PreviousIgnitionState == IgnitionState.Ign)
                    {
                        if (emulator.IsEnabled)
                        {
                            emulator.PlayerIsPlayingChanged += UnmountMassStorage;
                            FrontDisplay.RefreshLEDs(LedType.Orange, append: true);
                            Radio.PressOnOffToggle();
                            emulator.IsEnabled = false;
                        }
                        else
                        {
                            UnmountMassStorage(null, false);
                        }
                    }
                };

                BordmonitorMenu.PhoneButtonHold += () =>
                {
                    UnmountMassStorage(null, false);
                };

                Logger.Debug("Actions inited!");

                if (DateTime.Now.Year < 2019)
                {
                    InstrumentClusterElectronics.RequestDateTime();
                }

                if (launchMode == LaunchMode.MicroFramework)
                    Thread.Sleep(Timeout.Infinite);
            }
            catch (Exception ex)
            {
                error = true;
                LedBlinking(error_RedLed, 5, 100);
                Thread.Sleep(200);
                error_RedLed.Write(true);
                Logger.TryError(ex, "while modules initialization");
                ResetBoard();
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
#if NETMF || (OnBoardMonitorEmulator && DebugOnRealDeviceOverFTDI)
            ISerialPort iBusPort = new SerialPortTH3122(iBusComPort, iBusBusy);
#endif
#if OnBoardMonitorEmulator && (DEBUG || RELEASE)
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

            successInited_GreenLed.Write(true);

            short getDateTimeTimeout = 1500;
#if OnBoardMonitorEmulator
            getDateTimeTimeout = 0;
#endif

            //var dateTimeEventArgs = InstrumentClusterElectronics.GetDateTime(getDateTimeTimeout);
            //if (!dateTimeEventArgs.DateIsSet)
            //{
            //    Logger.Trace("Asking dateTime again");
            //   dateTimeEventArgs = InstrumentClusterElectronics.GetDateTime(getDateTimeTimeout);
            //}
            //Logger.Trace("dateTimeEventArgs.DateIsSet: " + dateTimeEventArgs.DateIsSet);
            //Logger.Trace("Acquired dateTime from IKE: " + dateTimeEventArgs.Value);          

            FrontDisplay.RefreshLEDs(LedType.Green);
            FrontDisplay.RefreshLEDs(LedType.Green);

            //nextButton = new InterruptPort((Cpu.Pin)FEZPandaIII.Gpio.Ldr1, true, Port.ResistorMode.PullUp, Port.InterruptMode.InterruptEdgeHigh);
            //nextButton.OnInterrupt += (p, s, t) =>
            //{
            //    if (!emulator.IsEnabled) { emulator.IsEnabled = true; }
            //    else { emulator.Player.Next(); }
            //};
            //prevButton = new InterruptPort((Cpu.Pin)FEZPandaIII.Gpio.Ldr0, true, Port.ResistorMode.PullUp, Port.InterruptMode.InterruptEdgeHigh);
            //prevButton.OnInterrupt += (p, s, t) =>
            //{
            //    //emulator.Player.Prev();
            //    //Manager.Instance.EnqueueMessage(new Message(DeviceAddress.Radio, DeviceAddress.CDChanger, CDChanger.DataSelectDisk6));
            //    //emulator.Player.IsRandom = false;
            //    //emulator.Player.DiskNumber = 6;
            //    emulator.IsEnabled = false;
            //};
        }

        public static void UnmountMassStorage(IAudioPlayer sender, bool isPlayingChangedValue)
        {
            UnmountMassStorage();
            emulator.PlayerIsPlayingChanged -= UnmountMassStorage;
            FrontDisplay.RefreshLEDs(LedType.Empty);
        }

        private static void UnmountMassStorage()
        {
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
                   // e.Message.SourceDevice == DeviceAddress.Radio && e.Message.DestinationDevice == DeviceAddress.CDChanger && e.Message.Data[0] == 0x01 
                   //||
                   // e.Message.SourceDevice == DeviceAddress.Radio &&
                   //e.Message.DestinationDevice == DeviceAddress.CDChanger
                   //||
                   //e.Message.SourceDevice == DeviceAddress.Radio && e.Message.Data.StartsWith(0x02, 0x00) // Radio poll response
                   //||
                   //e.Message.SourceDevice == DeviceAddress.CDChanger &&
                   //e.Message.DestinationDevice == DeviceAddress.Radio
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
                   !isMusicPlayed
                   ||
#if DEBUG
                   true;
#else
                   false;
#endif
        }

        private static void Manager_BeforeMessageReceived(MessageEventArgs e)
        {
            //iBusMessageSendReceiveBlinker_BlueLed.Write(Busy(true, 1));
            sleepTimeInMilliseconds = 0;
        }

        private static void Manager_AfterMessageReceived(MessageEventArgs e)
        {
            //iBusMessageSendReceiveBlinker_BlueLed.Write(Busy(false, 1));

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
            iBusMessageSendReceiveBlinker_BlueLed.Write(Busy(true, 2));
            sleepTimeInMilliseconds = 0;
        }

        private static void Manager_AfterMessageSent(MessageEventArgs e)
        {
            iBusMessageSendReceiveBlinker_BlueLed.Write(Busy(false, 2));

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
            sleepTimeInMilliseconds = 0;

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
            sleepTimeInMilliseconds = 0;

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
                //FrontDisplay.RefreshLEDs(LedType.Green);
                var logIco = "D < ";
                if (settings.LogMessageToASCII)
                {
                    Logger.Trace(e.Message.ToPrettyString(false, false), logIco);
                }
                else
                {
                    Logger.Trace(e.Message, logIco);
                }
                //FrontDisplay.RefreshLEDs(LedType.Empty);
            }
        }

        private static void DBusManager_BeforeMessageSent(MessageEventArgs e)
        {
            if (DBusLoggerPredicate(e))
            {
                //FrontDisplay.RefreshLEDs(LedType.Orange);
                var logIco = "D > ";
                if (settings.LogMessageToASCII)
                {
                    Logger.Trace(e.Message.ToPrettyString(false, false), logIco);
                }
                else
                {
                    Logger.Trace(e.Message, logIco);
                }
                //FrontDisplay.RefreshLEDs(LedType.Empty);
            }
        }

        static byte busy = 0;
        public static bool error = false;
        private static object _sync = new object();
        private static string traceFileName = "traceLog";
        private static byte traceFileNameNumber = 0;

        static void Logger_Logged(LoggerArgs args)
        {
            if (args.Priority == LogPriority.Trace || args.Priority == LogPriority.Error)
            {
                lock (_sync)
                {
                    if (StringHelpers.IsNullOrEmpty(rootDirectory))
                        return;

                    StreamWriter traceFile = null;
                    try
                    {
                        string filePath = rootDirectory + "\\" + traceFileName + (traceFileNameNumber == 0 ? "" : traceFileNameNumber.ToString()) + ".txt";
                        traceFile = new StreamWriter(filePath, append:true);
                        traceFile.WriteLine(args.LogString);
                        traceFile.Flush();
                        VolumeInfo.GetVolumes()[0].FlushAll();
                    }
                    catch
                    {
                        traceFileNameNumber++;
                    }
                    finally
                    {
                        if (traceFile != null)
                        {
                            traceFile.Dispose();
                        }
                    }
                }
            }
#if DEBUG || DebugOnRealDeviceOverFTDI
            if (Debugger.IsAttached)
            {
                Logger.FreeMemory();
                Debug.Print(args.LogString);
            }
#endif
        }

        static void LedBlinking(OutputPort led, byte blinkingCount, ushort interval)
        {
            for (byte i = 0; i < blinkingCount; i++)
            {
                led.Write(true);
                Thread.Sleep(interval);
                led.Write(false);
                Thread.Sleep(interval);
            }
        }

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
    }
}
