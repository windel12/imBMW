using System;
using Microsoft.SPOT;
using imBMW.Multimedia;
using imBMW.Features.Menu;
using GHI.IO.Storage;
using System.IO;
using Microsoft.SPOT.Hardware;
using Microsoft.SPOT.IO;
using System.Threading;
using System.Collections;
using System.Runtime.CompilerServices;
using GHI.Pins;
using imBMW.Tools;
using System.Diagnostics;
using imBMW.iBus.Devices.Real;

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

        OutputPort led2 = new OutputPort(FEZPandaIII.Gpio.Led2, false);

        private static InputPort DREQ;

        private static SPI spi;
        private static SPI.Configuration dataConfig;
        private static SPI.Configuration cmdConfig;

        private static byte[] block = new byte[32];
        private static byte[] cmdBuffer = new byte[4];

        Thread playerThread;
        ArrayList changingHistory = new ArrayList();
        int changingHistoryPreviousPointer = 0;
        static IDictionary Data = new Hashtable();


        public bool IsRandom { get; set; } = true;

        private bool isPlaying;
        public override bool IsPlaying
        {
            get { return isPlaying; }
            protected set
            {
                isPlaying = value;
                OnIsPlayingChanged(isPlaying);
            }
        }

        private bool ChangeTrack { get; set; }


        public override MenuScreen Menu
        {
            get { return new MenuScreen(FileName); }
        }

        public byte DiskNumber { get; set; } = 1;
        public byte TrackNumber { get; set; } = 1;
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
            SetVolume(Debugger.IsAttached ? (byte)220 : (byte)255);

            Logger.Log(LogPriority.Info, "Getting files and folders:");
            if (VolumeInfo.GetVolumes()[0].IsFormatted)
            {
                string rootDirectory = VolumeInfo.GetVolumes()[0].RootDirectory;
                string[] folders = Directory.GetDirectories(rootDirectory);
                for (int i = 1; i <= 6; i++)
                {
                    var folder = rootDirectory + "\\" + i;
                    var files = Directory.GetFiles(folder);
                    var musicFiles = new ArrayList();
                    foreach (var file in files)
                    {
                        if (file.EndsWith(".mp3")/* || file.EndsWith(".m4a")*/)
                        {
                            musicFiles.Add(file);
                        }
                    }
                    Data.Add(i.ToString(), musicFiles);
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
                TrackChanged += (sender, trackInfo) => { saveHistory(); };
                IsPlayingChanged += (sender, isPlaying) => { if(!isPlaying) { saveHistory();} };
            }
            else
            {
                Logger.Log(LogPriority.Warning, "Storage is not formatted. " + "Format on PC with FAT32/FAT16 first!");
            }

            playerThread = new Thread(() =>
            {
                PlayDirect();
            });
            playerThread.Priority = ThreadPriority.Highest;
            //playerThread.Start();
        }

        public override void Play()
        {
            base.Play();

            if (playerThread.ThreadState == ThreadState.Unstarted)
            {
                playerThread.Start();
            }
            if (playerThread.ThreadState == ThreadState.Suspended || playerThread.ThreadState == ThreadState.SuspendRequested)
            {
                playerThread.Resume();
            }
        }

        public override void Pause()
        {
            base.Pause();
        }

        public override void Next()
        {
            if (changingHistoryPreviousPointer > 0)
            {
                changingHistoryPreviousPointer--;
                var history = (byte[])changingHistory[changingHistory.Count - changingHistoryPreviousPointer - 1];
                DiskNumber = history[0];
                TrackNumber = history[1];
            }
            else
            {
                changingHistory.Add(new byte[2] {DiskNumber, TrackNumber});

                Random r = new Random();
                ArrayList filesOnDisk;
                if (IsRandom)
                {
                    do
                    {
                        DiskNumber = (byte) (r.Next(6) + 1);
                        filesOnDisk = (ArrayList) Data[DiskNumber.ToString()];
                    } while (filesOnDisk.Count == 0);
                    TrackNumber = (byte) (r.Next(filesOnDisk.Count) + 1);
                }
                else
                {
                    do
                    {
                        //DiskNumber = (byte) (DiskNumber == 6 ? 0 : ++DiskNumber);
                        filesOnDisk = (ArrayList) Data[DiskNumber.ToString()];
                    } while (filesOnDisk.Count == 0);
                    TrackNumber = (byte)(r.Next(filesOnDisk.Count) + 1);
                }
            }

            CurrentPosition = 0;
            ChangeTrack = true;
            OnTrackChanged();
        }

        public override void Prev()
        {
            if (changingHistory.Count > 0 && changingHistory.Count - 1 > changingHistoryPreviousPointer)
            {
                // save current track just for first time
                if (changingHistoryPreviousPointer == 0)
                {
                    changingHistory.Add(new byte[2] {DiskNumber, TrackNumber});
                }

                changingHistoryPreviousPointer++;
                var history = (byte[])changingHistory[changingHistory.Count - changingHistoryPreviousPointer - 1];
                DiskNumber = history[0];
                TrackNumber = history[1];
                CurrentPosition = 0;
                ChangeTrack = true;
                OnTrackChanged();
            }
        }

        public override bool RandomToggle()
        {
            IsRandom = !IsRandom;
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

        private void PlayDirect(bool resetWhenFinished = false)
        {
            byte[] buffer;
            ArrayList filesOnDisk = null;
            int size;
            FileStream stream = null;
            while (true)
            {
                Thread.Sleep(300);
                try
                {
                    if (!IsPlaying)
                        Thread.CurrentThread.Suspend();
                    buffer = new byte[2048];
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                    break;
                }

                try
                {
                    filesOnDisk = Data[DiskNumber.ToString()] as ArrayList;
                    FileName = (string) filesOnDisk[TrackNumber - 1];
                }
                catch (Exception ex)
                {
                    DiskNumber = TrackNumber = 1;
                    filesOnDisk = Data[DiskNumber.ToString()] as ArrayList;
                    FileName = (string)filesOnDisk[TrackNumber - 1];
                    Logger.Error(ex);
                }
                size = 0;
                
                try
                {
                    stream = new FileStream(FileName, FileMode.Open, FileAccess.Read);
                    stream.Seek(CurrentPosition, SeekOrigin.Begin);
                    do
                    {
                        size = stream.Read(buffer, 0, buffer.Length);
                        CurrentPosition += size;
                        SendData(buffer );
                        if (ChangeTrack)
                        {
                            break;
                        }
                        if(!IsPlaying)
                        {
                            break;
                        }
                    } while (size > 0);
                }
                catch (Exception ex)
                {
                    led2.Write(true);
                    IsPlaying = false;
                    Logger.Error(ex);
                }
                finally
                {
                    if (stream != null)
                    {
                        stream.Close();
                    }
                    if (IsPlaying && !ChangeTrack)
                    {
                        Next();
                    }
                    ChangeTrack = false;
                }
            }
        }

        #region prepare

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void SendData(byte[] data)
        {
            int size = data.Length - data.Length % 32;

            //if (cancelPlayback)
            //{
            //    Debug.Print("Wrote SM_CANCEL");
            //    SCIWrite(SCI_MODE, SM_CANCEL);
            //}

            ushort CANCEL = 0;
            for (int i = 0; i < size; i += 32)
            {
                Array.Copy(data, i, block, 0, 32);
                SDIWrite(block);
                //Debug.Print("Wrote 32 bytes of data");
                //if (cancelPlayback)
                //{
                //    CANCEL = SCIRead(SCI_MODE);
                //    Debug.Print("Checking if SM_CANCEL has cleared (" + CANCEL + ")");
                //    if (CANCEL != SM_CANCEL)
                //    {
                //        Debug.Print("SM_CANCEL cleared");
                //        StopPlayback();
                //        break;
                //    }
                //    else
                //        Debug.Print("SM_CANCEL has not cleared yet...");
                //}
            }
            //if (CANCEL == SM_CANCEL)
            //{
            //    Reset();
            //    cancelPlayback = false;
            //    BlueConePlayer.CancelPlayback = false;
            //}
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
