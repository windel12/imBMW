using System;
using System.Text;
using System.Threading;
using imBMW.iBus;
using imBMW.iBus.Devices.Real;
using imBMW.Tools;

namespace imBMW.Features.Multimedia
{
    public enum VolumioCommands
    {
        Common = 0x00,
        Playback = 0x01,
        System = 0x02
    }

    public enum CommonCommands
    {
        Init = 0x00
    }

    public enum PlaybackState
    {
        Stop = 0x01,
        Pause = 0x02,
        Play = 0x03
    }

    public enum SystemCommands
    {
        Ping = 0x01,
        Reboot = 0x02,
        Shutdown = 0x03
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
            if (m.Data[0] == (byte)VolumioCommands.Common)
            {
                if (m.Data[1] == (byte)CommonCommands.Init)
                {
                    var titleBytes = m.Data.Skip(2);
                    string message = new string(Encoding.UTF8.GetChars(titleBytes));
                    InstrumentClusterElectronics.ShowNormalTextWithGong(message, mode: TextMode.WithGong3);
                }
            }

            if (m.Data[0] == (byte) VolumioCommands.Playback)
            {
                if (m.Data[1] == (byte)PlaybackState.Stop)
                {
                    CurrentPlaybackState = PlaybackState.Stop;
                }
                if (m.Data[1] == (byte)PlaybackState.Pause)
                {
                    CurrentPlaybackState = PlaybackState.Pause;
                }
                if (m.Data[1] == (byte)PlaybackState.Play)
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
            }

            if (m.Data[0] == (byte) VolumioCommands.System)
            {
                if (m.Data[1] == (byte)SystemCommands.Reboot)
                {
                    var messageBytes = m.Data.Skip(2);
                    string message = new string(Encoding.UTF8.GetChars(messageBytes));
                    Thread.Sleep(300);
                    InstrumentClusterElectronics.ShowNormalTextWithoutGong(message);
                }
                if (m.Data[1] == (byte)SystemCommands.Shutdown)
                {
                    var e = ShuttedDown;
                    if (e != null)
                    {
                        var messageBytes = m.Data.Skip(2);
                        string message = new string(Encoding.UTF8.GetChars(messageBytes));
                        ShuttedDown(message);
                    }
                }
            }
        }

        public override void Pause()
        {
            VolumioManager.Instance.EnqueueMessage(new Message(DeviceAddress.imBMW, DeviceAddress.Volumio, (byte)VolumioCommands.Playback, (byte)PlaybackState.Pause));
            IsPlaying = false;
        }

        public override void Play()
        {
            VolumioManager.Instance.EnqueueMessage(new Message(DeviceAddress.imBMW, DeviceAddress.Volumio, (byte)VolumioCommands.Playback, (byte)PlaybackState.Play));
            IsPlaying = true;
        }

        public override void Prev()
        {
            VolumioManager.Instance.EnqueueMessage(new Message(DeviceAddress.imBMW, DeviceAddress.Volumio, (byte)VolumioCommands.Playback, (byte)PlaybackState.Pause));
            Thread.Sleep(Settings.Instance.Delay1);

            VolumioManager.Instance.EnqueueMessage(new Message(DeviceAddress.imBMW, DeviceAddress.Volumio, (byte)VolumioCommands.Playback, 0x04));
        }

        public override void Next()
        {
            VolumioManager.Instance.EnqueueMessage(new Message(DeviceAddress.imBMW, DeviceAddress.Volumio, (byte)VolumioCommands.Playback, (byte)PlaybackState.Pause));
            Thread.Sleep(Settings.Instance.Delay1);

            VolumioManager.Instance.EnqueueMessage(new Message(DeviceAddress.imBMW, DeviceAddress.Volumio, (byte)VolumioCommands.Playback, 0x05));
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
            VolumioManager.Instance.EnqueueMessage(new Message(DeviceAddress.imBMW, DeviceAddress.Volumio, (byte)VolumioCommands.System, (byte)SystemCommands.Reboot));
        }

        public static void Shutdown()
        {
            VolumioManager.Instance.EnqueueMessage(new Message(DeviceAddress.imBMW, DeviceAddress.Volumio, (byte)VolumioCommands.System, (byte)SystemCommands.Shutdown));
        }

        public static event ActionString ShuttedDown;
    }
}
