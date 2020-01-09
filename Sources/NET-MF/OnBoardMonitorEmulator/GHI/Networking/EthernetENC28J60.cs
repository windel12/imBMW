using Microsoft.SPOT.Hardware;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GHI.Networking
{
    public class EthernetENC28J60
    {
        private string _IPAddress;

        public EthernetENC28J60(SPI.SPI_module spi, Cpu.Pin chipSelect, Cpu.Pin externalInterrupt)
        {
        }

        public EthernetENC28J60(SPI.SPI_module spi, Cpu.Pin chipSelect, Cpu.Pin externalInterrupt, Cpu.Pin reset)
        { 
        }

        public void EnableStaticIP(string ipAddress, string subnetMask, string gatewayAddress)
        {
            _IPAddress = ipAddress;
        }

        public void Open()
        {

        }

        public string IPAddress
        {
            get { return _IPAddress; }
        }
    }
}
