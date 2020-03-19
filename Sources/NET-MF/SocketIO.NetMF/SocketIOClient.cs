///////////////////////////////////////////////////////////////////////////////
//	Copyright 2013 Niko Mäkitalo
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
using Microsoft.SPOT;
using System.Collections;
using System.Threading;

using JDI.WebSocket.Client;
using Json.NETMF;
//using Gadgeteer.Networking;
using JDI.Common.Logger;
using System.Net;

namespace SocketIO.NetMF
{
    public class SocketIOClient
    {

        private string _host = null;
        private string _port = null;

        private string socketioURI = null;
        private string handshakeURI = null;
        
        private enum SOCKETIO_CHANNEL : int { DISCONNECT = 0, CONNECT = 1, HEARTBEAT = 2, MESSAGE = 3, JSON_MESSAGE = 4, EVENT = 5, ACK = 6, ERROR = 7, NOOP = 8 };
        
        private static Boolean websocketConnectionUp;
        private static WebSocketClient websocketClient;

        private string session_id = "";

        private static string NAMESPACE = "socket.io";
        private static string VERSION = "1";
        private static string TRANSPORTID = "websocket";

        private static int HEARTBEAT_INTERVAL = 15000;
        private static Gadgeteer.Timer heartbeat_timer2 = null;

        private int messageId = 0;




        public SocketIOClient()
        {
            // Makes sure that the Logger has been initialized as WebSocket implementation uses this for logging
            try { Logger.Initialize(new DebugLogger(), LogLevel.Info); }
            catch (Exception e) { Debug.Print(e.ToString()); }
        }


        public void connect(string host, string port)
        {
            _host = host;
            _port = port;

            initWSClient();
            send_handshake();
        }


        public void disconnect()
        {
            websocketClient.SendText("0::/disconnect");
        }


        // Override these in your inherited class
        public virtual void onDisconnect() { Debug.Print("disconnected"); }
        public virtual void onConnect() { Debug.Print("connected"); }
        public virtual void onHeartbeat() { Debug.Print("got heartbeat"); }

        public virtual void onMessage(string message) { Debug.Print("got messag: " + message); }
        public virtual void onJsonMessage(Hashtable jsonObject) { Debug.Print("got json object"); }
        public virtual void onEvent(string name, ArrayList args) { Debug.Print("got event: " + name); }

        public virtual void onError(string reason) { Debug.Print("error: " + reason); }

        // This should not be overridden?
        public void onNoop() { Debug.Print("noop"); }



        // emit EVENT
        public void emit(string eventName, ArrayList arguments)
        {
            string nextId = getNextMessageId();
            Hashtable hashtable = new Hashtable();
            hashtable.Add("name", eventName);
            hashtable.Add("args", arguments);
            string json = JsonSerializer.SerializeObject(hashtable);
            string message = "5:" + nextId + "::" + json;
            Debug.Print("emitting: " + message);
            websocketClient.SendText(message);
            return;
        }

        // emit JSON_OBJECT
        public void emit(Hashtable jsonObject)
        {
            string nextId = getNextMessageId();
            string json = JsonSerializer.SerializeObject(jsonObject);
            string message = "4:" + nextId + "::" + json;
            Debug.Print("emitting: " + message);
            websocketClient.SendText(message);
            return;
        }


        // emit MESSAGE
        public void emit(string str)
        {
            string nextId = getNextMessageId();
            string message = "3:" + nextId + "::" + str;
            Debug.Print("emitting: " + message);
            websocketClient.SendText(message);
            return;
        }





        /// private methods
        
        private void initWSClient() 
        {
            socketioURI = "ws://" + _host + ":" + _port + "/" + NAMESPACE + "/" + VERSION + "/" + TRANSPORTID + "/" + session_id;
            handshakeURI = "http://" + _host + ":" + _port + "/" + NAMESPACE + "/" + VERSION + "/";

            WSOptions wsOptions = new WSOptions();
            wsOptions.MaskingEnabled = true;
            wsOptions.ActivityTimerEnabled = true;

            // init pusher client
            websocketClient = new WebSocketClient("websocket", wsOptions);

            // attach event handlers
            websocketClient.ConnectionChanged += new WSDelegates.ConnectionChangedEventHandler(websocketClient_ConnectionChanged);
            websocketClient.TextMessageReceived += new WSDelegates.TextMessageReceivedEventHandler(websocketClient_TextMessageReceived);
            websocketClient.Error += new WSDelegates.ErrorEventHandler(websocketClient_Error);

            Debug.Print("initialized ws client!!");

            return;
        }
        

        private void websocketClient_Error(string message, string stackTrace = null)
        {
            Debug.Print("Error in WebSocket connection, " + stackTrace.ToString());
            throw new Exception("Error in WebSocket connection, "+stackTrace.ToString());
        }


        private void websocketClient_ConnectionChanged(WebSocketState websocketState)
        {
            if (websocketState == WebSocketState.Connected)
            {
                Debug.Print("ws connected!");
                SocketIOClient.websocketConnectionUp = true;
            }
            else
            {
                Debug.Print("ws NOT connected!");
                SocketIOClient.websocketConnectionUp = false;
            }
        }

        private void websocketClient_TextMessageReceived(string message)
        {
            int value = int.Parse(message.Substring(0, 1));
            SOCKETIO_CHANNEL channel = (SOCKETIO_CHANNEL)value;

            string messageId = "";
            string dataString = "";

            string[] parts = message.Split(new char[] { ':' }, 4);
            switch (channel)
            {
                case SOCKETIO_CHANNEL.DISCONNECT:
                    Debug.Print("Disconnect");
                    onDisconnect();
                    break;

                case SOCKETIO_CHANNEL.CONNECT:
                    Debug.Print("Connect");
                    set_heartbeat_timer();
                    onConnect();
                    break;

                case SOCKETIO_CHANNEL.HEARTBEAT:
                    Debug.Print("Heartbeat");
                    onHeartbeat();
                    break;

                case SOCKETIO_CHANNEL.MESSAGE:
                    Debug.Print("Message");
                    messageId = parts[1];
                    dataString = parts[3];
                    if ("" != messageId)
                    {
                        websocketClient.SendText("6:::" + messageId);
                    }

                    onMessage(dataString);
                    break;

                case SOCKETIO_CHANNEL.JSON_MESSAGE:
                    Debug.Print("JSON message");
                    messageId = parts[1];
                    dataString = parts[3];

                    // parse json object
                    Hashtable jsonObject = JsonSerializer.DeserializeString(dataString) as Hashtable;
                    if ("" != messageId)
                    {
                        websocketClient.SendText("6:::" + messageId);
                    }
                    onJsonMessage(jsonObject);
                    break;

                case SOCKETIO_CHANNEL.EVENT:
                    Debug.Print("Event");
                    messageId = parts[1];
                    dataString = parts[3];

                    // parse json object
                    Hashtable data = JsonSerializer.DeserializeString(dataString) as Hashtable;
                    string name = data["name"].ToString();
                    ArrayList args = (ArrayList)data["args"];

                    /*
                    // for debugging
                    foreach (DictionaryEntry pair in data)
                    {
                        Debug.Print(pair.Key.ToString());
                        if (pair.Value is ArrayList)
                        {
                            Debug.Print("array");
                        }
                        else {
                            Debug.Print("not an array");
                            Debug.Print(pair.Value.ToString());
                        }
                    }
                    */

                    // get name and args
                    if ("" != messageId)
                    {
                        websocketClient.SendText("6:::" + messageId);
                    }
                    onEvent(name, args);
                    break;

                case SOCKETIO_CHANNEL.ACK:
                    Debug.Print("ACK");
                    if (parts[3] != null && parts[3].IndexOf('+') != -1)
                    {
                        Debug.Print("plussa oli!");
                    }

                    // TODO
                    /*
                        // From android impl
                        String[] ackParts = parts[3].split("\\+");
                        int ackId = Integer.valueOf(ackParts[0]);

                        String ackArgs = ackParts[1];

                        int startIndex = ackArgs.indexOf('[') + 1;

                        ackArgs = ackArgs.substring(startIndex, ackArgs.length() - 1);

                        Acknowledge acknowledge = mAcknowledges.get(ackId);

                        if (acknowledge != null) {

                            String[] params = ackArgs.split(",");
                            for (int i = 0; i < params.length; i++) {
                                params[i] = params[i].replace("\"", "");
                            }
                            acknowledge.acknowledge(params);
                        }

                        mAcknowledges.remove(ackId);
                    */
                    
                    break;


                case SOCKETIO_CHANNEL.ERROR:
                    Debug.Print("Error");
                    messageId = parts[1];
                    dataString = parts[3];
                    onError(dataString);
                    break;

                case SOCKETIO_CHANNEL.NOOP:
                    Debug.Print("Noop");
                    onNoop();
                    break;

                default:
                    Debug.Print("wrong channel");
                    break;
            }
        }


        private void set_heartbeat_timer()
        {
            heartbeat_timer2 = new Gadgeteer.Timer(HEARTBEAT_INTERVAL);
            heartbeat_timer2.Tick += new Gadgeteer.Timer.TickEventHandler((timer) => {
                send_heartbeat();
            });
            heartbeat_timer2.Start();
        }


        private void send_heartbeat(object stateInfo = null)
        {
            Debug.Print("sending heartbeat..");
            websocketClient.SendText("2::");
        }

        
        private void send_handshake()
        {
            //POSTContent pc = new POSTContent();
            //HttpRequest hr = HttpHelper.CreateHttpPostRequest(handshakeURI, pc, "");
            //hr.ResponseReceived += new HttpRequest.ResponseHandler(handshake_ResponseReceived);
            //hr.SendRequest();
            using (HttpWebRequest request = WebRequest.Create(handshakeURI) as HttpWebRequest)
            {
                request.Method = "POST";
                request.Timeout = 3000;
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    using (var stream = response?.GetResponseStream())
                    {
                        byte[] bytes = new byte[stream.Length];
                        stream.Read(bytes, 0, bytes.Length);
                        var result = new String(System.Text.UTF8Encoding.UTF8.GetChars(bytes, 0, bytes.Length));
                        handshake_ResponseReceived(result);
                    }
                }
            }

            Debug.Print("handshake sent");
        }


        private void handshake_ResponseReceived(string responseText)
        {
            websocketConnectionUp = false;
            Debug.Print("got response:");
            Debug.Print(responseText);
            string[] parts = (responseText).Split(':');
            if (parts.Length < 4)
            {
                throw new Exception("Error in socket.io handshake - wrong number of arguments received!");
            }
            else
            {
                session_id = parts[0];
                Debug.Print("Session ID: " + session_id);

                // updates the new session id to the uri
                update_socketio_uri();

                // sends connect through ws
                websocketConnectionUp = true;
                send_connect();
            }
        }


        // connects to WebSocket after handshake
        private void send_connect()
        {
            if (websocketConnectionUp)
            {
                Debug.Print("connect uri: " + socketioURI);
                websocketClient.Connect(socketioURI);
            }
            else
            {
                throw new Exception("Connection not UP!");
            }
        }


        private void update_socketio_uri()
        {
            socketioURI = "ws://" + _host + ":" + _port + "/" + NAMESPACE + "/" + VERSION + "/" + TRANSPORTID + "/" + session_id;
        }


        private string getNextMessageId()
        {
            ++messageId;
            return messageId.ToString();
        }
    }

}
