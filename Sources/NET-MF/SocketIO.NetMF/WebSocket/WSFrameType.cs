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

namespace JDI.WebSocket.Client
{
	public enum WSFrameType : byte
	{
		Continuation = 0x00,
		Text = 0x01,
		Binary = 0x02,
		Reserved = 0x03,
		Reserved04 = 0x04,
		Reserved05 = 0x05,
		Reserved06 = 0x06,
		Reserved07 = 0x07,
		Close = 0x08,
		Ping = 0x09,
		Pong = 0x0A,
		Reserved0B = 0x0B,
		Reserved0C = 0x0C,
		Reserved0D = 0x0D,
		Reserved0E = 0x0E,
		Reserved0F = 0x0F,
		Undefined = 0xFF
	}
}
