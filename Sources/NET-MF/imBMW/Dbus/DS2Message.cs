using imBMW.Tools;

namespace imBMW.iBus
{
    /// <summary>
    /// BMW DS2 Diagnostic Bus (DBus) message packet
    /// </summary>
    public class DS2Message : Message
    {
        public new static int PacketLengthMin { get { return 4; } }

        public DS2Message(DeviceAddress destination, params byte[] data)
            : base(DeviceAddress.OBD, destination, data)
        {
            PacketLength = data.Length + 3; // 3 - Destination + LengthByte + CRC;

            byte check = 0x00;
            check ^= (byte)DestinationDevice;
            check ^= (byte)PacketLength;
            foreach (byte b in data)
            {
                check ^= b;
            }

            CRC = check;
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
                packet[0] = (byte)DestinationDevice;
                packet[1] = (byte)PacketLength;
                Data.CopyTo(packet, 2);
                packet[PacketLength - 1] = CRC;

                this.packet = packet;
                return packet;
            }
        }

        public new static DS2Message TryCreate(byte[] packet, int length = -1)
        {
            if (length < 0)
            {
                length = packet.Length;
            }
            if (!IsValid(packet, length))
            {
                return null;
            }

            return new DS2Message((DeviceAddress)packet[0], packet.SkipAndTake(2, DS2Message.ParseDataLength(packet)));
        }

        protected new static bool IsValid(byte[] packet, int length = -1)
        {
            if (packet[0] == DBusMessage.formatByte)
            {
                return false;
            }
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
            for (int i = 0; i < packetLength - 1; i++)
            {
                check ^= packet[i];
            }
            return check == packet[packetLength - 1];
        }

        protected new static int ParsePacketLength(byte[] packet)
        {
            if (packet.Length < PacketLengthMin)
            {
                return 0;
            }
            return packet[1];
        }

        protected new static int ParseDataLength(byte[] packet)
        {
            if (packet.Length < PacketLengthMin)
            {
                return 0;
            }
            return ParsePacketLength(packet) - 3;
        }

        public Message ToIKBusMessage()
        {
            return new Message(DeviceAddress.Diagnostic, this.DestinationDevice, this.Data);
        }
    }
}
