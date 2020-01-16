////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
//
// Copied from codeplex by JASDev International.
// Modified to generate Base64 output using RFC4648.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;

namespace JDI.Common.Utils
{
	public static class ConvertEx
	{
		/// <summary>
		/// Converts a byte-array to its equivalent string representation encoded with base 64 digits, based on RFC4648.
		/// </summary>
		/// <param name="inArray">An array of 8-bit unsigned integers. </param>
		/// <returns>The String representation, in base 64, of the contents of inArray.</returns>
		public static string ToBase64String(byte[] inArray)
		{
			return ToBase64String(inArray, 0, inArray.Length);
		}

		public static string ToBase64String(byte[] inArray, int offset, int length)
		{
			if (inArray == null)
			{
				throw new ArgumentNullException();
			}

			if (length == 0) return "";

			if (offset + length > inArray.Length) throw new ArgumentOutOfRangeException();

			// Create array of characters with appropriate length.
			int inArrayLen = length;
			int outArrayLen = GetBase64EncodedLength(inArrayLen);
			char[] outArray = new char[outArrayLen];

			/* encoding starts from end of string */

			/*
			** Convert the input buffer bytes through the encoding table and
			** out into the output buffer.
			*/
			int iInputEnd = offset + (outArrayLen / CCH_B64_IN_QUARTET - 1) * CB_B64_OUT_TRIO;
			int iInput = offset, iOutput = 0;
			byte uc0 = 0, uc1 = 0, uc2 = 0;
			// Loop is for all trios except of last one.
			for (; iInput < iInputEnd; iInput += CB_B64_OUT_TRIO, iOutput += CCH_B64_IN_QUARTET)
			{
				uc0 = inArray[iInput];
				uc1 = inArray[iInput + 1];
				uc2 = inArray[iInput + 2];
				// Writes data to output character array.
				outArray[iOutput] = s_rgchBase64EncodingRFC4648[uc0 >> 2];
				outArray[iOutput + 1] = s_rgchBase64EncodingRFC4648[((uc0 << 4) & 0x30) | ((uc1 >> 4) & 0xf)];
				outArray[iOutput + 2] = s_rgchBase64EncodingRFC4648[((uc1 << 2) & 0x3c) | ((uc2 >> 6) & 0x3)];
				outArray[iOutput + 3] = s_rgchBase64EncodingRFC4648[uc2 & 0x3f];
			}

			// Now we process the last trio of bytes. This trio might be incomplete and thus require special handling.
			// This code could be incorporated into main "for" loop, but hte code would be slower becuase of extra 2 "if"
			uc0 = inArray[iInput];
			uc1 = ((iInput + 1) < (offset + inArrayLen)) ? inArray[iInput + 1] : (byte)0;
			uc2 = ((iInput + 2) < (offset + inArrayLen)) ? inArray[iInput + 2] : (byte)0;
			// Writes data to output character array.
			outArray[iOutput] = s_rgchBase64EncodingRFC4648[uc0 >> 2];
			outArray[iOutput + 1] = s_rgchBase64EncodingRFC4648[((uc0 << 4) & 0x30) | ((uc1 >> 4) & 0xf)];
			outArray[iOutput + 2] = s_rgchBase64EncodingRFC4648[((uc1 << 2) & 0x3c) | ((uc2 >> 6) & 0x3)];
			outArray[iOutput + 3] = s_rgchBase64EncodingRFC4648[uc2 & 0x3f];

			switch (inArrayLen % CB_B64_OUT_TRIO)
			{
				/*
				** One byte out of three, add padding and fall through
				*/
				case 1:
					outArray[outArrayLen - 2] = '=';
					goto case 2;
				/*
				** Two bytes out of three, add padding.
				*/
				case 2:
					outArray[outArrayLen - 1] = '=';
					break;
			}

			// Creates string out of character array and return it.
			return new string(outArray);
		}

		private const int CCH_B64_IN_QUARTET = 4;
		private const int CB_B64_OUT_TRIO = 3;

		static private int GetBase64EncodedLength(int binaryLen)
		{
			return (((binaryLen / 3) + (((binaryLen % 3) != 0) ? 1 : 0)) * 4);
		}

		private static char[] s_rgchBase64EncodingRFC4648 = new char[]
        {
            'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', /* 12 */
            'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', /* 24 */
            'Y', 'Z', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', /* 36 */
            'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', /* 48 */
            'w', 'x', 'y', 'z', '0', '1', '2', '3', '4', '5', '6', '7', /* 60 */
            '8', '9', '+', '/'            /* 64 */
        };

	}
}
