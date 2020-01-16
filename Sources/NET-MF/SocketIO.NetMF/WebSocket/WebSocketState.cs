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
	public enum WebSocketState
	{
		Initialized = 0, // initial state
		Connecting,
		Connected,
		Disconnecting,
		Disconnected
	}


	public static class WebSocketStateExtensions
	{
		public static string Name(this WebSocketState webSocketState)
		{
			string text = "";
			switch (webSocketState)
			{
				case WebSocketState.Initialized:
					text = "Initialized";
					break;
				case WebSocketState.Connecting:
					text = "Connecting";
					break;
				case WebSocketState.Connected:
					text = "Connected";
					break;
				case WebSocketState.Disconnecting:
					text = "Disconnecting";
					break;
				case WebSocketState.Disconnected:
					text = "Disconnected";
					break;
			}
			return text;
		}
	}
}
