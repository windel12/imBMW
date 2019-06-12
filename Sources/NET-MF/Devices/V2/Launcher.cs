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

        private static MassStorage _massStorage;

        public enum LaunchMode
        {
            MicroFramework,
            WPF
        }

        private static void ResetBoard()
        {
            Logger.TryTrace("Board will reset in 2 seconds!!!");
            FrontDisplay.RefreshLEDs(LedType.GreenBlinking);
            Thread.Sleep(200); // wait to add pause after previous blinking
            LedBlinking(successInited_GreenLed, 5, 200);

            if (_massStorage != null && _massStorage.Mounted)
            {
                if (FileLogger.FlushCallback != null)
                {
                    FileLogger.FlushCallback();
                }

                _massStorage.Unmount();
                Thread.Sleep(200);
            }

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

                //SDCard sd = null;
                settings = Settings.Init(/*sd != null ? sd + @"\imBMW.ini" : */null);

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
                    LedBlinking(error_RedLed, 5, 100);
                    ResetBoard();
                };
                Controller.UnknownDeviceConnected += (ss, ee) =>
                {
                    Logger.Print("Controller UnknownDeviceConnected!");
                    ResetBoard();
                };
                Controller.MassStorageConnected += (sender, massStorage) =>
                {
                    Logger.Print("Controller MassStorageConnected!");
                    LedBlinking(orangeLed, 2, 100);
                    RemovableMedia.Insert += (s, e) =>
                    {
                        Logger.Print("RemovableMedia Inserted!");
                        LedBlinking(successInited_GreenLed, 2, 100);
                        _removableMediaInsertedSync.Set();
                    };

                    _massStorage = massStorage;
                    Logger.Print("Mass Storage created!");
                    massStorage.Mount();
                    Logger.Print("Mass Storage mount...");
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

                LedBlinking(orangeLed, 1, 100);
                bool isSignalled = !error && _removableMediaInsertedSync.WaitOne(Debugger.IsAttached ? 30000 : 10000, true);
                if (!isSignalled)
                {
                    LedBlinking(error_RedLed, 3, 100);
                    ResetBoard();
                }
                else
                {
                    Logger.Print("Controller inited!");
                    LedBlinking(successInited_GreenLed, 3, 100);
                }

                rootDirectory = VolumeInfo.GetVolumes()[0].RootDirectory;

                //Logger.Logged += Logger_Logged;
                FileLogger.Init(rootDirectory, () => VolumeInfo.GetVolumes()[0].FlushAll());

                //Logger.Trace("\n\n\n\n\n");
                Logger.Trace("Watchdog.ResetCause: " + _resetCause.ToStringValue());

                Init();

                Logger.Trace("Started!");

                BordmonitorMenu.ResetButtonPressed += () =>
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
                    }
                    else
                    {
                        ResetBoard();
                    }
                };

                InstrumentClusterElectronics.RequestIgnitionStatus();

                Logger.Trace("Ignition state after init:" + InstrumentClusterElectronics.CurrentIgnitionState.ToStringValue());
                if (InstrumentClusterElectronics.CurrentIgnitionState == IgnitionState.Acc)
                {
                    InitWatchDogResetThread();
                }

                if (InstrumentClusterElectronics.CurrentIgnitionState == IgnitionState.Ign)
                {
                    InitWatchDogResetByIKEMessageReceiving();
                }

                InstrumentClusterElectronics.IgnitionStateChanged += (args) =>
                {
                    if (args.PreviousIgnitionState == IgnitionState.Off && args.CurrentIgnitionState == IgnitionState.Acc)
                    {
                        if (!WatchDogResetThreadInited)
                        {
                            InitWatchDogResetThread();
                        }
                    }

                    if (args.CurrentIgnitionState == IgnitionState.Ign)
                    {
                        if (!WatchDogResetByIKEInited)
                        {
                            DisposeWatchDogResetThread();
                            InitWatchDogResetByIKEMessageReceiving();
                        }
                    }

                    if (args.CurrentIgnitionState == IgnitionState.Acc && args.PreviousIgnitionState == IgnitionState.Ign)
                    {
                        if (emulator.IsEnabled)
                        {
                            Radio.PressOnOffToggle();
                            FrontDisplay.RefreshLEDs(LedType.Orange, true);
                            emulator.PlayerIsPlayingChanged += UnmountMassStorage;
                        }
                        else
                        {
                            UnmountMassStorage(null, false);
                        }
                    }
                };

                Logger.Trace("Actions inited!");

                if (launchMode == LaunchMode.MicroFramework)
                    Thread.Sleep(Timeout.Infinite);
            }
            catch (Exception ex)
            {
                error = true;
                error_RedLed.Write(true);
                Logger.TryError(ex, "while modules initialization");
                ResetBoard();
            }
        }

        public static void UnmountMassStorage(IAudioPlayer sender, bool isPlayingChangedValue)
        {
            _massStorage.Unmount();
            FrontDisplay.RefreshLEDs(LedType.Empty);
            emulator.PlayerIsPlayingChanged -= UnmountMassStorage;
        }

        public static void Init()
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
            ISerialPort iBusPort = new SerialPortTH3122(iBusComPort, iBusBusy, readBufferSize:ushort.MaxValue);
#endif
            Manager.Init(iBusPort);
            Logger.Trace("Manager inited");

            Manager.Instance.BeforeMessageReceived += Manager_BeforeMessageReceived;
            Manager.Instance.AfterMessageReceived += Manager_AfterMessageReceived;
            Manager.Instance.BeforeMessageSent += Manager_BeforeMessageSent;
            Manager.Instance.AfterMessageSent += Manager_AfterMessageSent;

#if NETMF || (OnBoardMonitorEmulator && (DEBUG || DebugOnRealDeviceOverFTDI))
            string kBusComPort = Serial.COM2;
            ISerialPort kBusPort = new SerialPortTH3122(kBusComPort, kBusBusy);
            KBusManager.Init(kBusPort, ThreadPriority.Normal);
            Logger.Trace("KBusManager inited");

            KBusManager.Instance.BeforeMessageReceived += KBusManager_BeforeMessageReceived;
            KBusManager.Instance.BeforeMessageSent += KBusManager_BeforeMessageSent;
#endif

#if NETMF || (OnBoardMonitorEmulator && DEBUG)
            ISerialPort dBusPort = new SerialPortTH3122("COM4", Cpu.Pin.GPIO_NONE, writeBufferSize: 1); // d31, d33
            dBusPort.AfterWriteDelay = 4;
            DBusManager.Init(dBusPort, ThreadPriority.Highest);
            Logger.Trace("DBusManager inited");

            DBusManager.Instance.BeforeMessageReceived += DBusManager_BeforeMessageReceived;
            DBusManager.Instance.BeforeMessageSent += DBusManager_BeforeMessageSent;
#endif


#if !NETMF && DebugOnRealDeviceOverFTDI
            if (!iBusPort.IsOpen)
                iBusPort.Open();
#endif

            FrontDisplay.RefreshLEDs(LedType.OrangeBlinking);

            //player = new iPodViaHeadset(Cpu.Pin.GPIO_NONE);
            //player = new BluetoothOVC3860(Serial.COM2/*, sd != null ? sd + @"\contacts.vcf" : null*/);
            Logger.Trace("Prepare creating VS1003Player");
            player = new VS1003Player(FEZPandaIII.Gpio.D25, FEZPandaIII.Gpio.D27, FEZPandaIII.Gpio.D24, FEZPandaIII.Gpio.D26);
            Logger.Trace("VS1003Player created");
            FrontDisplay.RefreshLEDs(LedType.Orange);
            if (settings.MenuMode != Tools.MenuMode.RadioCDC/* || Manager.FindDevice(DeviceAddress.OnBoardMonitor, 10000)*/)
            {
                //if (player is BluetoothWT32)
                //{
                //    ((BluetoothWT32)player).NowPlayingTagsSeparatedRows = true;
                //}
                if (settings.MenuMode == MenuMode.BordmonitorCDC)
                {
                    emulator = new CDChanger(player);
                    Logger.Trace("CDChanger media emulator created");
                    if (settings.NaviVersion == NaviVersion.MK2)
                    {
                        Localization.Current = new RadioLocalization();
                        SettingsScreen.Instance.CanChangeLanguage = false;
                    }
                    Bordmonitor.NaviVersion = settings.NaviVersion;
                    //BordmonitorMenu.FastMenuDrawing = settings.NaviVersion == NaviVersion.MK4;
                    BordmonitorMenu.Init(emulator);
                    Logger.Trace("BordmonitorMenu inited");
                    BluetoothScreen.Init();
                    Logger.Trace("BluetoothScreen inited");
                }
                else
                {
                    //emulator = new BordmonitorAUX(player);
                }

                //Bordmonitor.NaviVersion = settings.NaviVersion;
                //BordmonitorMenu.FastMenuDrawing = settings.NaviVersion == NaviVersion.MK4;
                //BordmonitorMenu.Init(emulator);

                Logger.Trace("Bordmonitor menu inited");
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

            //player.IsPlayingChanged += Player_IsPlayingChanged;
            //player.StatusChanged += Player_StatusChanged;
            //Logger.Info("Player events subscribed");

            successInited_GreenLed.Write(true);

            short getDateTimeTimeout = 1500;
#if OnBoardMonitorEmulator
            getDateTimeTimeout = 0;
#endif

            var dateTimeEventArgs = InstrumentClusterElectronics.GetDateTime(getDateTimeTimeout);
            if (!dateTimeEventArgs.DateIsSet)
            {
                Logger.Trace("Asking dateTime again");
               dateTimeEventArgs = InstrumentClusterElectronics.GetDateTime(getDateTimeTimeout);
            }
            Logger.Trace("dateTimeEventArgs.DateIsSet: " + dateTimeEventArgs.DateIsSet);
            Logger.Trace("Acquired dateTime from IKE: " + dateTimeEventArgs.Value);
            Utility.SetLocalTime(dateTimeEventArgs.Value);

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

            /* 
            var ign = new Message(DeviceAddress.InstrumentClusterElectronics, DeviceAddress.GlobalBroadcastAddress, "Ignition ACC", 0x11, 0x01);
            Manager.Instance.EnqueueMessage(ign);
            Manager.AddMessageReceiverForDestinationDevice(DeviceAddress.InstrumentClusterElectronics, m =>
            {
                if (m.Data.Compare(0x10))
                {
                    Manager.Instance.EnqueueMessage(ign);
                }
            });
            var b = Manager.FindDevice(DeviceAddress.Radio);
            */
        }

        static bool WatchDogResetByIKEInited = false;
        public static void InitWatchDogResetByIKEMessageReceiving()
        {
            if (_useWatchdog)
            {
                Manager.Instance.AddMessageReceiverForSourceDevice(DeviceAddress.InstrumentClusterElectronics, m =>
                {
                    if (m.Data[0] == 0x19 && m.Data.Length == 4) // Temperature
                    {
                        GHI.Processor.Watchdog.ResetCounter();
                    }
                });
                WatchDogResetByIKEInited = true;
                Logger.Trace("Watchdog reset started!");
            }
        }

        static bool WatchDogResetThreadInited = false;
        public static void InitWatchDogResetThread()
        {
            //Manager.AddMessageReceiverForSourceDevice(DeviceAddress.imBMWTest, (m) =>
            //{
            //    if (m.Data.Length > 1 && m.Data.StartsWith(0xEE, 0x00))
            //    {
            //        error = true;
            //    }
            //});
            //ActivateScreen.DisableWatchdogCounterReset += () =>
            //{
            //    error = true;
            //    RefreshLEDs(LedType.RedBlinking);
            //    Radio.PressOnOffToggle();
            //};

            //Manager.Instance.AddMessageReceiverForSourceDevice(DeviceAddress.Radio, (m) =>
            //{
            //    if (m.Data.Compare(MessageRegistry.DataPollResponse))
            //    {
            //        var e = RadioPollResponseReceived;
            //        if (e != null)
            //        {
            //            var args = new RadioPollEventArgs(m, true);
            //            e(args);
            //        }
            //    }
            //});

            if (_useWatchdog)
            {
                // Start a time counter reset thread
                WDTCounterReset = new Thread(WDTCounterResetLoop);
                WDTCounterReset.Start();
                WatchDogResetThreadInited = true;
                Logger.Trace("Watchdog reset thread started!");
            }
        }

        public static void DisposeWatchDogResetThread()
        {
            if (WDTCounterReset != null && WDTCounterReset.ThreadState != System.Threading.ThreadState.Suspended)
            {
                WDTCounterReset.Suspend();
                Logger.Trace("Watchdog reset thread suspended!");
            }
        }

        static Thread WDTCounterReset;
        static void WDTCounterResetLoop()
        {
            while (true)
            {
                GHI.Processor.Watchdog.ResetCounter();
                Thread.Sleep(watchDogTimeoutInMilliseconds / 5);

//                if (!error)
//                {
//                    byte retryCount = 3;
//                    do
//                    {
//                        LedBlinking(successInited_GreenLed, 3, 100);
//                        var radioPollEventArgs = PollRadio(_radioPollResponseWaitTimeout);
//                        if (radioPollEventArgs.ResponseReceived)
//                        {
//#if RELEASE
//                            GHI.Processor.Watchdog.ResetCounter();
//#endif
//                            LedBlinking(successInited_GreenLed, 1, 250);
//                            break;
//                        }
//                        retryCount--;
//                    } while (retryCount > 0);

//                    Thread.Sleep(watchDogTimeoutInMilliseconds - 10000);
//                }
            }
        }

//        private const int _radioPollResponseWaitTimeout = 2000;
//        private static ManualResetEvent _radioPollResponseSync = new ManualResetEvent(false);
//        private static RadioPollEventArgs _radioPollResult;
//        public delegate void RadioPollEventHandler(RadioPollEventArgs e);
//        public static event RadioPollEventHandler RadioPollResponseReceived;

//        static RadioPollEventArgs PollRadio(int timeout)
//        {
//            _radioPollResponseSync.Reset();
//            _radioPollResult = new RadioPollEventArgs(null, false);
//            RadioPollResponseReceived += RadioPollResponseCallback;
//            var pollRadioMessage = new Message(DeviceAddress.CDChanger, DeviceAddress.Radio, MessageRegistry.DataPollRequest);
//            Manager.Instance.EnqueueMessage(pollRadioMessage);
//#if NETMF
//            _radioPollResponseSync.WaitOne(timeout, true);
//#else
//            _radioPollResponseSync.WaitOne(timeout);
//#endif
//            RadioPollResponseReceived -= RadioPollResponseCallback;
//            return _radioPollResult;
//        }

//        private static void RadioPollResponseCallback(RadioPollEventArgs e)
//        {
//            _radioPollResult = e;
//            _radioPollResponseSync.Set();
//        }

//        public class RadioPollEventArgs
//        {
//            public Message ResponseMessage { get; private set; }

//            public bool ResponseReceived { get; private set; }

//            public RadioPollEventArgs(Message responseMessage, bool responseReceived = false)
//            {
//                ResponseMessage = responseMessage;
//                ResponseReceived = responseReceived;
//            }
//        }

        // Log just needed message
        private static bool IBusLoggerPredicate(MessageEventArgs e)
        {
            if (e.Message.SourceDevice == DeviceAddress.Radio && e.Message.DestinationDevice == DeviceAddress.CDChanger && e.Message.Data[0] == 0x01  // poll request
                //||
                //e.Message.SourceDevice == DeviceAddress.Radio && e.Message.DestinationDevice == DeviceAddress.GraphicsNavigationDriver && e.Message.Data.StartsWith(0x21, 0x60, 0x00)
                )
            {
                return false;
            }

            return
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
                   e.Message.SourceDevice == DeviceAddress.InstrumentClusterElectronics &&
                   e.Message.DestinationDevice == DeviceAddress.GlobalBroadcastAddress &&
                   e.Message.Data[0] == 0x19
                   ||
                   InstrumentClusterElectronics.CurrentIgnitionState == IgnitionState.Acc
                   ||
                   //e.Message.SourceDevice == DeviceAddress.MultiFunctionSteeringWheel &&
                   //e.Message.DestinationDevice == DeviceAddress.Radio
                   //|| 
#if DEBUG
                   true;
#else
                   false;
#endif
        }

        private static void Manager_BeforeMessageReceived(MessageEventArgs e)
        {
            iBusMessageSendReceiveBlinker_BlueLed.Write(Busy(true, 1));
        }

        private static void Manager_AfterMessageReceived(MessageEventArgs e)
        {
            iBusMessageSendReceiveBlinker_BlueLed.Write(Busy(false, 1));

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
                //e.Message.SourceDevice == DeviceAddress.IntegratedHeatingAndAirConditioning && e.Message.Data[0] == 0x83 // Air conditioning compressor status
                //|| 
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
                   e.Message.SourceDevice == DeviceAddress.InstrumentClusterElectronics 
                   ||
                   e.Message.SourceDevice == DeviceAddress.AuxilaryHeater ||
                   e.Message.DestinationDevice == DeviceAddress.AuxilaryHeater ||
                   e.Message.SourceDevice == DeviceAddress.IntegratedHeatingAndAirConditioning ||
                   e.Message.DestinationDevice == DeviceAddress.IntegratedHeatingAndAirConditioning ||
                   e.Message.SourceDevice == DeviceAddress.HeadlightVerticalAimControl ||
                   e.Message.DestinationDevice == DeviceAddress.HeadlightVerticalAimControl ||
                   e.Message.SourceDevice == DeviceAddress.Diagnostic ||
                   e.Message.DestinationDevice == DeviceAddress.Diagnostic || 
                   InstrumentClusterElectronics.CurrentIgnitionState == IgnitionState.Acc; 
        }

        private static void KBusManager_BeforeMessageReceived(MessageEventArgs e)
        {
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
                FrontDisplay.RefreshLEDs(LedType.Green);
                var logIco = "D < ";
                if (settings.LogMessageToASCII)
                {
                    Logger.Trace(e.Message.ToPrettyString(false, false), logIco);
                }
                else
                {
                    Logger.Trace(e.Message, logIco);
                }
                FrontDisplay.RefreshLEDs(LedType.Empty);
            }
        }

        private static void DBusManager_BeforeMessageSent(MessageEventArgs e)
        {
            if (DBusLoggerPredicate(e))
            {
                FrontDisplay.RefreshLEDs(LedType.Orange);
                var logIco = "D > ";
                if (settings.LogMessageToASCII)
                {
                    Logger.Trace(e.Message.ToPrettyString(false, false), logIco);
                }
                else
                {
                    Logger.Trace(e.Message, logIco);
                }
                FrontDisplay.RefreshLEDs(LedType.Empty);
            }
        }

        private static void Player_StatusChanged(IAudioPlayer player, string status, PlayerEvent playerEvent)
        {
            if (playerEvent == PlayerEvent.IncomingCall && !player.IsEnabled)
            {
                InstrumentClusterElectronics.Gong1();
            }
        }

        private static void Player_IsPlayingChanged(IAudioPlayer sender, bool isPlaying)
        {
            //RefreshLEDs();
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
