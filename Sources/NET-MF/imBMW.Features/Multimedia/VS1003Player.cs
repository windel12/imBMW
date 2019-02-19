using System;
using imBMW.Multimedia;
using imBMW.Features.Menu;
using System.IO;
using Microsoft.SPOT.Hardware;
using Microsoft.SPOT.IO;
using System.Threading;
using System.Collections;
using System.Runtime.CompilerServices;
using GHI.Pins;
using imBMW.Tools;
using System.Diagnostics;
using imBMW.Features.Multimedia.Models;
using ThreadState = System.Threading.ThreadState;

namespace imBMW.Features.Multimedia
{
    public class VS1003Player : AudioPlayerBase
    {
        #region Constants

        #region SCI Modes (8.7.1 in datasheet)

        const ushort SM_RESET = 0x0004;
        const ushort SM_CANCEL = 0x0008;
        const ushort SM_SDINEW = 0x0800;

        #endregion

        #region SCI Registers (8.7 in datasheet)

        const int SCI_MODE = 0x00;
        const int SCI_STATUS = 0x01;
        const int SCI_BASS = 0x02;
        const int SCI_CLOCKF = 0x03;
        const int SCI_DECODE_TIME = 0x04;
        const int SCI_AUDATA = 0x05;
        const int SCI_WRAM = 0x06;
        const int SCI_WRAMADDR = 0x07;
        const int SCI_HDAT0 = 0x08;
        const int SCI_HDAT1 = 0x09;
        const int SCI_VOL = 0x0B;

        #endregion

        #region WRAM Parameters (9.11.1 in datasheet)

        const ushort para_chipID0 = 0x1E00;
        const ushort para_chipID1 = 0x1E01;
        const ushort para_version = 0x1E02;
        const ushort para_playSpeed = 0x1E04;
        const ushort para_byteRate = 0x1E05;
        const ushort para_endFillByte = 0x1E06;
        const ushort para_posMsec0 = 0x1E27;
        const ushort para_posMsec1 = 0x1E28;

        #endregion

        #endregion

        //OutputPort led3 = new OutputPort(FEZPandaIII.Gpio.Led3, false);

        private static InputPort DREQ;

        private static SPI spi;
        private static SPI.Configuration dataConfig;
        private static SPI.Configuration cmdConfig;

        private static byte[] block = new byte[32];
        private static byte[] cmdBuffer = new byte[4];

        Thread playerThread;

        private Thread generateRandomDiskAndTrackThread;
        private readonly ManualResetEvent trackChangedSync = new ManualResetEvent(true);

        private Stack backChangingHistory = new Stack();
        private Stack nextChangingHistory = new Stack();

        static IDictionary StorageInfo = new Hashtable();
        private static Queue NextTracksQueue = new Queue();

        public static int FileNameOffset = 10;

        public override bool IsPlaying
        {
            get { return isPlaying; }
            protected set
            {
                OnIsPlayingChanging(isPlaying);
                isPlaying = value;
                OnIsPlayingChanged(isPlaying);
            }
        }

        private bool ChangeTrack { get; set; }

        public override MenuScreen Menu
        {
            get { return new MenuScreen("VS1003Player"); }
        }

        public int CurrentPosition { get; set; } = 0;

        public VS1003Player(Cpu.Pin MP3_DREQ, Cpu.Pin MP3_CS, Cpu.Pin MP3_DCS, Cpu.Pin MP3_RST)
        {
            #region prepare
            dataConfig = new SPI.Configuration(MP3_DCS, false, 0, 0, false, true, 1000, SPI.SPI_module.SPI2);
            cmdConfig = new SPI.Configuration(MP3_CS, false, 0, 0, false, true, 1000, SPI.SPI_module.SPI2);
            DREQ = new InputPort(MP3_DREQ, false, Port.ResistorMode.PullUp);

            spi = new SPI(dataConfig);

            DisableMidi();

            Thread.Sleep(100);

            //Reset();

            SCIWrite(SCI_CLOCKF, 0x8800);
            SCIWrite(SCI_VOL, 0x2828);

            //ClearPlayback();

            if (SCIRead(SCI_VOL) != (0x2828))
            {
                throw new Exception("VS1053: Failed to initialize MP3 Decoder.");
            }
            else
            {
                Logger.Log(LogPriority.Info, "VS1053: Initialized MP3 Decoder.");
            }

            #endregion
            SetVolume((byte)255);

            Logger.Trace("Getting files and folders:");
            if (VolumeInfo.GetVolumes()[0].IsFormatted)
            {
                string rootDirectory = VolumeInfo.GetVolumes()[0].RootDirectory;
                for (byte i = 1; i <= 6; i++)
                {
                    var folder = rootDirectory + "\\" + i;
                    var files = Directory.EnumerateFiles(folder);
                    byte filesCount = 0;
                    foreach(var fileObj in files)
                    {
                        if (((string)fileObj).EndsWith(".mp3") /* || file.EndsWith(".m4a")*/)
                        {
                            filesCount++;
                        }
                    }
                    files = null;
                    StorageInfo[i] = filesCount;
                }

                FileStream dataFile = null;
                byte[] lastTrackInfo = new byte[7] { 1, 1, 1, 0, 0, 0, 0 };
                try
                {
                    dataFile = File.Open(rootDirectory + "\\data.bin", FileMode.OpenOrCreate);
                    dataFile.Read(lastTrackInfo, 0, lastTrackInfo.Length);
                    DiskNumber = lastTrackInfo[0];
                    TrackNumber = lastTrackInfo[1];
                    IsRandom = lastTrackInfo[2] == 1;
                    CurrentPosition = BitConverter.ToInt32(lastTrackInfo, 3);
                }
                finally
                {
                    if (dataFile != null) { dataFile.Close(); }
                }

                try
                {
                    var diskAndTrack = GenerateConcreteDiskAndTrack(DiskNumber, TrackNumber);
                    FileName = diskAndTrack.fileName;
                    CurrentTrack = new TrackInfo(diskAndTrack.fileName);
                }
                catch (Exception ex)
                {
                    DiskNumber = TrackNumber = 1;
                    Logger.Error(ex);
                }

                // generate prepared next random tracks
                GeneratePreparedNextTracks(DiskNumber);

                Action saveHistory = () =>
                {
                    FileStream dataFileWrite = null;
                    try
                    {
                        dataFileWrite = File.Open(rootDirectory + "\\data.bin", FileMode.OpenOrCreate);
                        var cp = BitConverter.GetBytes(CurrentPosition);
                        byte isRandomValue = IsRandom ? (byte)1 : (byte)0;
                        byte[] lastTrackInfoWrite = new byte[7] {DiskNumber, TrackNumber, isRandomValue, cp[0], cp[1], cp[2], cp[3]};
                        dataFileWrite.Write(lastTrackInfoWrite, 0, lastTrackInfo.Length);
                    }
                    finally
                    {
                        if(dataFileWrite != null) { dataFileWrite.Close();}
                    }
                };
                //TrackChanged += (sender, trackInfo) => { saveHistory(); };
                IsPlayingChanging += (sender, isPlayingBeforeChaning) => { if(isPlaying) { saveHistory();} };
            }
            else
            {
                Logger.Log(LogPriority.Warning, "Storage is not formatted. " + "Format on PC with FAT32/FAT16 first!");
            }
        }

        private void GeneratePreparedNextTracks(byte diskNumber)
        {
            NextTracksQueue.Clear();
            for (byte i = 0; i < 2; i++)
            {
                var randomDiskAndTrack = GenerateRandomDiskAndTrack(diskNumber);
                NextTracksQueue.Enqueue(randomDiskAndTrack);
                Thread.Sleep(15); // for random working
            }
        }

        public override void Play()
        {
            base.Play();

            if (playerThread == null || playerThread.ThreadState == ThreadState.Unstarted ||
                playerThread.ThreadState == ThreadState.Stopped || playerThread.ThreadState == ThreadState.StopRequested ||
                playerThread.ThreadState == ThreadState.Aborted || playerThread.ThreadState == ThreadState.AbortRequested)
            {
                playerThread = new Thread(() =>
                {
                    PlayDirect();
                });
                playerThread.Priority = ThreadPriority.Highest;
                playerThread.Start();
            }
        }

        public override void Pause()
        {
            base.Pause();
        }

        private DiskAndTrack GenerateRandomDiskAndTrackNumbers(byte diskNumber)
        {
            var diskAndTrack = new DiskAndTrack();
            Random r = new Random();
            byte filesOnDisk;
            if (IsRandom)
            {
                do
                {
                    diskAndTrack.diskNumber = (byte)(r.Next(6) + 1);
                    filesOnDisk = (byte)StorageInfo[diskAndTrack.diskNumber];
                } while (filesOnDisk == 0);
                diskAndTrack.trackNumber = (byte)(r.Next(filesOnDisk) + 1);
            }
            else
            {
                do
                {
                    diskAndTrack.diskNumber = diskNumber;
                    filesOnDisk = (byte)StorageInfo[diskAndTrack.diskNumber];
                } while (filesOnDisk == 0);
                diskAndTrack.trackNumber = (byte)(r.Next(filesOnDisk) + 1);
            }
            return diskAndTrack;
        }

        private DiskAndTrack GenerateRandomDiskAndTrack(byte diskNumber)
        {
            var newRandomDiskAndTrack = GenerateRandomDiskAndTrackNumbers(diskNumber);
            return GenerateConcreteDiskAndTrack(newRandomDiskAndTrack.diskNumber, newRandomDiskAndTrack.trackNumber);
        }

        private DiskAndTrack GenerateConcreteDiskAndTrack(byte diskNumber, byte trackNumber)
        {
            DiskAndTrack result = null;

            if (VolumeInfo.GetVolumes()[0].IsFormatted)
            {
                string rootDirectory = VolumeInfo.GetVolumes()[0].RootDirectory;
                var folder = rootDirectory + "\\" + diskNumber;
                var files = Directory.EnumerateFiles(folder);

                byte trackIndex = 1;
                foreach (var fileObj in files)
                {
                    if (((string)fileObj).EndsWith(".mp3") /* || file.EndsWith(".m4a")*/)
                    {
                        if (trackIndex == trackNumber)
                        {
                            result = new DiskAndTrack(diskNumber, trackIndex, ((string)fileObj));
                            break;
                        }
                        trackIndex++;
                    }
                }
                files = null;
            }
            else
            {
                Logger.Warning("Storage is not formatted. " + "Format on PC with FAT32/FAT16 first!");
            }

            return result;
        }

        public override void Next()
        {
            trackChangedSync.WaitOne(5000, true);

            backChangingHistory.Push(new DiskAndTrack(DiskNumber, TrackNumber, FileName));

            if (nextChangingHistory.Count > 0)
            {
                var history = (DiskAndTrack)nextChangingHistory.Pop();
                DiskNumber = history.diskNumber;
                TrackNumber = history.trackNumber;
                FileName = history.fileName;

                OnTrackChanged();
            }
            else
            {
                var preparedItem = (DiskAndTrack)NextTracksQueue.Dequeue();
                DiskNumber = preparedItem.diskNumber;
                TrackNumber = preparedItem.trackNumber;
                FileName = preparedItem.fileName;

                OnTrackChanged();
                if (generateRandomDiskAndTrackThread == null)
                {
                    generateRandomDiskAndTrackThread = new Thread(() =>
                    {
                        while (true)
                        {
                            trackChangedSync.Reset();
                            var newRandomTrackItem = GenerateRandomDiskAndTrack(DiskNumber);
                            NextTracksQueue.Enqueue(newRandomTrackItem);
                            trackChangedSync.Set();
                            generateRandomDiskAndTrackThread.Suspend();
                        }
                    });
                    generateRandomDiskAndTrackThread.Priority = ThreadPriority.Lowest;
                    generateRandomDiskAndTrackThread.Start();
                }
                if (generateRandomDiskAndTrackThread.ThreadState == ThreadState.Suspended || generateRandomDiskAndTrackThread.ThreadState == ThreadState.SuspendRequested
                    || generateRandomDiskAndTrackThread.ThreadState == ThreadState.Stopped || generateRandomDiskAndTrackThread.ThreadState == ThreadState.StopRequested)
                {
                    generateRandomDiskAndTrackThread.Resume();
                }
            }
        }

        public override void Prev()
        {
            if (backChangingHistory.Count > 0)
            {
                nextChangingHistory.Push(new DiskAndTrack(DiskNumber, TrackNumber, FileName));

                var history = (DiskAndTrack)backChangingHistory.Pop();
                DiskNumber = history.diskNumber;
                TrackNumber = history.trackNumber;
                FileName = history.fileName;
                OnTrackChanged();

            }
        }

        protected override void OnTrackChanged()
        {
            CurrentTrack = new TrackInfo(FileName);
            base.OnTrackChanged();
            CurrentPosition = 0;
            ChangeTrack = true;
        }

        public override bool RandomToggle(byte newDiskNumber)
        {
            if (!IsRandom && newDiskNumber != DiskNumber)
            {
                GeneratePreparedNextTracks(newDiskNumber);
                return IsRandom;
            }
            IsRandom = !IsRandom;
            GeneratePreparedNextTracks(newDiskNumber);
            return IsRandom;
        }

        public override void VoiceButtonLongPress()
        {
        }

        public override void VoiceButtonPress()
        {
        }

        public override void VolumeDown()
        {
        }

        public override void VolumeUp()
        {
        }

        private static byte[] buffer = new byte[256];

        public void PlayDirect(bool resetWhenFinished = false)
        {
            //byte[] buffer;
            int size;
            FileStream stream = null;
            while (true)
            {
                //Thread.Sleep(50);
                try
                {
                    //if (!IsPlaying)
                    //    Thread.CurrentThread.Suspend();
                    //buffer = new byte[256];
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                    break;
                }

                size = 0;
                try
                {
                    stream = new FileStream(CurrentTrack.FileName, FileMode.Open, FileAccess.Read);
                    int id3v2_header_length = 10;
                    stream.Read(buffer, 0, id3v2_header_length);
                    // http://id3.org/id3v2.4.0-structure#line-39
                    int startPositionOfAudioStream = (int)buffer[6] << 21 | (int)buffer[7] << 14 | (int)buffer[8] << 7 | (int)buffer[9];
                    Logger.Info("Skip id3 tag bytes: " + startPositionOfAudioStream);
                    Logger.Info("FileName:" + CurrentTrack.FileName + " FileLength:" + stream.Length + " startPositionOfAudioStream:" + startPositionOfAudioStream);
                    if (CurrentPosition >= stream.Length)
                    {
                        CurrentPosition = 0;
                    }
                    if (startPositionOfAudioStream >= stream.Length)
                    {
                        startPositionOfAudioStream = 0;
                    }
                    if (CurrentPosition <= startPositionOfAudioStream)
                    {
                        CurrentPosition = startPositionOfAudioStream;
                    }
                    stream.Seek(CurrentPosition, SeekOrigin.Begin);
                    do
                    {
                        size = stream.Read(buffer, 0, buffer.Length);
                        //CurrentTrack.Time = GetDecodeTime();
                        //ushort byteRate = GetByteRate();
                        SendData(buffer);
                        if (ChangeTrack)
                        {
                            break;
                        }
                        if(!IsPlaying)
                        {
                            break;
                        }
                        CurrentPosition += size;
                    } while (size > 0);
                }
                catch (Exception ex)
                {
                    //led3.Write(true);
                    IsPlaying = false;
                    Logger.Error(ex);
                }
                finally
                {
                    if (stream != null)
                    {
                        stream.Dispose();
                    }
                    if (IsPlaying && !ChangeTrack)
                    {
                        Next();
                    }
                    ChangeTrack = false;
                    //ResetDecodeTime();
                }
                if (!IsPlaying)
                {
                    break;// close thread
                }
            }
        }

        #region prepare

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void SendData(byte[] data)
        {
            int size = data.Length - data.Length % 32;

            if (ChangeTrack)
            {
                //Logger.Log(LogPriority.Info, "Wrote SM_CANCEL");
                //SCIWrite(SCI_MODE, SM_CANCEL);
            }

            for (int i = 0; i < size; i += 32)
            {
                Array.Copy(data, i, block, 0, 32);
                SDIWrite(block);
                //Debug.Print("Wrote 32 bytes of data");
                if (ChangeTrack)
                {
                    ushort CANCEL = SCIRead(SCI_MODE);
                    if (CANCEL != SM_CANCEL)
                    {
                        Logger.Log(LogPriority.Info, "SM_CANCEL cleared");
                        StopPlayback();
                        break;
                    }
                    else
                        Logger.Log(LogPriority.Info, "SM_CANCEL has not cleared yet...");
                }
            }   
        }

        public void StopPlayback()
        {
            uint endFillByte = WRAMRead(para_endFillByte);
            ushort HDAT0;
            ushort HDAT1;
            do
            {
                for (int n = 0; n < 2052; n++) SDIWrite((byte)(0xFF & endFillByte));
                Logger.Log(LogPriority.Info, "Sent 2052 endFillByte, checking HDAT0 and HDAT1");
                HDAT0 = SCIRead(SCI_HDAT0);
                HDAT1 = SCIRead(SCI_HDAT1);
                Logger.Log(LogPriority.Info, "HDAT0: " + HDAT0 + ", HDAT1: " + HDAT1);
            }
            while (HDAT0 != 0 && HDAT1 != 0);
            //ChangeTrack = false;
        }

        public ushort GetByteRate()
        {
            return WRAMRead(para_byteRate);
        }

        private ushort GetDecodeTime()
        {
            return SCIRead(SCI_DECODE_TIME);
        }

        private void ResetDecodeTime()
        {
            SCIWrite(SCI_DECODE_TIME, 0);
        }

        /// <summary>
        /// Metod for setting the volume on the left and right channel.
        /// <remarks>0 for min volume, 255 for max</remarks>
        /// </summary>
        /// <param name="left_channel">The left channel.</param>
        /// <param name="right_channel">The right channel.</param>
        private void SetVolume(byte volume)
        {
            ushort vol = (ushort)(((255 - volume) << 8) | (255 - volume));
            while (!DREQ.Read())
                Thread.Sleep(1);
            SCIWrite(SCI_VOL, vol);
        }

        public void Reset()
        {
            while (!DREQ.Read())
                Thread.Sleep(1);
            SCIWrite(SCI_MODE, (ushort)(SM_SDINEW | SM_RESET));
            Thread.Sleep(1);
            while (!DREQ.Read())
                Thread.Sleep(1);
            Thread.Sleep(100);
        }

        public void DisableMidi()
        {
            var value = SCIRead(SCI_AUDATA);
            if (value != 0xac45)
            {
                Logger.Log(LogPriority.Info, "Skipping midi setup");
                return;
            }
            SCIWrite(SCI_WRAMADDR, 0xc017);
            value = SCIRead(SCI_WRAM);
            Logger.Log(LogPriority.Info, "Current GPIO direction: " + value);
            SCIWrite(SCI_WRAMADDR, 0xc019);
            value = SCIRead(SCI_WRAM);
            Logger.Log(LogPriority.Info, "Current GPIO values: " + value);

            SCIWrite(SCI_WRAMADDR, 0xc017);
            SCIWrite(SCI_WRAM, 3);
            SCIWrite(SCI_WRAMADDR, 0xc019);
            SCIWrite(SCI_WRAM, 0);

            //soft rest
            //while (!MP3_DREQ.Read()) ;
            //SCIWrite(SCI_MODE, SM_SDINEW | SM_RESET);
            Reset();
        }
        #endregion

        #region Private Methods

        /// <summary>
        /// Method from reading from WRAM.
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        private ushort WRAMRead(ushort address)
        {
            ushort tmp1, tmp2;
            SCIWrite(SCI_WRAMADDR, address);
            tmp1 = SCIRead(SCI_WRAM);
            SCIWrite(SCI_WRAMADDR, address);
            tmp2 = SCIRead(SCI_WRAM);
            if (tmp1 == tmp2) return tmp1;
            SCIWrite(SCI_WRAMADDR, address);
            tmp1 = SCIRead(SCI_WRAM);
            if (tmp1 == tmp2) return tmp1;
            SCIWrite(SCI_WRAMADDR, address);
            tmp1 = SCIRead(SCI_WRAM);
            if (tmp1 == tmp2) return tmp1;
            return tmp1;
        }

        /// <summary>
        /// Method for writing data to the decoder.
        /// </summary>
        /// <param name="writeBytes">Bytes to write.</param>
        private void SDIWrite(byte[] writeBytes)
        {
            if (spi.Config != dataConfig)
                spi.Config = dataConfig;
            while (!DREQ.Read())
                Thread.Sleep(1);
            spi.Write(writeBytes);
        }

        /// <summary>
        /// Method for writing a single byte to the decoder.
        /// </summary>
        /// <param name="writeByte">Byte to write.</param>
        private void SDIWrite(byte writeByte)
        {
            SDIWrite(new byte[] { writeByte });
        }

        /// <summary>
        /// Method for writing a command to the decoder.
        /// </summary>
        /// <param name="address">The address to write to.</param>
        /// <param name="data">The data to write.</param>
        private void SCIWrite(byte address, ushort data)
        {
            while (!DREQ.Read())
                Thread.Sleep(1);

            spi.Config = cmdConfig;
            cmdBuffer[0] = 0x02;
            cmdBuffer[1] = address;
            cmdBuffer[2] = (byte)(data >> 8);
            cmdBuffer[3] = (byte)data;

            spi.Write(cmdBuffer);
        }

        /// <summary>
        /// Method for reading a command from the decoder.
        /// </summary>
        /// <param name="address">The address to read from.</param>
        /// <returns>The data read.</returns>
        private ushort SCIRead(byte address)
        {
            ushort temp;

            while (!DREQ.Read())
                Thread.Sleep(1);

            spi.Config = cmdConfig;
            cmdBuffer[0] = 0x03;
            cmdBuffer[1] = address;
            cmdBuffer[2] = 0;
            cmdBuffer[3] = 0;

            spi.WriteRead(cmdBuffer, cmdBuffer, 2);

            temp = cmdBuffer[0];
            temp <<= 8;

            temp += cmdBuffer[1];

            return temp;
        }

        #endregion
    }
}
