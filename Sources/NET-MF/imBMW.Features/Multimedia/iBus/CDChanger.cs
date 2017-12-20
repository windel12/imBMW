using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using System.Threading;
using imBMW.Tools;
using imBMW.iBus.Devices.Real;
using imBMW.Multimedia;
using GHI.Pins;
using imBMW.Features.Multimedia;

namespace imBMW.iBus.Devices.Emulators
{
    public class CDChanger : MediaEmulator
    {
        enum PlayingType : byte
        {
            Normal = 0x09,
            Random = 0x1D
        }

        const int StopDelayMilliseconds = 1000;

        Thread announceThread;
        Timer stopDelay;

        #region Messages

        public static Message MessagePollResponse = new Message(DeviceAddress.CDChanger, DeviceAddress.Broadcast, 0x02, 0x00);
        public static Message MessageAnnounce = new Message(DeviceAddress.CDChanger, DeviceAddress.Broadcast, 0x02, 0x01);

        //static Message MessageStoppedDisk1Track1 = new Message(DeviceAddress.CDChanger, DeviceAddress.Radio, "Stopped D1 T1", 0x39, 0x00, 0x02, 0x00, 0x3F, 0x00, 0x01, 0x01); // try 39 00 0C ?
        //static Message MessagePausedDisk1Track1  = new Message(DeviceAddress.CDChanger, DeviceAddress.Radio, "Paused D1 T1",  0x39, 0x01, 0x0C, 0x00, 0x3F, 0x00, 0x01, 0x01);
        //static Message MessagePlayingDisk1Track1 = new Message(DeviceAddress.CDChanger, DeviceAddress.Radio, "Playing D1 T1", 0x39, 0x02, 0x09, 0x00, 0x3F, 0x00, 0x01, 0x01);

        Message GetMessageStopped(byte disk, byte track)
        {
            return new Message(DeviceAddress.CDChanger, DeviceAddress.Radio, "Stopped D" + disk + "T" + track, 0x39, 0x00, 0x02, 0x00, 0x3F, 0x00, disk, track); // try 39 00 0C ?
        }
        Message GetMessagePaused(byte disk, byte track)
        {
            //return new Message(DeviceAddress.CDChanger, DeviceAddress.Radio, "Paused D" + disk + "T" + track, 0x39, 0x00, 0x02, 0x00, 0x3F, 0x00, disk, track);
            return new Message(DeviceAddress.CDChanger, DeviceAddress.Radio, "Paused D" + disk + "T" + track, 0x39, 0x00, 0x02, 0x00, 0x3F, 0x00, disk, track);
        }
        Message GetMessagePlaying(byte disk, byte track)
        {
            return new Message(DeviceAddress.CDChanger, DeviceAddress.Radio, "Playing D" + disk + "T" + track, 0x39, 0x02, 0x09, 0x00, 0x3F, 0x00, disk, track);
        }

        byte status = 0x08;
        byte ack = 0x0c;
        Message GetMessagePlaylistLoaded(byte disk, byte track)
        {
            return new Message(DeviceAddress.CDChanger, DeviceAddress.Radio, "Playlist loaded" + disk + "T" + track, 0x39, status, ack, 0x00, 0x3F, 0x00, disk, track);
        }

        Message GetMessageCDCheck(byte disk, byte track)
        {
            return new Message(DeviceAddress.CDChanger, DeviceAddress.Radio, "Playlist loaded" + disk + "T" + track, 0x39, 0x09, 0x09, 0x00, 0x3F, 0x00, disk, track);
        }

        public static byte[] DataCurrentDiskTrackRequest = new byte[] { 0x38, 0x00, 0x00 };
        public static byte[] DataStop  = new byte[] { 0x38, 0x01, 0x00 };
        public static byte[] DataPause = new byte[] { 0x38, 0x02, 0x00 };
        public static byte[] DataPlay = new byte[] { 0x38, 0x03, 0x00 };

        //public static byte[] DataNextDisk = new byte[] { 0x38, 0x05, 0x00 };
        //public static byte[] DataPrevDisk = new byte[] { 0x38, 0x05, 0x01 };

        public static byte[] DataSelectDisk1 = new byte[] { 0x38, 0x06, 0x01 };
        public static byte[] DataSelectDisk2 = new byte[] { 0x38, 0x06, 0x02 };
        public static byte[] DataSelectDisk3 = new byte[] { 0x38, 0x06, 0x03 };
        public static byte[] DataSelectDisk4 = new byte[] { 0x38, 0x06, 0x04 };
        public static byte[] DataSelectDisk5 = new byte[] { 0x38, 0x06, 0x05 };
        public static byte[] DataSelectDisk6 = new byte[] { 0x38, 0x06, 0x06 };

        public static byte[] DataScanPlaylistOff = new byte[] { 0x38, 0x07, 0x00 };
        public static byte[] DataScanPlaylistOn = new byte[] { 0x38, 0x07, 0x01 };

        public static byte[] DataRandomPlay = new byte[] { 0x38, 0x08, 0x01 };

        public static byte[] DataNext = new byte[] { 0x38, 0x0A, 0x00 };
        public static byte[] DataPrev = new byte[] { 0x38, 0x0A, 0x01 };

        #endregion

        public CDChanger(IAudioPlayer player)
            : base(player)
        {
            Manager.AddMessageReceiverForDestinationDevice(DeviceAddress.CDChanger, ProcessCDCMessage);

            Player.TrackChanged += (s, e) => Manager.EnqueueMessage(GetMessagePlaying(Player.DiskNumber, Player.TrackNumber));
            InstrumentClusterElectronics.IgnitionStateChanged += args =>
            {
                if (args.CurrentIgnitionState == IgnitionState.Acc && args.PreviousIgnitionState == IgnitionState.Ign)
                {
                    if (IsEnabled)
                    {
                        Radio.PressOnOffToggle();
                    }
                }
            };

            announceThread = new Thread(announce);
            announceThread.Start();
        }

        #region Player control

        protected override void Play()
        {
            CancelStopDelay();
            base.Play();
        }

        protected override void Pause()
        {
            CancelStopDelay();
            base.Pause();
        }

        protected override void PlayPauseToggle()
        {
            CancelStopDelay();
            base.PlayPauseToggle();
        }

        #endregion

        #region CD-changer emulation

        void CancelStopDelay()
        {
            if (stopDelay != null)
            {
                stopDelay.Dispose();
                stopDelay = null;
            }
        }

        protected override void OnIsEnabledChanged(bool isEnabled, bool fire = true)
        {
            Player.PlayerHostState = isEnabled ? PlayerHostState.On : PlayerHostState.Off;
            if (isEnabled)
            {
                if (Player.IsPlaying)
                {
                    // Already playing - CDC turning off cancelled
                    //ShowPlayerStatus(player, player.IsPlaying); // TODO need it?
                }
                else
                {
                    Play();
                }

                if (announceThread.ThreadState != ThreadState.Suspended)
                {
                    announceThread.Suspend();
                }
            }

            base.OnIsEnabledChanged(isEnabled, isEnabled); // fire only if enabled

            if (!isEnabled)
            {
                CancelStopDelay();
                // Don't pause immediately - the radio can send "start play" command soon
                stopDelay = new Timer(delegate
                {
                    FireIsEnabledChanged();
                    Pause();

                    if (announceThread.ThreadState == ThreadState.Suspended)
                    {
                        announceThread.Resume();
                    }
                }, null, StopDelayMilliseconds, 0);
            }
        }

        void ProcessCDCMessage(Message m)
        {
            if(m.Data.Length == 3 && m.Data.StartsWith(0x38, 0x06))
            {
                Player.DiskNumber = m.Data[2];
                Player.TrackNumber = 1;
                if (Player.IsPlaying)
                {
                    Manager.EnqueueMessage(GetMessagePlaying(Player.DiskNumber, Player.TrackNumber));
                }
                else
                {
                    Manager.EnqueueMessage(GetMessageStopped(Player.DiskNumber, Player.TrackNumber));
                }
            }
            if (m.Data.Compare(DataCurrentDiskTrackRequest))
            {
                if (Player.IsPlaying)
                {
                    Manager.EnqueueMessage(GetMessagePlaying(Player.DiskNumber, Player.TrackNumber));
                }
                else
                {
                    Manager.EnqueueMessage(GetMessagePaused(Player.DiskNumber, Player.TrackNumber));
                    //Manager.EnqueueMessage(GetMessageStopped(Player.DiskNumber, Player.TrackNumber));
                }
                m.ReceiverDescription = "CD status request";
            }
            else if (m.Data.Compare(DataScanPlaylistOff) || m.Data.Compare(DataScanPlaylistOn))
            {
                if (!Player.IsPlaying)
                {
                    Manager.EnqueueMessage(GetMessagePlaylistLoaded(Player.DiskNumber, Player.TrackNumber));
                }
            }
            else if (m.Data.Compare(DataStop))
            {
                IsEnabled = false;
                Manager.EnqueueMessage(GetMessagePaused(Player.DiskNumber, Player.TrackNumber));
                m.ReceiverDescription = "Stop playing";
            }
            else if (m.Data.Compare(DataPause))
            {
                //IsEnabled = false;
                Pause();
                Manager.EnqueueMessage(GetMessagePaused(Player.DiskNumber, Player.TrackNumber));
                // TODO show "splash" only with bmw business (not with BM)
                //Radio.DisplayText("imBMW", TextAlign.Center);
                m.ReceiverDescription = "Pause";
            }
            else if (m.Data.Compare(DataPlay))
            {
                IsEnabled = true;
                Manager.EnqueueMessage(GetMessagePlaying(Player.DiskNumber, Player.TrackNumber));
                m.ReceiverDescription = "Start playing";
            }
            else if(m.Data.Compare(DataNext))
            {
                Next();
            }
            else if (m.Data.Compare(DataPrev))
            {
                Prev();
            }
            else if (m.Data.Compare(MessageRegistry.DataPollRequest))
            {
                Manager.EnqueueMessage(MessagePollResponse);
                //if (Player.IsPlaying)
                //{
                //    Manager.EnqueueMessage(MessagePlayingDisk1Track1);
                //}
                //else
                //{
                //    Manager.EnqueueMessage(MessagePausedDisk1Track1);
                //}
            }
            else if (m.Data.Compare(DataRandomPlay))
            {
                RandomToggle();
                Manager.EnqueueMessage(GetMessagePlaying(Player.DiskNumber, Player.TrackNumber));
                m.ReceiverDescription = "Random toggle";
            }
            /*else if (m.Data[0] == 0x38)
            {
                // TODO remove
                Logger.Warning("Need response!!!");
            }*/
        }

        static void announce()
        {
            while (true)
            {
                Manager.EnqueueMessage(MessageAnnounce, MessagePollResponse);
                Thread.Sleep(30000);
            }
        }

        #endregion
    }
}
