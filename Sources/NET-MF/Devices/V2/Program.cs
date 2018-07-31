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

namespace imBMW.Devices.V2
{
    public class Program
    {
        const string version = "FW1.0.12 HW2";
        static OutputPort LED;
        static OutputPort successInitLED;
        static OutputPort led4;
        static OutputPort resetPin;

        static Settings settings;
        static MediaEmulator emulator;
        static IAudioPlayer player;

        static InterruptPort nextButton;
        static InterruptPort prevButton;

        public static SDCard sd_card;
        private static string rootDirectory;

        public static void Main()
        {
            try
            {
                Debug.Print(Debug.GC(true).ToString());

                SDCard sd = null;
                settings = Settings.Init(sd != null ? sd + @"\imBMW.ini" : null);
                LED = new OutputPort(Pin.LED, false);
                successInitLED = new OutputPort(FEZPandaIII.Gpio.Led2, false);
                led4 = new OutputPort(Pin.LED4, false);
                resetPin = new OutputPort(FEZPandaIII.Gpio.D48, true);


                //SettingsScreen.Instance.Status = version.Length > 11 ? version.Replace(" ", "") : version;
                //Localization.SetCurrent(RussianLocalization.SystemName); //Localization.SetCurrent(settings.Language);
                //Features.Comfort.AutoLockDoors = settings.AutoLockDoors;
                //Features.Comfort.AutoUnlockDoors = settings.AutoUnlockDoors;
                //Features.Comfort.AutoCloseWindows = settings.AutoCloseWindows;
                //Features.Comfort.AutoCloseSunroof = settings.AutoCloseSunroof;

                sd_card = new SDCard(SDCard.SDInterface.MCI);
                sd_card.Mount();
                rootDirectory = VolumeInfo.GetVolumes()[0].RootDirectory;

                Init();

                Debug.EnableGCMessages(true);
                Logger.Info("Started!");

                Thread.Sleep(Timeout.Infinite);
            }
            catch (Exception ex)
            {
                led4.Write(true);
                Logger.Error(ex, "while modules initialization");
            }
        }
        public static void Init()
        {
            Logger.Logged += Logger_Logged;
            Logger.Info("Logger inited");

            string iBusComPort = Serial.COM1;
            var busy = Pin.TH3122SENSTA;

#if DEBUG_AT_HOME
            iBusComPort = "COM4";
            busy = Cpu.Pin.GPIO_NONE;
#endif

            // COM3 connected with COM2
            //ISerialPort fakeIbus = new SerialPortTH3122(Serial.COM3, Cpu.Pin.GPIO_NONE);

            ISerialPort iBusPort = new SerialPortTH3122(iBusComPort, busy);
            Manager.Init(iBusPort);
            Logger.Info("iBus manager inited");

            //ISerialPort dBusPort = new SerialPortTH3122("COM4", Pin.D_BUS_TH3122SENSTA);
            //DbusManager.Init(dBusPort);

            Manager.BeforeMessageReceived += Manager_BeforeMessageReceived;
            Manager.AfterMessageReceived += Manager_AfterMessageReceived;
            Manager.BeforeMessageSent += Manager_BeforeMessageSent;
            Manager.AfterMessageSent += Manager_AfterMessageSent;
            Logger.Info("iBus manager events subscribed");

            //player = new iPodViaHeadset(Cpu.Pin.GPIO_NONE);
            //player = new BluetoothOVC3860(Serial.COM2/*, sd != null ? sd + @"\contacts.vcf" : null*/);
            player = new VS1003Player(FEZPandaIII.Gpio.D25, FEZPandaIII.Gpio.D27, FEZPandaIII.Gpio.D24, FEZPandaIII.Gpio.D26);
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
                    BordmonitorMenu.ResetButtonPressed += () => { resetPin.Write(false); };
                }
                else
                {
                    //emulator = new BordmonitorAUX(player);
                }

                //Bordmonitor.NaviVersion = settings.NaviVersion;
                //BordmonitorMenu.FastMenuDrawing = settings.NaviVersion == NaviVersion.MK4;
                //BordmonitorMenu.Init(emulator);

                Logger.Info("Bordmonitor menu inited");
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

            player.IsPlayingChanged += Player_IsPlayingChanged;
            player.StatusChanged += Player_StatusChanged;
            Logger.Info("Player events subscribed");

            RefreshLEDs();
            successInitLED.Write(true);

            nextButton = new InterruptPort((Cpu.Pin)FEZPandaIII.Gpio.Ldr1, true, Port.ResistorMode.PullUp, Port.InterruptMode.InterruptEdgeHigh);
            nextButton.OnInterrupt += (p, s, t) =>
            {
                if (!emulator.IsEnabled) { emulator.IsEnabled = true; }
                else { emulator.Player.Next(); }
            };
            prevButton = new InterruptPort((Cpu.Pin)FEZPandaIII.Gpio.Ldr0, true, Port.ResistorMode.PullUp, Port.InterruptMode.InterruptEdgeHigh);
            prevButton.OnInterrupt += (p, s, t) =>
            {
                //emulator.Player.Prev();
                //Manager.EnqueueMessage(new Message(DeviceAddress.Radio, DeviceAddress.CDChanger, CDChanger.DataSelectDisk6));
                //emulator.Player.IsRandom = false;
                //emulator.Player.DiskNumber = 6;
                emulator.IsEnabled = false;
            };

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

        static bool restrictOutput = true;
        private static void Manager_AfterMessageReceived(MessageEventArgs e)
        {
            LED.Write(Busy(false, 1));

			if(e.Message.Data.Compare(MessageRegistry.DataAnnounce)
                || e.Message.Data.Compare(MessageRegistry.DataPollRequest)
                || e.Message.Data.Compare(MessageRegistry.DataPollResponse))
            {
                return;
            }

            // Show only messages which are described
            if (e.Message.Describe() == null) { return; }
            if (!restrictOutput)
            {
                var logIco = "< ";
                if (settings.LogMessageToASCII)
                {
                    Logger.Info(e.Message.ToPrettyString(true, true), logIco);
                }
                else
                {
                    Logger.Info(e.Message, logIco);
                }
                return;
            }

if ((
    e.Message.SourceDevice == DeviceAddress.Radio && e.Message.DestinationDevice == DeviceAddress.CDChanger || 
    e.Message.SourceDevice == DeviceAddress.CDChanger && e.Message.DestinationDevice == DeviceAddress.Radio ||
    e.Message.SourceDevice == DeviceAddress.GraphicsNavigationDriver || e.Message.DestinationDevice == DeviceAddress.GraphicsNavigationDriver ||
    e.Message.SourceDevice == DeviceAddress.OnBoardMonitor || e.Message.DestinationDevice == DeviceAddress.OnBoardMonitor ||
    e.Message.SourceDevice == DeviceAddress.InstrumentClusterElectronics || e.Message.DestinationDevice == DeviceAddress.InstrumentClusterElectronics ||
    e.Message.SourceDevice == DeviceAddress.NavigationEurope || e.Message.DestinationDevice == DeviceAddress.NavigationEurope ||
    e.Message.SourceDevice == DeviceAddress.NavigationEurope || e.Message.DestinationDevice == DeviceAddress.NavigationEurope
    )
                //&& e.Message.SourceDevice != DeviceAddress.GraphicsNavigationDriver
                //&& e.Message.DestinationDevice != DeviceAddress.GraphicsNavigationDriver
                //&& e.Message.SourceDevice != DeviceAddress.OnBoardMonitor
                //&& e.Message.DestinationDevice != DeviceAddress.OnBoardMonitor
                //&& e.Message.SourceDevice != DeviceAddress.CDChanger
                //&& e.Message.DestinationDevice != DeviceAddress.CDChanger
                && e.Message.DestinationDevice != DeviceAddress.Broadcast
                //&& e.Message.SourceDevice != DeviceAddress.Diagnostic
                //&& e.Message.DestinationDevice != DeviceAddress.Diagnostic
                )
            {
                var logIco = "< ";
                if (settings.LogMessageToASCII)
                {
                    Logger.Info(e.Message.ToPrettyString(true, true), logIco);
                }
                else
                {
                    Logger.Info(e.Message, logIco);
                }
            }
        }

        private static void Manager_BeforeMessageSent(MessageEventArgs e)
        {
            LED.Write(Busy(true, 2));
        }

        private static void Manager_AfterMessageSent(MessageEventArgs e)
        {
            LED.Write(Busy(false, 2));
            Logger.Info(e.Message, " >");
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
            RefreshLEDs();
        }


        static bool blinkerOn = true;
        static byte busy = 0;
        static bool error = false;

        static void Logger_Logged(LoggerArgs args)
        {
            if (args.Priority == LogPriority.Error)
            {
                error = true;
                led4.Write(true);
                RefreshLEDs();
                
                var errorFile = File.Open(rootDirectory + "\\errorLog.txt", FileMode.OpenOrCreate);
                errorFile.WriteString(args.LogString);
                errorFile.Close();
            }
            if (Debugger.IsAttached)
            {
                Debug.Print(args.LogString);
            }
        }

        static void RefreshLEDs()
        {
            if (!Manager.Inited)
            {
                return;
            }
            byte b = 0;
            if (error)
            {
                b = b.AddBit(0);
            }
            if (blinkerOn)
            {
                //b = b.AddBit(2);
            }
            if (player != null/* && player.IsPlaying*/)
            {
                b = b.AddBit(4);
            }
            Manager.EnqueueMessage(new Message(DeviceAddress.Telephone, DeviceAddress.FrontDisplay, "Set LEDs", 0x2B, b));
        }

        static bool Busy(bool busy, byte type)
        {
            if (busy)
            {
                Program.busy = Program.busy.AddBit(type);
            }
            else
            {
                Program.busy = Program.busy.RemoveBit(type);
            }
            return Program.busy > 0;
        }
    }
}
