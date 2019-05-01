using GHI.IO.Storage;
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

        static InterruptPort nextButton;
        static InterruptPort prevButton;

        public static SDCard sd_card;
        private static string rootDirectory;

        private static ManualResetEvent _removableMediaInsertedSync = new ManualResetEvent(false);

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
            RefreshLEDs(LedType.OrangeBlinking);
            Thread.Sleep(200); // wait to add pause after previous blinking
            LedBlinking(orangeLed, 5, 200);

            if (_massStorage != null && _massStorage.Mounted)
            {
                _massStorage.Unmount();
                Thread.Sleep(200);
            }

#if DEBUG
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
                //if (!Debugger.IsAttached)
                //{
                //    Thread.Sleep(60000);
                //}
#endif

                BluetoothScreen.BluetoothChargingState = true;
                BluetoothScreen.AudioSource = AudioSource.Bluetooth;

                _resetCause = GHI.Processor.Watchdog.LastResetCause;

                iBusMessageSendReceiveBlinker_BlueLed = new OutputPort(Pin.LED1, false);
                successInited_GreenLed = new OutputPort(Pin.LED2, false);
                orangeLed = new OutputPort(Pin.LED3, _resetCause == GHI.Processor.Watchdog.ResetCause.Watchdog);
                error_RedLed = new OutputPort(Pin.LED4, false);

#if RELEASE
                GHI.Processor.Watchdog.Enable(watchDogTimeoutInMilliseconds);
#endif

                SDCard sd = null;
                settings = Settings.Init(sd != null ? sd + @"\imBMW.ini" : null);

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
                    LedBlinking(error_RedLed, 5, 100);
                    ResetBoard();
                };
                Controller.UnknownDeviceConnected += (ss, ee) => ResetBoard();
                Controller.MassStorageConnected += (sender, massStorage) =>
                {
                    LedBlinking(orangeLed, 2, 100);
                    RemovableMedia.Insert += (s, e) =>
                    {
                        LedBlinking(successInited_GreenLed, 2, 100);
                        _removableMediaInsertedSync.Set();
                    };

                    _massStorage = massStorage;
                    massStorage.Mount();

                };
#if RELEASE
                Controller.Start();
#else
                if (Debugger.IsAttached)
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
                    LedBlinking(successInited_GreenLed, 3, 100);
                }

                rootDirectory = VolumeInfo.GetVolumes()[0].RootDirectory;

                //Logger.Logged += Logger_Logged;
                FileLogger.Init(rootDirectory + "\\" + "logs", () => VolumeInfo.GetVolumes()[0].FlushAll());

                Logger.Trace("\n\n\n\n\n");
                Logger.Trace("Watchdog.ResetCause: " + _resetCause.ToStringValue());

                Init();

                Logger.Trace("Started!");

#if NETMF
                InitWatchDogReset();
#endif

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
#if OnBoardMonitorEmulator && DEBUG
            ISerialPort iBusPort = new SerialPortTH3122(iBusComPort, iBusBusy, readBufferSize:ushort.MaxValue);
#endif
            Manager.Init(iBusPort);
            Logger.Trace("Manager inited");

            Manager.BeforeMessageReceived += Manager_BeforeMessageReceived;
            Manager.AfterMessageReceived += Manager_AfterMessageReceived;
            Manager.BeforeMessageSent += Manager_BeforeMessageSent;
            Manager.AfterMessageSent += Manager_AfterMessageSent;

#if NETMF || DEBUG
            string kBusComPort = Serial.COM2;
            ISerialPort kBusPort = new SerialPortTH3122(kBusComPort, kBusBusy);
            KBusManager.Init(kBusPort);
            Logger.Trace("KBusManager inited");

            KBusManager.Instance.BeforeMessageReceived += KBusManager_BeforeMessageReceived;
            KBusManager.Instance.BeforeMessageSent += KBusManager_BeforeMessageSent;

            ISerialPort dBusPort = new SerialPortTH3122("COM4", Cpu.Pin.GPIO_NONE, writeBufferSize: 1); // d31, d33
            dBusPort.AfterWriteDelay = 4;
            DBusManager.Init(dBusPort);
            Logger.Trace("DBusManager inited");

            DBusManager.Instance.BeforeMessageReceived += DBusManager_BeforeMessageReceived;
            DBusManager.Instance.BeforeMessageSent += DBusManager_BeforeMessageSent;
#endif

#if !NETMF && DebugOnRealDeviceOverFTDI
            if (!iBusPort.IsOpen)
                iBusPort.Open();
#endif

            RefreshLEDs(LedType.OrangeBlinking);

            //player = new iPodViaHeadset(Cpu.Pin.GPIO_NONE);
            //player = new BluetoothOVC3860(Serial.COM2/*, sd != null ? sd + @"\contacts.vcf" : null*/);
            Logger.Trace("Prepare creating VS1003Player");
            player = new VS1003Player(FEZPandaIII.Gpio.D25, FEZPandaIII.Gpio.D27, FEZPandaIII.Gpio.D24, FEZPandaIII.Gpio.D26);
            Logger.Trace("VS1003Player created");
            RefreshLEDs(LedType.Orange);
            if (settings.MenuMode != Tools.MenuMode.RadioCDC || Manager.FindDevice(DeviceAddress.OnBoardMonitor, 10000))
            {
                //if (player is BluetoothWT32)
                //{
                //    ((BluetoothWT32)player).NowPlayingTagsSeparatedRows = true;
                //}
                if (settings.MenuMode == MenuMode.BordmonitorCDC)
                {
                    emulator = new CDChanger(player);
                    if (settings.NaviVersion == NaviVersion.MK2)
                    {
                        Localization.Current = new RadioLocalization();
                        SettingsScreen.Instance.CanChangeLanguage = false;
                    }
                    Bordmonitor.NaviVersion = settings.NaviVersion;
                    //BordmonitorMenu.FastMenuDrawing = settings.NaviVersion == NaviVersion.MK4;
                    BordmonitorMenu.Init(emulator);
                    BluetoothScreen.Init();
                    BordmonitorMenu.ResetButtonPressed += () =>
                    {
                        emulator.PlayerIsPlayingChanged += (s, isPlayingChangedValue) =>
                        {
                            if (!isPlayingChangedValue)
                            {
                                ResetBoard();
                            }
                        };
                        RefreshLEDs(LedType.Empty);
                        Radio.PressOnOffToggle();
                    };
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

            RefreshLEDs(LedType.Green);
			RefreshLEDs(LedType.Green);

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
            //    //Manager.EnqueueMessage(new Message(DeviceAddress.Radio, DeviceAddress.CDChanger, CDChanger.DataSelectDisk6));
            //    //emulator.Player.IsRandom = false;
            //    //emulator.Player.DiskNumber = 6;
            //    emulator.IsEnabled = false;
            //};

            /* 
            var ign = new Message(DeviceAddress.InstrumentClusterElectronics, DeviceAddress.GlobalBroadcastAddress, "Ignition ACC", 0x11, 0x01);
            Manager.EnqueueMessage(ign);
            Manager.AddMessageReceiverForDestinationDevice(DeviceAddress.InstrumentClusterElectronics, m =>
            {
                if (m.Data.Compare(0x10))
                {
                    Manager.EnqueueMessage(ign);
                }
            });
            var b = Manager.FindDevice(DeviceAddress.Radio);
            */
        }

        public static void InitWatchDogReset()
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

            Manager.AddMessageReceiverForSourceDevice(DeviceAddress.Radio, (m) =>
            {
                if (m.Data.Compare(MessageRegistry.DataPollResponse))
                {
                    var e = RadioPollResponseReceived;
                    if (e != null)
                    {
                        var args = new RadioPollEventArgs(m, true);
                        e(args);
                    }
                }
            });

            // Start a time counter reset thread
            WDTCounterReset = new Thread(WDTCounterResetLoop);
            WDTCounterReset.Start();
        }

        static Thread WDTCounterReset;
        static void WDTCounterResetLoop()
        {
            while (true)
            {
                if (!error)
                {
                    byte retryCount = 3;
                    do
                    {
                        LedBlinking(orangeLed, 3, 100);
                        var radioPollEventArgs = PollRadio(_radioPollResponseWaitTimeout);
                        if (radioPollEventArgs.ResponseReceived)
                        {
#if RELEASE
                            GHI.Processor.Watchdog.ResetCounter();
#endif
                            LedBlinking(orangeLed, 1, 250);
                            break;
                        }
                        retryCount--;
                    } while (retryCount > 0);

                    Thread.Sleep(watchDogTimeoutInMilliseconds - 10000);
                }
            }
        }

        private const int _radioPollResponseWaitTimeout = 2000;
        private static ManualResetEvent _radioPollResponseSync = new ManualResetEvent(false);
        private static RadioPollEventArgs _radioPollResult;
        public delegate void RadioPollEventHandler(RadioPollEventArgs e);
        public static event RadioPollEventHandler RadioPollResponseReceived;

        static RadioPollEventArgs PollRadio(int timeout)
        {
            _radioPollResponseSync.Reset();
            _radioPollResult = new RadioPollEventArgs(null, false);
            RadioPollResponseReceived += RadioPollResponseCallback;
            var pollRadioMessage = new Message(DeviceAddress.CDChanger, DeviceAddress.Radio, MessageRegistry.DataPollRequest);
            Manager.EnqueueMessage(pollRadioMessage);
#if NETMF
            _radioPollResponseSync.WaitOne(timeout, true);
#else
            _radioPollResponseSync.WaitOne(timeout);
#endif
            RadioPollResponseReceived -= RadioPollResponseCallback;
            return _radioPollResult;
        }

        private static void RadioPollResponseCallback(RadioPollEventArgs e)
        {
            _radioPollResult = e;
            _radioPollResponseSync.Set();
        }

        public class RadioPollEventArgs
        {
            public Message ResponseMessage { get; private set; }

            public bool ResponseReceived { get; private set; }

            public RadioPollEventArgs(Message responseMessage, bool responseReceived = false)
            {
                ResponseMessage = responseMessage;
                ResponseReceived = responseReceived;
            }
        }

        // Log just needed message
        private static bool IBusLoggerPredicate(MessageEventArgs e)
        {
            if (e.Message.SourceDevice == DeviceAddress.Radio && e.Message.DestinationDevice == DeviceAddress.CDChanger && e.Message.Data[0] == 0x01) // poll request
            {
                return false;
            }

            return e.Message.SourceDevice == DeviceAddress.Radio && e.Message.DestinationDevice == DeviceAddress.CDChanger
                   ||
                   e.Message.SourceDevice == DeviceAddress.CDChanger && e.Message.DestinationDevice == DeviceAddress.Radio
                   ||
                   e.Message.SourceDevice == DeviceAddress.Radio && e.Message.DestinationDevice == DeviceAddress.InstrumentClusterElectronics
                   ||
                   e.Message.SourceDevice == DeviceAddress.InstrumentClusterElectronics && e.Message.DestinationDevice == DeviceAddress.FrontDisplay
                   ||
                   e.Message.SourceDevice == DeviceAddress.InstrumentClusterElectronics && e.Message.DestinationDevice == DeviceAddress.Broadcast;
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
            if (e.Message.SourceDevice == DeviceAddress.IntegratedHeatingAndAirConditioning && e.Message.Data[0] == 0x83 // Air conditioning compressor status
                || 
                e.Message.SourceDevice == DeviceAddress.IntegratedHeatingAndAirConditioning && e.Message.Data[0] == 0x86 // Some info for NavigationEurope
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
                   e.Message.SourceDevice == DeviceAddress.AuxilaryHeater ||
                   e.Message.DestinationDevice == DeviceAddress.AuxilaryHeater ||
                   e.Message.SourceDevice == DeviceAddress.IntegratedHeatingAndAirConditioning ||
                   e.Message.DestinationDevice == DeviceAddress.IntegratedHeatingAndAirConditioning ||
                   e.Message.SourceDevice == DeviceAddress.HeadlightVerticalAimControl ||
                   e.Message.DestinationDevice == DeviceAddress.HeadlightVerticalAimControl ||
                   e.Message.SourceDevice == DeviceAddress.Diagnostic ||
                   e.Message.DestinationDevice == DeviceAddress.Diagnostic
                   //|| (
                   //    e.Message.SourceDevice == DeviceAddress.InstrumentClusterElectronics && e.Message.DestinationDevice == DeviceAddress.GlobalBroadcastAddress
                   //    || e.Message.SourceDevice == DeviceAddress.InstrumentClusterElectronics && e.Message.DestinationDevice == DeviceAddress.Broadcast
                   //)
                   ; 
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
                        traceFile = new StreamWriter(filePath                            , append:true);
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

        public enum LedType : byte
        {
            Red = 1,
            RedBlinking = 2,
            Orange = 4,
            OrangeBlinking = 8,
            Green = 16,
            GreenBlinking = 32,
            Empty = 64
        }

        static void RefreshLEDs(LedType ledType)
        {
            if (!Manager.Inited)
            {
                return;
            }
            byte b = (byte)ledType;
            if (error || Logger.wasError)
            {
                b = b.AddBit(0);
            }
            //if (blinkerOn)
            //{
            //    //b = b.AddBit(2);
            //}
            //if (player != null/* && player.IsPlaying*/)
            //{
            //    b = b.AddBit(4);
            //}
            var message = new Message(DeviceAddress.Telephone, DeviceAddress.FrontDisplay, "Set LEDs", 0x2B, b);
            Manager.EnqueueMessage(message);
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
