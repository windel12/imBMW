using System;
using System.IO.Ports;
using System.Threading;
using imBMW.Enums;
using imBMW.Tools;

namespace imBMW.iBus
{
    public class VolumioManager : ManagerImpl
    {
        private static VolumioManager _instance;
        public static VolumioManager Instance
        {
            get
            {
                if (_instance == null/* || !_instance.Inited*/)
                {
                    //throw new Exception(nameof(VolumioManager) + " should be firstly be inited.");
                    _instance = new VolumioManager();
                }
                return _instance;
            }
        }

        private VolumioManager()
        {
        }

        public static void Init(ISerialPort port, ThreadPriority threadPriority = ThreadPriority.AboveNormal)
        {
            if (!Instance.Inited)
            {
                Instance.InitPort(port, QueueThreadName.VolumioUART, threadPriority);
            }
            else
            {
                throw new Exception(nameof(VolumioManager) + " already inited.");
            }
        }
    }
}
