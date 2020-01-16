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

using JDI.Common.Extensions;

namespace JDI.Common.Utils
{
	public static class ArrayUtil
	{
		/// <summary>
		/// Fills an array with a 0x00 byte value
		/// </summary>
		/// <param name="array">Byte array, may be null or empty</param>
		/// <param name="offset">Start index</param>
		/// <param name="count">Number of bytes to fill</param>
		public static void Fill(byte[] array, int offset, int count)
		{
			if (array == null || array.Length == 0 || array.Length < (offset + count))
				return;

			for (int i = offset; i < (offset + count); i++)
			{
				array[i] = 0x00;
			}
		}

		/// <summary>
		/// Fills an array with specified byte value
		/// </summary>
		/// <param name="array">Byte array, may be null or empty</param>
		/// <param name="offset">Start index</param>
		/// <param name="count">Number of bytes to fill</param>
		/// <param name="fillByte">Byte value to fill</param>
		public static void Fill(byte[] array, int offset, int count, byte fillByte)
		{
			if (array == null || array.Length == 0 || array.Length < (offset + count))
			{
				return;
			}

			for (int i = offset; i < (offset + count); i++)
			{
				array[i] = fillByte;
			}
		}

		/// <summary>
		/// Concatenates one or more byte arrays into a single array
		/// </summary>
		/// <param name="arrays">Comma-delimited list of byte arrays</param>
		/// <returns>All arrays concatenated into a single array</returns>
		public static byte[] Concat(params byte[][] arrays)
		{
			int destLength = 0;
			foreach (byte[] array in arrays)
			{
				destLength += array.Length;
			}

			byte[] destArray = new byte[destLength];

			if (destLength > 0)
			{
				int currentPos = 0;
				foreach (byte[] array in arrays)
				{
					Array.Copy(array, 0, destArray, currentPos, array.Length);
					currentPos += array.Length;
				}
			}

			return destArray;
		}

		/// <summary>
		/// Concatenates one or more byte, byte array, string, integer, or unsigned-integer objects into a single array
		/// </summary>
		/// <param name="list">Comma-delimited list of objects</param>
		/// <returns>All objects converted to byte arrays and concatenated into a single array</returns>
		public static byte[] Concat(params object[] list)
		{
			int objLength = 0;
			int destLength = 0;
			byte[] tempArray;
			foreach (object obj in list)
			{
				if (obj == null)
				{
					objLength = 0;
				}
				else if (obj is byte[])
				{
					objLength = ((byte[])obj).Length;
				}
				else if (obj is string)
				{
					objLength = ((string)obj).Length;
				}
				else if (obj is byte)
				{
					objLength = 1;
				}
				else if (obj is short || obj is Int16 || obj is UInt16)
				{
					objLength = 2;
				}
				else if (obj is int || obj is uint || obj is Int32 || obj is UInt32)
				{
					objLength = 4;
				}
				else
				{
					throw new ArgumentException("Parameters must be of type 'byte', 'byte[]', 'short', 'uint16', 'int', or 'uint'");
				}

				destLength += objLength;
			}

			byte[] destArray = new byte[destLength];

			if (destLength > 0)
			{
				int currentPos = 0;
				foreach (object obj in list)
				{
					if (obj == null)
					{
						objLength = 0;
					}
					else if (obj is byte[])
					{
						objLength = ((byte[])obj).Length;
						Array.Copy((byte[])obj, 0, destArray, currentPos, objLength);
					}
					else if (obj is string)
					{
						objLength = ((string)obj).Length;
						Array.Copy(Encoding.UTF8.GetBytes((string)obj), 0, destArray, currentPos, objLength);
					}
					else if (obj is byte)
					{
						objLength = 1;
						destArray[currentPos] = (byte)obj;
					}
					else if (obj is short || obj is Int16)
					{
						objLength = 2;
						tempArray = ((Int16)obj).ToBytes();
						Array.Copy(tempArray, 0, destArray, currentPos, objLength);
					}
					else if (obj is UInt16)
					{
						objLength = 2;
						tempArray = ((UInt16)obj).ToBytes();
						Array.Copy(tempArray, 0, destArray, currentPos, objLength);
					}
					else if (obj is int || obj is Int32)
					{
						objLength = 4;
						tempArray = ((Int32)obj).ToBytes(); 
						Array.Copy(tempArray, 0, destArray, currentPos, objLength);
					}
					else if (obj is uint || obj is UInt32)
					{
						objLength = 4;
						tempArray = ((UInt32)obj).ToBytes();
						Array.Copy(tempArray, 0, destArray, currentPos, objLength);
					}

					currentPos += objLength;
				}
			}

			return destArray;
		}

	}
}
