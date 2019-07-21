using System;
using System.IO;
using Microsoft.SPOT.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GHI.Pins;
using imBMW.Tools;
using imBMW.Features.Multimedia;

namespace OnBoardMonitorEmulatorTests
{
    [TestClass]
    public class VS1003PlayerTests
    {
        [TestMethod]
        public void Should_CorrectlyCalculateCurrentPosition_LastPositionGreaterThanCurrentFile()
        {
            // assert
            int currentPosition = 0x00ffffff;
            GenerateDataFile(1, 1, false, currentPosition);

            VS1003Player player = new VS1003Player(FEZPandaIII.Gpio.D25, FEZPandaIII.Gpio.D27, FEZPandaIII.Gpio.D24, FEZPandaIII.Gpio.D26);
            var mp3File = File.Open(player.CurrentTrack.FilePath, FileMode.OpenOrCreate);
            int startPositionOfAudioStream = 0x000000ff;
            ModifyId3Header(mp3File, 0x000000ff);

            // act
            player.PlayDirect();

            // assert
            Assert.AreEqual(player.CurrentPosition, startPositionOfAudioStream);
        }

        [TestMethod]
        public void Should_CorrectlyCalculateCurrentPosition_IfId3TagGreaterThanFileLength()
        {
            // assert
            int currentPosition = 0x01e3;
            GenerateDataFile(1, 1, false, currentPosition);

            VS1003Player player = new VS1003Player(FEZPandaIII.Gpio.D25, FEZPandaIII.Gpio.D27, FEZPandaIII.Gpio.D24, FEZPandaIII.Gpio.D26);
            var mp3File = File.Open(player.CurrentTrack.FilePath, FileMode.OpenOrCreate);
            ModifyId3Header(mp3File, 0x00ffffff);

            // act
            player.PlayDirect();

            // assert
            Assert.AreEqual(player.CurrentPosition, currentPosition);
        }

        [TestMethod]
        public void Should_SetCurrentPosition_ToStartPositionOfAudioStream()
        {
            // assert
            int currentPosition = 0;
            GenerateDataFile(1, 1, false, currentPosition);

            VS1003Player player = new VS1003Player(FEZPandaIII.Gpio.D25, FEZPandaIII.Gpio.D27, FEZPandaIII.Gpio.D24, FEZPandaIII.Gpio.D26);
            var mp3File = File.Open(player.CurrentTrack.FilePath, FileMode.OpenOrCreate);
            int startPositionOfAudioStream = 0x000000e3;
            ModifyId3Header(mp3File, startPositionOfAudioStream);

            // act
            player.PlayDirect();

            // assert
            Assert.AreEqual(player.CurrentPosition, startPositionOfAudioStream);
        }

        [TestMethod]
        public void Should_RestoreCurrentPosition_AccordingToSavedValue()
        {
            // assert
            int currentPosition = 0x00000900;
            GenerateDataFile(1, 1, false, currentPosition);

            VS1003Player player = new VS1003Player(FEZPandaIII.Gpio.D25, FEZPandaIII.Gpio.D27, FEZPandaIII.Gpio.D24, FEZPandaIII.Gpio.D26);
            var mp3File = File.Open(player.CurrentTrack.FilePath, FileMode.OpenOrCreate);
            int startPositionOfAudioStream = 0x000000e3;
            ModifyId3Header(mp3File, startPositionOfAudioStream);

            // act
            player.PlayDirect();

            // assert
            Assert.AreEqual(player.CurrentPosition, currentPosition);
        }

        [TestMethod]
        public void Should_ReadCurrentPosition_Of_PegboardNerds_SwampThing_mp3()
        {
            // assert
            GenerateDataFile(1, 2, false, 0);

            VS1003Player player = new VS1003Player(FEZPandaIII.Gpio.D25, FEZPandaIII.Gpio.D27, FEZPandaIII.Gpio.D24, FEZPandaIII.Gpio.D26);
            //var mp3File = File.Open(player.CurrentTrack.FileName, FileMode.OpenOrCreate);
            //int startPositionOfAudioStream = 0x000000e3;
            //ModifyId3Header(mp3File, startPositionOfAudioStream);

            // act
            player.PlayDirect();

            // assert
            Assert.AreEqual(player.CurrentPosition, 0x7f8ec);
        }

        private void GenerateDataFile(byte diskNumber, byte trackNumber, bool isRandom, int lastPosition)
        {
            byte[] lastPositionBytes = BitConverter.GetBytes(lastPosition);
            var data = new byte[] {diskNumber, trackNumber, isRandom ? (byte) 1 : (byte) 0};
            data = data.Combine(lastPositionBytes[0], lastPositionBytes[1], lastPositionBytes[2], lastPositionBytes[3]);

            var dataFile = File.Open(VolumeInfo.GetVolumes()[0].RootDirectory + "\\data.bin", FileMode.OpenOrCreate);
            dataFile.Write(data, 0, data.Length);
            dataFile.Dispose();
        }

        private void ModifyId3Header(FileStream fileStream, int startPositionOfAudioStream)
        {
            fileStream.Position = 6;
            var buffer = BitConverter.GetBytes(startPositionOfAudioStream);
            Array.Reverse(buffer);
            fileStream.Write(buffer, 0, buffer.Length);

            fileStream.Dispose();
        }
    }
}
