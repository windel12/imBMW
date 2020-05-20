using System;
using System.Text;
using System.Linq;
using imBMW.Features.Multimedia;
using imBMW.iBus;

namespace OnBoardMonitorEmulator.DevicesEmulation
{
    public static class VolumioUartPlayerEmulator
    {
        static VolumioUartPlayerEmulator()
        {
            VolumioManager.Instance.AddMessageReceiverForDestinationDevice(DeviceAddress.Volumio, ProcessToVolumioMessage);
        }

        public static void Init()
        {
            byte[] commands = new byte[2] { (byte)VolumioCommands.Common, (byte)CommonCommands.Init };
            byte[] message = Encoding.UTF8.GetBytes("Volumio READY!");
            VolumioManager.Instance.EnqueueMessage(new Message(DeviceAddress.Volumio, DeviceAddress.imBMW, commands.Concat(message).ToArray()));
        }

        static void ProcessToVolumioMessage(Message m)
        {
            if (m.Data[0] == (byte)VolumioCommands.System)
            {
                if (m.Data[1] == (byte)SystemCommands.Reboot)
                {
                    byte[] commands = new byte[2] { (byte)VolumioCommands.System, (byte)SystemCommands.Reboot };
                    byte[] message = Encoding.UTF8.GetBytes("Goint to reboot");
                    VolumioManager.Instance.EnqueueMessage(new Message(DeviceAddress.Volumio, DeviceAddress.imBMW, commands.Concat(message).ToArray()));
                }
                if (m.Data[1] == (byte)SystemCommands.Shutdown)
                {
                    byte[] commands = new byte[2] { (byte)VolumioCommands.System, (byte)SystemCommands.Shutdown };
                    byte[] message = Encoding.UTF8.GetBytes("Goint to reboot");
                    VolumioManager.Instance.EnqueueMessage(new Message(DeviceAddress.Volumio, DeviceAddress.imBMW, commands.Concat(message).ToArray()));
                }
            }
        }
    }
}
