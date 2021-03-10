using System;
using System.IO.Ports;
using imBMW.Enums;
using imBMW.Tools;

namespace imBMW.iBus
{
    public class Manager : ManagerImpl
    {
        private static Manager _instance;
        public static Manager Instance
        {
            get
            {
                if (_instance == null/* || !_instance.Inited*/)
                {
                    //throw new Exception(nameof(Manager) + " should be firstly be inited.");
                    _instance = new Manager();
                }
                return _instance;
            }
        }

        private Manager()
        {
        }

        public static void Init(ISerialPort port)
        {
            if (!Instance.Inited)
            {
                Instance.InitPort(port, QueueThreadName.iBus);
            }
            else
            {
                throw new Exception(nameof(Manager) + " already inited.");
            }
        }
    }
}
