using GHI.IO.Storage;
using imBMW.Devices.V2.Hardware;
using imBMW.Enums;
using imBMW.Features;
using imBMW.Features.Localizations;
using imBMW.Features.Menu;
using imBMW.Features.Menu.Screens;
using imBMW.Features.Multimedia;
using imBMW.iBus;
using imBMW.iBus.Devices.Real;
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
using System.IO;
using imBMW.Features.Multimedia.iBus;
using GHI.Usb.Host;
using GHI.IO;
using imBMW.Features.CanBus.Adapters;
using imBMW.Features.CanBus;

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

        //internal static SDCard _massStorage;
        internal static MassStorage _massStorage;

        internal static AppState State { get; set; }

        private static Timer watchdogTimer;

        internal static int idleTime;
        internal static int overallTime;

        internal static int requestIgnitionStateTimerPeriod;

        // TODO: revert to 30 seconds, instead of 20 minutes
        internal static readonly int idleTimeout;
        internal static readonly int sleepTimeout;
        internal static readonly int overallTimeout;

        static bool ErrorExist = false;

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

            watchdogTimer?.Dispose();

            bool result = UnmountMassStorage();
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

            LedBlinkingQueueThreadWorker = new QueueThreadWorker(LedBlinking, QueueThreadName.LedBlinking);
            _removableMediaInsertedSync = new ManualResetEvent(false);
            requestIgnitionStateTimerPeriod = watchDogTimeoutInMilliseconds / 3;

            idleTimeout = GetTimeoutInMilliseconds(20, 00);
            sleepTimeout = GetTimeoutInMilliseconds(15, 20);
            overallTimeout = GetTimeoutInMilliseconds(60, 00);
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

                Logger.Debug("Controller events attaching");

                Controller.DeviceConnectFailed += (sss, eee) =>
                {
                    Logger.Error("DeviceConnectFailed!");
                    LedBlinkingQueueThreadWorker.Enqueue(new LedBlinkingItem(redLed, 1, 100));

                    MassStorageMountState = MassStorageMountState.DeviceConnectFailed;
                    _removableMediaInsertedSync.Set();
                };
                Logger.Debug("Controller.DeviceConnectFailed attached");

                Controller.UnknownDeviceConnected += (ss, ee) =>
                {
                    Logger.Error("UnknownDeviceConnected!");
                    LedBlinkingQueueThreadWorker.Enqueue(new LedBlinkingItem(redLed, 2, 100));

                    MassStorageMountState = MassStorageMountState.UnknownDeviceConnected;
                    _removableMediaInsertedSync.Set();
                };
                Logger.Debug("Controller.UnknownDeviceConnected attached");

                Controller.MassStorageConnected += (sender, massStorage) =>
                {
                    Logger.Debug("Controller MassStorageConnected!");
                    LedBlinkingQueueThreadWorker.Enqueue(new LedBlinkingItem(orangeLed, 2, 100));
                    MassStorageMountState = MassStorageMountState.MassStorageConnected;

                    if (_massStorage != null)
                    {
                        greenLed.Write(true);
                    }

                    Logger.Debug("Controller massStorage.Mount()");
                    _massStorage = massStorage;
                    massStorage.Mount();
                    Logger.Debug("Controller massStorage mounted");
                };
                Logger.Debug("Controller.MassStorageConnected attached");

                RemovableMedia.Insert += (s, e) =>
                {
                    Logger.Debug("RemovableMedia Inserted!");

                    LedBlinkingQueueThreadWorker.Enqueue(new LedBlinkingItem(orangeLed, 3, 100));

                    if (VolumeInfo.GetVolumes()[0].IsFormatted)
                    {
                        string rootDirectory = VolumeInfo.GetVolumes()[0].RootDirectory;
                        string logsPath = Path.Combine(rootDirectory, "logs");
                        string errorsPath = rootDirectory;

                        ErrorExist = CheckIfErrorsExist(rootDirectory);

                        Logger.Debug("Settings initing");
                        settings = Settings.Init(rootDirectory + "\\imBMW.ini");
                        Logger.Debug("Settings inited");

                        Logger.Debug("FileLogger initing");
                        FileLogger.Init(logsPath, errorsPath, () => VolumeInfo.GetVolumes()[0].FlushAll());
                        Logger.Debug("FileLogger inited");

                        MassStorageMountState = MassStorageMountState.Mounted;
                        _removableMediaInsertedSync.Set();
                    }
                    else
                    {
                        MassStorageMountState = MassStorageMountState.MountedButUnformatted;
                    }
                };
                Logger.Debug("RemovableMedia.Insert attached");

                RemovableMedia.Eject += (s, e) =>
                {
                    FileLogger.Eject();
                    Logger.Print("RemovableMedia Ejected!");
                    LedBlinkingQueueThreadWorker.Enqueue(new LedBlinkingItem(greenLed, 3, 100));
                    MassStorageMountState = MassStorageMountState.Unmounted;
                };
                Logger.Debug("RemovableMedia.Eject attached");
                #endregion

                try
                {
                    //_massStorage = new SDCard(SDCard.SDInterface.SPI);
                    //_massStorage.Mount();
#if RELEASE
                    Logger.Debug("Controller starting");
                    Controller.Start();
                    Logger.Debug("Controller started");
#else
#if NETMF
                    // WARNING! Be aware, without this line you can get 'Controller -> DeviceConnectFailed' each time when you start debugging...
                    if (Debugger.IsAttached)
#endif
                    {
                        Controller.Start();
                    }
#endif

                    LedBlinkingQueueThreadWorker.Enqueue(new LedBlinkingItem(orangeLed, 1, 200));

                    Logger.Debug("_removableMediaInsertedSync.WaitOne");
#if OnBoardMonitorEmulator
                    bool isSignalled = _removableMediaInsertedSync.WaitOne(Debugger.IsAttached ? 1000 : 1000, true);
#else
                    bool isSignalled = _removableMediaInsertedSync.WaitOne(Debugger.IsAttached ? 5000 : 5000, true);
                    Logger.Debug("_removableMediaInsertedSync.WaitOne: " + isSignalled);
#endif
                    if (!isSignalled) // No Storage inserted
                    {
                        FrontDisplay.RefreshLEDs(LedType.Red, append: true);
                        LedBlinkingQueueThreadWorker.Enqueue(new LedBlinkingItem(redLed, 3, 100));
                    }
                    else
                    {
                        if (MassStorageMountState == MassStorageMountState.DeviceConnectFailed || MassStorageMountState == MassStorageMountState.UnknownDeviceConnected)
                        {
                            //InstrumentClusterElectronics.ShowNormalTextWithGong(MassStorageMountState.ToStringValue(), mode: TextMode.InfiniteGong1OnIgnOff);
                            FrontDisplay.RefreshLEDs(LedType.RedBlinking, append: true);
                            LedBlinkingQueueThreadWorker.Enqueue(new LedBlinkingItem(redLed, 4, 100));
                            //ResetBoard();
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
                    if (!ErrorExist || _resetCause == GHI.Processor.Watchdog.ResetCause.Watchdog)
                    {
                        InstrumentClusterElectronics.ShowNormalTextWithGong(MassStorageMountState.ToStringValue()
                            + "; " + (_resetCause == GHI.Processor.Watchdog.ResetCause.Normal ? "Normal" : "Watchdog"), mode: TextMode.WithGong1);
                    }
                }

                Logger.Debug("wait some time, till items from file logger queue will removed, to release some amount of memory");
                bool waitResult = FileLogger.WaitTillQueueBeEmpty(1000);
                Logger.Debug("FileLogger.WaitTillQueueBeEmpty(1000): " + waitResult + " ; FileLogger.QueueCount: " + FileLogger.QueueCount);

                InstrumentClusterElectronics.RequestDateTime();

                Init();

                InitCAN();

                Logger.Debug("Started!");

                Logger.Debug("BordmonitorMenu events attaching");
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
                Logger.Debug("BordmonitorMenu.MenuButtonHold attached");

                BordmonitorMenu.SwitchScreenButtonHold += () =>
                {
                    UnmountMassStorage();
                    Logger.Warning("UNMOUNTED!");
                };
                Logger.Debug("BordmonitorMenu.SwitchScreenButtonHold attached");

                Logger.Debug("BodyModule events attaching");
                BodyModule.RemoteKeyButtonPressed += (e) =>
                {
                    idleTime = 0;
                    Logger.Trace("Clearing interval, because remote key pressed: idleTime = 0");
                };
                Logger.Debug("BodyModule.RemoteKeyButtonPressed attached");

                BodyModule.DoorWindowStatusChanged += (byte1, byte2) =>
                {
                    idleTime = 0;
                    Logger.Trace("Clearing interval, because door/window status changed: idleTime = 0");
                };
                Logger.Debug("BodyModule.DoorWindowStatusChanged attached");

                Manager.Instance.AddMessageReceiverForSourceDevice(DeviceAddress.InstrumentClusterElectronics, m =>
                {
                    if (m.Data[0] == 0x11 && m.Data.Length == 2) // Ignition status
                    {
                        GHI.Processor.Watchdog.ResetCounter();
                        if (watchdogIkeRequestSent)
                        {
                            Logger.Trace("Watchdog counter was resetted via response from IKE!");
                            watchdogIkeRequestSent = false;
                        }
                    }
                });
                watchdogTimer = new Timer(WatchdogTimerHandler, null, 0, requestIgnitionStateTimerPeriod);

                imBMWTestCommandsHandler();

                Logger.Debug("Actions inited!");

                byte count = 0;
                InterruptPort ldr1_button = new InterruptPort(FEZPandaIII.Gpio.Ldr1, true, Port.ResistorMode.PullUp, Port.InterruptMode.InterruptEdgeLow);
                ldr1_button.OnInterrupt += (uint data1, uint data2, DateTime time) =>
                {
                    if (++count == 4)
                    {
                        Logger.Debug("Manually unmounting storage, by pressing LDR1 button four times.");
                        UnmountMassStorage();
                    }
                    else
                    {
                        //DBusMessage getDataMessage = new DBusMessage(DeviceAddress.OBD, DeviceAddress.DDE,
                        //    new byte[] { 0x2C, 0x10 }
                        //        .Combine(DigitalDieselElectronics.admVDF)
                        //        .Combine(DigitalDieselElectronics.dzmNmit)
                        //        .Combine(DigitalDieselElectronics.ldmP_Lsoll)
                        //        .Combine(DigitalDieselElectronics.ldmP_Llin)
                        //        .Combine(DigitalDieselElectronics.ehmFLDS)
                        //        .Combine(DigitalDieselElectronics.zumPQsoll)
                        //        .Combine(DigitalDieselElectronics.zumP_RAIL)
                        //        .Combine(DigitalDieselElectronics.ehmFKDR)
                        //        .Combine(DigitalDieselElectronics.mrmM_EAKT)
                        //        .Combine(DigitalDieselElectronics.aroIST_4));

                        //DBusManager.Instance.EnqueueMessage(getDataMessage);
                        Emulator.Player.Next();
                    }
                };

                Logger.Debug("Going to Thread.Sleep(Timeout.Infinite)");
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

        private static void InitManagers()
        {
            var iBusComPort = Serial.COM1;
            var iBusBusy = Pin.TH3122SENSTA;
            var kBusBusy = Pin.K_BUS_TH3122SENSTA;

            var debugPin = new InputPort(FEZPandaIII.Gpio.D21, false, Port.ResistorMode.PullUp);
            bool isDebug = !debugPin.Read();
#if OnBoardMonitorEmulator
            LedBlinkingQueueThreadWorker.Enqueue(new LedBlinkingItem(greenLed, 2, 100));
            iBusComPort = "COM1";
            iBusBusy = Cpu.Pin.GPIO_NONE;
            kBusBusy = Cpu.Pin.GPIO_NONE;
#endif
            if (isDebug)
            {
                LedBlinkingQueueThreadWorker.Enqueue(new LedBlinkingItem(greenLed, 2, 100));
                iBusComPort = "COM1";
                iBusBusy = Cpu.Pin.GPIO_NONE;
                kBusBusy = Cpu.Pin.GPIO_NONE;
            }
#if NETMF
            Logger.Debug("iBusPort creating");
            ISerialPort iBusPort = new SerialPortTH3122(iBusComPort, iBusBusy);
            Logger.Debug("iBusPort created");
#endif
#if OnBoardMonitorEmulator
            ISerialPort iBusPort = new SerialPortTH3122(iBusComPort, iBusBusy, readBufferSize: ushort.MaxValue);
#endif
            Logger.Debug("Manager initing");
            Manager.Init(iBusPort);
            Logger.Debug("Manager inited with " + iBusComPort);

            Manager.Instance.BeforeMessageReceived += Manager_BeforeMessageReceived;
            Manager.Instance.AfterMessageReceived += Manager_AfterMessageReceived;
            Manager.Instance.BeforeMessageSent += Manager_BeforeMessageSent;
            Manager.Instance.AfterMessageSent += Manager_AfterMessageSent;
            Logger.Debug("Manager callbacks attached");


#if NETMF || (OnBoardMonitorEmulator && (DEBUG || DebugOnRealDeviceOverFTDI))

            Logger.Debug("kBusPort creating");
            string kBusComPort = Serial.COM2;
            ISerialPort kBusPort = new SerialPortTH3122(kBusComPort, kBusBusy);
            Logger.Debug("kBusPort created");

            Logger.Debug("KBusManager initing");
            KBusManager.Init(kBusPort, ThreadPriority.Normal);
            Logger.Debug("KBusManager inited with COM2");

            KBusManager.Instance.AfterMessageReceived += KBusManager_AfterMessageReceived;
            KBusManager.Instance.AfterMessageSent += KBusManager_AfterMessageSent;
            Logger.Debug("KBusManager callbacks attached");


            Logger.Debug("volumioPort creating");
            string volumioComPort = Serial.COM3;
            ISerialPort volumioPort = new SerialPortTH3122(volumioComPort, Cpu.Pin.GPIO_NONE);
            Logger.Debug("volumioPort created");

            Logger.Debug("VolumioManager initing");
            VolumioManager.Init(volumioPort, ThreadPriority.Normal);
            Logger.Debug("VolumioManager inited with COM3");

            VolumioManager.Instance.BeforeMessageReceived += VolumioManager_BeforeMessageReceived;
            VolumioManager.Instance.AfterMessageSent += VolumioManager_AfterMessageSent;
            Logger.Debug("VolumioManager callbacks attached");
#endif

            //#if NETMF || (OnBoardMonitorEmulator && DEBUG)
            //if (!isDebug)
            //{
            //    ISerialPort dBusPort = new SerialPortTH3122("COM4", Cpu.Pin.GPIO_NONE); // d31, d33
            //    //dBusPort.AfterWriteDelay = 4;
            //    DBusManager.Init(dBusPort, ThreadPriority.Normal);
            //    Logger.Debug("DBusManager inited with COM4");

            //    DBusManager.Instance.AfterMessageReceived += DBusManager_AfterMessageReceived;
            //    DBusManager.Instance.AfterMessageSent += DBusManager_AfterMessageSent;
            //}
            //#endif


#if !NETMF && DebugOnRealDeviceOverFTDI
            if (!iBusPort.IsOpen)
                iBusPort.Open();
#endif
        }

        public static void DisposeManagers()
        {
            Manager.Instance.BeforeMessageReceived -= Manager_BeforeMessageReceived;
            Manager.Instance.AfterMessageReceived -= Manager_AfterMessageReceived;
            Manager.Instance.BeforeMessageSent -= Manager_BeforeMessageSent;
            Manager.Instance.AfterMessageSent -= Manager_AfterMessageSent;
            Manager.Instance.Dispose();

            KBusManager.Instance.AfterMessageReceived -= KBusManager_AfterMessageReceived;
            KBusManager.Instance.AfterMessageSent -= KBusManager_AfterMessageSent;
            KBusManager.Instance.Dispose();

            VolumioManager.Instance.BeforeMessageReceived -= VolumioManager_BeforeMessageReceived;
            VolumioManager.Instance.AfterMessageSent -= VolumioManager_AfterMessageSent;
            VolumioManager.Instance.Dispose();

            //DBusManager.Instance.AfterMessageReceived -= DBusManager_AfterMessageReceived;
            //DBusManager.Instance.AfterMessageSent -= DBusManager_AfterMessageSent;
            //DBusManager.Instance.Dispose();
        }

        public static void Init()
        {
            //player = new iPodViaHeadset(Cpu.Pin.GPIO_NONE);
            //player = new BluetoothOVC3860(Serial.COM2/*, sd != null ? sd + @"\contacts.vcf" : null*/);
            Logger.Debug("VolumioRestApiPlayer creating");
            Cpu.Pin chipSelect_RT = FEZPandaIII.Gpio.D27;
            Cpu.Pin externalInterrupt_WS = FEZPandaIII.Gpio.D24;
            Cpu.Pin reset_BR = FEZPandaIII.Gpio.D26;
            //Player = new VolumioRestApiPlayer(chipSelect_RT, externalInterrupt_WS, reset_BR);
            Player = new VolumioUartPlayer();
            Logger.Debug("VolumioRestApiPlayer created");
            FrontDisplay.RefreshLEDs(LedType.GreenBlinking);
            if (settings.MenuMode != Tools.MenuMode.RadioCDC/* || Manager.FindDevice(DeviceAddress.OnBoardMonitor, 10000)*/)
            {
                //if (player is BluetoothWT32)
                //{
                //    ((BluetoothWT32)player).NowPlayingTagsSeparatedRows = true;
                //}
                if (settings.MenuMode == MenuMode.BordmonitorCDC)
                {
                    Logger.Debug("CDChanger media emulator creating");
                    Emulator = new CDChanger(Player);
                    Logger.Debug("CDChanger media emulator created");
                    if (settings.NaviVersion == NaviVersion.MK2)
                    {
                        Localization.Current = new RadioLocalization();
                        SettingsScreen.Instance.CanChangeLanguage = false;
                    }
                    Logger.Debug("Bordmonitor static constructor calling");
                    Bordmonitor.NaviVersion = settings.NaviVersion;
                    Logger.Debug("Bordmonitor class created");

                    //BordmonitorMenu.FastMenuDrawing = settings.NaviVersion == NaviVersion.MK4;

                    Logger.Debug("BordmonitorMenu initing");
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

        public static void InitCAN()
        {
            Logger.Debug("Going to InitCAN");

            Logger.Debug("CanNativeAdapter creating");
            var speed = CanAdapterSettings.CanSpeed.Kbps100;
            CanAdapter.Current = new CanNativeAdapter(ControllerAreaNetwork.Channel.One, speed);
            Logger.Debug("CanNativeAdapter created");

            CanAdapter.Current.MessageReceived += Can_MessageReceived;
            CanAdapter.Current.MessageSent += Can_MessageSent;
            CanAdapter.Current.Error += Can_ErrorReceived;

            Logger.Debug("CanAdapter.Current enabling");
            CanAdapter.Current.IsEnabled = true;
            Logger.Debug("CanAdapter.Current enabled");
        }

        private static bool watchdogIkeRequestSent = false;
        internal static void WatchdogTimerHandler(object obj)
        {
            Logger.Trace("idleTime: " + GetTimeSpanFromMilliseconds(idleTime));
            Logger.Trace("overallTime: " + GetTimeSpanFromMilliseconds(overallTime));

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

            bool sleepByTimeout = idleTime >= sleepTimeout;
            bool sleepByOverallTimeout = overallTime >= overallTimeout;

            if (sleepByTimeout || sleepByOverallTimeout)
            {
                lock (lockObj)
                {
                    if (State != AppState.Sleep)
                    {
                        if (sleepByOverallTimeout)
                        {
                            Logger.FatalError(ErrorIdentifier.SleepModeFlowBrokenErrorId);
                        }
                        SleepMode();
                    }
                }
            }

            if (InstrumentClusterElectronics.CurrentIgnitionState == IgnitionState.Off)
            {
                idleTime += requestIgnitionStateTimerPeriod;
                overallTime += requestIgnitionStateTimerPeriod;
            }
            else
            {
                idleTime = 0;
                overallTime = 0;
            }

            if ((!Settings.Instance.WatchdogResetOnIKEResponse || InstrumentClusterElectronics.CurrentIgnitionState == IgnitionState.Off) && InstrumentClusterElectronics.CurrentIgnitionState != IgnitionState.Unknown)
            {
                GHI.Processor.Watchdog.ResetCounter();
                Logger.Trace("Watchdog counter was resetted via simple timer!");
            }
            else
            {
                watchdogIkeRequestSent = true;
                Logger.Trace("Going to request ignition status to reset watchdog counter");
                InstrumentClusterElectronics.RequestIgnitionStatus();
            }

            if (InstrumentClusterElectronics.CurrentIgnitionState == IgnitionState.Ign)
            {
                Thread.Sleep(1000);
                LightControlModule.UpdateThermalOilLevelSensorValues();
            }
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
            ActionString volumioUartPlayerShuttedDown = message => waitHandle.Set();
            VolumioUartPlayer.ShuttedDown += volumioUartPlayerShuttedDown;
            VolumioUartPlayer.Shutdown();
            bool result = waitHandle.WaitOne(5000, true);
            Logger.Trace("Shutdown waitHandle result: " + result);
            VolumioUartPlayer.ShuttedDown -= volumioUartPlayerShuttedDown;

            UnmountMassStorage();
            DisposeManagers();

            State = AppState.Sleep;
        }

        internal static bool UnmountMassStorage()
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

                bool result = waitHandle.WaitOne(3000, true);
                greenLed.Write(false);
                return result;
            }

            greenLed.Write(false);
            Thread.Sleep(200);
            greenLed.Write(true);
            return false;
        }

        private static void DateTimeChanged(DateTimeEventArgs e)
        {
            if (DateTime.Now.Year < 2020)
            {
                Logger.Debug("Acquired dateTime from IKE: " + e.Value);
            }
            Utility.SetLocalTime(e.Value);
            //InstrumentClusterElectronics.DateTimeChanged -= DateTimeChanged;
        }

        private static bool CheckIfErrorsExist(string rootDirectory)
        {
            FileStream dataFile = null;
            try
            {
                dataFile = File.Open(Path.Combine(rootDirectory, FileLogger.ERROR_FILE_NAME), FileMode.OpenOrCreate);
                if (dataFile != null && dataFile.Length > 1)
                {
                    InstrumentClusterElectronics.ShowNormalTextWithGong("ERRORS FOUND. CHECK LOG!", mode: TextMode.InfiniteGong1OnIgnOff);
                    return true;
                }
            }
            finally
            {
                if (dataFile != null)
                {
                    dataFile.Close();
                }
            }
            return false;
        }

        private static void Can_MessageSent(CanAdapter can, CanMessage message)
        {
            Logger.Trace(message.ToString(), "CAN>");
        }

        private static void Can_ErrorReceived(CanAdapter can, string message)
        {
            Logger.Trace(message, "CAN-ERR");
        }

        private static bool value;
        private static void Can_MessageReceived(CanAdapter can, CanMessage message)
        {
            value = !value;
            blueLed.Write(value);
            Logger.Trace(message.ToString(), "CAN<");
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
				//|| e.Message.Data[0] == 0x23    // display text
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

        private static bool IBusMessageLoggingBeforeReceivedPredicate(MessageEventArgs e)
        {
            if (e.Message.Data[0] == 0x38) // RAD > CDC: 38 XX XX
            {
                return true;
            }

            return false;
        }

        // sometimes, LCM can send message, to clear idleTime, but body module not reset his counter for idle
        // see: \traces\2020.09.09\ #53, #7, #13, #16, #25, #30, #39
        //private static bool ShouldClearIdleTime(Message m)
        //{
        //    if (m.Data[0] == 0x5C || // Instrument cluster lighting status
        //        m.Data[0] == 0xA7 || // TMC status request
        //        m.Data[0] == 0xA8 || // TMC data

        //        // [I < ] CCM > GLO: 02 00 [Poll response] (see  "traces\2020.10.04_GarageRacer\traceLog34.log"  and some other in this folder)
        //        m.SourceDevice == DeviceAddress.CheckControlModule && m.SourceDevice == DeviceAddress.GlobalBroadcastAddress && m.Data[0] == 0x02
        //    ) 
        //    {
        //        return false; 

        //    }

        //    return true;
        //}

        internal static void Manager_BeforeMessageReceived(MessageEventArgs e)
        {
            //if (ShouldClearIdleTime(e.Message))
            //{
            //    idleTime = 0;
            //}

            if (IBusMessageLoggingBeforeReceivedPredicate(e))
            {
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

        private static void Manager_AfterMessageReceived(MessageEventArgs e)
        {
            if (IBusLoggerPredicate(e) && IBusReaderPredicate(e) && !IBusMessageLoggingBeforeReceivedPredicate(e))
            {
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
            //blueLed.Write(Busy(true, 2));
        }

        private static void Manager_AfterMessageSent(MessageEventArgs e)
        {
            //blueLed.Write(Busy(false, 2));

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
            //if (ShouldClearIdleTime(e.Message))
            //{
            //    idleTime = 0;
            //}

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

        private static void VolumioManager_BeforeMessageReceived(MessageEventArgs e)
        {
            var logIco = "V < ";
            if (settings.LogMessageToASCII)
            {
                Logger.Trace(e.Message.ToPrettyString(false, false), logIco);
            }
            else
            {
                Logger.Trace(e.Message, logIco);
            }
        }

        private static void VolumioManager_AfterMessageSent(MessageEventArgs e)
        {
            var logIco = "V > ";
            if (settings.LogMessageToASCII)
            {
                Logger.Trace(e.Message.ToPrettyString(false, false), logIco);
            }
            else
            {
                Logger.Trace(e.Message, logIco);
            }
        }

        private static void DBusManager_AfterMessageReceived(MessageEventArgs e)
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

            orangeLed.Write(true);
            FrontDisplay.RefreshLEDs(LedType.Green, append: true);
            Thread.Sleep(250);
            orangeLed.Write(false);
            FrontDisplay.RefreshLEDs(LedType.Green, remove: true);
        }

        private static void DBusManager_AfterMessageSent(MessageEventArgs e)
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

            orangeLed.Write(true);
            FrontDisplay.RefreshLEDs(LedType.Orange, append: true);
            Thread.Sleep(250);
            orangeLed.Write(false);
            FrontDisplay.RefreshLEDs(LedType.Orange, remove: true);
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


        private static void imBMWTestCommandsHandler()
        {
            Manager.Instance.AddMessageReceiverForSourceDevice(DeviceAddress.imBMWTest, m =>
            {
                if (m.Data[0] == 0x02)
                {
                    SleepMode();
                }

                if (m.Data[0] == 0x03)
                {
                    //VolumioManager.Instance.EnqueueMessage(new Message(DeviceAddress.AirBagModule, DeviceAddress.BodyModule, 0x91));
                }
            });
        }
    }
}
