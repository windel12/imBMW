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
        new byte[] packet;
        //byte check;
        string dataString;

        /// <summary>0xB8</summary>
        public static byte formatByte = 0xB8;

        public DBusMessage(DeviceAddress source, DeviceAddress destination, params byte[] data)
            : base(source, destination, data)
        {
            //Type          ParameterName                       HexValue    Mnemonic
            //-------------------------------------------------------------------------------
            //HeaderByte    FormatByte                          B8          FMT
            //HeaderByte    TargetByte                          12          TGT
            //HeaderByte    SourceByte                          F1          SRC
            //HeaderByte    LengthByte                          ??          LEN         4
            //ServiceID     dynamicallyDefinedLocalIdentifier   2C          DDLI        ||D||
            //ParameterType recordLocalIdentifier               10          RLI_        ||A||
            //ParameterType PID#1 HighByte                      OF                      ||T||
            //ParameterType PID#1 LowByte                       10                      ||A||
            //Checksum      ChecksumByte                        ??(7C)      CS

            //Type          ParameterName                       HexValue    Mnemonic
            //-------------------------------------------------------------------------------
            //HeaderByte    FormatByte                          B8          FMT
            //HeaderByte    TargetByte                          F1          TGT
            //HeaderByte    SourceByte                          12          SRC
            //HeaderByte    LengthByte                          ??          LEN         4
            //ServiceID     Positive response message           6C          DDLI        ||D||
            //ParameterType recordLocalIdentifier               10          RLI_        ||A||
            //ParameterType PID#1 HighByte                      OF                      ||T||
            //ParameterType PID#1 LowByte                       10                      ||A||
            //Checksum      ChecksumByte                        ??(7C)      CS

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

        public override byte[] Packet
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

        public new static DBusMessage TryCreate(byte[] packet, int length = -1)
        {
            if (length < 0)
            {
                length = packet.Length;
            }
            if (!IsValid(packet, length))
            {
                return null;
            }

            return new DBusMessage((DeviceAddress)packet[2], (DeviceAddress)packet[1], packet.SkipAndTake(4, DBusMessage.ParseDataLength(packet)));
        }

        protected new static bool IsValid(byte[] packet, int length = -1)
        {
            return IsValid(packet, ParsePacketLength, length);
        }

        protected new static bool IsValid(byte[] packet, IntFromByteArray packetLengthCallback, int length = -1)
        {
            if (length < 0)
            {
                length = packet.Length;
            }
            if (length < PacketLengthMin)
            {
                return false;
            }

            int packetLength = packetLengthCallback(packet);
            if (length < packetLength || packetLength < PacketLengthMin)
            {
                return false;
            }

            byte check = 0x00;
            for (int i = 0; i < packetLength; i++)
            {
                check ^= packet[i];
            }
            return check == packet[packetLength];
        }

        public new static bool CanStartWith(byte[] packet, int length = -1)
        {
            return CanStartWith(packet, ParsePacketLength, length);
        }

        protected new static bool CanStartWith(byte[] packet, IntFromByteArray packetLengthCallback, int length = -1)
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

        protected new static int ParsePacketLength(byte[] packet)
        {
            if (packet.Length < PacketLengthMin)
            {
                return 0;
            }
            return packet[3] + 4;
        }

        protected new static int ParseDataLength(byte[] packet)
        {
            if (packet.Length < PacketLengthMin)
            {
                return 0;
            }
            return ParsePacketLength(packet) - 4;
        }
    }
}