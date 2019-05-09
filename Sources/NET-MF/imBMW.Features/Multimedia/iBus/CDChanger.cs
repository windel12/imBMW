using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using System.Threading;
using imBMW.Tools;
using imBMW.iBus.Devices.Real;
using imBMW.Multimedia;
using GHI.Pins;
using imBMW.Features.Menu.Screens;
using imBMW.Features.Multimedia;
using imBMW.iBus;

namespace imBMW.iBus.Devices.Emulators
{
    public class CDChanger : MediaEmulator
    {
        enum PlayingType : byte
        {
            Normal = 0x09,
            Random = 0x29
        }

        const ushort StopDelayMilliseconds = 1000;

        Thread announceThread;
        Timer stopDelay;

        bool skipNextTrackMessage = false;

        #region Messages

        /// <summary> 02 00 </summary>
        public static imBMW.iBus. Message MessagePollResponse = new Message(DeviceAddress.CDChanger, DeviceAddress.Broadcast, 0x02, 0x00);
        /// <summary> 02 01 </summary>
        public static Message MessageAnnounce = new Message(DeviceAddress.CDChanger, DeviceAddress.Broadcast, 0x02, 0x01);

        //static Message MessageStoppedDisk1Track1 = new Message(DeviceAddress.CDChanger, DeviceAddress.Radio, "Stopped D1 T1", 0x39, 0x00, 0x02, 0x00, 0x3F, 0x00, 0x01, 0x01); // try 39 00 0C ?
        //static Message MessagePausedDisk1Track1  = new Message(DeviceAddress.CDChanger, DeviceAddress.Radio, "Paused D1 T1",  0x39, 0x01, 0x0C, 0x00, 0x3F, 0x00, 0x01, 0x01);
        //static Message MessagePlayingDisk1Track1 = new Message(DeviceAddress.CDChanger, DeviceAddress.Radio, "Playing D1 T1", 0x39, 0x02, 0x09, 0x00, 0x3F, 0x00, 0x01, 0x01);

        private static byte disksMask = 0x00; // 0x3F
        Message StatusStopped(byte disk, byte track)
        {
            return new Message(DeviceAddress.CDChanger, DeviceAddress.Radio, "Stopped D" + disk + "T" + track, 0x39, 0x00, 0x02, 0x00, disksMask, 0x00, disk, track); // try 39 00 0C ?
        }
        Message StatusPlaying(byte disk, byte track)
        {
            //return new Message(DeviceAddress.CDChanger, DeviceAddress.Radio, "Paused D" + disk + "T" + track, 0x39, 0x00, 0x02, 0x00, 0x3F, 0x00, disk, track);
            byte state = 0x09;// (byte)(Player.IsRandom ? (byte)PlayingType.Random : (byte)PlayingType.Normal);
            return new Message(DeviceAddress.CDChanger, DeviceAddress.Radio, "Paused D" + disk + "T" + track, 0x39, 0x00, state, 0x00, disksMask, 0x00, disk, track);
        }
        Message StatusStartPlaying(byte disk, byte track)
        {
            byte state = 0x09;// (byte)(Player.IsRandom ? (byte)PlayingType.Random : (byte)PlayingType.Normal);
            return new Message(DeviceAddress.CDChanger, DeviceAddress.Radio, "Playing D" + disk + "T" + track, 0x39, 0x02, state, 0x00, disksMask, 0x00, disk, track);
        }

        byte status = 0x08;
        byte ack = 0x0c;
        Message GetMessagePlaylistLoaded(byte disk, byte track)
        {
            return new Message(DeviceAddress.CDChanger, DeviceAddress.Radio, "Playlist loaded" + disk + "T" + track, 0x39, status, ack, 0x00, disksMask, 0x00, disk, track);
        }

        Message GetMessageCDCheck(byte disk, byte track)
        {
            return new Message(DeviceAddress.CDChanger, DeviceAddress.Radio, "Playlist loaded" + disk + "T" + track, 0x39, 0x09, 0x09, 0x00, disksMask, 0x00, disk, track);
        }

        /// <summary>0x38, 0x00, 0x00 </summary>
        public static byte[] DataCurrentDiskTrackRequest = new byte[] { 0x38, 0x00, 0x00 };
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

        /// <summary>0x38, 0x08, 0x01 </summary>
        public static byte[] DataRandomPlay = new byte[] { 0x38, 0x08, 0x01 };

        public static byte[] DataNext = new byte[] { 0x38, 0x0A, 0x00 };
        public static byte[] DataPrev = new byte[] { 0x38, 0x0A, 0x01 };

        #endregion

        public CDChanger(IAudioPlayer player)
            : base(player)
        {
            Manager.Instance.AddMessageReceiverForDestinationDevice(DeviceAddress.CDChanger, ProcessCDCMessage);
            Manager.Instance.AddMessageReceiverForDestinationDevice(DeviceAddress.Radio, ProcessToRadioMessage);

            MultiFunctionSteeringWheel.ButtonPressed += button =>
            {
                if (button == MFLButton.VolumeDown)
                {
                    skipNextTrackMessage = true;

                    CancelStopDelay();
                    stopDelay = new Timer(delegate
                    {
                        skipNextTrackMessage = false;
                        CancelStopDelay();
                    }, null, 1000, 0);
                }
            };

            //Radio.OnOffChanged += Radio_OnOffChanged;

            /*Player.TrackChanged += (s, e) => {
                //Manager.Instance.EnqueueMessage(StatusPlayed(Player.DiskNumber, Player.TrackNumber));
                Manager.Instance.EnqueueMessage(StatusStartPlaying(Player.DiskNumber, Player.TrackNumber));
            };*/
            
            announceThread = new Thread(announce);
            announceThread.Start();
        }

        private void Radio_OnOffChanged(bool turnedOn)
        {
            IsEnabled = turnedOn;
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
                BluetoothScreen.BluetoothChargingState = false;
                BluetoothScreen.AudioSource = AudioSource.SDCard;
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
                }, null, 0/*StopDelayMilliseconds*/, 0);
            }
        }

        void ProcessToRadioMessage(Message m)
        {
            // BM buttons
            if (m.Data.Length == 2 && m.Data[0] == 0x48)
            {
                if(m.Data[1] == 0x06 && IsEnabled) // radio knob off
                {
                    IsEnabled = false;
                }

                Action1 randomToggle = (newDiskNumber) => 
                {
                    RandomToggle(newDiskNumber);
                };
                if (m.Data[1] == 0x91 && IsEnabled) // 1
                    randomToggle(1);
                if (m.Data[1] == 0x81 && IsEnabled) // 2
                    randomToggle(2);
                if (m.Data[1] == 0x92 && IsEnabled) // 3
                    randomToggle(3);
                if (m.Data[1] == 0x82 && IsEnabled) // 4
                    randomToggle(4);
                if (m.Data[1] == 0x93 && IsEnabled) // 5
                    randomToggle(5);
                if (m.Data[1] == 0x83 && IsEnabled) // 6
                    randomToggle(6);
            }
        }

        void ProcessCDCMessage(Message m)
        {
            if (m.Data.Compare(DataCurrentDiskTrackRequest))
            {
                if (Player.IsPlaying && this.IsEnabled)
                {
                    //Manager.Instance.EnqueueMessage(StatusPlaying(Player.DiskNumber, Player.TrackNumber));
                    Manager.Instance.EnqueueMessage(StatusStartPlaying(Player.DiskNumber, Player.TrackNumber));
                }
                else
                {
                    Manager.Instance.EnqueueMessage(StatusStopped(Player.DiskNumber, Player.TrackNumber));
                }
                //m.ReceiverDescription = "CD status request";
            }
            if (m.Data.Compare(DataPlay))
            {
                IsEnabled = true;
                Manager.Instance.EnqueueMessage(StatusStartPlaying(Player.DiskNumber, Player.TrackNumber));
                m.ReceiverDescription = "Start playing";
            }
            else if (m.Data.Compare(DataStop))
            {
                IsEnabled = false;
                Manager.Instance.EnqueueMessage(StatusStopped(Player.DiskNumber, Player.TrackNumber));
                m.ReceiverDescription = "Stop playing";
            }
            else if (m.Data.Compare(DataPause))
            {
                Pause();
                Manager.Instance.EnqueueMessage(StatusStopped(Player.DiskNumber, Player.TrackNumber));
                m.ReceiverDescription = "Pause";
            }            
            else if(m.Data.Length == 3 && m.Data.StartsWith(0x38, 0x06)) // select disk
            {
                if (Player.IsPlaying)
                {
                    Manager.Instance.EnqueueMessage(StatusStartPlaying(Player.DiskNumber, Player.TrackNumber));
                }
                else
                {
                    Manager.Instance.EnqueueMessage(StatusStopped(Player.DiskNumber, Player.TrackNumber));
                }
            }
            else if (m.Data.Compare(DataRandomPlay))
            {
                RandomToggle(Player.DiskNumber);
                Manager.Instance.EnqueueMessage(StatusStartPlaying(Player.DiskNumber, Player.TrackNumber));
                m.ReceiverDescription = "Random toggle";
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
                    FrontDisplay.RefreshLEDs(LedType.Orange, append:true);
                    Logger.Warning("Errorneous 'Next' command was successfully skipped");
                    FrontDisplay.RefreshLEDs(LedType.Orange, remove:true);
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
                Manager.Instance.EnqueueMessage(MessageAnnounce, MessagePollResponse);
                Thread.Sleep(30000);
            }
        }

        #endregion
    }
}
