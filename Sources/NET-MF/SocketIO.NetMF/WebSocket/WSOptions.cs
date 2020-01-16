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

namespace JDI.WebSocket.Client
{
	public class WSOptions
	{
		public WSOptions()
		{
			this.Origin = WSConst.Origin;
			this.SubProtocol = "";
			this.Extensions = "";
			this.MaskingEnabled = WSConst.MaskingEnabled;
			this.MaxReceiveFrameLength = WSConst.MaxReceiveFrameLength;
			this.MaxSendQueueSize = WSConst.MaxSendQueueSize;
			this.ActivityTimerEnabled = WSConst.ActivityTimerEnabled;

			this.ActivityTimeout = WSConst.WaitActivityTimeout;
			this.HandshakeTimeout = WSConst.WaitHandshakeTimeout;
			this.ReceiveTimeout = WSConst.WaitReceiveTimeout;
			this.CloseMsgTimeout = WSConst.WaitCloseMsgTimeout;
			this.PingRespTimeout = WSConst.WaitPingRespTimeout;
		}

		public string Origin { get; set; }
		public string SubProtocol { get; set; }
		public string Extensions { get; set; }
		public bool MaskingEnabled { get; set; }
		public int MaxReceiveFrameLength { get; set; }
		public int MaxSendQueueSize { get; set; }
		public bool ActivityTimerEnabled { get; set; }

		public int ActivityTimeout { get; set; }
		public int HandshakeTimeout { get; set; }
		public int ReceiveTimeout { get; set; }
		public int CloseMsgTimeout { get; set; }
		public int PingRespTimeout { get; set; }
	}
}
