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
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;


using JDI.Common;
using JDI.Common.Extensions;
using JDI.Common.Logger;
using JDI.Common.Net;
using JDI.Common.Security;
using JDI.Common.Utils;


namespace JDI.WebSocket.Client
{
	public class WebSocketClient : IDisposable
	{
		#region Constructors and IDisposable

		/// <summary>
		/// Provides a client for connecting to WebSocket services.
		/// </summary>
		/// <param name="logSourceID">Identifier to be used when calling the Logger service.</param>
		/// <param name="options">Collection of WebSocket options.</param>
		public WebSocketClient(string logSourceID, WSOptions options = null)
		{
			this.loggerID = logSourceID;

			this.options = options;
			if (this.options == null)
				this.options = new WSOptions();

			this.origin = this.options.Origin;
			this.subProtocol = this.options.SubProtocol;
			this.extensions = this.options.Extensions;

			this.activityTimerEnabled = this.options.ActivityTimerEnabled;
			this.sendFramesMasked = this.options.MaskingEnabled;

			this.waitHandshakeTimeout = this.options.HandshakeTimeout;
			this.waitCloseMsgTimeout = this.options.CloseMsgTimeout;
			this.waitReceiveTimeout = this.options.ReceiveTimeout;
			this.waitActivityTimeout = this.options.ActivityTimeout;
			this.waitPingRespTimeout = this.options.PingRespTimeout;

			this.activityTimer = new TimerEx();

			this.state = WebSocketState.Initialized;

			this.eventLock = new object();
			this.sendLock = new object();
			this.isDisposed = false;

			this.receiveBuffer = new byte[this.options.MaxReceiveFrameLength];
			this.lastRcvdFrame = null;

			this.sendQueue = new WSFrameQueue(this.options.MaxSendQueueSize);

			// start state machine
			try
			{
				this.runThreadLoop = true;
				this.runStateMachine = new AutoResetEvent(false);
				this.stateMachineThread = new Thread(new ThreadStart(this.WSStateMachine));
				this.stateMachineThread.Start();
			}
			catch (Exception ex)
			{
				Logger.WriteError(this.loggerID, ex.Message, ex.StackTrace);
			}
		}

		~WebSocketClient()
		{
			Dispose( false );
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (this.isDisposed)
				return;

			if (disposing)
			{
				// disconnect
				this.Disconnect();
				DateTime timeoutTime = DateTime.Now.AddSeconds(20);
				while (this.state != WebSocketState.Disconnected && DateTime.Now < timeoutTime)
					Thread.Sleep(500);

				// kill thread
				this.runThreadLoop = false;
				this.runStateMachine.Set();
				this.stateMachineThread.Join(1000);

				// clear the message queue
				if (this.sendQueue != null)
					this.sendQueue.Clear();

				// kill timer
				if (this.activityTimer != null)
					this.activityTimer.Dispose();

				// cleanup streams
				if (this.socketStream != null)
					this.socketStream.Close();

				// cleanup sockets
				if (this.socket != null)
					this.socket.Close();
			}

			// set everything to null
			this.eventLock = null;
			this.sendLock = null;
			this.activityTimer = null;
			this.sendQueue = null;
			this.socket = null;
			this.socketStream = null;
			this.receiveBuffer = null;
			this.lastRcvdFrame = null;
			this.serverUri = null;
			this.loggerID = null;
			this.origin = null;
			this.subProtocol = null;
			this.extensions = null;
			this.securityKey = null;
			this.options = null;

			// make sure we don't dispose again
			this.isDisposed = true;
		}

		#endregion


		#region Properties

		/// <summary>
		/// Get the log source ID.
		/// </summary>
		public string LoggerID
		{
			get { return this.loggerID; }
		}

		/// <summary>
		/// Get the header Origin value.
		/// </summary>
		public string Origin
		{
			get { return this.origin; }
		}

		/// <summary>
		/// Gets the negotiated websocket sub-protocol.
		/// </summary>
		public string SubProtocol
		{
			get
			{
				if (this.State == WebSocketState.Connected)
					return this.subProtocol;
				return "";
			}
		}

		/// <summary>
		/// Gets a comma-delimited list of the negotiated websocket extensions.
		/// </summary>
		public string Extensions
		{
			get
			{
				if (this.State == WebSocketState.Connected)
					return this.extensions;
				return "";
			}
		}

		/// <summary>
		/// Gets the websocket protocol version in use.
		/// </summary>
		public string Version
		{
			get { return WSConst.ProtocolVersion; }
		}

		/// <summary>
		/// Get the current state of the websocket client.
		/// </summary>
		public WebSocketState State
		{
			get { return this.state; }
		}

		#endregion


		#region Events

		/// <summary>
		/// Event that fires when the WebSocket connection state has changed.
		/// </summary>
		public event WSDelegates.ConnectionChangedEventHandler ConnectionChanged;

		/// <summary>
		/// Event that fires when a text frame is received from the server.
		/// </summary>
		public event WSDelegates.TextMessageReceivedEventHandler TextMessageReceived;

		/// <summary>
		/// Event that fires when a data message is received from the server.
		/// </summary>
		public event WSDelegates.DataMessageReceivedEventHandler DataMessageReceived;

		/// <summary>
		/// Event that fires when an error occurs.
		/// </summary>
		public event WSDelegates.ErrorEventHandler Error;

		#endregion


		#region Event Invoke Methods

		protected void OnConnectionStateChanged()
		{
			WSDelegates.ConnectionChangedEventHandler handler = null;
			lock (this.eventLock)
			{
				handler = this.ConnectionChanged;
			}
			if (handler != null)
			{
				handler(this.state);
			}
		}

		protected void OnTextReceived(string payload)
		{
			WSDelegates.TextMessageReceivedEventHandler handler = null;
			lock (this.eventLock)
			{
				handler = this.TextMessageReceived;
			}
			if (handler != null)
			{
				handler(payload);
			}
		}

		protected void OnDataReceived(byte[] payload)
		{
			WSDelegates.DataMessageReceivedEventHandler handler = null;
			lock (this.eventLock)
			{
				handler = this.DataMessageReceived;
			}
			if (handler != null)
			{
				handler(payload);
			}
		}

		protected void OnError(WSErrorCode errorCode, string message, string stackTrace = null)
		{
			Logger.WriteError(this.loggerID, string.Concat("Error: (", errorCode.ToString(), ")", message), stackTrace);
			WSDelegates.ErrorEventHandler handler = null;
			lock (this.eventLock)
			{
				handler = this.Error;
			}
			if (handler != null)
			{
				handler(message, stackTrace);
			}
		}

		#endregion


		#region Methods

		/// <summary>
		/// Connect - Begins the asynchronous (non-blocking) connection process.
		/// </summary>
		/// <param name="serverUrl">Url of server to connect to.</param>
		public void Connect(string serverUrl)
		{
			if (this.State == WebSocketState.Initialized || this.State == WebSocketState.Disconnected)
			{
				try
				{
					this.serverUri = new UriEx(serverUrl);

					IPHostEntry hostEntry = Dns.GetHostEntry(this.serverUri.Host);
					IPAddress ipAddress = hostEntry.AddressList[0];
					this.serverEndpoint = new IPEndPoint(ipAddress, this.serverUri.Port);

					// start the connection process
					Logger.WriteDebug(this.loggerID, "Connecting to " + serverUrl + " ...");
					this.state = WebSocketState.Connecting;
					this.subState = SubState.OpenTcpConnection;
					this.runStateMachine.Set();
				}
				catch (Exception ex)
				{
					this.OnError(WSErrorCode.NativeError, ex.Message, ex.StackTrace);
				}
			}
		}

		/// <summary>
		/// Disconnect - Begins the asynchronous (non-blocking) disconnection process.
		/// </summary>
		public void Disconnect()
		{
			this.Disconnect((int)WSConst.CloseStatusCode.Normal, "Bye!");
		}

		/// <summary>
		/// Disconnect - Begins the asynchronous (non-blocking) disconnection process.
		/// </summary>
		/// <param name="statusCode">WebSocket close status code (from RFC6455)</param>
		/// <param name="reason">Optional reason text.</param>
		public void Disconnect(UInt16 statusCode, string reason = null)
		{
			if (this.state == WebSocketState.Initialized)
			{
				this.state = WebSocketState.Disconnected;
				this.subState = SubState.Disconnected;
			}
			else if (this.state == WebSocketState.Connecting || this.state == WebSocketState.Connected)
			{
				Logger.WriteDebug(this.loggerID, "Disconnecting...");
				this.closeStatus = statusCode;
				this.closeReason = reason;
				this.state = WebSocketState.Disconnecting;
				this.subState = SubState.SendCloseFrame;
			}
		}

		/// <summary>
		/// SendText - Begins the asynchronous (non-blocking) send process.
		/// </summary>
		/// <param name="text">Text data to send.</param>
		public void SendText(string text)
		{
			if (this.state == WebSocketState.Connected)
			{
				this.EnqueueMessage(WSFrameType.Text, this.options.MaskingEnabled, text);
			}
		}

		/// <summary>
		/// SendData - Begins the asynchronous (non-blocking) send process.
		/// </summary>
		/// <param name="data">Binary data to send.</param>
		public void SendData(byte[] data)
		{
			if (this.state == WebSocketState.Connected)
			{
				this.EnqueueMessage(WSFrameType.Binary, this.options.MaskingEnabled, data);
			}
		}

		#endregion


		#region State Machine Methods

		#region The Main State Machine

		protected void WSStateMachine()
		{
			while (this.runThreadLoop)
			{
				try
				{
					switch (this.state)
					{
						case WebSocketState.Initialized:
							// idle
							this.runStateMachine.WaitOne();	// wait to be signalled
							break;
						case WebSocketState.Connecting:
							// attempt to connect to server
							this.OnConnectionStateChanged();
							this.smConnect();
							break;
						case WebSocketState.Connected:
							// receive and parse incoming data
							this.OnConnectionStateChanged();
							this.smReceive();
							break;
						case WebSocketState.Disconnecting:
							// disconnect from server
							this.OnConnectionStateChanged();
							this.smDisconnect();
							break;
						case WebSocketState.Disconnected:
							// idle
							this.OnConnectionStateChanged();
							this.runStateMachine.WaitOne();	// wait to be signalled
							break;
						default:
							// shouldn't get here
							throw new NotSupportedException("ReadyState " + this.State.ToString() + " is invalid.");
					}
				}
				catch (Exception ex)
				{
					if (this.socketStream != null)
					{
						this.socketStream.Close();
						this.socketStream = null;
					}
					if (this.socket != null)
					{
						this.socket.Close();
						this.socket = null;
					}
					this.OnError(WSErrorCode.NativeError, ex.Message, ex.StackTrace);
					this.state = WebSocketState.Disconnected;
					this.subState = SubState.Disconnected;
				}
			}
		}

		#endregion

		#region smConnect methods

		protected void smConnect()
		{
			while (this.state == WebSocketState.Connecting)
			{
				switch (this.subState)
				{
					case SubState.OpenTcpConnection:
						// open tcp connection
						this.smOpenTcpConnection();
						break;
					case SubState.SendHandshake:
						// send handshake
						this.smSendHandshake();
						break;
					case SubState.WaitForHandshake:
						// wait for handshake response
						this.smWaitForHandshake();
						break;
					case SubState.ProcessHandshake:
						// process handshake response
						this.smProcessHandshake();
						break;
					case SubState.Connected:
						// connection successful
						this.smConnected();
						return;
					case SubState.Failed:
						// connection failed
						this.smConnectFailed();
						return;
				}
			}
		}

		protected void smOpenTcpConnection()
		{
			// create and init socket
			this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			this.socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
			this.socket.SendTimeout = WSConst.SendTimeout;
			this.socket.ReceiveTimeout = WSConst.ReceiveTimeout;

			// connect to server
			this.socket.Connect(this.serverEndpoint);
			
			// get data stream
			if (this.serverUri.Scheme == WSConst.SchemeWSS)
			{
				this.socketStream = new SslStreamEx(this.socket);
				((SslStreamEx)this.socketStream).AuthenticateAsClient(this.serverUri.Host);
			}
			else
			{
				this.socketStream = new NetworkStream(this.socket, true);
			}

			// go to next state
			this.subState = SubState.SendHandshake;
		}

		protected void smSendHandshake()
		{
			/* Implements RFC6455 websocket specification
			 * 
			 *  GET <target path> HTTP/1.1\r\n
			 *  Host: <target web address>\r\n
			 *  Upgrade: websocket\r\n
			 *  Connection: upgrade\r\n
			 *  Origin: <optional name of origin>\r\n
			 *  Sec-WebSocket-Version: 13\r\n
			 *  Sec-WebSocket-Extensions: <zero or more websocket extensions>\r\n
			 *  Sec-WebSocket-Protocol: <zero or more higher level protocols>\r\n
			 *  Sec-WebSocket-Key: <generated key>\r\n
			 *  \r\n
			 * 
			*/

			// create security key
			this.securityKey = this.GetSecurityKey();

			// create headers
			StringBuilder sb = new StringBuilder();
			sb.Append(string.Concat("GET ", this.serverUri.GetPathAndQuery(), " HTTP/1.1", WSConst.HeaderEOL));
			sb.Append(string.Concat("Host: ", this.serverUri.Host, WSConst.HeaderEOL));
			sb.Append(string.Concat("Upgrade: webSocket", WSConst.HeaderEOL));
			sb.Append(string.Concat("Connection: upgrade", WSConst.HeaderEOL));
			if (this.origin.Length > 0)
			{
				sb.Append(string.Concat("Origin: ", this.origin, WSConst.HeaderEOL));
			}
			if (this.extensions.Length > 0)
			{
				sb.Append(string.Concat("Sec-WebSocket-Extensions: ", this.extensions, WSConst.HeaderEOL));
			}
			if (this.subProtocol.Length > 0)
			{
				sb.Append(string.Concat("Sec-WebSocket-Protocol: ", this.subProtocol, WSConst.HeaderEOL));
			}
			sb.Append(string.Concat("Sec-WebSocket-Version: ", WSConst.ProtocolVersion, WSConst.HeaderEOL));
			sb.Append(string.Concat("Sec-WebSocket-Key: ", this.securityKey, WSConst.HeaderEOL));
			sb.Append(WSConst.HeaderEOL);
			string headers = sb.ToString();

			// send headers
			byte[] headerBytes = headers.ToByteArray();
			this.socketStream.Write(headerBytes, 0, headerBytes.Length);

			Logger.WriteDebug(this.loggerID, string.Concat("Handshake sent: ", headers));

			// go to next state
			this.subState = SubState.WaitForHandshake;
		}

		protected void smWaitForHandshake()
		{
			/* Implements RFC6455 websocket specification
			* 
			*  HTTP/1.1 101 Switching Protocols\r\n
			*  Upgrade: websocket\r\n
			*  Connection: upgrade\r\n
			*  Sec-WebSocket-Extensions: <zero or more websocket extensions>\r\n
			*  Sec-WebSocket-Protocol: <zero or more higher level protocols>\r\n
			*  Sec-WebSocket-Accept: <response key>\r\n
			*  <other headers, cookies, etc>
			*  \r\n
			* 
			*/

			int bytesRead = 0;
			this.bytesInBuffer = 0;
			this.posHeaderEOF = 0;
			this.bytesInBuffer = 0;

			// TODO - modify to extract individual headers as they arrive in the buffer
			if (this.socket.Poll(this.waitHandshakeTimeout, SelectMode.SelectRead) == true && this.socket.Available > 0)
			{
				do
				{
					bytesRead = this.socketStream.Read(this.receiveBuffer, this.bytesInBuffer, this.receiveBuffer.Length - this.bytesInBuffer);
					if (bytesRead > 0)
					{
						this.bytesInBuffer += bytesRead;
						this.posHeaderEOF = this.receiveBuffer.IndexOf(WSConst.HeaderEOF);
						if (posHeaderEOF >= 0)
						{
							this.LogBufferContent("Handshake received: ", this.receiveBuffer, 0, this.bytesInBuffer);
							this.subState = SubState.ProcessHandshake;
							return;
						}
					}
				}
				while (this.socket.Available > 0 && this.bytesInBuffer < this.receiveBuffer.Length);
			}

			Logger.WriteError(this.loggerID, "Handshake not received.");
			this.subState = SubState.Failed;
		}

		protected void smProcessHandshake()
		{
			/* Implements RFC6455 websocket specification
			 * 
			 *  HTTP/1.1 101 Switching Protocols\r\n
			 *  Upgrade: websocket\r\n
			 *  Connection: upgrade\r\n
			 *  Sec-WebSocket-Extensions: <zero or more websocket extensions>\r\n
			 *  Sec-WebSocket-Protocol: <zero or more higher level protocols>\r\n
			 *  Sec-WebSocket-Accept: <response key>\r\n
			 *  \r\n
			 * 
			*/

			// get response headers
			ArrayList headers = this.receiveBuffer.Split(WSConst.HeaderEOL, 0, this.posHeaderEOF + WSConst.HeaderEOF.Length, false);
		
		//  for debugging	
		//	this.socketBuffer = Encoding.UTF8.GetBytes("HTTP/1.1 101 Switching Protocols\r\nUpgrade: websocket\r\nConnection: upgrade\r\nSec-WebSocket-Extensions: ext1, ext3\r\nSec-WebSocket-Protocol: prot2!#%'-_@~$*+.^|\r\nSec-WebSocket-Accept: Kfh9QIsMVZcl6xEPYxPHzW8SZ8w=\r\n\r\n" + "\x81\x53{\"event\":\"pusher:connection_established\",\"data\":\"{\\\"socket_id\\\":\\\"23086.813011\\\"}\"}");
		//	this.bytesInBuffer = this.socketBuffer.Length;
		//	this.posHeaderEOF = this.socketBuffer.IndexOf(WSConstants.HeaderEOF);
		//	ArrayList headers = this.socketBuffer.Split(WSConstants.HeaderEOL, 0, this.posHeaderEOF + WSConstants.HeaderEOF.Length, false);

			// remove headers from socket buffer
			if (this.posHeaderEOF >= 0)
			{
				this.posHeaderEOF += WSConst.HeaderEOF.Length;
				Array.Copy(this.receiveBuffer, this.posHeaderEOF, this.receiveBuffer, 0, this.bytesInBuffer - this.posHeaderEOF);
				this.bytesInBuffer = this.bytesInBuffer - this.posHeaderEOF;
			}

			// log response headers
			if (Logger.GetLogLevel() == LogLevel.Debug)
			{
				for (int i = 0; i < headers.Count; i++)
				{
					Logger.WriteDebug(this.loggerID, string.Concat("Handshake header: ", headers[i]));
				}
			}

			// validate response headers
			Regex regex = null;
			string header = "";
			bool isDone = false;
			bool responseIsValid = false;
			int headerIndex = 0;
			int step = 0;
			while (!isDone)
			{
				Continue:
				step++;

				switch (step)
				{
					case 1:
						// check for response code 101
						for (headerIndex = 0; headerIndex < headers.Count; headerIndex++)
						{
							header = ((string)headers[headerIndex]);
							regex = new Regex(@"http/\d+\.\d+\s+101", RegexOptions.IgnoreCase);
							if (regex.IsMatch(header))
							{
								headers.RemoveAt(headerIndex);
								goto Continue;
							}
							else
							{
								// TODO - process other response codes per RFC2616
							}
						}
						break;
					case 2:
						// check for 'upgrade: websocket'
						for (headerIndex = 0; headerIndex < headers.Count; headerIndex++)
						{
							header = ((string)headers[headerIndex]);
							regex = new Regex(@"upgrade:\s+websocket", RegexOptions.IgnoreCase);
							if (regex.IsMatch(header))
							{
								headers.RemoveAt(headerIndex);
								goto Continue;
							}
						}
						break;
					case 3:
						// check for 'connection: upgrade'
						for (headerIndex = 0; headerIndex < headers.Count; headerIndex++)
						{
							header = ((string)headers[headerIndex]);
							regex = new Regex(@"connection:\s+upgrade", RegexOptions.IgnoreCase);
							if (regex.IsMatch(header))
							{
								headers.RemoveAt(headerIndex);
								goto Continue;
							}
						}
						break;
					case 4:
						// check that 'sec-websocket-extensions:' list is valid
						// TODO - validate extensions header
						goto Continue;
						/*
						if (this.extensionList.Length == 0)
						{
							goto Continue;
						}
						for (headerIndex = 0; headerIndex < headers.Count; headerIndex++)
						{
							header = ((string)headers[headerIndex]);
							regex = new Regex(@"(sec-websocket-extensions):\s+(.+)", RegexOptions.IgnoreCase);
							Match match = regex.Match(header);
							if (match.Success && this.IsValidExtensionsResponse(match.Groups[2].Value))
							{
								headers.RemoveAt(headerIndex);
								goto Continue;
							}
						}
						break;
						*/
					case 5:
						// check that 'sec-websocket-protocol:' list is valid
						if (this.subProtocol.Length == 0)
						{
							goto Continue;
						}
						for (headerIndex = 0; headerIndex < headers.Count; headerIndex++)
						{
							header = ((string)headers[headerIndex]);
							regex = new Regex(@"sec-websocket-protocol:\s+([A-Za-z0-9!#%'-_@~\$\*\+\.\^\|]+$)", RegexOptions.IgnoreCase);
							Match match = regex.Match(header);
							if (match.Success && this.IsValidProtocolResponse(match.Groups[1].Value))
							{
								headers.RemoveAt(headerIndex);
								goto Continue;
							}
						}
						break;
					case 6:
						// check that 'sec-websocket-accept: secKey' is valid
						for (headerIndex = 0; headerIndex < headers.Count; headerIndex++)
						{
							header = ((string)headers[headerIndex]);
							regex = new Regex(@"sec-websocket-accept:\s+([A-Za-z0-9+/=]+)", RegexOptions.IgnoreCase);
							Match match = regex.Match(header);
							if (match.Success && this.IsValidSecurityResponse(match.Groups[1].Value))
							{
								headers.RemoveAt(headerIndex);
								goto Continue;
							}
						}
						break;
					case 7:
						// TODO - check for cookie header
						goto Continue;
					case 8:
						// done
						responseIsValid = true;
						break;
				}

				// stop looping
				isDone = true;
			}

			// TODO - cleanup headers list
			headers.Clear();
			headers = null;

			if (!responseIsValid)
			{
				Logger.WriteError(this.loggerID, "Handshake response is invalid");
				this.subState = SubState.Failed;
				return;
			}

			// go to next state
			this.subState = SubState.Connected;
		}

		protected void smConnected()
		{
			Logger.WriteDebug(this.loggerID, "Connected");
			this.state = WebSocketState.Connected;
			this.subState = SubState.Connected;
		}

		protected void smConnectFailed()
		{
			if (this.socketStream != null)
			{
				this.socketStream.Close();
				this.socketStream = null;
			}
			if (this.socket != null)
			{
				this.socket.Close();
				this.socket = null;
			}
			this.state = WebSocketState.Disconnected;
			this.subState = SubState.Disconnected;
		}

		#endregion

		#region smReceive methods

		protected void smReceive()
		{
			int bytesRead = 0;
			this.lastRcvdFrame = null;

			// start activity timer
			if (this.activityTimerEnabled)
			{
				this.activityTimer.Start(this.waitActivityTimeout, ActivityTimerState.WaitingForMessage);
			}

			while (this.State == WebSocketState.Connected)
			{
				// process data in buffer
				if (this.bytesInBuffer > 0)
				{
					if (WSFrame.TryParse(this.receiveBuffer, 0, this.bytesInBuffer, this.options.MaxReceiveFrameLength, out this.lastRcvdFrame) == true)
					{
						// restart activity timer
						if (this.activityTimerEnabled)
						{
							this.activityTimer.Restart(ActivityTimerState.WaitingForMessage);
						}

						// remove data-frame from buffer
						Array.Copy(this.receiveBuffer, this.lastRcvdFrame.FrameData.Length, this.receiveBuffer, 0, this.bytesInBuffer - this.lastRcvdFrame.FrameData.Length);
						this.bytesInBuffer = this.bytesInBuffer - this.lastRcvdFrame.FrameData.Length;

						// take action based on opcode
						switch (this.lastRcvdFrame.OpCode)
						{
							case WSFrameType.Continuation:
								// TODO - implement continuation frames
								break;
							case WSFrameType.Text:
								this.LogBufferContent("Text frame received: ", this.lastRcvdFrame.FrameData, 0, this.lastRcvdFrame.FrameData.Length);
								this.OnTextReceived(this.lastRcvdFrame.PayloadText);
								break;
							case WSFrameType.Binary:
								this.LogBufferContent("Data frame received: ", this.lastRcvdFrame.FrameData, 0, this.lastRcvdFrame.FrameData.Length);
								this.OnDataReceived(this.lastRcvdFrame.PayloadBytes);
								break;
							case WSFrameType.Close:
								this.LogBufferContent("Close frame received: ", this.lastRcvdFrame.FrameData, 0, this.lastRcvdFrame.FrameData.Length);
								// based on RFC6455 we must stop sending and receiving messages after Close response is sent
								this.activityTimer.Stop();
								this.state = WebSocketState.Disconnecting;
								this.subState = SubState.CloseTcpConnection;
								this.smSendCloseResponse();
								return;
							case WSFrameType.Ping:
								this.LogBufferContent("Ping frame received: ", this.lastRcvdFrame.FrameData, 0, this.lastRcvdFrame.FrameData.Length);
								this.smSendPongMessage();
								break;
							case WSFrameType.Pong:
								this.LogBufferContent("Pong frame received: ", this.lastRcvdFrame.FrameData, 0, this.lastRcvdFrame.FrameData.Length);
								// no need to do anything
								break;
							default:
								this.LogBufferContent("Unknown frame received: ", this.lastRcvdFrame.FrameData, 0, this.lastRcvdFrame.FrameData.Length);
								break;
						}
					}
				}

				// send queued messages
				if (this.DequeueAndSendMessages() && this.activityTimerEnabled)
				{
					this.activityTimer.Restart(ActivityTimerState.MessageSent);
				}

				// get more data
				if (this.socket.Poll(this.waitReceiveTimeout, SelectMode.SelectRead) == true && this.socket.Available > 0)
				{
					bytesRead = this.socketStream.Read(this.receiveBuffer, this.bytesInBuffer, this.receiveBuffer.Length - this.bytesInBuffer);
					this.bytesInBuffer += bytesRead;
					continue;
				}

				// check activity timer
				if (this.activityTimer.HasTimedOut)
				{
					switch ((ActivityTimerState)this.activityTimer.State)
					{
						case ActivityTimerState.MessageSent:
						case ActivityTimerState.WaitingForMessage:
							this.smSendPingMessage();
							activityTimer.Restart(ActivityTimerState.WaitingForPingResponse);
							break;
						case ActivityTimerState.WaitingForPingResponse:
							Logger.WriteError(this.loggerID, string.Concat("Ping response timed out."));
							activityTimer.Stop();
							this.state = WebSocketState.Disconnecting;
							this.subState = SubState.CloseTcpConnection;
							break;
					}
				}
			}
		}

		protected void smSendPingMessage()
		{
			WSFrame wsFrame = WSFrame.CreateFrame(WSFrameType.Ping, this.options.MaskingEnabled, "Hello");
			this.LogBufferContent("Sending ping frame: ", wsFrame.FrameData, 0, wsFrame.FrameData.Length);
			this.socketStream.Write(wsFrame.FrameData, 0, wsFrame.FrameData.Length);
		}

		protected void smSendPongMessage()
		{
			WSFrame wsFrame = WSFrame.CreateFrame(WSFrameType.Pong, this.options.MaskingEnabled, "Hello");
			this.LogBufferContent("Sending pong frame: ", wsFrame.FrameData, 0, wsFrame.FrameData.Length);
			this.socketStream.Write(wsFrame.FrameData, 0, wsFrame.FrameData.Length);
		}

		#endregion

		#region smDisconnect methods

		protected void smDisconnect()
		{
			while (this.state == WebSocketState.Disconnecting)
			{
				switch (this.subState)
				{
					case SubState.SendCloseFrame:
						// send close frame
						this.smSendCloseFrame();
						break;
					case SubState.WaitForCloseResponse:
						// wait for close response
						this.smWaitCloseResponse();
						break;
					case SubState.CloseTcpConnection:
						// close tcp connection
						this.smCloseTcpConnection();
						break;
					case SubState.Disconnected:
						// disconnection successful
						this.smDisconnected();
						return;
					case SubState.Failed:
						// disconnection failed
						this.smDisconnectFailed();
						return;
					default:
						// shouldn't get here
						throw new NotSupportedException("ConnectionState " + this.subState.ToString() + " is invalid.");
				}
			}
		}

		protected void smSendCloseFrame()
		{
			WSFrame wsFrame = WSFrame.CreateFrame(WSFrameType.Close, this.options.MaskingEnabled, ArrayUtil.Concat(this.closeStatus, this.closeReason));
			this.LogBufferContent("Sending close frame: ", wsFrame.FrameData, 0, wsFrame.FrameData.Length);
			this.socketStream.Write(wsFrame.FrameData, 0, wsFrame.FrameData.Length);
			this.subState = SubState.WaitForCloseResponse;
		}

		protected void smSendCloseResponse()
		{
			byte[] payLoad = null;
			if (this.lastRcvdFrame.OpCode == WSFrameType.Close && this.lastRcvdFrame.PayloadLength > 0)
			{
				// reply with the same status code and reason
				payLoad = this.lastRcvdFrame.PayloadBytes;
			}

			WSFrame wsFrame = WSFrame.CreateFrame(WSFrameType.Close, this.options.MaskingEnabled, payLoad);
			this.LogBufferContent("Sending close frame: ", wsFrame.FrameData, 0, wsFrame.FrameData.Length);
			this.socketStream.Write(wsFrame.FrameData, 0, wsFrame.FrameData.Length);
		}

		protected void smWaitCloseResponse()
		{
			int bytesRead = 0;
			this.posHeaderEOF = 0;
			this.bytesInBuffer = 0;

			if (this.socket.Poll(this.waitCloseMsgTimeout, SelectMode.SelectRead) == true && this.socket.Available > 0)
			{
				do
				{
					bytesRead = this.socketStream.Read(this.receiveBuffer, this.bytesInBuffer, this.receiveBuffer.Length - this.bytesInBuffer);
					if (bytesRead > 0)
					{
						this.bytesInBuffer += bytesRead;
						if (WSFrame.TryParse(this.receiveBuffer, 0, this.bytesInBuffer, this.options.MaxReceiveFrameLength, out this.lastRcvdFrame) == true)
						{
							// remove data-frame from buffer
							Array.Copy(this.receiveBuffer, this.lastRcvdFrame.FrameData.Length, this.receiveBuffer, 0, this.bytesInBuffer - this.lastRcvdFrame.FrameData.Length);
							this.bytesInBuffer = this.bytesInBuffer - this.lastRcvdFrame.FrameData.Length;

							// check for close frame
							if (this.lastRcvdFrame.OpCode == WSFrameType.Close)
							{
								this.LogBufferContent("Close frame received: ", this.lastRcvdFrame.FrameData, 0, this.lastRcvdFrame.FrameData.Length);
								this.subState = SubState.CloseTcpConnection;
								return;
							}

							this.LogBufferContent("Data frame received: ", this.lastRcvdFrame.FrameData, 0, this.lastRcvdFrame.FrameData.Length);
						}
					}
				}
				while (this.socket.Available > 0 && this.bytesInBuffer < this.receiveBuffer.Length);
			}

			Logger.WriteError(this.loggerID, "Close frame not received.");
			this.subState = SubState.Failed;
		}

		protected void smCloseTcpConnection()
		{
			if (this.socketStream != null)
			{
				this.socketStream.Close();
				this.socketStream = null;
			}
			if (this.socket != null)
			{
				this.socket.Close();
				this.socket = null;
			}
			this.subState = SubState.Disconnected;
		}

		protected void smDisconnected()
		{
			this.state = WebSocketState.Disconnected;
			this.subState = SubState.Disconnected;
		}

		protected void smDisconnectFailed()
		{
			if (this.socketStream != null)
			{
				this.socketStream.Close();
				this.socketStream = null;
			}
			if (this.socket != null)
			{
				this.socket.Close();
				this.socket = null;
			}
			this.state = WebSocketState.Disconnected;
			this.subState = SubState.Disconnected;
		}

		#endregion

		#endregion


		#region Private and Protected Methods

		protected string GetSecurityKey()
		{
			byte[] secBytes = CryptoUtils.GetRandomBytes(16);
			return ConvertEx.ToBase64String(secBytes);
		}

		protected bool IsValidExtensionsResponse(string responseExtensionList)
		{
			// TODO - implementation extensions validation
			return false;
			/*
			// multiple extensions are allowed per RFC6455
			string[] responseExtensions = responseExtensionList.Split(',');
			string[] requestedExtensions = this.extensionList.Split(',');
			this.extensionList = "";
			for (int i=0; i < requestedExtensions.Length; i++)
			{
				requestedExtensions[i] = requestedExtensions[i].Trim().ToLower();
				for (int j=0; j < responseExtensions.Length; j++)
				{
					responseExtensions[j] = responseExtensions[j].Trim().ToLower();
					if (responseExtensions[j] == requestedExtensions[i])
					{
						if (this.extensionList.Length > 0)
							this.extensionList = string.Concat(this.extensionList, ",", requestedExtensions[i]);
						else
							this.extensionList = requestedExtensions[i];
						break;
					}
				}
			}
			return this.extensionList.Length > 0;
			*/
		}

		protected bool IsValidProtocolResponse(string responseProtocol)
		{
			// only one protocol is allowed per RFC6455
			responseProtocol = responseProtocol.Trim().ToLower();
			string[] requestedProtocols = this.subProtocol.Split(',');
			this.subProtocol = "";
			for (int i = 0; i < requestedProtocols.Length; i++)
			{
				if (responseProtocol == requestedProtocols[i].Trim().ToLower())
				{
					this.subProtocol = requestedProtocols[i];
					break;
				}
			}
			return this.subProtocol.Length > 0;
		}

		protected bool IsValidSecurityResponse(string responseSecKey)
		{
			// build expected secKey
			byte[] tempbytes = Encoding.UTF8.GetBytes(string.Concat(this.securityKey, WSConst.HeaderSecurityGUID));
			string expectedSecKey = ConvertEx.ToBase64String(CryptoUtils.ComputeSha1Hash(tempbytes));

			Logger.WriteDebug(this.loggerID, "Expected sec key: " + expectedSecKey);
			Logger.WriteDebug(this.loggerID, "Response sec key: " + responseSecKey);

			if (expectedSecKey == responseSecKey)
				return true;
			
			return false;
		}

		protected void EnqueueMessage(WSFrameType opCode, bool isMasked, string payLoad, bool highPriority = false)
		{
			WSFrame wsFrame = WSFrame.CreateFrame(opCode, isMasked, payLoad);
			if (highPriority)
				this.sendQueue.Poke(wsFrame);
			else
				this.sendQueue.Enqueue(wsFrame);
		}

		protected void EnqueueMessage(WSFrameType opCode, bool isMasked, byte[] payLoad, bool highPriority = false)
		{
			WSFrame wsFrame = WSFrame.CreateFrame(opCode, isMasked, payLoad);
			if (highPriority)
				this.sendQueue.Poke(wsFrame);
			else
				this.sendQueue.Enqueue(wsFrame);
		}

		protected bool DequeueAndSendMessages()
		{
			bool messagesSent = this.sendQueue.Count > 0;
			lock (this.sendLock)
			{
				while (this.sendQueue.Count > 0)
				{
					WSFrame wsFrame = this.sendQueue.Dequeue();
					this.LogBufferContent("Sending data frame: ", wsFrame.FrameData, 0, wsFrame.FrameData.Length);
					this.socketStream.Write(wsFrame.FrameData, 0, wsFrame.FrameData.Length);
				}
			}
			return messagesSent;
		}

		protected void LogBufferContent(string prefix, byte[] buffer, int startIndex, int length)
		{
			if (Logger.GetLogLevel() == LogLevel.Debug)
				Logger.WriteDebug(this.loggerID, string.Concat(prefix, buffer.ToHex(startIndex, length)));
		}

		#endregion


		#region Member Fields

		protected object eventLock;
		protected object sendLock;
		protected bool isDisposed;

		protected bool runThreadLoop;
		protected AutoResetEvent runStateMachine;
		protected Thread stateMachineThread;

		protected string loggerID;
		protected string origin;
		protected string subProtocol;
		protected string extensions;
		protected WebSocketState state;
		protected SubState subState;
		protected WSOptions options;

		protected string securityKey;

		protected bool activityTimerEnabled;
		protected bool sendFramesMasked;
		protected TimerEx activityTimer;

		protected int waitHandshakeTimeout;	// timeout in seconds to wait for handshake response
		protected int waitCloseMsgTimeout;	// timeout in seconds to wait for close response
		protected int waitReceiveTimeout;	// timeout in seconds to wait for receive data
		protected int waitActivityTimeout;	// timeout in seconds to wait for receive data
		protected int waitPingRespTimeout;	// timeout in seconds to wait for receive data

		protected UriEx serverUri;
		protected EndPoint serverEndpoint;
		protected Socket socket;
		protected Stream socketStream;

		private WSFrame lastRcvdFrame;
		protected UInt16 closeStatus;
		protected string closeReason;

		protected byte[] receiveBuffer;
		protected int bytesInBuffer;
		protected int posHeaderEOF;

		private WSFrameQueue sendQueue;

		protected enum SubState
		{
			Initialized,
			OpenTcpConnection,
			SendHandshake,
			WaitForHandshake,
			ProcessHandshake,
			Connected,
			SendCloseFrame,
			WaitForCloseResponse,
			CloseTcpConnection,
			Disconnected,
			Failed
		}

		protected enum ActivityTimerState
		{
			MessageSent,
			WaitingForMessage,
			WaitingForPingResponse
		}

		#endregion
	}
}
