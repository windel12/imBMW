using System;
using System.Collections;
using System.IO.Ports;
using System.Threading;
using imBMW.Tools;

namespace imBMW.iBus
{
    public static class Manager
    {
        public static string PORT_NAME = "iBus";
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
                Instance.InitPort(port, Manager.PORT_NAME);
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
}
