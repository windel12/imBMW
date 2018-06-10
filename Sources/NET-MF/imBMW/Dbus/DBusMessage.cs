using System;
using imBMW.Tools;
using System.Text;
using imBMW.iBus;

namespace imBMW.Diagnostics
{
    /// <summary>
    /// BMW DS2 Diagnostic Bus (DBus) message packet
    /// </summary>
    public class DBusMessage : Message
    {
        byte[] packet;
        byte check;
        int packetLength;
        string dataString;

        byte formatByte = 0xB8;

        static new int PacketLengthMin { get { return 5; } }
        
        public DBusMessage(params byte[] data)
            : this(null, data)
        { }

        public DBusMessage(string description, params byte[] data)
            : base(DeviceAddress.OBD, DeviceAddress.DDE, description, data)
        {
            //Type          ParameterName           HexValue    Mnemonic
            //----------------------------------------------------------
            //HeaderByte    FormatByte              B8          FMT
            //HeaderByte    TargetByte              12          TGT
            //HeaderByte    SourceByte              F1          SRC
            //HeaderByte    LengthByte              ??          LEN         4
            //ServiceID     ServiceID               2C          DDLI        ||D||
            //ParameterType LocalIdentifier         10          RLI_        ||A||
            //ParameterType PID#1 HighByte          OF                      ||T||
            //ParameterType PID#1 LowByte           10                      ||A||
            //Checksum      ChecksumByte            ??(7C)      CS

            byte check = 0x00;
            check ^= (byte)formatByte;
            check ^= (byte)DestinationDevice;
            check ^= (byte)SourceDevice;
            check ^= (byte)data.Length;
            foreach (byte b in data)
            {
                check ^= b;
            }

            PacketLength = data.Length + 5; // 5 - FormatByte + TargetByte + SourceByte + LengthByte + CRC;
            CRC = check;
        }

        public string DataString
        {
            get
            {
                if (dataString == null)
                {
                    dataString = Encoding.UTF8.GetString(Data);
                }
                return dataString;
            }
        }

        public new byte CRC
        {
            get
            {
                return check;
            }
            private set
            {
                check = value;
            }
        }

        public new int PacketLength
        {
            get
            {
                return packetLength;
            }
            private set
            {
                packetLength = value;
            }
        }

        public new byte[] Packet
        {
            get
            {
                if (this.packet != null)
                {
                    return this.packet;
                }

                byte[] packet = new byte[PacketLength];
                packet[0] = formatByte;
                packet[1] = (byte) DestinationDevice;
                packet[2] = (byte) SourceDevice;
                packet[3] = (byte) Data.Length;
                Data.CopyTo(packet, 4);
                packet[PacketLength - 1] = CRC;

                this.packet = packet;
                return packet;
            }
        }

        public Message ToIBusMessage()
        {
            return new Message(SourceDevice, DestinationDevice, ReceiverDescription, Data);
        }

        public static new DBusMessage TryCreate(byte[] packet, int length = -1)
        {
            if (length < 0)
            {
                length = packet.Length;
            }
            if (!IsValid(packet))
            {
                return null;
            }

            return new DBusMessage(packet.SkipAndTake(4, ParseDataLength(packet)));
        }

        public static bool IsValid(byte[] packet)
        {
            return IsValid(packet, (byte)packet.Length);
        }

        public static new bool IsValid(byte[] packet, int length)
        {
            if (length < PacketLengthMin)
            {
                return false;
            }

            byte packetLength = (byte)ParsePacketLength(packet);
            if (length < packetLength || packetLength < PacketLengthMin)
            {
                return false;
            }

            byte check = 0x00;
            for (byte i = 0; i < packetLength - 1; i++)
            {
                check ^= packet[i];
            }
            return check == packet[packetLength - 1];
        }

        public static new bool CanStartWith(byte[] packet, int length = -1)
        {
            return CanStartWith(packet, ParsePacketLength, length);
        }

        protected static new bool CanStartWith(byte[] packet, IntFromByteArray packetLengthCallback, int length = -1)
        {
            if (length < 0)
            {
                length = packet.Length;
            }

            if (length < PacketLengthMin)
            {
                return true;
            }

            byte packetLength = (byte)(packet[1] + 2);
            if (packetLength < PacketLengthMin)
            {
                return false;
            }

            if (length >= packetLength && !IsValid(packet, length))
            {
                return false;
            }

            return true;
        }

        protected static new int ParsePacketLength(byte[] packet)
        {
            if (packet.Length < PacketLengthMin)
            {
                return 0;
            }
            return packet[1];
        }

        protected static new int ParseDataLength(byte[] packet)
        {
            if (packet.Length < PacketLengthMin)
            {
                return 0;
            }
            return ParsePacketLength(packet) - 3;
        }
    }
}