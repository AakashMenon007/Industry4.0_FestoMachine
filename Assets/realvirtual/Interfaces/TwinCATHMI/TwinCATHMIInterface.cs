// realvirtual.io (formerly game4automation) (R) a Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 realvirtual GmbH - Usage of this source code only allowed based on License conditions see https://realvirtual.io/unternehmen/lizenz  
#pragma warning disable 1998
#pragma warning disable 0414
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using NaughtyAttributes;
#if REALVIRTUAL_JSON
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#endif
using UnityEditor;
#if REALVIRTUAL_BESTHTTP
using WebSocket = BestHTTP.WebSocket.WebSocket;
using BestHTTP.WebSocket;
#endif
namespace realvirtual
{
    //! TwinCAT HMI Interface which is communicating over WebSockets. Thus this interface also works in WebGL Builds. TwinCAT HMI Server needs to be installed on the PLC side for using this interface
    [HelpURL("https://doc.realvirtual.io/components-and-scripts/interfaces/twincat-hmi")]
    public class TwinCATHMIInterface : InterfaceBaseClass
    { 
#if !REALVIRTUAL_BESTHTTP || !REALVIRTUAL_JSON
        [InfoBox("This interface requires BESTHTTP And Newtonsoft JSON . You need to purchase BESTHTTP on the Unity Asset Store. Newtonsoft JSON needs to be added in the manifest.json. After both is installed please put REALVIRTUAL_BESTHTTP and REALVIRTUAL_JSON into your Scripting Define Symbols in Player Settings",EInfoBoxType.Error)]
#endif
     public string ServerAdress;
#if REALVIRTUAL_BESTHTTP
        #region PublicVariables
        
        [ReadOnly] public string Status; //!< The current status
        [ReadOnly] public bool IsConnecting = false;
        public bool DebugMode = false; //!< Debug modes prints out additional console logs
        public int IntervallTime = 100; //!< The intervall time of subscriptions- all subscriptions in this intervall are collected on PLC side into one messagge
        public bool SubsribeOutputs = true; //!< true if interface should subscribe for output changes
        public bool PollOutputs = false; //!< True if outputs should be polled in a cyclic manner
        public string SymbolTable = ""; //!< Path to the symbol table
        public bool UseAuthentification = false;
        [ShowIf("UseAuthentification")] public string DomainName; //!< Domain name for authentification
        [ShowIf("UseAuthentification")] public string UserName; //!< User name for authentification
        [ShowIf("UseAuthentification")] public string Password; //!< Password for authentification
        [ReadOnly] public int NumInputs; //! Number of inputs
        [ReadOnly] public int NumOutputs; //! Number of outputs
        public TwinCATSignalReadEvent EventSignalRead; //!< Event which is called if before ReadSymbol has been called
        
        #endregion
        
        #region PrivateVariables
        private WebSocket websock;
        private NativeWebSocket.WebSocket nwebsock;
        private InterfaceSignal[] subscriptions;
        private  Hashtable signals = new Hashtable();
        private  Hashtable writeids = new Hashtable();
        private int id = 1;
        private float lastpoll;
        private bool importsymbols = false;
        private int reconnectcounter;
        private bool disconnectedbefore = false;
        #endregion
        
        
        #region PublicMethods
        //! Writes a Symbol - datastructures are automatically transferred to json
        public void WriteSymbol (string symbol, object signal)
        {
            EventSignalRead.AddListener(OnSymbolWritten);
            WriteSymbol(symbol, signal, id++);
        }

        private void OnSymbolWritten(int id, bool error, string reply)
        {
            // Here check if error is false and the id is the same as given back in WriteSymbolControlled
        }

        public int WriteSymbolControlled (string symbol, object signal)
        {
            WriteSymbol(symbol, signal, id++);
            return id;
        }

        //! Reads a Symbol - data will be returned by EventSignalRead
        public void ReadSymbol(string symbol, int id)
        {
            TwinCATReadSymbol message;
            message = new TwinCATReadSymbol(symbol);
            message.id = id++;
            string messagejson = JsonUtility.ToJson(message);
            SendWebSocketMessage(messagejson);
        }
        
        //! Opens the interface
        public override void OpenInterface()
        {
            UpdateInterfaceSignals(ref NumInputs, ref NumOutputs);
            Connect();
        }
        
        //! Closes the interface
        public override void CloseInterface()
        {
            if (UseAuthentification && UserName != "")
                Logout();
            if (websock != null)
                websock.Close();
            IsConnected = false;
            OnDisconnected();
        }
        #endregion
        
        #region PrivateMethods

        private void CreateSubscriptions()
        {
            id++;
            var writeid = id++;
            List<string> symbols = new List<string>();
            List<InterfaceSignal> subscribedsignals = new List<InterfaceSignal>();
            
            foreach (var signal in InterfaceSignals)
            {
                if (signal.Signal.gameObject.activeSelf)
                    if (signal.Direction == InterfaceSignal.DIRECTION.OUTPUT)
                    {
                        symbols.Add(signal.Signal.Name);    
                        subscribedsignals.Add(signal);
                    }

                if ((signal.Direction == InterfaceSignal.DIRECTION.INPUT))
                {
                    signal.Signal.SignalChanged += OnPLCInputChanged;
                    writeids.Add(signal.Signal.name,writeid);
                }
            }
            Subscribe(symbols.ToArray(), id, PollOutputs);
            subscriptions = subscribedsignals.ToArray();
        }
        
        private void CreateSignalHashtable()
        {
            signals.Clear();
            foreach (var signal in InterfaceSignals)
            {
                if (signal.Signal.gameObject.activeSelf)
                {
                    signals.Add(signal.Signal.Name,signal.Signal);
                }
            }
        }

        private void WriteSignal(Signal obj)
        {
            #if REALVIRTUAL_JSON
            var type = obj.GetType();
            if (type == typeof(PLCInputFloat))
            {
                var signal = (PLCInputFloat) obj;
                WriteSymbol(signal.Name, signal.Value, id++);
            }
            if (type == typeof(PLCInputInt))
            {
                var signal = (PLCInputInt) obj;
                WriteSymbol(obj.Name, signal.Value, id++);
            }
            if (type == typeof(PLCInputBool))
            {
                var signal = (PLCInputBool) obj;
                WriteSymbol(obj.Name, signal.Value, id++);
            }
            if (type == typeof(PLCInputText))
            {
                var signal = (PLCInputText) obj;
               var jtoken = JToken.Parse(signal.Value);
               if  (jtoken.Type == JTokenType.Array)
                {
                    try
                    {
                        var obj1 = jtoken.ToObject<float[]>();
                        WriteSymbol(obj.name, obj1, id++);
                    }
                    catch (Exception e) { Debug.LogError(e.Message); };

                }
                  
               else
                    WriteSymbol(obj.Name, signal.Value, id++);
            }
#endif
        }
        
        private void OnPLCInputChanged(Signal obj)
        {
            id++;
            WriteSignal(obj);
        }

        void Login()
        {
            var username = UserName;
            if (DomainName != "")
                username = DomainName + "::" + UserName;
            var password = Password;
            var message =
                "{\"requestType\": \"ReadWrite\",\"commands\": [{{\"commandOptions\": [\"SendErrorMessage\",\"SendWriteValue\"],\"symbol\": \"Login\",\"writeValue\":{\"userName\":\"" +
                username + "\",\"password\":\"" + password + "\",\"persistend\":true}}]}";
            if (DebugMode)
                Debug.Log("Login:" + message);
            SendWebSocketMessage(message);
        }

        void Logout()
        {
            var message =
                "{\"requestType\": \"ReadWrite\",\"commands\": [{\"commandOptions\": [\"SendErrorMessage\"],\"SendWriteValue\"],\"symbol\": \"Logout\"}]}";
            if (DebugMode)
                Debug.Log("Logout:"+message);
            SendWebSocketMessage(message);
        }
        
        void MapSymbols()
        {
            var message =
                "{\"requestType\": \"ReadWrite\",\"commands\": [{\"commandOptions\": [\"SendErrorMessage\"],\"symbol\": \"AddSymbols\",\"writeValue\":{\"domain\": \"ADS\"}}]}";
            SendWebSocketMessage(message);
        }

        void Subscribe(string[] symbol, int id, bool poll)
        {
            TwinCATSubscribe message;
            message = new TwinCATSubscribe(symbol, id, IntervallTime, poll);
            string messagejson = JsonUtility.ToJson(message);
            SendWebSocketMessage(messagejson);
        }

        void WriteSymbol(string symbol, float value, int id)
        {
            TwinCATWriteFloat message;
            message = new TwinCATWriteFloat(symbol, value);
            message.id = id;
            string messagejson = JsonUtility.ToJson(message);
            SendWebSocketMessage(messagejson);
        }  
        
        void WriteSymbol(string symbol, int value,int id)
        {
            TwinCATWriteInt message;
            message = new TwinCATWriteInt(symbol, value);
            message.id = id;
            string messagejson = JsonUtility.ToJson(message);
            SendWebSocketMessage(messagejson);
        }
        void WriteSymbol(string symbol, bool value, int id)
        {
            TwinCATWriteBool message;
            message = new TwinCATWriteBool(symbol, value);
            message.id = id;
            string messagejson = JsonUtility.ToJson(message);
            SendWebSocketMessage(messagejson);
        }
        
        void WriteSymbol(string symbol, object value, int id)
        {
            TwinCATWriteText message;
            message = new TwinCATWriteText(symbol, value.ToString());
            message.id = id;
            string messagejson = JsonUtility.ToJson(message);
            #if REALVIRTUAL_JSON
            var jobject = JObject.Parse(messagejson);
            var newjvalue = JToken.FromObject(value);
            jobject.SelectToken("commands")[0].SelectToken("writeValue").Replace(newjvalue);
            messagejson = jobject.ToString(Formatting.None);
            SendWebSocketMessage(messagejson);
#endif
        }
        
        void Connect()
        {
            signals = new Hashtable();
            writeids = new Hashtable();
            IsConnecting = true;
            Debug.Log($"TwinCAT Websocket Interface - trying to connect to {ServerAdress}");
          
                websock = new WebSocket(new Uri(ServerAdress));
                websock.OnOpen += OnWebSocketOpen;
                websock.OnClosed += OnWebSocketClosed;
                websock.OnError += OnError;
                websock.OnMessage += WebsockOnMessage;
                websock.Open();
        }

        private void WebsockOnMessage(WebSocket webSocket, string message)
        {
            #if REALVIRTUAL_JSON
            if (DebugMode)
                Debug.Log("TwinCAT Websocket Interface - Received Message:" + message);
            JObject o = JObject.Parse(message);
            var requesttype = (string) o.SelectToken("requestType");

            if (requesttype == "ReadWrite")
            {
                var commands = o.SelectToken("commands");
                foreach (var command in commands)
                {
                    var symbol = (string) command.SelectToken("symbol");
                    var error = command.SelectToken("error");
                    if (error != null)
                    {
                        var domain = error.SelectToken("domain");
                        var errormessage = (string) error.SelectToken("message");
                        var errorreason = (string) error.SelectToken("reason");
                        Debug.LogError(
                            $"TwinCAT Websocket Interface - Error in WebsockOnMessage [{symbol}] Domain [{domain}] Message [{errormessage}]  Reason [{errorreason}]");
                    }
                    else
                    {
                        OnReadWrite(message);
                    }
                }
            }
            if (requesttype == "Subscription")
            {
                #if REALVIRTUAL_JSON
                OnSubscribedValueChanged(o);
                #endif
            }
           #endif
        }

        private void OnReadWrite(string message)
        {
            int id = 0;
            bool error = false;
#if REALVIRTUAL_JSON
            /// GetID
            JObject o = JObject.Parse(message);
            id = (int) o.SelectToken("id");
            var commands = o.SelectToken("commands");
            var errortoken = commands.SelectToken("error");
            if (errortoken != null)
                error = true;
            
#endif
            EventSignalRead.Invoke( id, error,  message);
            if (DebugMode)
                Debug.Log("On Read Write " + message);
        }

        #if REALVIRTUAL_JSON
        private void OnSubscribedValueChanged(JObject o)
        {
            var commands = o.SelectToken("commands");
            
            foreach (var command in commands)
            {
                var symbol = (string)command.SelectToken("symbol");
                var error = command.SelectToken("error");
                if (error != null)
                {
                    var domain = error.SelectToken("domain");
                    var errormessage = (string) error.SelectToken("message");
                    var errorreason = (string) error.SelectToken("reason");
                    Debug.LogError($"TwinCAT Websocket Interface - Error in creating subscription for [{symbol}] Domain [{domain}] Message [{errormessage}]  Reason [{errorreason}]");
                }
                else
                {
                    if (signals.ContainsKey(symbol))
                    {
                        var signal = (Signal)signals[symbol];
                
                        if (signal.GetType() == typeof(PLCOutputBool))
                        {
                            bool value = (bool) command.SelectToken("readValue");
                            ((PLCOutputBool) signal).Value = value;
                            if (DebugMode)
                                Debug.Log($"TwinCAT Websocket Interface - Signal [{symbol}], changed to [{value}]");
                        }
                        if (signal.GetType() == typeof(PLCOutputFloat))
                        {
                            float value = (float) command.SelectToken("readValue");
                            ((PLCOutputFloat) signal).Value = value;
                            if (DebugMode)
                                Debug.Log($"TwinCAT Websocket Interface - Signal [{symbol}], changed to [{value}]");
                        }
                        if (signal.GetType() == typeof(PLCOutputInt))
                        {
                            int value = (int) command.SelectToken("readValue");
                            ((PLCOutputInt) signal).Value = value;
                            if (DebugMode)
                                Debug.Log($"TwinCAT Websocket Interface - Signal [{symbol}], changed to [{value}]");
                        }
                        if (signal.GetType() == typeof(PLCOutputText))
                        {
                            string value = command.SelectToken("readValue").ToString(Newtonsoft.Json.Formatting.None);
                            ((PLCOutputText) signal).Value = value;
                            if (DebugMode)
                                Debug.Log($"TwinCAT Websocket Interface - Signal [{symbol}], changed to [{value}]");
                        }
                
                    }
                    else
                    {
                        Debug.LogError($"TwinCAT Websocket Interface - Unknown symbol received in Subscription [{symbol}]");
                    }
                }
               
            }
        }
        #endif

        private void WriteAllInputs()
        {
            foreach (var interfaceSignal in InterfaceSignals)
            {
                if ((interfaceSignal.Direction == InterfaceSignal.DIRECTION.INPUT))
                {
                   WriteSignal(interfaceSignal.Signal);
                }
            }
        }
        
        private void OnWebSocketOpen(WebSocket webSocket)
            //private void OnWebSocketOpen()
        {
            Debug.Log($"TwinCAT Websocket Interface - successfull connected to {ServerAdress}");
            if (UseAuthentification && UserName != "")
            {
                Login();
            }
            MapSymbols();
            CreateSignalHashtable();
            if (SubsribeOutputs)
                 CreateSubscriptions();
            OnConnected();
            // if (importsymbols)
               // OnImportSymbolsConnected();
            IsConnecting = false;
            WriteAllInputs();   // Write all PLC Inputs on Connection
        }

        private void OnWebSocketClosed(WebSocket webSocket, UInt16 code, string message)
        {
            if (websock != null)
            {
                websock.Close();
                websock = null;
            }
            OnDisconnected();
            Status = "Not connected";
            Debug.Log("WebSocket is now Closed!");
        }

        private void OnBinaryMessageReceived(WebSocket webSocket, byte[] message)
        {
            Debug.Log("Binary Message received from server. Length: " + message.Length);
        }

        void OnError(WebSocket ws, string error)
        {
            Debug.LogError($"TwinCAT Websocket Interface - {error} Errror in connection to {ServerAdress}");
            Status = error;
            IsConnecting = false;
        }

        async void SendWebSocketMessage(string message)
        {
            if (DebugMode)
                Debug.Log("TwinCAT Websocket Interface - Send Message:" + message);
            websock.Send(message);
        }
        
        [Button("Import Symbols")]
        void ImportSymbols()
        {
            importsymbols = true;
            if (IsConnected==false)
                Connect();
        } 
              
        [Button("Select symbol table")]
        private void SelectSymbolTable()
        {
            #if UNITY_EDITOR
            var File = "";
            File = EditorUtility.OpenFilePanel("Select file to import", File, "csv");
            SymbolTable = File;
            #endif
        }

        [Button("Import Symbols from CSV")]
        public void ImportSymbolsFromCSV()
        {
            List<string> group = new List<string>();
            List<string> name = new List<string>();
            List<string> symbol = new List<string>();
            List<string> io = new List<string>();
            List<string> dtype = new List<string>();
            List<string> comment = new List<string>();
            try
            {
                using (StreamReader sr = new System.IO.StreamReader(SymbolTable))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        var tmp = "\"";
                        var newline = line.Replace(tmp, string.Empty);
                        var values = newline.Split(';');
                        group.Add(values[0]);
                        name.Add(values[1]);
                        symbol.Add(values[2]);
                        io.Add(values[3]);
                        dtype.Add(values[4]);
                        comment.Add(values[5]);
                    }
                }
            }
            catch (Exception e)
            {
                Error("Error in reading PLC Signal table " + e.ToString());
            }

            for (int i = 1; i < symbol.Count; i++)
            {
                var direction = InterfaceSignal.DIRECTION.OUTPUT;
                var type = InterfaceSignal.TYPE.BOOL;
                if (io[i].ToUpper() == "INPUT")
                    direction = InterfaceSignal.DIRECTION.INPUT;

                if (dtype[i].ToUpper() == "FLOAT")
                    type = InterfaceSignal.TYPE.REAL;
                if (dtype[i].ToUpper() == "INT")
                    type = InterfaceSignal.TYPE.INT;
                InterfaceSignal newsignal = new InterfaceSignal(symbol[i], direction, type);
                newsignal.SymbolName = symbol[i];
                if (name[i]!="")
                   newsignal.Name = name[i];
                else
                    newsignal.Name = symbol[i];
                newsignal.Comment = comment[i];
                var go = AddSignal(newsignal);
              
                if (name[i] != "")
                {
                    go.gameObject.name = name[i];
                    go.Name = symbol[i];
                }
                else
                {
                    go.Name = "";
                }
                 
                go.transform.parent = this.transform;
                if (group[i] != "")
                {
                    var groupobj = Global.AddGameObjectIfNotExisting(group[i], this.gameObject);
                    go.transform.parent = groupobj.transform;
                }
                #if UNITY_EDITOR
                EditorUtility.SetDirty(go);
                #endif
            }
        }

        void OnImportSymbolsConnected()
        {
            ReadSymbol("ADS.ListSymbols",0);
        }
        
        // Update is called once per frame
        void Update()
        {
            
            // get collider on current gamobject
            
            
            if (websock != null)
            {  
                Status = websock.State.ToString();
                if (websock.State != WebSocketStates.Open)
                    IsConnected = false;
            }
            else
            {
                Status = "Not connected";
            }
               
            if (!IsConnected && !IsConnecting) // Connection broken
            {
                if (!disconnectedbefore)
                {
                    disconnectedbefore = true;
                    OnDisconnected();
                }
                    
                reconnectcounter++;
                if (reconnectcounter > 60)
                {
                    reconnectcounter = 0;
                    IsConnecting = true;
                    disconnectedbefore = false;
                    Connect();
                }
            }
        }
        #endregion
#endif
    }
}