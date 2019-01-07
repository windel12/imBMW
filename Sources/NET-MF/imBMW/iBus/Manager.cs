using System;
using System.Collections;
using System.IO.Ports;
using System.Threading;
using imBMW.Tools;

namespace imBMW.iBus
{
    public static class Manager
    {
        private static ManagerImpl _instance;
        private static ManagerImpl Instance
        {
            get
            {
                if (_instance == null/* || !_instance.Inited*/)
                {
                    //throw new Exception(nameof(Manager) + " should be firstly be inited.");
                    _instance = new ManagerImpl();
                }
                return _instance;
            }
        }

        public static void Init(ISerialPort port)
        {
            if (!Instance.Inited)
            {
                Instance.InitPort(port, "iBus");
            }
            else
            {
                throw new Exception(nameof(Manager) + " already inited.");
            }
        }

        public static bool Inited => Instance.Inited;

        public static void ProcessMessage(Message m) => Instance.ProcessMessage(m);

        public static void EnqueueMessage(Message m) => Instance.EnqueueMessage(m);

        public static void EnqueueMessage(params Message[] messages) => Instance.EnqueueMessage(messages);

        public static event MessageEventHandler BeforeMessageReceived
        {
            add { Instance.BeforeMessageReceived += value; }
            remove { Instance.BeforeMessageReceived -= value; }
        }

        public static event MessageEventHandler AfterMessageReceived
        {
            add { Instance.AfterMessageReceived += value; }
            remove { Instance.AfterMessageReceived -= value; }
        }

        public static event MessageEventHandler BeforeMessageSent
        {
            add { Instance.BeforeMessageSent += value; }
            remove { Instance.BeforeMessageSent -= value; }
        }

        public static event MessageEventHandler AfterMessageSent
        {
            add { Instance.AfterMessageSent += value; }
            remove { Instance.AfterMessageSent -= value; }
        }

        public static void AddMessageReceiverForSourceDevice(DeviceAddress source, MessageReceiver callback) => 
            Instance.AddMessageReceiverForSourceDevice(source, callback);

        public static void AddMessageReceiverForDestinationDevice(DeviceAddress destination, MessageReceiver callback) => 
            Instance.AddMessageReceiverForDestinationDevice(destination, callback);

        public static void AddMessageReceiverForSourceAndDestinationDevice(DeviceAddress source, DeviceAddress destination, MessageReceiver callback) => 
            Instance.AddMessageReceiverForSourceAndDestinationDevice(source, destination, callback);

        public static void AddMessageReceiverForSourceOrDestinationDevice(DeviceAddress source, DeviceAddress destination, MessageReceiver callback) => 
            Instance.AddMessageReceiverForSourceOrDestinationDevice(source, destination, callback);

        public static bool FindDevice(DeviceAddress device) => Instance.FindDevice(device);
        public static bool FindDevice(DeviceAddress device, int timeout) => Instance.FindDevice(device, timeout);
    }

    public class ManagerImpl
    {
        protected internal ISerialPort _port;
        public bool Inited { get; private set; }

        QueueThreadWorker messageWriteQueue;
        //QueueThreadWorker messageReadQueue;

        protected DateTime lastMessage = DateTime.Now;
#if NETMF
        protected byte[] messageBuffer = new byte[Message.PacketLengthMax];
#endif
#if OnBoardMonitorEmulator
        protected byte[] messageBuffer = new byte[ushort.MaxValue];
#endif

        protected int messageBufferLength = 0;
        protected object bufferSync = new object();

        internal ManagerImpl()
        {
        }

        public void InitPort(ISerialPort port, string queueThreadWorkerName = "")
        {
            messageWriteQueue = new QueueThreadWorker(SendMessage, queueThreadWorkerName);
            //messageReadQueue = new QueueThreadWorker(ProcessMessage);

            _port = port;
            _port.DataReceived += new SerialDataReceivedEventHandler(bus_DataReceived);

            Inited = true;
        }

        #region Message reading and processing

        protected virtual void bus_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            ISerialPort port = (ISerialPort)sender;
            if (port.AvailableBytes == 0)
            {
                Logger.Warning("Available bytes lost! " + port.ToString());
                return;
            }
            lock (bufferSync)
            {
                byte[] data = port.ReadAvailable();
                if (messageBufferLength + data.Length > messageBuffer.Length)
                {
                    Logger.Info("Buffer overflow. Extending it. " + port.ToString());
                    byte[] newBuffer = new byte[messageBuffer.Length * 2];
                    Array.Copy(messageBuffer, newBuffer, messageBufferLength);
                    messageBuffer = newBuffer;
                }
                if (data.Length == 1)
                {
#if DebugOnRealDeviceOverFTDI && OnBoardMonitorEmulator // remove 0x00 when FTDI sends it for first time
                    if (data[0] == 0x00)
                        return;
#endif
                    messageBuffer[messageBufferLength++] = data[0];
                }
                else
                {
                    Array.Copy(data, 0, messageBuffer, messageBufferLength, data.Length);
                    messageBufferLength += data.Length;
                }
                while (messageBufferLength >= Message.PacketLengthMin)
                {
                    Message m = Message.TryCreate(messageBuffer, messageBufferLength);
                    if (m == null)
                    {
                        if (!Message.CanStartWith(messageBuffer, messageBufferLength))
                        {
                            Logger.Warning("Buffer skip: non-iBus data detected: " + messageBuffer[0].ToHex());
                            SkipBuffer(1);
                            continue;
                        }
                        return;
                    }
                    ProcessMessage(m);
                    //#if DEBUG
                    //m.PerformanceInfo.TimeEnqueued = DateTime.Now;
                    //#endif
                    //messageReadQueue.Enqueue(m);
                    SkipBuffer(m.PacketLength);
                }
                lastMessage = DateTime.Now;
            }
        }

        protected void SkipBuffer(int count)
        {
            messageBufferLength -= count;
            if (messageBufferLength > 0)
            {
                Array.Copy(messageBuffer, count, messageBuffer, 0, messageBufferLength);
            }
        }

        public void ProcessMessage(Message m)
        {
            #if DEBUG
            m.PerformanceInfo.TimeStartedProcessing = DateTime.Now;
            #endif

            MessageEventArgs args = null;
            try
            {
                var e = BeforeMessageReceived;
                if (e != null)
                {
                    args = new MessageEventArgs(m);
                    e(args);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "on before message received " + m.ToPrettyString());
            }

            if (args != null && args.Cancel)
            {
                return;
            }

            foreach (MessageReceiverRegistration receiver in MessageReceiverList.ToArray())
            {
                try
                {
                    receiver.Process(m);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "while processing message: " + m.ToPrettyString());
                }
            }

            #if DEBUG
            m.PerformanceInfo.TimeEndedProcessing = DateTime.Now;
            #endif

            try
            {
                var e = AfterMessageReceived;
                if (e != null)
                {
                    if (args == null)
                    {
                        args = new MessageEventArgs(m);
                    }
                    e(args);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "on after message received " + m.ToPrettyString());
            }
        }

        #endregion

        #region Message writing and queue

        void SendMessage(object o)
        {
            if (o is byte[])
            {
                _port.Write((byte[])o);
                Thread.Sleep(_port.AfterWriteDelay);
                return;
            }

            Message m = (Message)o;

            #if DEBUG
            m.PerformanceInfo.TimeStartedProcessing = DateTime.Now;
            #endif

            MessageEventArgs args = null;
            var e = BeforeMessageSent;
            if (e != null)
            {
                args = new MessageEventArgs(m);
                e(args);
                if (args.Cancel)
                {
                    return;
                }
            }

            _port.Write(m.Packet);

            #if DEBUG
            m.PerformanceInfo.TimeEndedProcessing = DateTime.Now;
            #endif

            e = AfterMessageSent;
            if (e != null)
            {
                if (args == null)
                {
                    args = new MessageEventArgs(m);
                }
                e(args);
            }

            Thread.Sleep(m.AfterSendDelay > 0 ? m.AfterSendDelay : _port.AfterWriteDelay); // Don't flood iBus
        }

        public void EnqueueMessage(Message m)
        {
            #if DEBUG
            m.PerformanceInfo.TimeEnqueued = DateTime.Now;
            #endif
            try
            {
                messageWriteQueue.Enqueue(m);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void EnqueueMessage(params Message[] messages)
        {
            #if DEBUG
            var now = DateTime.Now;
            foreach (Message m in messages)
            {
                if (m != null)
                {
                    m.PerformanceInfo.TimeEnqueued = now;
                }
            }
            #endif
            messageWriteQueue.EnqueueArray(messages);
        }

        #endregion

        #region Message receiver registration

        /// <summary>
        /// Fired before processing the message by registered receivers.
        /// Message processing could be cancelled in this event
        /// </summary>
        public event MessageEventHandler BeforeMessageReceived;

        /// <summary>
        /// Fired after processing the message by registered receivers
        /// </summary>
        public event MessageEventHandler AfterMessageReceived;

        /// <summary>
        /// Fired before sending the message.
        /// Message processing could be cancelled in this event
        /// </summary>
        public event MessageEventHandler BeforeMessageSent;

        /// <summary>
        /// Fired after sending the message
        /// </summary>
        public event MessageEventHandler AfterMessageSent;

        ArrayList messageReceiverList;

        protected ArrayList MessageReceiverList
        {
            get
            {
                if (messageReceiverList == null)
                {
                    messageReceiverList = new ArrayList();
                }
                return messageReceiverList;
            }
            private set { messageReceiverList = value; }
        }

        public void AddMessageReceiverForSourceDevice(DeviceAddress source, MessageReceiver callback)
        {
            MessageReceiverList.Add(new MessageReceiverRegistration(source, DeviceAddress.Unset, callback, MessageReceiverRegistration.MatchType.Source));
        }

        public void AddMessageReceiverForDestinationDevice(DeviceAddress destination, MessageReceiver callback)
        {
            MessageReceiverList.Add(new MessageReceiverRegistration(DeviceAddress.Unset, destination, callback, MessageReceiverRegistration.MatchType.Destination));
        }

        public void AddMessageReceiverForSourceAndDestinationDevice(DeviceAddress source, DeviceAddress destination, MessageReceiver callback)
        {
            MessageReceiverList.Add(new MessageReceiverRegistration(source, destination, callback, MessageReceiverRegistration.MatchType.SourceAndDestination));
        }

        public void AddMessageReceiverForSourceOrDestinationDevice(DeviceAddress source, DeviceAddress destination, MessageReceiver callback)
        {
            MessageReceiverList.Add(new MessageReceiverRegistration(source, destination, callback, MessageReceiverRegistration.MatchType.SourceOrDestination));
        }

        #endregion

        #region Device searching on iBus

        const int findDeviceTimeout = 2000;

        DeviceAddress findDevice;
        ManualResetEvent findDeviceSync = new ManualResetEvent(false);
        ArrayList foundDevices = new ArrayList();

        public bool FindDevice(DeviceAddress device)
        {
            return FindDevice(device, findDeviceTimeout);
        }

        public bool FindDevice(DeviceAddress device, int timeout)
        {
            if (foundDevices.Contains(device))
            {
                return true;
            }
            lock (foundDevices)
            {
                findDevice = device;
                findDeviceSync.Reset(); 
                AfterMessageReceived += SaveFoundDevice;
                EnqueueMessage(new Message(DeviceAddress.Diagnostic, device, MessageRegistry.DataPollRequest));
                findDeviceSync.WaitOne(timeout, true);
                AfterMessageReceived -= SaveFoundDevice;
                return foundDevices.Contains(device);
            }
        }

        void SaveFoundDevice(MessageEventArgs e)
        {
            if (!foundDevices.Contains(e.Message.SourceDevice))
            {
                foundDevices.Add(e.Message.SourceDevice);
            }
            if (findDevice == e.Message.SourceDevice)
            {
                findDeviceSync.Set();
            }
        }

        #endregion
    }
}
