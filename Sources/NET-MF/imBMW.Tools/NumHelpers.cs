using System;

namespace imBMW.Tools
{
    public static class NumHelpers
    {
        const string hexChars = "0123456789ABCDEF";

        public static String ToHex(this byte b)
        {
            return hexChars[b >> 4].ToString() + hexChars[b & 0x0F].ToString();
        }

        public static short ToShort(byte byte1, byte byte2)
        {
            return (short)((byte2 << 8) + byte1);
        }

        public static void FromShort(short number, out byte byte1, out byte byte2)
        {
            byte2 = (byte)(number >> 8);
            byte1 = (byte)(number & 255);
        }

        public static byte Invert(this byte b)
        {
            return (byte)~b;
        }

        public static bool HasBits(this byte b, byte bits)
        {
            return (b & bits) != 0;
        }

        public static bool HasBit(this byte b, byte bitIndex)
        {
            checkByteBitIndex(bitIndex);
            return b.HasBits((byte)(1 << bitIndex));
        }

        public static byte RemoveBits(this byte b, byte bits)
        {
            return (byte)(b & bits.Invert());
        }

        public static byte RemoveBit(this byte b, byte bitIndex)
        {
            checkByteBitIndex(bitIndex);
            return b.RemoveBits((byte)(1 << bitIndex));
        }

        public static byte AddBits(this byte b, byte bits)
        {
            return (byte)(b | bits);
        }

        public static byte AddBits(this byte b, int bits)
        {
            return AddBits(b, (byte) bits);
        }

        public static byte AddBit(this byte b, byte bitIndex)
        {
            checkByteBitIndex(bitIndex);
            return b.AddBits((byte)(1 << bitIndex));
        }

        static void checkByteBitIndex(byte bitIndex)
        {
            if (bitIndex < 0 || bitIndex > 7)
            {
                throw new ArgumentException("bitIndex");
            }
        }
    }
}
