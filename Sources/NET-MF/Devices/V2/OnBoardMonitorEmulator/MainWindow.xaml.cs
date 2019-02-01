using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using imBMW.iBus;
using Microsoft.SPOT.Hardware;
using imBMW.Devices.V2;
using imBMW.Features.Menu;
using imBMW.Features.Menu.Screens;
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

        public static byte[] PhoneButtonClick = {0x48, 0x08};
        public static byte[] MenuButonHold = { 0x48, 0x74 };

        private bool _isEnabled = false;
        public bool IsEnabled
        {
            get { return _isEnabled; }
            set
            {
                _isEnabled = value;
                RadioEmulator.IsEnabled = value; 
            }
        }

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

        public MainWindow()
        {
            InitializeComponent();

            InstrumentClusterElectronicsEmulator.Init();
            NavigationModuleEmulator.Init();
            AuxilaryHeaterEmulator.Init();
            RadioEmulator.Init();
            DDEEmulator.Init();
            FrontDisplayEmulator.Init();
            HeadlightVerticalAimControlEmulator.Init();

            Launcher.Launch(Launcher.LaunchMode.WPF);

            InstrumentClusterElectronicsEmulator.StartAnounce();

            Bordmonitor.ReplyToScreenUpdates = true;
            Bordmonitor.TextReceived += Bordmonitor_TextReceived;
            FrontDisplayEmulator.LedChanged += FrontDisplayEmulator_LedChanged;

            BordmonitorMenu.Instance.CurrentScreen = HomeScreen.Instance;
#if !DebugOnRealDeviceOverFTDI
            Launcher.emulator.IsEnabled = true;
#endif
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

        private void FrontDisplayEmulator_LedChanged(Launcher.LedType ledType)
        {
            this.Dispatcher.Invoke(() =>
            {
                if (ledType == Launcher.LedType.Red || ledType == Launcher.LedType.RedBlinking)
                {
                    RedIndicator.Fill = new SolidColorBrush(Colors.Red);
                }
                if (ledType == Launcher.LedType.Orange || ledType == Launcher.LedType.OrangeBlinking)
                {
                    OrangeIndicator.Fill = new SolidColorBrush(Colors.Orange);
                }
                if (ledType == Launcher.LedType.Green || ledType == Launcher.LedType.GreenBlinking)
                {
                    GreenIndicator.Fill = new SolidColorBrush(Colors.Green);
                }
                if (ledType == Launcher.LedType.Empty)
                {
                    RedIndicator.Fill = new SolidColorBrush(Colors.White);
                    OrangeIndicator.Fill = new SolidColorBrush(Colors.White);
                    GreenIndicator.Fill = new SolidColorBrush(Colors.White);
                }
            });
        }

        private void WriteMessage(Message message)
        {
            Manager.EnqueueMessage(message);
        }

        private void Knob1Button_Click(object sender, RoutedEventArgs e)
        {
            var dataPlayMessage = new Message(DeviceAddress.Radio, DeviceAddress.CDChanger, IsEnabled ? DataStop : DataPlay);
            WriteMessage(dataPlayMessage);
            IsEnabled = !IsEnabled;
            var button = (sender as Button);
            button.FontWeight = IsEnabled ? FontWeights.Bold : FontWeights.Normal;
            button.Foreground = new SolidColorBrush(IsEnabled ? Colors.Orange : Colors.Black);
        }

        private void Knob2Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
#if DebugOnRealDeviceOverFTDI
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
            var message = new Message(DeviceAddress.OnBoardMonitor, DeviceAddress.Broadcast, 0x48, 0x34);
            WriteMessage(message);
        }

        private void MenuButton_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var message = new Message(DeviceAddress.OnBoardMonitor, DeviceAddress.Broadcast, MenuButonHold);
            WriteMessage(message);
        }

        private void PhoneButton_Click(object sender, RoutedEventArgs e)
        {
            var message = new Message(DeviceAddress.OnBoardMonitor, DeviceAddress.Broadcast, PhoneButtonClick);
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
            var message = new Message(DeviceAddress.OnBoardMonitor, DeviceAddress.Broadcast, 0x48, 0x87);
            WriteMessage(message);
        }

        private void ArrowsButton_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var message = new Message(DeviceAddress.OnBoardMonitor, DeviceAddress.Radio, 0x48, 0x94);
            WriteMessage(message);
        }

        private void imBMWTest_Click(object sender, RoutedEventArgs e)
        {
            var message = new Message(DeviceAddress.imBMWTest, DeviceAddress.Broadcast, 0xEE, 0x00);
            WriteMessage(message);
        }
    }
}
