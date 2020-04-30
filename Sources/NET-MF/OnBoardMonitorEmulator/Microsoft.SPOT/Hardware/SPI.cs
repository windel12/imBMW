using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.SPOT.Hardware
{
    public sealed class SPI : IDisposable
    {
        private byte[] data;

        public SPI(Configuration config)
        {
            Config = config;
        }

        ~SPI()
        {   
        }

        public Configuration Config { get; set; }

        public void Write(byte[] writeBuffer)
        {
            Thread.Sleep(1);
        }

        public void WriteRead(byte[] writeBuffer, byte[] readBuffer)
        {
            WriteRead(writeBuffer, readBuffer, 0);
        }

        public void WriteRead(byte[] writeBuffer, byte[] readBuffer, int startReadOffset)
        {
            if (writeBuffer[1] == 0x05)
            {
                readBuffer[0] = 172;
                readBuffer[1] = 69;
            }
            if (writeBuffer[1] == 0x0B)
            {
                readBuffer[0] = 40;
                readBuffer[1] = 40;
            }
        }

        public void Dispose()
        {   
        }

        public enum SPI_module
        {
            SPI1 = 0,
            SPI2 = 1,
            SPI3 = 2,
            SPI4 = 3
        }

        public class Configuration
        {
            public readonly Cpu.Pin BusyPin;
            public readonly bool BusyPin_ActiveState;
            public readonly bool ChipSelect_ActiveState;
            public readonly uint ChipSelect_HoldTime;
            public readonly Cpu.Pin ChipSelect_Port;
            public readonly uint ChipSelect_SetupTime;
            public readonly bool Clock_Edge;
            public readonly bool Clock_IdleState;
            public readonly uint Clock_RateKHz;
            public readonly SPI_module SPI_mod;

            public Configuration(Cpu.Pin ChipSelect_Port, bool ChipSelect_ActiveState, uint ChipSelect_SetupTime,
                uint ChipSelect_HoldTime, bool Clock_IdleState, bool Clock_Edge, uint Clock_RateKHz, SPI_module SPI_mod)
            {
                
            }

            public Configuration(Cpu.Pin ChipSelect_Port, bool ChipSelect_ActiveState, uint ChipSelect_SetupTime,
                uint ChipSelect_HoldTime, bool Clock_IdleState, bool Clock_Edge, uint Clock_RateKHz, SPI_module SPI_mod,
                Cpu.Pin BusyPin, bool BusyPin_ActiveState)
            {
                
            }
        }
    }
}
