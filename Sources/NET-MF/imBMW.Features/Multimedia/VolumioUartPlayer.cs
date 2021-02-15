using System;
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
        //public string CurrentTrackTitle { get; set; }
        public short CurrentTrackDuration { get; set; }

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

                    //Logger.Trace("waking up KBus, to be in sync with IBus, because Volumio init message on IKE is waking up IBus"); // // (see "traces\2020.10.04_GarageRacer\traceLog42.log")
                    //BodyModule.RequestDoorWindowStatusViaIbus(); 
                }
                if (m.Data[1] == (byte) CommonCommands.DisplayMessage)
                {
                    var titleBytes = m.Data.Skip(2);
                    string message = new string(Encoding.UTF8.GetChars(titleBytes));
                    InstrumentClusterElectronics.ShowNormalTextWithoutGong(message);
                }
                if (m.Data[1] == (byte)CommonCommands.DisplayMessageWithGong)
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
                    CurrentTrackDuration = (short)((m.Data[m.Data.Length - 2] << 8) + m.Data[m.Data.Length - 1]);

                    var prevState = CurrentPlaybackState;
                    CurrentPlaybackState = PlaybackState.Play;

                    var titleBytes = m.Data.SkipAndTake(2, m.Data.Length - 2 - 2);
                    string title = new string(Encoding.UTF8.GetChars(titleBytes));
                    title = title.Trim('"');
                    var prevTitle = CurrentTrackTitle;
                    CurrentTrackTitle = title;

                    if (CurrentPlaybackState != prevState || CurrentPlaybackState == PlaybackState.Play && CurrentTrackTitle != prevTitle)
                    {
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
            VolumioManager.Instance.EnqueueMessage(new Message(DeviceAddress.imBMW, DeviceAddress.Volumio, "Prev", (byte)VolumioCommands.Playback, (byte)PlaybackState.Prev));
        }

        public override void Next()
        {
            //if (Settings.Instance.Delay1 == 0)
            //{
                VolumioManager.Instance.EnqueueMessage(new Message(DeviceAddress.imBMW, DeviceAddress.Volumio, "Next", (byte) VolumioCommands.Playback, (byte) PlaybackState.Next));
            //}
            //else
            //{
            //    byte offset = 0;
            //    try
            //    {
            //        offset = (byte) (Settings.Instance.Delay4 / 1000);
            //    }
            //    catch
            //    {
            //        offset = 0;
            //    }
            //    byte position_h = (byte) (CurrentTrackDuration >> 8);
            //    byte position_l = (byte) ((CurrentTrackDuration & 0x00FF) - offset);
            //    VolumioManager.Instance.EnqueueMessage(new Message(DeviceAddress.imBMW, DeviceAddress.Volumio, "Seek", (byte) VolumioCommands.Playback, (byte) PlaybackState.Seek, position_h, position_l));
            //}
        }
        
        public override void ClearQueue()
        {
            VolumioManager.Instance.EnqueueMessage(new Message(DeviceAddress.imBMW, DeviceAddress.Volumio, "Clear queue", (byte) VolumioCommands.ClearQueue));
        }

        public override void AddPlaylistToQueue(byte number)
        {
            VolumioManager.Instance.EnqueueMessage(new Message(DeviceAddress.imBMW, DeviceAddress.Volumio, "Select playlist #" + number, (byte)VolumioCommands.AddPlaylistToQueue, number));
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
