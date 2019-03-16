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

                Logger.FreeMemory();
                _resetCause = GHI.Processor.Watchdog.LastResetCause;

                iBusMessageSendReceiveBlinker_BlueLed = new OutputPort(Pin.LED1, false);
                successInited_GreenLed = new OutputPort(Pin.LED2, false);
                orangeLed = new OutputPort(Pin.LED3, _resetCause == GHI.Processor.Watchdog.ResetCause.Watchdog);
                error_RedLed = new OutputPort(Pin.LED4, false);

                // Timeout 30 seconds
                ushort timeoutInMilliseconds = 1000 * 20;
#if !RELEASE
                if (Debugger.IsAttached)
                {
                    try
                    {
                        //GHI.Processor.Watchdog.Enable(timeoutInMilliseconds * 2);
                    }
                    catch (Exception ex)
                    {
                    }
                }
                else
                {
                    //GHI.Processor.Watchdog.Enable(timeoutInMilliseconds);
                }
#else
                GHI.Processor.Watchdog.Enable(timeoutInMilliseconds);
#endif

                Logger.FreeMemory();
                SDCard sd = null;
                settings = Settings.Init(sd != null ? sd + @"\imBMW.ini" : null);
                Logger.FreeMemory();

                //SettingsScreen.Instance.Status = version.Length > 11 ? version.Replace(" ", "") : version;
                //Localization.SetCurrent(RussianLocalization.SystemName); //Localization.SetCurrent(settings.Language);
                //Features.Comfort.AutoLockDoors = settings.AutoLockDoors;
                //Features.Comfort.AutoUnlockDoors = settings.AutoUnlockDoors;
                //Features.Comfort.AutoCloseWindows = settings.AutoCloseWindows;
                //Features.Comfort.AutoCloseSunroof = settings.AutoCloseSunroof;

                byte sdCardMountRetryCount = 0;

                /*RemovableMedia.Insert += (a, b) =>
                {
                    _removableMediaInsertedSync.Set();
                };

                Logger.FreeMemory();
                do
                {
                    try
                    {
                        sd_card = new SDCard(SDCard.SDInterface.SPI);
                        sd_card.Mount();

                        error = false;
                        break;
                    }
                    catch (Exception ex)
                    {
                        sdCardMountRetryCount++;
                        error = true;

                        LedBlinking(error_RedLed, 5, 100);
                        Thread.Sleep(100);
                    }
                } while (sdCardMountRetryCount < 3);
                
                bool isSignalled = !error && _removableMediaInsertedSync.WaitOne(5000, true);
                if (!isSignalled)
                {
                    ResetBoard();
                }*/



                Controller.DeviceConnectFailed += (sss, eee) =>
                {
                    ResetBoard();
                };
                Controller.UnknownDeviceConnected += (ss, ee) =>
                {
                    ResetBoard();
                };
                Controller.MassStorageConnected += (sender, massStorage) =>
                {
                    LedBlinking(iBusMessageSendReceiveBlinker_BlueLed, 1, 100);
                    RemovableMedia.Insert += (s, e) =>
                    {
                        LedBlinking(successInited_GreenLed, 1, 100);
                        _removableMediaInsertedSync.Set();
                    };

                    do
                    {
                        try
                        {
                            massStorage.Mount();
                            error = false;
                            break;
                        }
                        catch (Exception ex)
                        {
                            sdCardMountRetryCount++;
                            error = true;

                            LedBlinking(error_RedLed, 5, 100);
                            Thread.Sleep(100);
                        }
                    } while (sdCardMountRetryCount < 3);
                };
                if (Debugger.IsAttached)
                {
                    Controller.Start();
                }
                else
                {
#if RELEASE
                    Controller.Start();
#endif
                }

                bool isSignalled = !error && _removableMediaInsertedSync.WaitOne(10000, true);
                if (!isSignalled)
                {
                    ResetBoard();
                }

                rootDirectory = VolumeInfo.GetVolumes()[0].RootDirectory;
                Logger.FreeMemory();

                Logger.Logged += Logger_Logged;
                Logger.Trace("\n\n\n\n\n");
                Logger.Trace("Logger inited! sdCardMountRetryCount:" + sdCardMountRetryCount);
                Logger.Trace("Watchdog.ResetCause: " + _resetCause.ToStringValue());

                Init();

                Logger.Trace("Started!");

#if RELEASE
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
#if NETMF
            ISerialPort iBusPort = new SerialPortTH3122(iBusComPort, iBusBusy);
#endif
#if OnBoardMonitorEmulator
            ISerialPort iBusPort = new SerialPortTH3122(iBusComPort, iBusBusy, readBufferSize:byte.MaxValue);
#endif
            Manager.Init(iBusPort);
            Logger.Trace("Manager inited");

            string kBusComPort = Serial.COM2;
            ISerialPort kBusPort = new SerialPortTH3122(kBusComPort, kBusBusy);
            KBusManager.Init(kBusPort);
            Logger.Trace("KBusManager inited");

            ISerialPort dBusPort = new SerialPortTH3122("COM4", Cpu.Pin.GPIO_NONE, writeBufferSize: 1); // d31, d33
            dBusPort.AfterWriteDelay = 4;
            DBusManager.Init(dBusPort);
            Logger.Trace("DBusManager inited");

#if !NETMF && DebugOnRealDeviceOverFTDI
            if (!iBusPort.IsOpen)
                iBusPort.Open();
#endif
            Manager.BeforeMessageReceived += Manager_BeforeMessageReceived;
            Manager.AfterMessageReceived += Manager_AfterMessageReceived;
            Manager.BeforeMessageSent += Manager_BeforeMessageSent;
            Manager.AfterMessageSent += Manager_AfterMessageSent;

            KBusManager.Instance.BeforeMessageReceived += KBusManager_BeforeMessageReceived;
            KBusManager.Instance.BeforeMessageSent += KBusManager_BeforeMessageSent;

            DBusManager.Instance.BeforeMessageReceived += DBusManager_BeforeMessageReceived;
            DBusManager.Instance.BeforeMessageSent += DBusManager_BeforeMessageSent;

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
                    Logger.FreeMemory();
                    emulator = new CDChanger(player);
                    Logger.FreeMemory();
                    if (settings.NaviVersion == NaviVersion.MK2)
                    {
                        Logger.FreeMemory();
                        Localization.Current = new RadioLocalization();
                        SettingsScreen.Instance.CanChangeLanguage = false;
                        Logger.FreeMemory();
                    }
                    Bordmonitor.NaviVersion = settings.NaviVersion;
                    //BordmonitorMenu.FastMenuDrawing = settings.NaviVersion == NaviVersion.MK4;
                    Logger.FreeMemory();
                    BordmonitorMenu.Init(emulator);
                    BluetoothScreen.Init();
                    Logger.FreeMemory();
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

            var dateTimeEventArgs = InstrumentClusterElectronics.GetDateTime(1500);
            if (!dateTimeEventArgs.DateIsSet)
            {
                Logger.Trace("Ask dateTime again");
               dateTimeEventArgs = InstrumentClusterElectronics.GetDateTime(1500);
            }
            Logger.Trace("dateTimeEventArgs.DateIsSet: " + dateTimeEventArgs.DateIsSet);
            Logger.Trace("Aquired dateTime from IKE: " + dateTimeEventArgs.Value);
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
            Manager.AddMessageReceiverForSourceDevice(DeviceAddress.imBMWTest, (m) =>
            {
                if (m.Data.Length > 1 && m.Data.StartsWith(0xEE, 0x00))
                {
                    error = true;
                }
            });
            ActivateScreen.DisableWatchdogCounterReset += () =>
            {
                error = true;
                RefreshLEDs(LedType.RedBlinking);
                Radio.PressOnOffToggle();
            };

            // Start a time counter reset thread
            WDTCounterReset = new Thread(WDTCounterResetLoop);
            WDTCounterReset.Start();
        }

        static Thread WDTCounterReset;
        static void WDTCounterResetLoop()
        {
            while (true)
            {
                // reset time counter every 5 seconds
                Thread.Sleep(1000);

                if (!error)
                {
                    LedBlinking(orangeLed, 1, 100);
                    GHI.Processor.Watchdog.ResetCounter();
                }
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
                        traceFile = new StreamWriter(
                            rootDirectory + "\\" + traceFileName + (traceFileNameNumber == 0 ? "" : traceFileNameNumber.ToString()) + ".txt", append: true);
                    }
                    catch
                    {
                        traceFileNameNumber++;
                    }
                    finally
                    {
                        if (traceFile != null)
                        {
                            traceFile.WriteLine(args.LogString);
                            traceFile.Flush();
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
