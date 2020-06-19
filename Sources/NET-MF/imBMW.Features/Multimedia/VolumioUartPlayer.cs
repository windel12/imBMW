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
        Play = 0x03,
        Next = 0x04,
        Prev = 0x05
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
                    InstrumentClusterElectronics.ShowNormalTextWithGong(message, mode: TextMode.WithGong2);
                    m.ReceiverDescription = "Init";
                }
            }

            if (m.Data[0] == (byte) VolumioCommands.Playback)
            {
                if (m.Data[1] == (byte)PlaybackState.Stop)
                {
                    CurrentPlaybackState = PlaybackState.Stop;
                    m.ReceiverDescription = "Stop";
                }
                if (m.Data[1] == (byte)PlaybackState.Pause)
                {
                    CurrentPlaybackState = PlaybackState.Pause;
                    m.ReceiverDescription = "Pause";
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
                    m.ReceiverDescription = "Play";
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
                    m.ReceiverDescription = "Reboot";
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
                    m.ReceiverDescription = "Shutdown";
                }
            }
        }

        public override void Pause()
        {
            VolumioManager.Instance.EnqueueMessage(new Message(DeviceAddress.imBMW, DeviceAddress.Volumio, "Pause", (byte)VolumioCommands.Playback, (byte)PlaybackState.Pause));
            IsPlaying = false;
        }

        public override void Play()
        {
            VolumioManager.Instance.EnqueueMessage(new Message(DeviceAddress.imBMW, DeviceAddress.Volumio, "Play", (byte)VolumioCommands.Playback, (byte)PlaybackState.Play));
            IsPlaying = true;
        }

        public override void Prev()
        {
            VolumioManager.Instance.EnqueueMessage(new Message(DeviceAddress.imBMW, DeviceAddress.Volumio, "Pause", (byte)VolumioCommands.Playback, (byte)PlaybackState.Pause));
            Thread.Sleep(Settings.Instance.Delay1);

            VolumioManager.Instance.EnqueueMessage(new Message(DeviceAddress.imBMW, DeviceAddress.Volumio, "Prev", (byte)VolumioCommands.Playback, (byte)PlaybackState.Next));
        }

        public override void Next()
        {
            VolumioManager.Instance.EnqueueMessage(new Message(DeviceAddress.imBMW, DeviceAddress.Volumio, "Pause", (byte)VolumioCommands.Playback, (byte)PlaybackState.Pause));
            Thread.Sleep(Settings.Instance.Delay1);

            VolumioManager.Instance.EnqueueMessage(new Message(DeviceAddress.imBMW, DeviceAddress.Volumio, "Next", (byte)VolumioCommands.Playback, (byte)PlaybackState.Prev));
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
            VolumioManager.Instance.EnqueueMessage(new Message(DeviceAddress.imBMW, DeviceAddress.Volumio, "Reboot", (byte)VolumioCommands.System, (byte)SystemCommands.Reboot));
        }

        public static void Shutdown()
        {
            VolumioManager.Instance.EnqueueMessage(new Message(DeviceAddress.imBMW, DeviceAddress.Volumio, "Shutdown", (byte)VolumioCommands.System, (byte)SystemCommands.Shutdown));
        }

        public static event ActionString ShuttedDown;
    }
}
