using System;
using System.Collections;
using System.IO.Ports;
using System.Threading;
using imBMW.Tools;

namespace imBMW.iBus
{
    public class ManagerImpl
    {
        protected internal ISerialPort _port;
        protected internal string _portName;
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

        public void InitPort(ISerialPort port, string queueThreadWorkerName = "", ThreadPriority threadPriority = ThreadPriority.AboveNormal)
        {
            messageWriteQueue = new QueueThreadWorker(SendMessage, queueThreadWorkerName, threadPriority);
            //messageReadQueue = new QueueThreadWorker(ProcessMessage);

            _port = port;
            _portName = queueThreadWorkerName;
            _port.DataReceived += bus_DataReceived;

            Inited = true;
        }

        public void Dispose()
        {
            messageWriteQueue.Dispose();
            if (_port != null)
            {
                _port.DataReceived -= bus_DataReceived;
                if (_port.IsOpen)
                {
                    _port.Close();
                }

                _port = null;
            }
        }

        #region Message reading and processing
        protected internal virtual void bus_DataReceived(object sender, SerialDataReceivedEventArgs e)
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
                    Logger.Warning("Buffer overflow. Extending it. " + _portName);
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
                bool shouldDisplayCorruptedMessageBuffer = true;
                while (messageBufferLength >= Message.PacketLengthMin)
                {
                    Message m = Message.TryCreate(messageBuffer, messageBufferLength);
                    if (m == null)
                    {
                        if (!Message.CanStartWith(messageBuffer, messageBufferLength))
                        {
                            if (shouldDisplayCorruptedMessageBuffer)
                            {
                                //Logger.Warning("Buffer skip: non-" + _portName + " data detected: " + messageBuffer[0].ToHex());
                                //Logger.Warning("Buffer skip: non-" + _portName + " data detected: " + messageBuffer.ToHex());
                                shouldDisplayCorruptedMessageBuffer = false;
                            }

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

                    shouldDisplayCorruptedMessageBuffer = true;
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

        protected virtual void SendData(Message m)
        {
            _port.Write(m.Packet);
        }

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

            SendData(m);

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
            m = null; // will it optimize memory usage???
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

        //#region Device searching on iBus

        //const ushort findDeviceTimeout = 2000;

        //DeviceAddress findDevice;
        //ManualResetEvent findDeviceSync = new System.Threading.ManualResetEvent(false);
        //ArrayList foundDevices = new ArrayList();

        //public bool FindDevice(DeviceAddress device)
        //{
        //    return FindDevice(device, findDeviceTimeout);
        //}

        //public bool FindDevice(DeviceAddress device, ushort timeout)
        //{
        //    if (foundDevices.Contains(device))
        //    {
        //        return true;
        //    }
        //    lock (foundDevices)
        //    {
        //        findDevice = device;
        //        findDeviceSync.Reset();
        //        AfterMessageReceived += SaveFoundDevice;
        //        EnqueueMessage(new Message(DeviceAddress.Diagnostic, device, MessageRegistry.DataPollRequest));
        //        findDeviceSync.WaitOne(timeout, true);
        //        AfterMessageReceived -= SaveFoundDevice;
        //        return foundDevices.Contains(device);
        //    }
        //}

        //void SaveFoundDevice(MessageEventArgs e)
        //{
        //    if (!foundDevices.Contains(e.Message.SourceDevice))
        //    {
        //        foundDevices.Add(e.Message.SourceDevice);
        //    }
        //    if (findDevice == e.Message.SourceDevice)
        //    {
        //        findDeviceSync.Set();
        //    }
        //}

        //#endregion
    }
}
