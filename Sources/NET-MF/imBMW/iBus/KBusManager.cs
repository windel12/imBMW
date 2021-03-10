using System;
using System.IO.Ports;
using System.Threading;
using imBMW.Enums;
using imBMW.Tools;

namespace imBMW.iBus
{
    public class KBusManager : ManagerImpl
    {
        private static KBusManager _instance;
        public static KBusManager Instance
        {
            get
            {
                if (_instance == null/* || !_instance.Inited*/)
                {
                    //throw new Exception(nameof(KBusManager) + " should be firstly be inited.");
                    _instance = new KBusManager();
                }
                return _instance;
            }
        }

        private KBusManager()
        {
        }

        public static void Init(ISerialPort port, ThreadPriority threadPriority = ThreadPriority.AboveNormal)
        {
            if (!Instance.Inited)
            {
                Instance.InitPort(port, QueueThreadName.kBus, threadPriority);
            }
            else
            {
                throw new Exception(nameof(KBusManager) + " already inited.");
            }
        }
    }
}
