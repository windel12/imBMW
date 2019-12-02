using System;
using System.IO.Ports;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using imBMW.iBus;
using imBMW.Devices.V2;
using imBMW.iBus.Devices.Emulators;
using imBMW.Tools;
using imBMW.iBus.Devices.Real;
using OnBoardMonitorEmulator.DevicesEmulation;

namespace OnBoardMonitorEmulator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ISerialPort port;

        /// <summary>0x38, 0x00, 0x00 </summary>
        public static byte[] DataCurrentDiskTrackRequest = new byte[] { 0x38, 0x00, 0x00 };
        /// <summary>0x38, 0x01, 0x00 </summary>
        public static byte[] DataStop = new byte[] { 0x38, 0x01, 0x00 };
        /// <summary>0x38, 0x02, 0x00 </summary>
        public static byte[] DataPause = new byte[] { 0x38, 0x02, 0x00 };
        /// <summary>0x38, 0x03, 0x00 </summary>
        public static byte[] DataPlay = new byte[] { 0x38, 0x03, 0x00 };

        public static byte[] DataSelectDisk1 = new byte[] { 0x38, 0x06, 0x01 };
        public static byte[] DataSelectDisk2 = new byte[] { 0x38, 0x06, 0x02 };
        public static byte[] DataSelectDisk3 = new byte[] { 0x38, 0x06, 0x03 };
        public static byte[] DataSelectDisk4 = new byte[] { 0x38, 0x06, 0x04 };
        public static byte[] DataSelectDisk5 = new byte[] { 0x38, 0x06, 0x05 };
        public static byte[] DataSelectDisk6 = new byte[] { 0x38, 0x06, 0x06 };

        public static byte[] DataScanPlaylistOff = new byte[] { 0x38, 0x07, 0x00 };
        public static byte[] DataScanPlaylistOn = new byte[] { 0x38, 0x07, 0x01 };

        /// <summary>0x38, 0x08, 0x01 </summary>
        public static byte[] DataRandomPlay = new byte[] { 0x38, 0x08, 0x01 };

        public static byte[] DataNext = new byte[] { 0x38, 0x0A, 0x00 };
        public static byte[] DataPrev = new byte[] { 0x38, 0x0A, 0x01 };

        public static byte[] PhoneButtonClick = { 0x48, 0x08 };
        public static byte[] PhoneButtonHold = { 0x48, 0x48 };
        public static byte[] MenuButonClick = { 0x48, 0x34 };
        public static byte[] MenuButonHold = { 0x48, 0x74 };

        //private bool _isEnabled = false;
        //public bool IsEnabled
        //{
        //    get { return _isEnabled; }
        //    set
        //    {
        //        _isEnabled = value;
        //        RadioEmulator.IsEnabled = value;
        //    }
        //}

        private GraphicsNavigationDriverState _state;

        public GraphicsNavigationDriverState State
        {
            get { return _state; }
            set
            {
                if (value == GraphicsNavigationDriverState.BordComputer)
                {
                    BordComputerGrid.Visibility = Visibility.Visible;
                    CDChangerPanel.Visibility = Visibility.Hidden;
                }
                else
                {
                    BordComputerGrid.Visibility = Visibility.Hidden;
                    CDChangerPanel.Visibility = Visibility.Visible;
                }
            }
        }

        private readonly ViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();

            _viewModel = new ViewModel();
            DataContext = _viewModel;

            InstrumentClusterElectronicsEmulator.Init();
            NavigationModuleEmulator.Init();
            AuxilaryHeaterEmulator.Init();
            RadioEmulator.Init();
            DigitalDieselElectronicsEmulator.Init();
            FrontDisplayEmulator.Init();
            HeadlightVerticalAimControlEmulator.Init();
            IntegratedHeatingAndAirConditioningEmulator.Init();

            Launcher.Launch(Launcher.LaunchMode.WPF);

            InstrumentClusterElectronics.IgnitionStateChanged += (e) =>
            {
                if (e.PreviousIgnitionState == IgnitionState.Acc && e.CurrentIgnitionState == IgnitionState.Ign)
                {
                    InstrumentClusterElectronicsEmulator.StartAnnounce();
                }

                if (e.PreviousIgnitionState == IgnitionState.Ign && e.CurrentIgnitionState == IgnitionState.Acc)
                {
                    InstrumentClusterElectronicsEmulator.StopAnnounce();
                }
            };

            Bordmonitor.ReplyToScreenUpdates = true;
            Bordmonitor.TextReceived += Bordmonitor_TextReceived;
            FrontDisplayEmulator.LedChanged += FrontDisplayEmulator_LedChanged;
            InstrumentClusterElectronicsEmulator.OBCTextChanged += InstrumentClusterElectronicsEmulator_OBCTextChanged;

            //BordmonitorMenu.Instance.CurrentScreen = HomeScreen.Instance;

            Manager.Instance.AddMessageReceiverForSourceDevice(DeviceAddress.Radio, m =>
            {
                this.Dispatcher.Invoke(() =>
                {
                    if (m.Data.StartsWith(Radio.DataRadioOn))
                    {
                        EnableRadio();
                    }
                    if (m.Data.StartsWith(Radio.DataRadioOff))
                    {
                        DisableRadio();
                    }
                });
            });
#if !DebugOnRealDeviceOverFTDI
            //Launcher.emulator.IsEnabled = true;
#endif
        }

        private void EnableRadio()
        {
            RadioEmulator.IsEnabled = true;
            Knob1Button.FontWeight = FontWeights.Bold;
            Knob1Button.Foreground = new SolidColorBrush(Colors.Orange);
        }

        private void DisableRadio()
        {
            RadioEmulator.IsEnabled = false;
            Knob1Button.FontWeight = FontWeights.Normal;
            Knob1Button.Foreground = new SolidColorBrush(Colors.Black);
            OBCDisplay.Text = "";
        }

        private void Bordmonitor_TextReceived(BordmonitorText args)
        {
            this.Dispatcher.Invoke(() =>
            {
                if (State != GraphicsNavigationDriverState.MediaScreen)
                    State = GraphicsNavigationDriverState.MediaScreen;

                switch (args.Field)
                {
                    case BordmonitorFields.Title:
                        label0.Content = args.Text; break;
                    case BordmonitorFields.T1:
                        label1.Content = args.Text; break;
                    case BordmonitorFields.T2:
                        label2.Content = args.Text; break;
                    case BordmonitorFields.T3:
                        label3.Content = args.Text; break;
                    case BordmonitorFields.T4:
                        label4.Content = args.Text; break;
                    case BordmonitorFields.T5:
                        label5.Content = args.Text; break;
                    case BordmonitorFields.Status:
                        label6.Content = args.Text; break;
                }
                if (args.Field == BordmonitorFields.Item)
                {
                    var items = args.ParseItems();
                    var length = items.Length <= 10 ? items.Length : 10;
                    for (int i = 0; i < length; i++)
                    {
                        var item = items[i];
                        var indexTextBlock = this.FindName("index" + (item.Index+1)) as TextBlock;
                        indexTextBlock.Text = item.Text;
                    }
                }
            });
        }

        private void FrontDisplayEmulator_LedChanged(LedType ledType)
        {
            this.Dispatcher.Invoke(() =>
            {
                RedIndicator.Fill = OrangeIndicator.Fill = GreenIndicator.Fill = new SolidColorBrush(Colors.White);
                RedIndicator.Opacity = OrangeIndicator.Opacity = GreenIndicator.Opacity = 1;

                if ((ledType & LedType.Red) != 0 || (ledType & LedType.RedBlinking) != 0)
                {
                    RedIndicator.Fill = new SolidColorBrush(Colors.Red);
                    if (ledType == LedType.RedBlinking)
                        RedIndicator.Opacity = 0.5;
                }
                if ((ledType & LedType.Orange) != 0 || (ledType & LedType.OrangeBlinking) != 0)
                {
                    OrangeIndicator.Fill = new SolidColorBrush(Colors.Orange);
                    if (ledType == LedType.OrangeBlinking)
                        OrangeIndicator.Opacity = 0.5;
                }
                if ((ledType & LedType.Green) != 0 || (ledType & LedType.GreenBlinking) != 0)
                {
                    GreenIndicator.Fill = new SolidColorBrush(Colors.Green);
                    if (ledType == LedType.GreenBlinking)
                        GreenIndicator.Opacity = 0.5;
                }
                if (ledType == LedType.Empty)
                {
                    RedIndicator.Fill = new SolidColorBrush(Colors.White);
                    OrangeIndicator.Fill = new SolidColorBrush(Colors.White);
                    GreenIndicator.Fill = new SolidColorBrush(Colors.White);
                }
            });
        }

        private void InstrumentClusterElectronicsEmulator_OBCTextChanged(string message)
        {
            this.Dispatcher.Invoke(() => { OBCDisplay.Text = message; });
        }

        private void WriteMessage(Message message)
        {
            Manager.Instance.EnqueueMessage(message);
        }

        private void Knob1Button_Click(object sender, RoutedEventArgs e)
        {
#if DebugOnRealDeviceOverFTDI
            Manager.Instance.EnqueueMessage(new Message(DeviceAddress.Radio, DeviceAddress.CDChanger, RadioEmulator.IsEnabled ? CDChanger.DataStop : CDChanger.DataPlay));
            if (RadioEmulator.IsEnabled)
                DisableRadio();
            else
                EnableRadio();
#else
            Radio.PressOnOffToggle();
#endif
        }

        private void Knob2Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
#if DebugOnRealDeviceOverFTDI

            //var showOBCTextMessage = Radio.GetDisplayTextRadioMessage("CD 1-1", TextAlign.Center);
            //InstrumentClusterElectronicsEmulator.ProcessMessageToIKE(showOBCTextMessage);
            var message = new Message(DeviceAddress.Radio, DeviceAddress.CDChanger, DataNext);
            WriteMessage(message);
#else
            Radio.PressNext();
#endif
        }

        private void PrevButton_Click(object sender, RoutedEventArgs e)
        {
#if DebugOnRealDeviceOverFTDI
            var message = new Message(DeviceAddress.Radio, DeviceAddress.CDChanger, DataPrev);
            WriteMessage(message);
#else
            Radio.PressPrev();
#endif
        }

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            State = GraphicsNavigationDriverState.BordComputer;
            var message = new Message(DeviceAddress.OnBoardMonitor, DeviceAddress.LocalBroadcastAddress, MenuButonClick);
            WriteMessage(message);
        }

        private void MenuButton_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            var message = new Message(DeviceAddress.OnBoardMonitor, DeviceAddress.LocalBroadcastAddress, MenuButonHold);
            WriteMessage(message);
        }

        private void PhoneButton_Click(object sender, RoutedEventArgs e)
        {
            var message = new Message(DeviceAddress.OnBoardMonitor, DeviceAddress.LocalBroadcastAddress, PhoneButtonClick);
            WriteMessage(message);
        }

        private void PhoneButton_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            var message = new Message(DeviceAddress.OnBoardMonitor, DeviceAddress.LocalBroadcastAddress, PhoneButtonHold);
            WriteMessage(message);
        }

        private void index_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var textBlock = sender as TextBlock;
            var index = (byte)(int.Parse(new String(textBlock.Name.Skip(5).ToArray())) -1);
            Bordmonitor.PressItem(index);
        }

        private void DiskButton_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var button = sender as Button;
            var index = int.Parse(button.Content.ToString());
            byte secondByte = 0x00;
            switch (index)
            {
                case 1: secondByte = 0x91; break;
                case 2: secondByte = 0x81; break;
                case 3: secondByte = 0x92; break;
                case 4: secondByte = 0x82; break;
                case 5: secondByte = 0x93; break;
                case 6: secondByte = 0x83; break;
            }
            var message = new Message(DeviceAddress.OnBoardMonitor, DeviceAddress.Radio, 0x48, secondByte);
            WriteMessage(message);
        }

        private void ClockButton_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var message = new Message(DeviceAddress.OnBoardMonitor, DeviceAddress.LocalBroadcastAddress, 0x48, 0x87);
            WriteMessage(message);
        }

        private void ArrowsButton_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var message = new Message(DeviceAddress.OnBoardMonitor, DeviceAddress.Radio, 0x48, 0x94);
            WriteMessage(message);
        }

        private void imBMWTest_Click(object sender, RoutedEventArgs e)
        {
            var message = new Message(DeviceAddress.imBMWTest, DeviceAddress.LocalBroadcastAddress, 0xEE, 0x00);
            WriteMessage(message);
        }

        private void MFLButton_VolumeUp(object sender, RoutedEventArgs e)
        {
            MultiFunctionSteeringWheel.VolumeUp();
        }

        private void MFLButton_VolumeDown(object sender, RoutedEventArgs e)
        {
            MultiFunctionSteeringWheel.VolumeDown();
        }

        private void rpmSpeedAnnounceInterval_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            int newInterval = int.Parse((sender as TextBox).Text);
            InstrumentClusterElectronicsEmulator.rpmSpeedAnounceTimer.Change(0, newInterval);
        }

        private void temperatureAnounceInterval_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            int newInterval = int.Parse((sender as TextBox).Text);
            InstrumentClusterElectronicsEmulator.temperatureAnounceTimer.Change(0, newInterval);
        }

        private void AC_Click(object sender, RoutedEventArgs e)
        {
            IntegratedHeatingAndAirConditioningEmulator.AirConditioningCompressorStatus_FirstByte += 0x02;
        }

        private void IgnitionSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var ignitionState = (IgnitionState) e.NewValue;
            IgnitionStateLabel.Content = string.Format("Ignition status: {0}", ignitionState.ToString());
            InstrumentClusterElectronicsEmulator.IgnitionState = ignitionState;
        }

        private void lockButton_Click(object sender, RoutedEventArgs e)
        {
            var message = new Message(DeviceAddress.BodyModule, DeviceAddress.GlobalBroadcastAddress, 0x72, 0x12);
            KBusManager.Instance.EnqueueMessage(message);
        }

        private void unlockButton_Click(object sender, RoutedEventArgs e)
        {
            var message = new Message(DeviceAddress.BodyModule, DeviceAddress.GlobalBroadcastAddress, 0x72, 0x22);
            KBusManager.Instance.EnqueueMessage(message);
        }

        private void idleButton_Click(object sender, RoutedEventArgs e)
        {
            var message = new Message(DeviceAddress.imBMWTest, DeviceAddress.GlobalBroadcastAddress, 0x01);
            Manager.Instance.EnqueueMessage(message);
        }

        private void sleepButton_Click(object sender, RoutedEventArgs e)
        {
            var message = new Message(DeviceAddress.imBMWTest, DeviceAddress.GlobalBroadcastAddress, 0x02);
            Manager.Instance.EnqueueMessage(message);
        }

        private void wakeUpButton_Click(object sender, RoutedEventArgs e)
        {
            var message = new Message(DeviceAddress.imBMWTest, DeviceAddress.GlobalBroadcastAddress, 0x03);
            Manager.Instance.EnqueueMessage(message);
        }
    }
}
