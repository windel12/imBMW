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
using System.Text;
using System.Collections;

namespace JDI.Common.Extensions
{
	public static class ArrayExtensions
	{
		// TODO - test these extensions
		#region To Hex Extensions

		/// <summary>
		/// Outputs a space separated array of hexadecimal characters
		/// </summary>
		/// <returns>Space separated array of hexadecimal characters</returns>
		public static string ToHex(this byte[] array)
		{
			if (array == null || array.Length == 0)
				return String.Empty;

			return ToHex(array, 0, array.Length);
		}

		/// <summary>
		/// Outputs a space separated array of hexadecimal characters
		/// </summary>
		/// <param name="length">Number of bytes to process</param>
		/// <returns>Space separated array of hexadecimal characters</returns>
		public static string ToHex(this byte[] array, int length)
		{
			if (array == null || array.Length == 0 || length == 0)
				return String.Empty;

			return ToHex(array, 0, length);
		}

		/// <summary>
		/// Outputs a space separated array of hexadecimal characters
		/// </summary>
		/// <param name="offset">Start index</param>
		/// <param name="length">Number of bytes to process</param>
		/// <returns>Space separated array of hexadecimal characters</returns>
		public static string ToHex(this byte[] array, int offset, int length)
		{
			if (array == null || array.Length == 0 || array.Length < (offset + length))
				return String.Empty;

			byte dataByte;
			char[] chars = new char[length * 3];
			int end = offset + length;
			int j = 0;
			for (int i = offset; i < end; i++)
			{
				dataByte = array[i];
				chars[j++] = hexValues[(dataByte >> 4) & 0x0F];
				chars[j++] = hexValues[dataByte & 0x0F];
				chars[j++] = ' ';
			}

			return new string(chars, 0, chars.Length);
		}

		#endregion


		#region To Integer Extensions

		/// <summary>
		/// Outputs an Int16 value
		/// </summary>
		/// <param name="offset">Start index</param>
		/// <param name="byteOrder">Byte order</param>
		/// <returns>Int16 value</returns>
		public static Int16 ToInt16(this byte[] array, int offset, JDIConst.ByteOrder byteOrder = JDIConst.ByteOrder.Network)
		{
			Int16 value = 0;
			switch (byteOrder)
			{
				case JDIConst.ByteOrder.LittleEndian:
					value = (Int16)((array[offset + 1] << 8) + (array[offset]));
					break;
				case JDIConst.ByteOrder.BigEndian:
				case JDIConst.ByteOrder.Network:
					value = (Int16)((array[offset] << 8) + (array[offset + 1]));
					break;
			}
			return value;
		}

		/// <summary>
		/// Outputs an Int32 value
		/// </summary>
		/// <param name="offset">Start index</param>
		/// <param name="byteOrder">Byte order</param>
		/// <returns>Int32 value</returns>
		public static Int32 ToInt32(this byte[] array, int offset, JDIConst.ByteOrder byteOrder = JDIConst.ByteOrder.Network)
		{
			Int32 value = 0;
			switch (byteOrder)
			{
				case JDIConst.ByteOrder.LittleEndian:
					value = (Int32)((array[offset + 3] << 24) + (array[offset + 2] << 16) + (array[offset + 1] << 8) + (array[offset]));
					break;
				case JDIConst.ByteOrder.BigEndian:
				case JDIConst.ByteOrder.Network:
					value = (Int32)((array[offset] << 24) + (array[offset + 1] << 16) + (array[offset + 2] << 8) + (array[offset + 3]));
					break;
			}
			return value;
		}

		/// <summary>
		/// Outputs an Int64 value
		/// </summary>
		/// <param name="offset">Start index</param>
		/// <param name="byteOrder">Byte order</param>
		/// <returns>Int64 value</returns>
		public static Int64 ToInt64(this byte[] array, int offset, JDIConst.ByteOrder byteOrder = JDIConst.ByteOrder.Network)
		{
			Int64 value = 0;
			switch (byteOrder)
			{
				case JDIConst.ByteOrder.LittleEndian:
					value = (Int64)((array[offset + 7] << 56) + (array[offset + 6] << 48) + (array[offset + 5] << 40) + (array[offset + 4] << 32) + (array[offset + 3] << 24) + (array[offset + 2] << 16) + (array[offset + 1] << 8) + (array[offset]));
					break;
				case JDIConst.ByteOrder.BigEndian:
				case JDIConst.ByteOrder.Network:
					value = (Int64)((array[offset] << 56) + (array[offset + 1] << 48) + (array[offset + 2] << 40) + (array[offset + 3] << 32) + (array[offset + 4] << 24) + (array[offset + 5] << 16) + (array[offset + 6] << 8) + (array[offset + 7]));
					break;
			}
			return value;
		}

		/// <summary>
		/// Outputs an UInt16 value
		/// </summary>
		/// <param name="offset">Start index</param>
		/// <param name="byteOrder">Byte order</param>
		/// <returns>UInt16 value</returns>
		public static UInt16 ToUInt16(this byte[] array, int offset, JDIConst.ByteOrder byteOrder = JDIConst.ByteOrder.Network)
		{
			UInt16 value = 0;
			switch (byteOrder)
			{
				case JDIConst.ByteOrder.LittleEndian:
					value = (UInt16)((array[offset + 1] << 8) + (array[offset]));
					break;
				case JDIConst.ByteOrder.BigEndian:
				case JDIConst.ByteOrder.Network:
					value = (UInt16)((array[offset] << 8) + (array[offset + 1]));
					break;
			}
			return value;
		}

		/// <summary>
		/// Outputs an UInt32 value
		/// </summary>
		/// <param name="offset">Start index</param>
		/// <param name="byteOrder">Byte order</param>
		/// <returns>UInt32 value</returns>
		public static UInt32 ToUInt32(this byte[] array, int offset, JDIConst.ByteOrder byteOrder = JDIConst.ByteOrder.Network)
		{
			UInt32 value = 0;
			switch (byteOrder)
			{
				case JDIConst.ByteOrder.LittleEndian:
					value = (UInt32)((array[offset + 3] << 24) + (array[offset + 2] << 16) + (array[offset + 1] << 8) + (array[offset]));
					break;
				case JDIConst.ByteOrder.BigEndian:
				case JDIConst.ByteOrder.Network:
					value = (UInt32)((array[offset] << 24) + (array[offset + 1] << 16) + (array[offset + 2] << 8) + (array[offset + 3]));
					break;
			}
			return value;
		}

		/// <summary>
		/// Outputs an UInt64 value
		/// </summary>
		/// <param name="offset">Start index</param>
		/// <param name="byteOrder">Byte order</param>
		/// <returns>UInt64 value</returns>
		public static UInt64 ToUInt64(this byte[] array, int offset, JDIConst.ByteOrder byteOrder = JDIConst.ByteOrder.Network)
		{
			UInt64 value = 0;
			switch (byteOrder)
			{
				case JDIConst.ByteOrder.LittleEndian:
					value = (UInt64)((array[offset + 7] << 56) + (array[offset + 6] << 48) + (array[offset + 5] << 40) + (array[offset + 4] << 32) + (array[offset + 3] << 24) + (array[offset + 2] << 16) + (array[offset + 1] << 8) + (array[offset]));
					break;
				case JDIConst.ByteOrder.BigEndian:
				case JDIConst.ByteOrder.Network:
					value = (UInt64)((array[offset] << 56) + (array[offset + 1] << 48) + (array[offset + 2] << 40) + (array[offset + 3] << 32) + (array[offset + 4] << 24) + (array[offset + 5] << 16) + (array[offset + 6] << 8) + (array[offset + 7]));
					break;
			}
			return value;
		}

		#endregion


		#region To String Extensions

		/// <summary>
		/// Outputs a UTF8 string converted from all bytes in this array.
		/// </summary>
		/// <param name="offset">Start index</param>
		/// <param name="length">Number of bytes to process</param>
		/// <returns>A string converted from all bytes in this array.</returns>
		public static string ToString(this byte[] array)
		{
			if (array == null || array.Length == 0)
				return String.Empty;

			return new string(Encoding.UTF8.GetChars(array));
		}

		/// <summary>
		/// Outputs a UTF8 string converted from a subset of bytes in this array.
		/// </summary>
		/// <param name="offset">Start index</param>
		/// <param name="length">Number of bytes to convert.</param>
		/// <returns>A string converted from bytes in this array.</returns>
		public static string ToString(this byte[] array, int offset, int length)
		{
			if (array == null || array.Length == 0 || array.Length < (offset + length))
				return String.Empty;

			return new string(Encoding.UTF8.GetChars(array, offset, length));
		}

		#endregion


		#region Misc Functions

		/// <summary>
		/// Outputs a formatted (##-##-##-##-##-##) MAC Address string
		/// </summary>
		/// <param name="offset">Start index</param>
		/// <returns>Formatted MAC address string</returns>
		public static string ToMACAddress(this byte[] array, int offset)
		{
			if (array == null || array.Length == 0 || offset < 0 || offset >= array.Length)
				return String.Empty;

			string[] macAddr = new string[6];

			macAddr[0] = array[offset + 0].ToHex();
			macAddr[1] = array[offset + 1].ToHex();
			macAddr[2] = array[offset + 2].ToHex();
			macAddr[3] = array[offset + 3].ToHex();
			macAddr[4] = array[offset + 4].ToHex();
			macAddr[5] = array[offset + 5].ToHex();

			return String.Concat(macAddr[0], "-", macAddr[1], "-", macAddr[2], "-", macAddr[3], "-", macAddr[4], "-", macAddr[5]);
		}

		/// <summary>
		/// Outputs a formatted (###.###.###.###) IP Address string
		/// </summary>
		/// <param name="offset">Start index</param>
		/// <returns>Formatted IP address string</returns>
		public static string ToIPAddress(this byte[] array, int offset)
		{
			if (array == null || array.Length == 0 || offset < 0 || offset >= array.Length)
				return String.Empty;

			string[] ipAddr = new string[4];

			ipAddr[0] = array[offset + 0].ToString();
			ipAddr[1] = array[offset + 1].ToString();
			ipAddr[2] = array[offset + 2].ToString();
			ipAddr[3] = array[offset + 3].ToString();

			return String.Concat(ipAddr[0], ".", ipAddr[1], ".", ipAddr[2], ".", ipAddr[3]);
		}

		/// <summary>
		/// Reports the zero-based index of the first occurrence of the specified string in this instance. The search starts at a specified position.
		/// </summary>
		/// <param name="array"></param>
		/// <param name="value">The string to find.</param>
		/// <param name="startIndex">The search starting position.</param>
		/// <returns>The zero-based index position of value if value is found, or -1 if it is not. If value is null or a zero-length string, the return value is startIndex.</returns>
		public static int IndexOf(this byte[] array, string value, int startIndex = 0)
		{
			if (array == null || value == null || value.Length == 0 || startIndex < 0 || startIndex >= array.Length)
				return -1;

			byte[] byteValue = value.ToByteArray();
			return IndexOf(array, byteValue, startIndex);
		}

		/// <summary>
		/// Reports the zero-based index of the first occurrence of the specified byte-array in this instance. The search starts at a specified position.
		/// </summary>
		/// <param name="array"></param>
		/// <param name="value">The bytes to find.</param>
		/// <param name="startIndex">The search starting position.</param>
		/// <returns>The zero-based index position of value if value is found, or -1 if it is not. Invalid paramaters result in a return a value of -1.</returns>
		public static int IndexOf(this byte[] array, byte[] value, int startIndex = 0)
		{
			if (array == null || value == null || value.Length == 0 || startIndex < 0 || startIndex >= array.Length)
				return -1;

			int i = startIndex;
			int j = 0;
			int k = 0;
			while (i < array.Length)
			{
				j = i;
				k = 0;
				while (k < value.Length && array[j] == value[k])
				{
					j++;
					k++;
				}
				if (k == value.Length)
				{
					return i;
				}
				i++;
			}

			return -1;
		}

		/// <summary>
		/// Returns a arraylist of strings that contains the substrings in this instance that are delimited by 'delimiter'.
		/// </summary>
		/// <param name="array"></param>
		/// <param name="delimiter">String that delimits the substrings.</param>
		/// <param name="startIndex">The search start position.</param>
		/// <param name="length">The number of bytes to search.</param>
		/// <param name="toLower">Will the convert the substrings to lower case if 'true'.</param>
		/// <returns>An arraylist containing the substrings found in this instance. Invalid paramaters result in a return of an empty arraylist.</returns>
		public static ArrayList Split(this byte[] array, string delimiter, int startIndex, int length, bool toLower = false)
		{
			if (array == null || startIndex < 0 || length < 0 || array.Length < (startIndex + length))
				return new ArrayList();

			byte[] byteSeparator = delimiter.ToByteArray();
			return Split(array, byteSeparator, startIndex, length, toLower);
		}

		/// <summary>
		/// Returns a arraylist of strings that contains the substrings in this instance that are delimited by 'delimiter'.
		/// </summary>
		/// <param name="array"></param>
		/// <param name="delimiter">Character that delimits the substrings.</param>
		/// <param name="startIndex">The search start position.</param>
		/// <param name="length">The number of bytes to search.</param>
		/// <param name="toLower">Will the convert the substrings to lower case if 'true'.</param>
		/// <returns>An arraylist containing the substrings found in this instance. Invalid paramaters result in a return of an empty arraylist.</returns>
		public static ArrayList Split(this byte[] array, char delimiter, int startIndex, int length, bool toLower=false)
		{
			if (array == null || startIndex < 0 || length < 0 || array.Length < (startIndex + length))
				return new ArrayList();

			byte[] byteSeparator = new byte[] { (byte)delimiter };
			return Split(array, byteSeparator, startIndex, length, toLower);
		}

		/// <summary>
		/// Returns a arraylist of strings that contains the substrings in this instance that are delimited by 'delimiter'.
		/// </summary>
		/// <param name="array"></param>
		/// <param name="delimiter">Bytes that delimit the substrings.</param>
		/// <param name="startIndex">The search start position.</param>
		/// <param name="length">The number of bytes to search.</param>
		/// <param name="toLower">Will the convert the substrings to lower case if 'true'.</param>
		/// <returns>An arraylist containing the substrings found in this instance. Invalid paramaters result in a return of an empty arraylist.</returns>
		public static ArrayList Split(this byte[] array, byte[] delimiter, int startIndex, int length, bool toLower = false)
		{
			if (array == null || startIndex < 0 || length < 0 || array.Length < (startIndex + length))
				return new ArrayList();

			ArrayList list = new ArrayList();

			int posMax = startIndex + length;
			int posDelimiter = 0;
			while ((startIndex + delimiter.Length) <= posMax && (posDelimiter = IndexOf(array, delimiter, startIndex)) >= 0)
			{
				if (toLower)
				{
					list.Add((new string(Encoding.UTF8.GetChars(array, startIndex, posDelimiter - startIndex))).ToLower());
				}
				else
				{
					list.Add(new string(Encoding.UTF8.GetChars(array, startIndex, posDelimiter - startIndex)));
				}
				startIndex = posDelimiter + delimiter.Length;
			}
			return list;
		}

		/// <summary>
		/// Outputs a sub-array
		/// </summary>
		/// <param name="offset">Start index of sub-array</param>
		/// <param name="length">Length of sub-array</param>
		/// <returns>Byte array extracted from a larger byte array. Invalid paramaters result in a return of an empty byte-array.</returns>
		public static byte[] SubArray(this byte[] array, int startIndex, int length)
		{
			if (array == null || array.Length < (startIndex + length))
				return new byte[0];

			byte[] bytes = new byte[length];
			Array.Copy(array, startIndex, bytes, 0, length);
			return bytes;
		}

		#endregion


		#region Member Fields

		private static string hexValues = "0123456789ABCDEF";

		#endregion
	}
}
