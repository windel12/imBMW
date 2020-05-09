using System;
using System.Text;
using imBMW.iBus;
using imBMW.iBus.Devices.Real;
using imBMW.Multimedia;
using imBMW.Tools;
using Microsoft.SPOT;

namespace imBMW.Features.Multimedia
{
    public enum PlaybackState
    {
        Stop = 0x01,
        Pause = 0x02,
        Play = 0x03,
    }

    public class VolumioUartPlayer : AudioPlayerBase
    {
        public PlaybackState CurrentPlaybackState { get; set; }
        public string CurrentTitle { get; set; }

        public VolumioUartPlayer()
        {
            Inited = true;
            VolumioManager.Instance.AddMessageReceiverForSourceDevice(DeviceAddress.Volumio, ProcessVolumioMessage);
        }

        public void ProcessVolumioMessage(Message m)
        {
            if (m.Data[0] == 0x01 && m.Data[1] == (byte)PlaybackState.Stop)
            {
                CurrentPlaybackState = PlaybackState.Stop;
            }
            if (m.Data[0] == 0x01 && m.Data[1] == (byte)PlaybackState.Pause)
            {
                CurrentPlaybackState = PlaybackState.Pause;
            }
            if (m.Data[0] == 0x01 && m.Data[1] == (byte)PlaybackState.Play)
            {
                var prevState = CurrentPlaybackState;
                CurrentPlaybackState = PlaybackState.Play;
                if (CurrentPlaybackState != prevState)
                {
                    var titleBytes = m.Data.Skip(2);
                    string title = new string(Encoding.UTF8.GetChars(titleBytes));
                    CurrentTitle = title;
                    OnTrackChanged(title);
                }
            }

            if (m.Data[0] == 0x02 && m.Data[1] == 0x02) // reboot
            {
                var messageBytes = m.Data.Skip(2);
                string message = new string(Encoding.UTF8.GetChars(messageBytes));
                InstrumentClusterElectronics.ShowNormalTextWithoutGong(message);
            }
            if (m.Data[0] == 0x02 && m.Data[1] == 0x03) // shutdown
            {
                var messageBytes = m.Data.Skip(2);
                string message = new string(Encoding.UTF8.GetChars(messageBytes));
                InstrumentClusterElectronics.ShowNormalTextWithoutGong(message);
            }
        }

        public override void Pause()
        {
            VolumioManager.Instance.EnqueueMessage(new Message(DeviceAddress.imBMW, DeviceAddress.Volumio, 0x01, 0x02));
        }

        public override void Play()
        {
            VolumioManager.Instance.EnqueueMessage(new Message(DeviceAddress.imBMW, DeviceAddress.Volumio, 0x01, 0x03));
        }

        public override void Prev()
        {
            VolumioManager.Instance.EnqueueMessage(new Message(DeviceAddress.imBMW, DeviceAddress.Volumio, 0x01, 0x04));
        }

        public override void Next()
        {
            VolumioManager.Instance.EnqueueMessage(new Message(DeviceAddress.imBMW, DeviceAddress.Volumio, 0x01, 0x05));
        }

        

        public override string ChangeTrackTo(string fileName)
        {
            return "";
        }

        public override bool RandomToggle(byte diskNumber)
        {
            return true;
        }

        public static void Reboot()
        {
            VolumioManager.Instance.EnqueueMessage(new Message(DeviceAddress.imBMW, DeviceAddress.Volumio, 0x02, 0x02));
        }

        public static void Shutdown()
        {
            VolumioManager.Instance.EnqueueMessage(new Message(DeviceAddress.imBMW, DeviceAddress.Volumio, 0x02, 0x03));
        }
    }
}
