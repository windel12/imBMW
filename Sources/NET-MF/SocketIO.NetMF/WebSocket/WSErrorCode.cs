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
	public enum WSErrorCode
	{
		Success,						// Indicates that there was no native error information for the exception. 
		InvalidMessageType,				// Indicates that a WebSocket frame with an unknown opcode was received. 
		Faulted,						// Indicates a general error. 
		NativeError,					// Indicates that an unknown native error occurred. 
		NotAWebSocket,					// Indicates that the incoming request was not a valid websocket request. 
		UnsupportedVersion,				// Indicates that the client requested an unsupported version of the WebSocket protocol. 
		UnsupportedProtocol,			// Indicates that the client requested an unsupported WebSocket subprotocol. 
		HeaderError,					// Indicates an error occurred when parsing the HTTP headers during the opening handshake. 
		ConnectionClosedPrematurely,	// Indicates that the connection was terminated unexpectedly. 
		InvalidState					// Indicates the WebSocket is an invalid state for the given operation (such as being closed or aborted). 
	}
}
