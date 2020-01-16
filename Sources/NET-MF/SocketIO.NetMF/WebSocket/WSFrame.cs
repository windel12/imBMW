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

using JDI.Common;
using JDI.Common.Security;
using JDI.Common.Extensions;
using JDI.Common.Logger;
using JDI.Common.Utils;

namespace JDI.WebSocket.Client
{
	internal class WSFrame : IDisposable
	{
		#region Constructors

		protected WSFrame()
		{
			this.frameData = null;
			this.maskIndex = 0;
			this.payloadLength = 0;
			this.payloadIndex = 0;
		}

		public void Dispose()
		{
			this.frameData = null;
		}

		#endregion


		#region Properties

		/// <summary>
		/// Gets the Final bit.
		/// </summary>
		public bool FIN
		{
			get { return ((this.frameData[0] & 0x80) == 0x80); }
		}

		/// <summary>
		/// Gets the Reserved bits.
		/// </summary>
		public byte RSV
		{
			get { return (byte)((this.frameData[0] >> 4) & 0x07); }
		}

		/// <summary>
		/// Gets the OpCode.
		/// </summary>
		public WSFrameType OpCode
		{
			get { return (WSFrameType)(this.frameData[0] & 0x0F); }
		}

		/// <summary>
		/// Gets a boolean indicating the state of the Mask bit.
		/// </summary>
		public bool IsMasked
		{
			get { return ((this.frameData[1] & 0x80) == 0x80); }
		}

		/// <summary>
		/// Gets the mask start index.
		/// </summary>
		public int MaskIndex
		{
			get { return this.maskIndex; }
		}

		/// <summary>
		/// Gets the mask data.
		/// </summary>
		public byte[] Mask
		{
			get { return (this.maskIndex > 0 ? this.frameData.SubArray(this.maskIndex, 4) : null); }
		}

		/// <summary>
		/// Gets the payload start index.
		/// </summary>
		public int PayloadIndex
		{
			get { return this.payloadIndex; }
		}

		/// <summary>
		/// Gets the payload length.
		/// </summary>
		public int PayloadLength
		{
			get { return this.payloadLength; }
		}

		/// <summary>
		/// Gets the payload as bytes.
		/// </summary>
		public byte[] PayloadBytes
		{
			get { return (this.payloadLength > 0 ? this.frameData.SubArray(this.payloadIndex, this.payloadLength) : null); }
		}

		/// <summary>
		/// Gets the payload as text.
		/// </summary>
		public string PayloadText
		{
			get { return (this.payloadLength > 0 ? this.frameData.ToString(this.payloadIndex, this.payloadLength) : null); }
		}

		/// <summary>
		/// Gets the full frame data.
		/// </summary>
		public byte[] FrameData
		{
			get { return this.frameData; }
		}

		#endregion


		#region Static Methods

		/// <summary>
		/// Creates a BFP frame with the opcode specified, but no payload.
		/// </summary>
		/// <param name="opCode">BFP opcode.</param>
		/// <returns></returns>
		public static WSFrame CreateFrame(WSFrameType opCode)
		{
			return CreateFrame(opCode, false, (byte[])null);
		}

		/// <summary>
		/// Creates a BFP frame with the opcode and payload string specified.
		/// </summary>
		/// <param name="opCode">BFP opcode.</param>
		/// <param name="isMasked">Boolean indicating whether to mask the payload.</param>
		/// <param name="payLoad">Payload string.</param>
		/// <returns></returns>
		public static WSFrame CreateFrame(WSFrameType opCode, bool isMasked, string payLoad)
		{
			return CreateFrame(opCode, isMasked, Encoding.UTF8.GetBytes(payLoad));
		}

		/// <summary>
		/// Creates an RFC 6455 BFP frame with the opcode and payload bytes specified.
		/// </summary>
		/// <param name="opCode">BFP opcode.</param>
		/// <param name="isMasked">Boolean indicating whether to mask the payload.</param>
		/// <param name="payLoad">Payload bytes.</param>
		/// <returns></returns>
		public static WSFrame CreateFrame(WSFrameType opCode, bool isMasked, byte[] payLoad)
		{
			// TODO - add code to support continuation frames
			// - add long maxFrameLength parameter
			// - return array of WSFrame objects

			byte controlByte0 = (byte)(0x80 | (byte)opCode);
			byte controlByte1 = (isMasked && payLoad != null && payLoad.Length > 0 ? (byte)0x80 : (byte)0x00);
			byte[] mask = (isMasked ? CryptoUtils.GetRandomBytes(4) : new byte[0]);
			int maskIndex = 0;
			Int32 payloadIndex = 0;
			UInt16 length16 = 0;
			UInt32 length32 = 0;
			UInt64 length64 = 0;
			byte[] frameData = null;

			if (payLoad == null || payLoad.Length == 0)
			{
				length32 = 0;
				payloadIndex = 0;
				frameData = ArrayUtil.Concat(controlByte0, controlByte1);
			}
			else if (payLoad.Length <= 125)
			{
				controlByte1 = (byte)(controlByte1 | (byte)payLoad.Length);
				length32 = (UInt32)payLoad.Length;
				maskIndex = (isMasked ? 2 : 0);
				payloadIndex = (isMasked ? 6 : 2);
				frameData = ArrayUtil.Concat(controlByte0, controlByte1, mask, payLoad);
			}
			else if (payLoad.Length <= UInt16.MaxValue)
			{
				controlByte1 = (byte)(controlByte1 | (byte)126);
				length16 = (UInt16)payLoad.Length;
				length32 = length16;
				maskIndex = (isMasked ? 4 : 0);
				payloadIndex = (isMasked ? 8 : 4);
				frameData = ArrayUtil.Concat(controlByte0, controlByte1, length16, mask, payLoad);
			}
			else
			{
				controlByte1 = (byte)(controlByte1 | (byte)127);
				length32 = (UInt32)payLoad.Length;
				length64 = (UInt64)payLoad.Length;
				maskIndex = (isMasked ? 10 : 0);
				payloadIndex = (isMasked ? 14 : 10);
				frameData = ArrayUtil.Concat(controlByte0, controlByte1, length64, mask, payLoad);
			}

			WSFrame dataFrame = new WSFrame()
			{
				maskIndex = maskIndex,
				payloadIndex = payloadIndex,
				payloadLength = (Int32)length32,
				frameData = frameData
			};

			if (isMasked)
			{
			//	Logger.WriteDebug("wsframe", string.Concat("raw: ", frameData.ToHex()));
				ApplyMask(dataFrame);
			}

			return dataFrame;
		}

		/// <summary>
		/// Parses a byte-array at startPos to its equivalent RFC 6455 BFP frame. The return value indicates whether the operation succeeded.
		/// </summary>
		/// <param name="dataBuffer">A source byte-array containing the bytes to convert.</param>
		/// <param name="startPos">Start position in the source byte-array.</param>
		/// <param name="length">Number of bytes to parse.</param>
		/// <param name="maxFrameLength">Maximum frame size allowed.</param>
		/// <param name="dataFrame">The resulting BFP frame or null.</param>
		/// <returns>true if the source byte-array was converted successfully; otherwise, false</returns>
		public static bool TryParse(byte[] dataBuffer, int startPos, int length, int maxFrameLength, out WSFrame dataFrame)
		{
			dataFrame = null;

			if (dataBuffer == null || dataBuffer.Length == 0 || dataBuffer.Length < (startPos + length))
				return false;

			byte controlByte0 = dataBuffer[startPos];
			byte controlByte1 = dataBuffer[startPos + 1];
			byte length8 = (byte)(controlByte1 & 0x7F);
			bool isMasked = ((controlByte1 & 0x80) == 0x80);
			int maskIndex = 0;
			byte[] mask = new byte[4];

			// get payload index and length
			Int32 payloadIndex = 0;
			UInt64 payloadLength64 = 0;
			if (length8 == 127)
			{
				maskIndex = (isMasked ? 10 : 0);
				payloadIndex = (isMasked ? 14 : 10);
				payloadLength64 = dataBuffer.ToUInt64(startPos + 2);
			}
			else if (length8 == 126)
			{
				maskIndex = (isMasked ? 4 : 0);
				payloadIndex = (isMasked ? 8 : 4);
				payloadLength64 = dataBuffer.ToUInt16(startPos + 2);
			}
			else // must be <= 125
			{
				maskIndex = (isMasked ? 2 : 0);
				payloadIndex = (isMasked ? 6 : 2);
				payloadLength64 = length8;
			}

			// calculate frame length
			Int32 frameLength = payloadIndex + (Int32)payloadLength64;

			// check for max allowed length
			if (frameLength > maxFrameLength)
			{
				throw new NotSupportedException(string.Concat("Maximum frame size of ", maxFrameLength, " bytes has been exceeded."));
			}

			// create WSFrame if full frame has been received
			if (frameLength <= length)
			{
				dataFrame = new WSFrame()
				{
					maskIndex = maskIndex,
					payloadIndex = payloadIndex,
					payloadLength = (Int32)payloadLength64,
					frameData = new byte[frameLength]
				};

				Array.Copy(dataBuffer, startPos, dataFrame.frameData, 0, frameLength);

				if (isMasked)
				{
					ApplyMask(dataFrame);
				}

				return true;
			}

			return false;
		}

		public static void ApplyMask(WSFrame wsFrame)
		{
			if (wsFrame != null && wsFrame.payloadLength > 0)
			{
				int maskIndex = 0;
				for (int i = 0; i < wsFrame.payloadLength; i++)
				{
					maskIndex = wsFrame.maskIndex + (i % 4);
					wsFrame.frameData[wsFrame.payloadIndex + i] = (byte)(wsFrame.frameData[wsFrame.payloadIndex + i] ^ wsFrame.frameData[maskIndex]);
				}
			}
		}

		#endregion


		#region Member Fields

		protected int maskIndex;
		protected int payloadIndex;
		protected int payloadLength;
		protected byte[] frameData;

		#endregion

	}
}
