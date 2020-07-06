using System;
using System.IO.Ports;
using System.Threading;
using GHI.IO;
using imBMW.Tools;

namespace imBMW.iBus
{
    public class DBusManager : ManagerImpl
    {
        public static string PORT_NAME = "dBus";
        private static DBusManager _instance;

        static SignalGenerator sg = new SignalGenerator(FEZPandaIII.Gpio.D29, true);

        public static DBusManager Instance
        {
            get
            {
                if (_instance == null/* || !_instance.Inited*/)
                {
                    //throw new Exception(nameof(DBusManager) + " should be firstly be inited.");
                    _instance = new DBusManager();
                }
                return _instance;
            }
        }

        private DBusManager()
        {
        }

        public static void Init(ISerialPort port, ThreadPriority threadPriority = ThreadPriority.AboveNormal)
        {
            if (!Instance.Inited)
            {
                Instance.InitPort(port, PORT_NAME, threadPriority);
            }
            else
            {
                throw new Exception(nameof(DBusManager) + " already inited.");
            }
        }

        public static ISerialPort Port
        {
            get { return Instance._port; }
        }

        protected override void SendData(Message m)
        {
#if OnBoardMonitorEmulator
            //_port.Write(m.Packet);
            foreach (var b in m.Packet)
            {
                _port.Write(b);
                Thread.Sleep(4);

            }
#else
            SignalGeneratorHelper.Set(sg, false, m.Packet, 4000, false);
#endif
        }

        protected internal override void bus_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            ISerialPort port = (ISerialPort)sender;
            if (port.AvailableBytes == 0)
            {
                Logger.Trace("Available bytes lost! " + port.ToString());
                return;
            }
            lock (bufferSync)
            {
                byte[] data = port.ReadAvailable();
                Logger.Trace("DBus data received: " + data.ToHex());

                if (messageBufferLength + data.Length > messageBuffer.Length)
                {
                    Logger.Trace("Buffer overflow. Extending it. " + port.ToString());
                    byte[] newBuffer = new byte[messageBuffer.Length * 2];
                    Array.Copy(messageBuffer, newBuffer, messageBufferLength);
                    messageBuffer = newBuffer;
                }
                if (data.Length == 1)
                {
                    messageBuffer[messageBufferLength++] = data[0];
                }
                else
                {
                    Array.Copy(data, 0, messageBuffer, messageBufferLength, data.Length);
                    messageBufferLength += data.Length;
                }

                Logger.Trace("messageBuffer: " + messageBuffer.ToHex());

                // TODO: Uncomment this and fix issue(because of partial writing to port, sometimes CanStartWith return false for correct packet)
                while (messageBufferLength >= DBusMessage.PacketLengthMin || messageBufferLength >= DS2Message.PacketLengthMin)
                {
                    Message dBusMessage = DBusMessage.TryCreate(messageBuffer, messageBufferLength);
                    Message ds2Message = DS2Message.TryCreate(messageBuffer, messageBufferLength);
                    Message m = ds2Message ?? dBusMessage;
                    if (m == null)
                    {
                        if (!DBusMessage.CanStartWith(messageBuffer, messageBufferLength))
                        {
                            Logger.Trace("Buffer skip: non-dBus data detected: " + messageBuffer[0].ToHex());
                            SkipBuffer(1);
                            continue;
                        }
                        return;
                    }
                    ProcessMessage(m);
                    SkipBuffer(m.PacketLength);
                    Logger.Trace("Message received and buffer skipped: " + messageBuffer.ToHex());
                }
                lastMessage = DateTime.Now;
            }
        }
    }
}
