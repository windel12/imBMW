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

using JDI.Common;

namespace JDI.WebSocket.Client
{
	internal static class WSConst
	{
		public const string SchemeWS = "ws";
		public const string SchemeWSS = "wss";

		public const string HeaderEOL = "\r\n";
		public const string HeaderEOF = "\r\n\r\n";
		public const string HeaderSecurityGUID = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

		public const string Origin = "jdi-websocket";
		public const string ProtocolVersion = "13";

		public const int SendTimeout = 5000;		// timeout in milliseconds
		public const int ReceiveTimeout = 5000;		// timeout in milliseconds

		public const int WaitHandshakeTimeout = 5 * JDIConst.MicrosecondsPerSecond;
		public const int WaitCloseMsgTimeout = 5 * JDIConst.MicrosecondsPerSecond;
		public const int WaitReceiveTimeout = 1 * JDIConst.MicrosecondsPerSecond;
		public const int WaitActivityTimeout = 120;
		public const int WaitPingRespTimeout = 30;

		public const bool ActivityTimerEnabled = true;
		public const bool MaskingEnabled = true;

		public const int MaxReceiveFrameLength = 1024;
		public const int MaxSendQueueSize = 5;

		public enum CloseStatusCode
		{
			Normal = 1000,
			GoingAway = 1001,
			ProtocolError = 1002,
			DataTypeUnacceptable = 1003,
			//	Reserverd = 1004,
			NoStatusCode = 1005,
			NoCloseFrameReceived = 1006,
			DataTypeError = 1007,
			PolicyError = 1008,
			DataTooLarge = 1009,
			ExtensionNotSupported = 1010,
			UnexpectedError = 1011,
			NoTLSHandshake = 1015
		}

	}
}
