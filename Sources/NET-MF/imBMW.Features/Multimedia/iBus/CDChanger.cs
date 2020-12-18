using System;
using System.Threading;
using imBMW.iBus;
using imBMW.iBus.Devices.Real;
using imBMW.Tools;
using imBMW.Enums;

namespace imBMW.Features.Multimedia.iBus
{
    public class CDChanger : MediaEmulator
    {
        //Thread announceThread;
        Timer stopDelay;

        bool skipNextTrackMessage = false;

        private static byte disksMask = 0x00; // 0x3F

        #region Messages

        /// <summary> 02 00 </summary>
        public static imBMW.iBus. Message MessagePollResponse = new Message(DeviceAddress.CDChanger, DeviceAddress.LocalBroadcastAddress, 0x02, 0x00);
        /// <summary> 02 01 </summary>
        public static Message MessageAnnounce = new Message(DeviceAddress.CDChanger, DeviceAddress.LocalBroadcastAddress, 0x02, 0x01);

        /// <summary>0x39, 0x00, 0x02 </summary>
        public Message StatusStopped(byte disk, byte track)
        {
            return new Message(DeviceAddress.CDChanger, DeviceAddress.Radio, "Stop, Silence", 0x39, 0x00, 0x02, 0x00, disksMask, 0x00, disk, track); // try 39 00 0C ?
        }

        /// <summary>0x39, 0x02, 0x09 </summary>
        public Message StatusPlaying(byte disk, byte track)
        {
            return new Message(DeviceAddress.CDChanger, DeviceAddress.Radio, "Play, CommonPlayback", 0x39, 0x02, 0x09, 0x00, disksMask, 0x00, disk, track);
        }

        /// <summary>0x39, 0x07, 0x09 </summary>
        Message StatusEndTrack(byte disk, byte track)
        {
            return new Message(DeviceAddress.CDChanger, DeviceAddress.Radio, "End, CommonPlayback", 0x39, 0x07, 0x09, 0x00, disksMask, 0x00, disk, track);
        }

        byte status = 0x08;
        byte ack = 0x0c;
        Message GetMessagePlaylistLoaded(byte disk, byte track)
        {
            return new Message(DeviceAddress.CDChanger, DeviceAddress.Radio, "Playlist loaded" + disk + "T" + track, 0x39, status, ack, 0x00, disksMask, 0x00, disk, track);
        }

        //Message GetMessageCDCheck(byte disk, byte track)
        //{
        //    return new Message(DeviceAddress.CDChanger, DeviceAddress.Radio, "Playlist loaded" + disk + "T" + track, 0x39, 0x09, 0x09, 0x00, disksMask, 0x00, disk, track);
        //}

        /// <summary>0x38, 0x00, 0x00 </summary>
        public static byte[] DataStatusRequest = new byte[] { 0x38, 0x00, 0x00 };
        /// <summary>0x38, 0x01, 0x00 </summary>
        public static byte[] DataStop  = new byte[] { 0x38, 0x01, 0x00 };
        /// <summary>0x38, 0x02, 0x00 </summary>
        public static byte[] DataPause = new byte[] { 0x38, 0x02, 0x00 };
        /// <summary>0x38, 0x03, 0x00 </summary>
        public static byte[] DataPlay = new byte[] { 0x38, 0x03, 0x00 };

        //public static byte[] DataNextDisk = new byte[] { 0x38, 0x05, 0x00 };
        //public static byte[] DataPrevDisk = new byte[] { 0x38, 0x05, 0x01 };

        public static byte[] GetDataSelectDisk(byte diskNumber) => new byte[] { 0x38, 0x06, diskNumber };

        public static byte[] DataScanPlaylistOff = new byte[] { 0x38, 0x07, 0x00 };
        public static byte[] DataScanPlaylistOn = new byte[] { 0x38, 0x07, 0x01 };

        /// <summary>0x38, 0x08 </summary>
        public static byte[] DataSetRandom = new byte[] { 0x38, 0x08 };

        public static byte[] DataNext = new byte[] { 0x38, 0x0A, 0x00 };
        public static byte[] DataPrev = new byte[] { 0x38, 0x0A, 0x01 };

        #endregion

        public CDChanger(IAudioPlayer player) : base(player)
        {
            if (!Settings.Instance.SuspendCDChangerResponseEmulation)
            {
                Manager.Instance.AddMessageReceiverForDestinationDevice(DeviceAddress.CDChanger, ProcessCDCMessage);
                Manager.Instance.AddMessageReceiverForDestinationDevice(DeviceAddress.Radio, ProcessToRadioMessage);

                MultiFunctionSteeringWheel.ButtonPressed += button =>
                {
                    if (button == MFLButton.VolumeDown/* || button == MFLButton.VolumeUp*/)
                    {
                        skipNextTrackMessage = true;

                        CancelStopDelay();
                        stopDelay = new Timer(delegate
                        {
                            skipNextTrackMessage = false;
                            CancelStopDelay();
                        }, null, 1000, 0);
                    }
                    if (button == MFLButton.Dial)
                    {
                        DigitalSignalProcessingAudioAmplifier.ChangeSource(AudioSource.CD);
                    }
                };

                //Radio.OnOffChanged += Radio_OnOffChanged;

                Player.IsPlayingChanged += (s, isPlaying) =>
                {
                    if (isPlaying)
                        Manager.Instance.EnqueueMessage(StatusPlaying(Player.DiskNumber, Player.TrackNumber));
                    else
                        Manager.Instance.EnqueueMessage(StatusStopped(Player.DiskNumber, Player.TrackNumber));
                };
                Player.TrackChanged += (s, e) =>
                {
                    //Manager.Instance.EnqueueMessage(StatusEndTrack(Player.DiskNumber, Player.TrackNumber));

                    // do not answer "39 02 09 00 00 00 disk track" to avoid showing label on IKE and GND
                    //Manager.Instance.EnqueueMessage(StatusPlaying(Player.DiskNumber, ++Player.TrackNumber));
                    };

                //announceThread = new Thread(announceCallback);
                //announceThread.Start();

                announce();
            }
        }

        private void Radio_OnOffChanged(bool turnedOn)
        {
            IsEnabled = turnedOn;
        }

        #region Player control

        public override void Play()
        {
            //CancelStopDelay();
            base.Play();
        }

        public override void Pause()
        {
            //CancelStopDelay();
            base.Pause();
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

                //if (announceThread.ThreadState != ThreadState.Suspended)
                //{
                //    announceThread.Suspend();
                //}
            }

            base.OnIsEnabledChanged(isEnabled, isEnabled); // fire only if enabled

            if (!isEnabled)
            {
                //CancelStopDelay();
                // Don't pause immediately - the radio can send "start play" command soon
                //stopDelay = new Timer(delegate
                //{
                    FireIsEnabledChanged();
                    Pause();

                //if (announceThread.ThreadState == ThreadState.Suspended)
                //{
                //    announceThread.Resume();
                //}
                //}, null, 1000, 0);
            }
        }

        void ProcessCDCMessage(Message m)
        {
            if (m.Data.Compare(DataStatusRequest))
            {
                if (Player.IsPlaying && this.IsEnabled)
                {
                    Manager.Instance.EnqueueMessage(StatusPlaying(Player.DiskNumber, Player.TrackNumber));
                }
                else
                {
                    Manager.Instance.EnqueueMessage(StatusStopped(Player.DiskNumber, Player.TrackNumber));
                }
            }
            else if (m.Data.Compare(DataStop))
            {
                IsEnabled = false;
            }
            else if (m.Data.Compare(DataPause))
            {
                Pause();
            }
            else if (m.Data.Compare(DataPlay))
            {
                IsEnabled = true;
            }
            else if(m.Data.Length == 3 && m.Data.StartsWith(0x38, 0x06)) // select disk
            {
                if (Player.IsPlaying)
                {
                    Manager.Instance.EnqueueMessage(StatusPlaying(Player.DiskNumber, Player.TrackNumber));
                }
                else
                {
                    Manager.Instance.EnqueueMessage(StatusStopped(Player.DiskNumber, Player.TrackNumber));
                }
            }
            else if (m.Data.StartsWith(DataSetRandom))
            {
                Player.IsRandom = m.Data[2] == 0x01;
                //RandomToggle(Player.DiskNumber);
                //Manager.Instance.EnqueueMessage(StatusPlaying(Player.DiskNumber, Player.TrackNumber));
            }
            else if (m.Data.Compare(DataScanPlaylistOff) || m.Data.Compare(DataScanPlaylistOn))
            {
                if (!Player.IsPlaying)
                {
                    Manager.Instance.EnqueueMessage(GetMessagePlaylistLoaded(Player.DiskNumber, Player.TrackNumber));
                }
            }
            else if(m.Data.Compare(DataNext))
            {
                if (!skipNextTrackMessage)
                {
                    Next();
                }
                else
                {
                    skipNextTrackMessage = false;
                    Logger.Warning("Errorneous 'Next' command was successfully skipped");
                }
            }
            else if (m.Data.Compare(DataPrev))
            {
                Prev();
            }
            else if (m.Data.Compare(MessageRegistry.DataPollRequest))
            {
                Manager.Instance.EnqueueMessage(MessagePollResponse);
            }
        }

        void ProcessToRadioMessage(Message m)
        {
            if (m.Data.Length == 2 && m.Data[0] == 0x48)
            {
                if (m.Data[1] == 0x06 && IsEnabled) // radio knob off
                {
                    IsEnabled = false;
                }

                if (m.Data[1] == 0x51 || m.Data[1] == 0x41 || m.Data[1] == 0x52 || m.Data[1] == 0x42 || m.Data[1] == 0x53 || m.Data[1] == 0x43) // Hold any button
                {
                    Player.ClearQueue();
                    Logger.Trace("Player.ClearQueue()");
                }

                if (m.Data[1] == 0x91) // release 1
                    Player.AddPlaylistToQueue(1);
                if (m.Data[1] == 0x81) // release 2
                    Player.AddPlaylistToQueue(2);
                if (m.Data[1] == 0x92) // release 3
                    Player.AddPlaylistToQueue(3);
                if (m.Data[1] == 0x82) // release 4
                    Player.AddPlaylistToQueue(4);
                if (m.Data[1] == 0x93) // release 5
                    Player.AddPlaylistToQueue(5);
                if (m.Data[1] == 0x83) // release 6
                    Player.AddPlaylistToQueue(6);
            }
        }

        static void announceCallback()
        {
            while (true)
            {
                Manager.Instance.EnqueueMessage(MessageAnnounce, MessagePollResponse);
                Thread.Sleep(30000);
            }
        }

        static void announce()
        {
            Manager.Instance.EnqueueMessage(MessageAnnounce/*, MessageAnnounce, MessageAnnounce*/);
        }

        #endregion
    }
}
