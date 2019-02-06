using System;
using System.IO.Ports;
using Microsoft.SPOT;

namespace imBMW.iBus
{
    public class KBusManager : ManagerImpl
    {
        public static string PORT_NAME = "kBus";
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

        public static void Init(ISerialPort port)
        {
            if (!Instance.Inited)
            {
                Instance.InitPort(port, KBusManager.PORT_NAME);
            }
            else
            {
                throw new Exception(nameof(KBusManager) + " already inited.");
            }
        }
    }
}
