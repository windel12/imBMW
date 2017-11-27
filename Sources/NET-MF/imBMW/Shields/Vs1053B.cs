using System;
using System.Threading;
using Microsoft.SPOT.Hardware;
using System.IO;

namespace imBMW
{
    public class Vs1053B
    {
        private InputPort MP3_DREQ;
        private OutputPort MP3_RESET;
        private SPI _spi;
        private SPI.Configuration sci_config;
        private SPI.Configuration sdi_config;
        private byte[] cmd_buffer;
        private short volume;

        private const byte CMD_WRITE = 0x02;
        private const byte CMD_READ = 0x03;
        private const ushort SM_RESET = 0x04;
        private const ushort SM_CANCEL = 0x10;
        private const ushort SM_TESTS = 0x20;
        private const ushort SM_SDINEW = 0x800;
        private const ushort SM_ADPCM = 0x1000;
        private const ushort SM_LINE1 = 0x4000;
        private const int SCI_MODE = 0x00;
        private const int SCI_STATUS = 0x01;
        private const int SCI_BASS = 0x02;
        private const int SCI_CLOCKF = 0x03;
        private const int SCI_WRAM = 0x06;
        private const int SCI_WRAMADDR = 0x07;
        private const int SCI_HDAT0 = 0x08;
        private const int SCI_HDAT1 = 0x09;
        private const int SCI_AIADDR = 0x0A;
        private const int SCI_VOL = 0x0B;
        private const int SCI_AICTRL0 = 0x0C;
        private const int SCI_AICTRL1 = 0x0D;
        private const int SCI_AICTRL2 = 0x0E;
        private const int SCI_AICTRL3 = 0x0F;

        private ushort mp3_sci_read(byte register)
        {
            ushort temp;
            _spi.Config = sci_config;
            while (!MP3_DREQ.Read()) ;
            cmd_buffer[0] = CMD_READ;
            cmd_buffer[1] = register;
            cmd_buffer[2] = 0;
            cmd_buffer[3] = 0;
            byte[] readBuffer = new byte[4];
            _spi.WriteRead(cmd_buffer, readBuffer);
            temp = readBuffer[2];
            temp <<= 8;
            temp += readBuffer[3];
            return temp;
        }

        private void mp3_sci_write(byte register, ushort data)
        {
            _spi.Config = sci_config;
            while (!MP3_DREQ.Read()) ;
            cmd_buffer[0] = CMD_WRITE;
            cmd_buffer[1] = register;
            cmd_buffer[2] = (byte)(data >> 8);
            cmd_buffer[3] = (byte)data;
            _spi.Write(cmd_buffer);
        }

        private void reset()
        {
            while (!MP3_DREQ.Read()) ;
            mp3_sci_write(SCI_MODE, SM_SDINEW | SM_RESET);
            while (!MP3_DREQ.Read()) ;
            mp3_sci_write(SCI_CLOCKF, 0xa000);
        }

        public Vs1053B(Cpu.Pin MP3_DREQ, Cpu.Pin MP3_CS, Cpu.Pin MP3_DCS, Cpu.Pin MP3_RST)
        {
            cmd_buffer = new byte[4];
            this.sci_config = new SPI.Configuration(MP3_CS, false, 0, 0, false, true, 1000, SPI.SPI_module.SPI1);
            this.sdi_config = new SPI.Configuration(MP3_DCS, false, 0, 0, false, true, 1000, SPI.SPI_module.SPI1);
            this.MP3_DREQ = new InputPort(MP3_DREQ, false, Port.ResistorMode.Disabled);
            this.MP3_RESET = new OutputPort(MP3_RST, true);
            this._spi = new SPI(sci_config);
            MP3_RESET.Write(false);
            Thread.Sleep(1);
            MP3_RESET.Write(true);
            Thread.Sleep(10);
            reset();
            mp3_sci_write(SCI_MODE, SM_SDINEW);
            mp3_sci_write(SCI_CLOCKF, 0x8800);
            Thread.Sleep(100);
            SPI.Configuration temp = sdi_config;
            sdi_config = new SPI.Configuration(temp.ChipSelect_Port, temp.BusyPin_ActiveState, temp.ChipSelect_SetupTime, temp.ChipSelect_HoldTime, temp.Clock_IdleState, temp.Clock_Edge, 3000, temp.SPI_mod);
            Thread.Sleep(100);
            mp3_sci_write(SCI_VOL, 0x0101);
            if (mp3_sci_read(SCI_VOL) != (0x0101))
            {
                throw new Exception("Initialization Error");
            }
            SetVolume(255);
        }

        public void SetVolume(short value)
        {
            if (value > 255)
                volume =  255;
            else if (value < 0)
                volume = 0;
            else
                volume = value;

            mp3_sci_write(SCI_VOL, (ushort)((255 - volume) << 8 | (255 - volume)));
        }

        public void Play(string fileName, bool resetWhenFinished = false)
        {
            if (_spi.Config != sdi_config)
                _spi.Config = sdi_config;
            byte[] block = new byte[32];
            using (FileStream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                long size = stream.Length - stream.Length % 32;
                int left_over = (int)(stream.Length - size);
                for (int i = 0; i < size; i += 32)
                {
                    stream.Read(block, 0, 32);
                    while (!MP3_DREQ.Read()) ;
                    _spi.Write(block);
                }
                block = null;
                block = new byte[left_over];
                stream.Read(block, 0, left_over);
                while (!MP3_DREQ.Read()) ;
                _spi.Write(block);
            }
            if (resetWhenFinished)
                reset();
        }
    }
}
