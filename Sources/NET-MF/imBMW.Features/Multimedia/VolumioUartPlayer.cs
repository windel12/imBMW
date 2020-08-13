using System.Text;
using imBMW.Enums;
using imBMW.Enums.Volumio;
using imBMW.iBus;
using imBMW.iBus.Devices.Real;
using imBMW.Tools;

namespace imBMW.Features.Multimedia
{
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
                        // TODO: it sends "CDC > RAD: 39 02 09 00 00 00 01 01 {Play, CommonPlayback}" second time after first play
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
                    ThreadSleep(300);
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
            if (Settings.Instance.Delay1 == 0)
            {
                VolumioManager.Instance.EnqueueMessage(new Message(DeviceAddress.imBMW, DeviceAddress.Volumio, "Stop", (byte)VolumioCommands.Playback, (byte)PlaybackState.Stop));
            }
            else
            {
                VolumioManager.Instance.EnqueueMessage(new Message(DeviceAddress.imBMW, DeviceAddress.Volumio, "Pause", (byte)VolumioCommands.Playback, (byte)PlaybackState.Pause));
            }
            ThreadSleep(Settings.Instance.Delay2);

            VolumioManager.Instance.EnqueueMessage(new Message(DeviceAddress.imBMW, DeviceAddress.Volumio, "Prev", (byte)VolumioCommands.Playback, (byte)PlaybackState.Prev));
        }

        public override void Next()
        {
            if (Settings.Instance.Delay1 == 0)
            {
                VolumioManager.Instance.EnqueueMessage(new Message(DeviceAddress.imBMW, DeviceAddress.Volumio, "Stop", (byte)VolumioCommands.Playback, (byte)PlaybackState.Stop));
            }
            else
            {
                VolumioManager.Instance.EnqueueMessage(new Message(DeviceAddress.imBMW, DeviceAddress.Volumio, "Pause", (byte)VolumioCommands.Playback, (byte)PlaybackState.Pause));
            }
            ThreadSleep(Settings.Instance.Delay2);

            VolumioManager.Instance.EnqueueMessage(new Message(DeviceAddress.imBMW, DeviceAddress.Volumio, "Next", (byte)VolumioCommands.Playback, (byte)PlaybackState.Next));
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

        private static void ThreadSleep(int millisecondTimeout)
        {
            if (millisecondTimeout != 0)
            {
                System.Threading.Thread.Sleep(millisecondTimeout);
            }
        }

        public static event ActionString ShuttedDown;
    }
}
