///////////////////////////////////////////////////////////////////////////////
//	Copyright 2013 JASDev International
//
//	Licensed under the Apache License, Version 2.0 (the "License");
//	you may not use this file except in compliance with the License.
//	You may obtain a copy of the License at
//
//		http://www.apache.org/licenses/LICENSE-2.0
//
//	Unless required by applicable law or agreed to in writing, software
//	distributed under the License is distributed on an "AS IS" BASIS,
//	WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//	See the License for the specific language governing permissions and
//	limitations under the License.
///////////////////////////////////////////////////////////////////////////////

using System;

namespace JDI.Common.Extensions
{
	public static class IntExtensions
	{
		// TODO - test these extensions
		#region Byte Extensions

		public static string ToHex(this byte value)
		{
			char[] charValues = new char[2];
			charValues[0] = hexValues[(value >> 4) & 0x0F];
			charValues[1] = hexValues[(value) & 0x0F];
			return new string(charValues);
		}

		#endregion

		#region Int16 Extensions

		public static byte[] ToBytes(this Int16 value, JDIConst.ByteOrder byteOrder = JDIConst.ByteOrder.Network)
		{
			byte[] bytes = new byte[2];
			switch (byteOrder)
			{
				case JDIConst.ByteOrder.LittleEndian:
					bytes[1] = (byte)((value >> 8) & 0xFF);
					bytes[0] = (byte)((value >> 0) & 0xFF);
					break;
				case JDIConst.ByteOrder.BigEndian:
				case JDIConst.ByteOrder.Network:
					bytes[0] = (byte)((value >> 8) & 0xFF);
					bytes[1] = (byte)((value >> 0) & 0xFF);
					break;
			}
			return bytes;
		}

		public static string ToHex(this Int16 value)
		{
			char[] charValues = new char[4];
			charValues[0] = hexValues[(byte)((value >> 12) & 0x0F)];
			charValues[1] = hexValues[(byte)((value >> 8) & 0x0F)];
			charValues[2] = hexValues[(byte)((value >> 4) & 0x0F)];
			charValues[3] = hexValues[(byte)((value >> 0) & 0x0F)];
			return new string(charValues);
		}

		#endregion

		#region Int32 Extensions

		public static byte[] ToBytes(this Int32 value, JDIConst.ByteOrder byteOrder = JDIConst.ByteOrder.Network)
		{
			byte[] bytes = new byte[4];
			switch (byteOrder)
			{
				case JDIConst.ByteOrder.LittleEndian:
					bytes[3] = (byte)((value >> 24) & 0xFF);
					bytes[2] = (byte)((value >> 16) & 0xFF);
					bytes[1] = (byte)((value >> 8) & 0xFF);
					bytes[0] = (byte)((value >> 0) & 0xFF);
					break;
				case JDIConst.ByteOrder.BigEndian:
				case JDIConst.ByteOrder.Network:
					bytes[0] = (byte)((value >> 24) & 0xFF);
					bytes[1] = (byte)((value >> 16) & 0xFF);
					bytes[2] = (byte)((value >> 8) & 0xFF);
					bytes[3] = (byte)((value >> 0) & 0xFF);
					break;
			}
			return bytes;
		}

		public static string ToHex(this Int32 value)
		{
			char[] charValues = new char[8];
			charValues[0] = hexValues[(byte)((value >> 28) & 0x0F)];
			charValues[1] = hexValues[(byte)((value >> 24) & 0x0F)];
			charValues[2] = hexValues[(byte)((value >> 20) & 0x0F)];
			charValues[3] = hexValues[(byte)((value >> 16) & 0x0F)];
			charValues[4] = hexValues[(byte)((value >> 12) & 0x0F)];
			charValues[5] = hexValues[(byte)((value >> 8) & 0x0F)];
			charValues[6] = hexValues[(byte)((value >> 4) & 0x0F)];
			charValues[7] = hexValues[(byte)((value >> 0) & 0x0F)];
			return new string(charValues);
		}

		// TODO - finish implementing int.Format extension method
		public static string Format(this Int32 value, string format)
		{
			return value.ToString();
		}

		#endregion

		#region UInt16 Extensions

		public static byte[] ToBytes(this UInt16 value, JDIConst.ByteOrder byteOrder = JDIConst.ByteOrder.Network)
		{
			byte[] bytes = new byte[2];
			switch (byteOrder)
			{
				case JDIConst.ByteOrder.LittleEndian:
					bytes[1] = (byte)((value >> 8) & 0xFF);
					bytes[0] = (byte)((value >> 0) & 0xFF);
					break;
				case JDIConst.ByteOrder.BigEndian:
				case JDIConst.ByteOrder.Network:
					bytes[0] = (byte)((value >> 8) & 0xFF);
					bytes[1] = (byte)((value >> 0) & 0xFF);
					break;
			}
			return bytes;
		}

		public static string ToHex(this UInt16 value)
		{
			char[] charValues = new char[4];
			charValues[0] = hexValues[(byte)((value >> 12) & 0x0F)];
			charValues[1] = hexValues[(byte)((value >> 8) & 0x0F)];
			charValues[2] = hexValues[(byte)((value >> 4) & 0x0F)];
			charValues[3] = hexValues[(byte)((value >> 0) & 0x0F)];
			return new string(charValues);
		}

		#endregion

		#region UInt32 Extensions

		public static byte[] ToBytes(this UInt32 value, JDIConst.ByteOrder byteOrder = JDIConst.ByteOrder.Network)
		{
			byte[] bytes = new byte[4];
			switch (byteOrder)
			{
				case JDIConst.ByteOrder.LittleEndian:
					bytes[3] = (byte)((value >> 24) & 0xFF);
					bytes[2] = (byte)((value >> 16) & 0xFF);
					bytes[1] = (byte)((value >> 8) & 0xFF);
					bytes[0] = (byte)((value >> 0) & 0xFF);
					break;
				case JDIConst.ByteOrder.BigEndian:
				case JDIConst.ByteOrder.Network:
					bytes[0] = (byte)((value >> 24) & 0xFF);
					bytes[1] = (byte)((value >> 16) & 0xFF);
					bytes[2] = (byte)((value >> 8) & 0xFF);
					bytes[3] = (byte)((value >> 0) & 0xFF);
					break;
			}
			return bytes;
		}

		public static string ToHex(this UInt32 value)
		{
			char[] charValues = new char[8];
			charValues[0] = hexValues[(byte)((value >> 28) & 0x0F)];
			charValues[1] = hexValues[(byte)((value >> 24) & 0x0F)];
			charValues[2] = hexValues[(byte)((value >> 20) & 0x0F)];
			charValues[3] = hexValues[(byte)((value >> 16) & 0x0F)];
			charValues[4] = hexValues[(byte)((value >> 12) & 0x0F)];
			charValues[5] = hexValues[(byte)((value >> 8) & 0x0F)];
			charValues[6] = hexValues[(byte)((value >> 4) & 0x0F)];
			charValues[7] = hexValues[(byte)((value >> 0) & 0x0F)];
			return new string(charValues);
		}

		#endregion


		#region Member Fields

		private static string hexValues = "0123456789ABCDEF";

		#endregion
	}
}
