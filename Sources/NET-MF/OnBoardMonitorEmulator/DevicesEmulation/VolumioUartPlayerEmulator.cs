using System;
using System.Text;
using System.Linq;
using imBMW.Features.Multimedia;
using imBMW.iBus;
using System.Threading.Tasks;
using System.Threading;

namespace OnBoardMonitorEmulator.DevicesEmulation
{
    public static class VolumioUartPlayerEmulator
    {
        static Timer Timer;

        static VolumioUartPlayerEmulator()
        {
            VolumioManager.Instance.AddMessageReceiverForDestinationDevice(DeviceAddress.Volumio, ProcessToVolumioMessage);
        }

        public static void Init()
        {
            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(2000);
                byte[] commands = new byte[2] { (byte)VolumioCommands.Common, (byte)CommonCommands.Init };
                byte[] message = Encoding.UTF8.GetBytes("Volumio READY!");
                VolumioManager.Instance.EnqueueMessage(new Message(DeviceAddress.Volumio, DeviceAddress.imBMW, commands.Concat(message).ToArray()));
            });

            Timer = new Timer((obj) =>
            {
                byte[] data = DigitalDieselElectronicsEmulator.GenerateData().Data;
                var message = new Message(DeviceAddress.Volumio, DeviceAddress.imBMW, data);
                VolumioManager.Instance.EnqueueMessage(message);
            }, null, 0, 2000);
        }

        static void ProcessToVolumioMessage(Message m)
        {
            if (m.Data[0] == (byte) VolumioCommands.Playback)
            {
                if (m.Data[1] == (byte) PlaybackState.Stop)
                {
                    byte[] commands = new byte[2] {(byte) VolumioCommands.Playback, (byte) PlaybackState.Stop};
                    byte[] message = Encoding.UTF8.GetBytes("STOP!");
                    VolumioManager.Instance.EnqueueMessage(new Message(DeviceAddress.Volumio, DeviceAddress.imBMW, commands.Concat(message).ToArray()));
                }

                if (m.Data[1] == (byte) PlaybackState.Pause)
                {
                    byte[] commands = new byte[2] {(byte) VolumioCommands.Playback, (byte) PlaybackState.Pause};
                    byte[] message = Encoding.UTF8.GetBytes("PAUSE!");
                    VolumioManager.Instance.EnqueueMessage(new Message(DeviceAddress.Volumio, DeviceAddress.imBMW, commands.Concat(message).ToArray()));
                }

                if (m.Data[1] == (byte) PlaybackState.Play)
                {
                    for(int i = 0; i < 3; i++)
                    {
                        byte[] commands = new byte[2] { (byte)VolumioCommands.Playback, (byte)PlaybackState.Play };
                        byte[] message = Encoding.UTF8.GetBytes("PLAY!");
                        VolumioManager.Instance.EnqueueMessage(new Message(DeviceAddress.Volumio, DeviceAddress.imBMW, commands.Concat(message).ToArray()));
                    }
                }
            }

            if (m.Data[0] == (byte) VolumioCommands.System)
            {
                if (m.Data[1] == (byte) SystemCommands.Reboot)
                {
                    byte[] commands = new byte[2] {(byte) VolumioCommands.System, (byte) SystemCommands.Reboot};
                    byte[] message = Encoding.UTF8.GetBytes("Goint to reboot");
                    VolumioManager.Instance.EnqueueMessage(new Message(DeviceAddress.Volumio, DeviceAddress.imBMW, commands.Concat(message).ToArray()));
                }

                if (m.Data[1] == (byte) SystemCommands.Shutdown)
                {
                    byte[] commands = new byte[2] {(byte) VolumioCommands.System, (byte) SystemCommands.Shutdown};
                    byte[] message = Encoding.UTF8.GetBytes("Goint to reboot");
                    VolumioManager.Instance.EnqueueMessage(new Message(DeviceAddress.Volumio, DeviceAddress.imBMW, commands.Concat(message).ToArray()));
                }
            }


            if (m.Data[0] == (byte) 0x6C && m.Data[1] == (byte)0x10)
            {
                
            }
        }
    }
}
