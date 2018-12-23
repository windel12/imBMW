using GHI.IO.Storage;
using GHI.Pins;
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
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using Microsoft.SPOT.IO;
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Threading;
using imBMW.Diagnostics;
using Debug = Microsoft.SPOT.Debug;

namespace imBMW.Devices.V2
{
    public class Launcher
    {
        const string version = "FW1.0.12 HW2";
        static OutputPort LED;
        static OutputPort successInitLED;
        static OutputPort errorLED;
        static OutputPort resetPin;

        static Settings settings;
        public static MediaEmulator emulator;
        public static IAudioPlayer player;

        static InterruptPort nextButton;
        static InterruptPort prevButton;

        public static SDCard sd_card;
        private static string rootDirectory;

        public enum LaunchMode
        {
            MicroFramework,
            WPF
        }

        public static void Launch(LaunchMode launchMode = LaunchMode.MicroFramework)
        {
            try
            {
                SDCard sd = null;
                settings = Settings.Init(sd != null ? sd + @"\imBMW.ini" : null);
                LED = new OutputPort(Pin.LED, false);
                successInitLED = new OutputPort(FEZPandaIII.Gpio.Led2, false);
                errorLED = new OutputPort(Pin.LED4, false);
                resetPin = new OutputPort(Pin.ResetPin, true);

                //SettingsScreen.Instance.Status = version.Length > 11 ? version.Replace(" ", "") : version;
                //Localization.SetCurrent(RussianLocalization.SystemName); //Localization.SetCurrent(settings.Language);
                //Features.Comfort.AutoLockDoors = settings.AutoLockDoors;
                //Features.Comfort.AutoUnlockDoors = settings.AutoUnlockDoors;
                //Features.Comfort.AutoCloseWindows = settings.AutoCloseWindows;
                //Features.Comfort.AutoCloseSunroof = settings.AutoCloseSunroof;

                int sdCardMountRetryCount = 0;
                do
                {
                    try
                    {
                        sd_card = new SDCard(SDCard.SDInterface.MCI);
                        sd_card.Mount();
                        rootDirectory = VolumeInfo.GetVolumes()[0].RootDirectory;
                        break;
                    }
                    catch (Exception ex)
                    {
                        sdCardMountRetryCount++;
                        error = true;
                        Thread.Sleep(333);
                    }
                } while (sdCardMountRetryCount < 6);

                Init();

                Logger.Info("Started!");

                if(launchMode == LaunchMode.MicroFramework)
                    Thread.Sleep(Timeout.Infinite);
            }
            catch (Exception ex)
            {
                errorLED.Write(true);
                Logger.Error(ex, "while modules initialization");
            }
        }
        public static void Init()
        {
            Logger.Logged += Logger_Logged;
            Logger.Trace("Logger inited");

            var iBusBusy = Pin.TH3122SENSTA;
            var kBusBusy = Pin.K_BUS_TH3122SENSTA;
#if DEBUG || DebugOnRealDeviceOverFTDI
            iBusBusy = Cpu.Pin.GPIO_NONE;
            kBusBusy = Cpu.Pin.GPIO_NONE;
#endif

            string iBusComPort = Serial.COM1;
            ISerialPort iBusPort = new SerialPortTH3122(iBusComPort, iBusBusy);
            Manager.Init(iBusPort);

            string kBusComPort = Serial.COM2;
            ISerialPort kBusPort = new SerialPortTH3122(kBusComPort, kBusBusy);
            KBusManager.Init(kBusPort);

            ISerialPort dBusPort = new SerialPortTH3122("COM4", Cpu.Pin.GPIO_NONE, writeBufferSize: 1); // d31, d33
            dBusPort.AfterWriteDelay = 4;
            DBusManager.Init(dBusPort);

#if !NETMF && DebugOnRealDeviceOverFTDI
            if(!iBusPort.IsOpen)
                iBusPort.Open();
#endif

#if DEBUG
            Manager.BeforeMessageReceived += Manager_BeforeMessageReceived;
            Manager.AfterMessageReceived += Manager_AfterMessageReceived;
            Manager.BeforeMessageSent += Manager_BeforeMessageSent;
            Manager.AfterMessageSent += Manager_AfterMessageSent;
#endif

            KBusManager.Instance.BeforeMessageReceived += KBusManager_BeforeMessageReceived;
            KBusManager.Instance.BeforeMessageSent += KBusManager_BeforeMessageSent;

            //player = new iPodViaHeadset(Cpu.Pin.GPIO_NONE);
            //player = new BluetoothOVC3860(Serial.COM2/*, sd != null ? sd + @"\contacts.vcf" : null*/);
            player = new VS1003Player(FEZPandaIII.Gpio.D25, FEZPandaIII.Gpio.D27, FEZPandaIII.Gpio.D24, FEZPandaIII.Gpio.D26);
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
                    BordmonitorMenu.ResetButtonPressed += () =>
                    {
                        RefreshLEDs(LedType.Empty);
                        Radio.PressOnOffToggle();
                        Thread.Sleep(100);
                        resetPin.Write(false);
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

            RefreshLEDs(LedType.GreenBlinking);
            RefreshLEDs(LedType.GreenBlinking);
            RefreshLEDs(LedType.GreenBlinking);
            successInitLED.Write(true);

            var dateTimeEventArgs = new DateTimeEventArgs(DateTime.Now, false);
            dateTimeEventArgs = InstrumentClusterElectronics.GetDateTime();
            Logger.Trace("Aquired dateTime from IKE: " + dateTimeEventArgs.Value);
            Utility.SetLocalTime(dateTimeEventArgs.Value);

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

        private static void Manager_BeforeMessageReceived(MessageEventArgs e)
        {
            LED.Write(Busy(true, 1));
        }

        private static void Manager_AfterMessageReceived(MessageEventArgs e)
        {
            LED.Write(Busy(false, 1));

            if (e.Message.Data.Compare(MessageRegistry.DataAnnounce)
                || e.Message.Data.Compare(MessageRegistry.DataPollRequest)
                || e.Message.Data.Compare(MessageRegistry.DataPollResponse))
            {
                return;
            }

            // Show only messages which are described
            if (e.Message.Describe() == null) { return; }
      
            var logIco = "< ";
            if (settings.LogMessageToASCII)
            {
                Logger.Info(e.Message.ToPrettyString(false, false), logIco);
            }
            else
            {
                Logger.Info(e.Message, logIco);
            }
        }

        private static void Manager_BeforeMessageSent(MessageEventArgs e)
        {
            LED.Write(Busy(true, 2));
        }

        private static void Manager_AfterMessageSent(MessageEventArgs e)
        {
            LED.Write(Busy(false, 2));

            var logIco = " >";
            if (settings.LogMessageToASCII)
            {
                Logger.Info(e.Message.ToPrettyString(false, false), logIco);
            }
            else
            {
                Logger.Info(e.Message, logIco);
            }
        }

        // Log just needed message
        private static bool KBusLoggerPredicate(MessageEventArgs e)
        {
            if (e.Message.SourceDevice == DeviceAddress.IntegratedHeatingAndAirConditioning && e.Message.DestinationDevice == DeviceAddress.InstrumentClusterElectronics && e.Message.Data[0] == 0x83 // Air conditioning compressor status
                || 
                e.Message.SourceDevice == DeviceAddress.IntegratedHeatingAndAirConditioning && e.Message.DestinationDevice == DeviceAddress.NavigationEurope && e.Message.Data[0] == 0x86 // Some info for NavigationEurope
                ||
                e.Message.SourceDevice == DeviceAddress.InstrumentClusterElectronics && e.Message.Data[0] == 0x11 // Ignition status
                ||
                //e.Message.SourceDevice == DeviceAddress.InstrumentClusterElectronics && e.Message.Data[0] == 0x13 // IKE Sensor status
                //||
                e.Message.SourceDevice == DeviceAddress.InstrumentClusterElectronics && e.Message.Data[0] == 0x15 // Country coding status
                ||
                e.Message.SourceDevice == DeviceAddress.InstrumentClusterElectronics && e.Message.Data[0] == 0x17 // Odometer
                ||
                e.Message.SourceDevice == DeviceAddress.InstrumentClusterElectronics && e.Message.Data[0] == 0x18 // Speed/RPM
                ||
                e.Message.SourceDevice == DeviceAddress.InstrumentClusterElectronics && e.Message.Data[0] == 0x19 // Temperature
                ) 
            {
                return false;
            }

            return e.Message.SourceDevice == DeviceAddress.AuxilaryHeater ||
                   e.Message.SourceDevice == DeviceAddress.IntegratedHeatingAndAirConditioning ||
                   e.Message.DestinationDevice == DeviceAddress.AuxilaryHeater ||
                   e.Message.DestinationDevice == DeviceAddress.IntegratedHeatingAndAirConditioning ||
                   (e.Message.SourceDevice == DeviceAddress.InstrumentClusterElectronics && e.Message.DestinationDevice == DeviceAddress.GlobalBroadcastAddress); 
        }

        private static void KBusManager_BeforeMessageReceived(MessageEventArgs e)
        {
            if (KBusLoggerPredicate(e))
            {
                var logIco = "KBUS: <- ";
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
                var logIco = "KBUS: -> ";
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


        static bool blinkerOn = true;
        static byte busy = 0;
        static bool error = false;

        static void Logger_Logged(LoggerArgs args)
        {
            if (args.Priority == LogPriority.Error)
            {
                error = true;
                errorLED.Write(true);
                RefreshLEDs(LedType.Red);

                StreamWriter errorFile = new StreamWriter(rootDirectory + "\\errorLog.txt", append: true);
                errorFile.WriteLine(args.LogString);
                errorFile.Close();
            }
            if (args.Priority == LogPriority.Trace)
            {
                StreamWriter traceFile = new StreamWriter(rootDirectory + "\\traceLog.txt", append: true);
                traceFile.WriteLine(args.LogString);
                traceFile.Dispose();
            }
#if DEBUG
            if (Debugger.IsAttached)
            {
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
            if (error)
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
