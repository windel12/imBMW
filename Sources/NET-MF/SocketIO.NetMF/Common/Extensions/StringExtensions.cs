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
using System.Text.RegularExpressions;

namespace JDI.Common.Extensions
{
	public static class StringExtensions
	{
		// TODO - test these extensions

		#region To Functions

		/// <summary>
		/// Outputs a string as a byte array
		/// </summary>
		/// <returns>Byte array</returns>
		public static byte[] ToByteArray(this string s)
		{
			return Encoding.UTF8.GetBytes(s);
		}

		#endregion

		#region Parse Functions

		/// <summary>
		/// Converts a string to a Boolean value
		/// </summary>
		/// <param name="defaultValue">Default value to return if string cannot be parsed</param>
		/// <returns>Boolean value</returns>
		public static bool ParseBool(this string s, bool defaultValue = false)
		{
			if (s == null)
				return defaultValue;

			s = s.Trim();
			if (s.Length == 0)
				return defaultValue;

			s = s.ToLower();
			if (s == "1" || s == "true")
			{
				return true;
			}
			else if (s == "0" || s == "false")
			{
				return false;
			}

			return defaultValue;
		}

		/// <summary>
		/// Converts a hex string to a Byte value
		/// </summary>
		/// <param name="defaultValue">Default value to return if string cannot be parsed</param>
		/// <returns>Byte value</returns>
		public static byte ParseByte(this string s, byte defaultValue = 0)
		{
			if (s == null)
				return defaultValue;

			s = s.Trim();
			if (s.Length != 2)
				return defaultValue;

			s = s.ToUpper();
			int value0 = hexValues.IndexOf(s[0]);
			int value1 = hexValues.IndexOf(s[1]);
			if (value0 < 0 || value1 < 0)
				return defaultValue;

			return (byte)((value0 << 4) + value1);
		}

		/// <summary>
		/// Converts a hex string to an Int16 value
		/// </summary>
		/// <param name="defaultValue">Default value to return if string cannot be parsed</param>
		/// <returns>Int16 value</returns>
		public static Int16 ParseInt16(this string s, Int16 defaultValue = 0)
		{
			if (s == null)
				return defaultValue;

			s = s.Trim();
			if (s.Length != 4)
				return defaultValue;

			s = s.ToUpper();
			int value0 = hexValues.IndexOf(s[0]);
			int value1 = hexValues.IndexOf(s[1]);
			int value2 = hexValues.IndexOf(s[2]);
			int value3 = hexValues.IndexOf(s[3]);
			if (value0 < 0 || value1 < 0 || value2 < 0 || value3 < 0)
				return defaultValue;

			return (Int16)((value0 << 12) + (value1 << 8) + (value2 << 4) + value3);
		}
		/// <summary>
		/// Converts a hex string to an Int32 value
		/// </summary>
		/// <param name="defaultValue">Default value to return if string cannot be parsed</param>
		/// <returns>Int32 value</returns>
		public static Int32 ParseInt32(this string s, Int32 defaultValue = 0)
		{
			if (s == null)
				return defaultValue;

			s = s.Trim();
			if (s.Length != 8)
				return defaultValue;

			s = s.ToUpper();
			int value0 = hexValues.IndexOf(s[0]);
			int value1 = hexValues.IndexOf(s[1]);
			int value2 = hexValues.IndexOf(s[2]);
			int value3 = hexValues.IndexOf(s[3]);
			int value4 = hexValues.IndexOf(s[4]);
			int value5 = hexValues.IndexOf(s[5]);
			int value6 = hexValues.IndexOf(s[6]);
			int value7 = hexValues.IndexOf(s[7]);
			if (value0 < 0 || value1 < 0 || value2 < 0 || value3 < 0 || value4 < 0 || value5 < 0 || value6 < 0 || value7 < 0)
				return defaultValue;

			return (Int32)((value0 << 28) + (value1 << 24) + (value2 << 20) + (value3 << 16) + (value4 << 12) + (value5 << 8) + (value6 << 4) + value7);
		}

		/// <summary>
		/// Converts a hex string to an UInt16 value
		/// </summary>
		/// <param name="defaultValue">Default value to return if string cannot be parsed</param>
		/// <returns>UInt16 value</returns>
		public static UInt16 ParseUInt16(this string s, UInt16 defaultValue = 0)
		{
			if (s == null)
				return defaultValue;

			s = s.Trim();
			if (s.Length != 4)
				return defaultValue;

			s = s.ToUpper();
			int value0 = hexValues.IndexOf(s[0]);
			int value1 = hexValues.IndexOf(s[1]);
			int value2 = hexValues.IndexOf(s[2]);
			int value3 = hexValues.IndexOf(s[3]);
			if (value0 < 0 || value1 < 0 || value2 < 0 || value3 < 0)
				return defaultValue;

			return (UInt16)((value0 << 12) + (value1 << 8) + (value2 << 4) + value3);
		}

		/// <summary>
		/// Converts a hex string to an UInt32 value
		/// </summary>
		/// <param name="defaultValue">Default value to return if string cannot be parsed</param>
		/// <returns>UInt32 value</returns>
		public static UInt32 ParseUInt32(this string s, UInt32 defaultValue = 0)
		{
			if (s == null)
				return defaultValue;

			s = s.Trim();
			if (s.Length != 8)
				return defaultValue;

			s = s.ToUpper();
			int value0 = hexValues.IndexOf(s[0]);
			int value1 = hexValues.IndexOf(s[1]);
			int value2 = hexValues.IndexOf(s[2]);
			int value3 = hexValues.IndexOf(s[3]);
			int value4 = hexValues.IndexOf(s[4]);
			int value5 = hexValues.IndexOf(s[5]);
			int value6 = hexValues.IndexOf(s[6]);
			int value7 = hexValues.IndexOf(s[7]);
			if (value0 < 0 || value1 < 0 || value2 < 0 || value3 < 0 || value4 < 0 || value5 < 0 || value6 < 0 || value7 < 0)
				return defaultValue;

			return (UInt32)((value0 << 28) + (value1 << 24) + (value2 << 20) + (value3 << 16) + (value4 << 12) + (value5 << 8) + (value6 << 4) + value7);
		}

		/// <summary>
		/// Converts formatted (##-##-##-##-##-##) MAC address to a byte array.
		/// </summary>
		/// <returns>Byte array</returns>
		public static byte[] ParseMACAddress(this string s)
		{
			string[] macValues = s.Split('-');

			if (s.Length != 17 || macValues.Length != 6)
				return null;

			byte[] macBytes = new byte[6];

			macBytes[0] = macValues[0].ParseByte(0);
			macBytes[1] = macValues[1].ParseByte(0);
			macBytes[2] = macValues[2].ParseByte(0);
			macBytes[3] = macValues[3].ParseByte(0);
			macBytes[4] = macValues[4].ParseByte(0);
			macBytes[5] = macValues[5].ParseByte(0);

			return macBytes;
		}

		/// <summary>
		/// Converts formatted (###.###.###.###) IP address  to a byte array
		/// </summary>
		/// <returns>Byte array</returns>
		public static byte[] ParseIPAddress(this string s)
		{
			string[] ipValues = s.Split('.');

			if (s.Length < 7 || s.Length > 15 || ipValues.Length != 4)
				return null;

			byte[] ipBytes = new byte[4];

			ipBytes[0] = (byte)int.Parse(ipValues[0]);
			ipBytes[1] = (byte)int.Parse(ipValues[1]);
			ipBytes[2] = (byte)int.Parse(ipValues[2]);
			ipBytes[3] = (byte)int.Parse(ipValues[3]);

			return ipBytes;
		}

		#endregion

		#region TryParse Functions

		/// <summary>
		/// Converts a string to a boolean value
		/// </summary>
		/// <param name="value">Boolean value as output parameter</param>
		/// <returns>True if successful</returns>
		public static bool TryParse(this string s, out bool value)
		{
			value = false;

			if (s == null)
				return false;

			s = s.Trim();
			if (s.Length == 0)
				return false;

			s = s.ToLower();
			if (s == "1" || s == "true")
			{
				value = true;
				return true;
			}
			else if (s == "0" || s == "false")
			{
				value = false;
				return true;
			}

			return false;
		}

		/// <summary>
		/// Converts a hex string to a byte value
		/// </summary>
		/// <param name="value">Byte value as output parameter</param>
		/// <returns>True if successful</returns>
		public static bool TryParseHex(this string s, out byte value)
		{
			value = 0;

			if (s == null)
				return false;

			s = s.Trim();
			if (s.Length != 2)
				return false;

			s = s.ToUpper();
			int value0 = hexValues.IndexOf(s[0]);
			int value1 = hexValues.IndexOf(s[1]);
			if (value0 < 0 || value1 < 0)
				return false;

			value = (byte)((value0 << 4) + value1);

			return true;
		}

		/// <summary>
		/// Converts a hex string to an Int16 value
		/// </summary>
		/// <param name="value">Int16 value as output parameter</param>
		/// <returns>True if successful</returns>
		public static bool TryParseHex(this string s, out Int16 value)
		{
			value = 0;

			if (s == null)
				return false;

			s = s.Trim();
			if (s.Length != 4)
				return false;

			s = s.ToUpper();
			int value0 = hexValues.IndexOf(s[0]);
			int value1 = hexValues.IndexOf(s[1]);
			int value2 = hexValues.IndexOf(s[2]);
			int value3 = hexValues.IndexOf(s[3]);
			if (value0 < 0 || value1 < 0 || value2 < 0 || value3 < 0)
				return false;

			value = (Int16)((value0 << 12) + (value1 << 8) + (value2 << 4) + value3);

			return true;
		}

		/// <summary>
		/// Converts a hex string to an Int32 value
		/// </summary>
		/// <param name="value">Int32 value as output parameter</param>
		/// <returns>True if successful</returns>
		public static bool TryParseHex(this string s, out Int32 value)
		{
			value = 0;

			if (s == null)
				return false;

			s = s.Trim();
			if (s.Length != 8)
				return false;

			s = s.ToUpper();
			int value0 = hexValues.IndexOf(s[0]);
			int value1 = hexValues.IndexOf(s[1]);
			int value2 = hexValues.IndexOf(s[2]);
			int value3 = hexValues.IndexOf(s[3]);
			int value4 = hexValues.IndexOf(s[4]);
			int value5 = hexValues.IndexOf(s[5]);
			int value6 = hexValues.IndexOf(s[6]);
			int value7 = hexValues.IndexOf(s[7]);
			if (value0 < 0 || value1 < 0 || value2 < 0 || value3 < 0 || value4 < 0 || value5 < 0 || value6 < 0 || value7 < 0)
				return false;

			value = (Int32)((value0 << 28) + (value1 << 24) + (value2 << 20) + (value3 << 16) + (value4 << 12) + (value5 << 8) + (value6 << 4) + value7);

			return true;
		}

		/// <summary>
		/// Converts a hex string to an UInt16 value
		/// </summary>
		/// <param name="value">UInt16 value as output parameter</param>
		/// <returns>True if successful</returns>
		public static bool TryParseHex(this string s, out UInt16 value)
		{
			value = 0;

			if (s == null)
				return false;

			s = s.Trim();
			if (s.Length != 4)
				return false;

			s = s.ToUpper();
			int value0 = hexValues.IndexOf(s[0]);
			int value1 = hexValues.IndexOf(s[1]);
			int value2 = hexValues.IndexOf(s[2]);
			int value3 = hexValues.IndexOf(s[3]);
			if (value0 < 0 || value1 < 0 || value2 < 0 || value3 < 0)
				return false;

			value = (UInt16)((value0 << 12) + (value1 << 8) + (value2 << 4) + value3);

			return true;
		}

		/// <summary>
		/// Converts a hex string to an UInt32 value
		/// </summary>
		/// <param name="value">UInt32 value as output parameter</param>
		/// <returns>True if successful</returns>
		public static bool TryParseHex(this string s, out UInt32 value)
		{
			value = 0;

			if (s == null)
				return false;

			s = s.Trim();
			if (s.Length != 8)
				return false;

			s = s.ToUpper();
			int value0 = hexValues.IndexOf(s[0]);
			int value1 = hexValues.IndexOf(s[1]);
			int value2 = hexValues.IndexOf(s[2]);
			int value3 = hexValues.IndexOf(s[3]);
			int value4 = hexValues.IndexOf(s[4]);
			int value5 = hexValues.IndexOf(s[5]);
			int value6 = hexValues.IndexOf(s[6]);
			int value7 = hexValues.IndexOf(s[7]);
			if (value0 < 0 || value1 < 0 || value2 < 0 || value3 < 0 || value4 < 0 || value5 < 0 || value6 < 0 || value7 < 0)
				return false;

			value = (UInt32)((value0 << 28) + (value1 << 24) + (value2 << 20) + (value3 << 16) + (value4 << 12) + (value5 << 8) + (value6 << 4) + value7);

			return true;
		}

		#endregion

		#region Replace Functions

		/// <summary>
		/// Returns a new string in which all occurrences of a specified string in the current instance are replaced with another specified string.
		/// </summary>
		/// <param name="s"></param>
		/// <param name="oldValue">The string to be replaced.</param>
		/// <param name="newValue">The string to replace all occurrences of 'oldValue'</param>
		/// <returns>A string that is equivalent to the current string except that all instances of 'oldValue' are replaced with 'newValue'.</returns>
		public static string Replace(this string s, string oldValue, string newValue)
		{
			// TODO - improve this string.Replace extension
			string pattern = string.Concat("(", oldValue, "?)");
			Regex regex = new Regex(pattern);
			return regex.Replace(s, newValue);
		}

		#endregion


		#region Member Fields

		private static string hexValues = "0123456789ABCDEF";

		#endregion
	}
}
