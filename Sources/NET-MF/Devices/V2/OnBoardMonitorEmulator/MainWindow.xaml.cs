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

namespace OnBoardMonitorEmulator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SerialPort port;

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

        public static byte[] MenuButonHold = { 0x48, 0x74 };

        public bool IsEnabled = false;

        public MainWindow()
        {
            InitializeComponent();

            port = new SerialPort("COM4");
            port.BaudRate = 9600;
            port.Parity = Parity.Even;
            port.DataBits = 8;
            port.StopBits = StopBits.One;
            port.Handshake = Handshake.None;
            port.RtsEnable = true;

            port.Open();
        }

        private void WriteMessage(Message message)
        {
            if (!port.IsOpen)
                port.Open();

            port.Write(message.Packet, 0, message.Packet.Length);
            port.Close();
        }

        private void Knob1Button_Click(object sender, RoutedEventArgs e)
        {
            var dataPlayMessage = new Message(DeviceAddress.Radio, DeviceAddress.CDChanger, IsEnabled ? DataStop : DataPlay);
            WriteMessage(dataPlayMessage);
            IsEnabled = !IsEnabled;
        }

        private void Knob2Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            var message = new Message(DeviceAddress.Radio, DeviceAddress.CDChanger, DataNext);
            WriteMessage(message);
        }

        private void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            var message = new Message(DeviceAddress.Radio, DeviceAddress.CDChanger, DataPrev);
            WriteMessage(message);
        }

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            var message = new Message(DeviceAddress.OnBoardMonitor, DeviceAddress.Broadcast, MenuButonHold);
            WriteMessage(message);
        }
    }
}
