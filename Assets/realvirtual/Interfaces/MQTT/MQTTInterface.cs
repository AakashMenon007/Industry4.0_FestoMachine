// realvirtual.io (formerly game4automation) (R) a Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 realvirtual GmbH - Usage of this source code only allowed based on License conditions see https://realvirtual.io/unternehmen/lizenz  
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
#if REALVIRTUAL_BESTHTTP || REALVIRTUAL_BESTMQTT
using BestMQTT;
using BestMQTT.Packets.Builders;
#endif
using UnityEngine;

using NaughtyAttributes;
using UnityEditor;
using Debug = UnityEngine.Debug;


namespace realvirtual
{
    [HelpURL("https://doc.realvirtual.io/components-and-scripts/interfaces/mqtt")]
    //! MQTT Interface with security (username and password), TLS, supports Websocket, MQTT3.1.1, MQTT5.
    public class MQTTInterface : InterfaceBaseClass
    {
        #if !REALVIRTUAL_BESTHTTP && !REALVIRTUAL_BESTMQTT
        [InfoBox("For using this interface you need to purchase on the Unity Asset Store BestHTTP and BestMQTT and you need to set REALVIRTUAL_BESTHTTP and REALVIRTUAL_BESTMQTT in the Scripting Define Symbols (Project Settings > Player)",EInfoBoxType.Warning)]
        #endif
        public string Broker;  //!< Adress of the MQTT Broker.
        #if REALVIRTUAL_BESTHTTP && REALVIRTUAL_BESTMQTT
        public int Port;  //!< MQTT Communication Port
        public bool DebugMode; //!< true for additional Debug informations
        public bool Websocket = false; //!< true for using Websocket
        public bool MQTT5 = false; //!< if true MQTT5 is used, otherwise MQTT3.1.1
        public int SendCycleMs = 100; //!< is collecting all PLCInputs for this maximum time and sending all the updates at once
        public bool Security = false; //!< true for using security, Password and TLS
        [ShowIf("Security")] public string UserName; //!< if Username is "" no username and password authentification
        [ShowIf("Security")] public string Password;
        [ShowIf("Security")] public bool TLS; //!< true if TLS should be used
        
        public bool DeleteLastSessionPackets = true; //!< if false on connecting to a server all not sended packets from the session before will be send
        private MQTTClient client;
        private bool subscribed;
        private bool importingsignals;
        private bool isconnecting;
        private bool reconnect;
        private List<Signal> signalstosend = new List<Signal>();
        private float lastsend = 0;
        private float lastconnecting;

        
        //! Opens the interface
        public override void OpenInterface()
        {
            signalstosend.Clear();
            importingsignals = false;
            Connect();
        }
        
        //! Closes the interface
        public override void CloseInterface()
        {
            Disconnect();
        }

        
        void Connect()
        {
            /* if (DebugMode)
                BestHTTP.HTTPManager.Logger.Level = BestHTTP.Logger.Loglevels.All;
            else
                BestHTTP.HTTPManager.Logger.Level = BestHTTP.Logger.Loglevels.Error; */
            subscribed = false;
            isconnecting = true;
            lastconnecting = Time.unscaledTime;
            if (DeleteLastSessionPackets && !reconnect)
                SessionHelper.Delete(Broker, SessionHelper.Get(Broker));

            try
            {
                MQTTClientBuilder builder = new MQTTClientBuilder();
                var connection = new ConnectionOptionsBuilder();
                if (!Websocket)
                    connection.WithTCP(Broker, Port);
                else
                    connection.WithWebSocket(Broker, Port);
                if (!MQTT5)
                    connection.WithProtocolVersion(SupportedProtocolVersions.MQTT_3_1_1);
                else
                    connection.WithProtocolVersion(SupportedProtocolVersions.MQTT_5_0);
                if (Security && TLS)
                    connection.WithTLS();
            
                builder.WithOptions(connection);
                builder.WithEventHandler(OnStateChanged);
                builder.WithEventHandler(OnDisconnected);
                builder.WithEventHandler(OnError);
                client = builder.CreateClient();
                client.BeginConnect(ConnectPacketBuilderCallback);
            }
            catch (Exception e)
            {
                Debug.LogError("MQTT Connection Error " + e.Message);
            }

            reconnect = false;
        }

        void Subscribe()
        {
            if (importingsignals)
            {
                // Subscribing above topics for getting all singals during Signal import
                var transforms = GetComponentsInChildren<Transform>();
                bool nothing = true;
                foreach (var trans in transforms)
                {
                    if (trans.gameObject.GetComponent<Signal>() == null && trans.gameObject != this.gameObject)
                    {
                        client.CreateSubscriptionBuilder(trans.gameObject.name)
                            .WithMessageCallback(OnMessage)
                            .BeginSubscribe();
                        nothing = false;
                    }
                }
                if (nothing) // if no sub gameobject for subscribing to the topics all topics are imported
                    client.CreateSubscriptionBuilder("#")
                        .WithMessageCallback(OnMessage)
                        .BeginSubscribe();
            }
            else
            {
                // only subsribe for before imported and remaining signals - signals could have been deleted, manually added or renaimed
                var signals = GetComponentsInChildren<Signal>();
                foreach (var signal in signals)
                {
                    var signalname = signal.Name;
                    if (signalname == "")
                        signalname = signal.name;
                    if (!signal.IsInput())
                    {
                        client.CreateSubscriptionBuilder(signalname)
                            .WithMessageCallback(OnMessage)
                            .BeginSubscribe();
                    }
                    else
                    {
                        // Change Event for signals
                        signal.SignalChanged += SignalOnSignalChanged;
                    }
                }
            }

            subscribed = true;
        }

        private void SendSignal(Signal obj)
        {
            if (!IsConnected)
                return;
            
            var value = obj.GetValue().ToString();
            client.CreateApplicationMessageBuilder(obj.Name)
                .WithPayload(value)
                .WithQoS(BestMQTT.Packets.QoSLevels.AtLeastOnceDelivery)
                .WithRetain()
                .BeginPublish();
        }

        private void SignalOnSignalChanged(Signal obj)
        {
            if (SendCycleMs == 0)
                SendSignal(obj);
            else
            {
                if (!signalstosend.Contains(obj))
                    signalstosend.Add(obj);
            }
               

        }

        private void OnMessage(MQTTClient client, SubscriptionTopic subscriptiontopic, string topicName, ApplicationMessage message)
        {
            // Convert the raw payload to a string
            var payload = Encoding.UTF8.GetString(message.Payload.Data, message.Payload.Offset, message.Payload.Count);
            if (importingsignals)
            {
                var parent = GetChildByName(subscriptiontopic.Filter.OriginalFilter);
                if (parent == null)
                    parent = this.gameObject;
                // get gameobject because of slashes needs to be searched by name
                var children = parent.GetComponentsInChildren<Transform>();
                GameObject go = null;
                foreach (var child in children)
                {
                    if (child.name == topicName)
                    {
                        go = child.gameObject;
                        break;
                    }
                }

                if (go == null)
                {
                    go = new GameObject(topicName);
                    go.transform.parent = parent.transform;
                }
                
                var signal = go.GetComponent<Signal>();
                if (signal == null)
                {
                     signal = go.AddComponent<PLCOutputFloat>();
                }
                go.transform.parent = parent.transform;
                signal.SetValue(payload);
            }
            else
            {
                var signal = Global.GetComponentByName<Signal>(this.gameObject,subscriptiontopic.Filter.OriginalFilter);
                if (signal != null)
                {
                    signal.SetValue(payload);
                }
            }
            
            if (DebugMode) 
                Debug.Log($"Topic:{subscriptiontopic} Content-Type: '{message.ContentType}' Payload: '{payload}'");
        }

        void Disconnect()
        {
            client?.CreateDisconnectPacketBuilder()
                .BeginDisconnect();
        }

        private ConnectPacketBuilder ConnectPacketBuilderCallback(MQTTClient client, ConnectPacketBuilder builder)
        {

            if (Security)
                return builder.WithUserNameAndPassword(UserName, Password);
            else
                return builder;
        }

// Called when the MQTTClient transfered to a new internal state.
        private void OnStateChanged(MQTTClient client, ClientStates oldState, ClientStates newState)
        {
            if (DebugMode)
                 Debug.Log($"{oldState} => {newState}");
            if (newState == ClientStates.Connected)
            {
                Debug.Log("MQTT interface connected to Broker " + Broker);
                IsConnected = true;
                if (importingsignals)
                    Subscribe();
                OnConnected();
                isconnecting = false;
            }
            else
            {
                IsConnected = false;
                OnDisconnected();
            }
        }

// Called when the client disconnects from the server. The disconnection can be client or server initiated or because of an error.
        private void OnDisconnected(MQTTClient client, DisconnectReasonCodes code, string reason)
        {
            if (DebugMode)
                Debug.Log($"OnDisconnected - code: {code}, reason: '{reason}'");
            isconnecting = false;
            IsConnected = false;
      
            OnDisconnected();
        }


// Called when an error happens that the plugin can't recover from. After this event an OnDisconnected event is raised too.
        private void OnError(MQTTClient client, string reason)
        {
            Debug.Log($"OnError reason: '{reason}'");
        }
        
        // Create Signals
        [Button("Import Signals")]
         async void ImportSignals()
        {
            importingsignals = true;
            Connect();
            System.Threading.Tasks.Task task = EndManualImport();
            await task;
        }

         async System.Threading.Tasks.Task EndManualImport()
         {
             for (int i = 0; i < 5; i++)
             {
                 #if UNITY_EDITOR
                 EditorUtility.DisplayProgressBar("Importing", "Please wait - connecting and importing signals...", (float)i/4);
                 #endif
                 await  System.Threading.Tasks.Task.Delay((int)(1 * 1000f));
             }
#if UNITY_EDITOR
             EditorUtility.ClearProgressBar();
             #endif
             importingsignals = false;
             Disconnect();
         }

        [Button("Delete all Signals")]
        void DeleteAllSignals()
        {
            #if UNITY_EDITOR
            if (EditorUtility.DisplayDialog("Warning",
                    "Are you sure to delete all Signals in the MQTT interface", "YES", "NO"))
            {
                Global.DestroyObjectsByComponent<Signal>(this.gameObject);
            }
            #endif
  
        }


        // Update is called once per frame
        void Update()
        {
            if (IsConnected && !subscribed)
                Subscribe();
            if (!IsConnected && !isconnecting)
            {
                if (Time.time > lastconnecting + 5)  // try reconnect all 5 seconds
                {
                 
                    reconnect = true;
                    Debug.Log("MQTT Broker disconnected - trying to reconnect");
                    Connect();
                }
            }
                
        }

        private void FixedUpdate()
        {
            if (SendCycleMs == 0)  // No Signal batching
                return;

            if (!IsConnected)
                return;

            if (Time.fixedUnscaledTime >= lastsend + SendCycleMs / 1000.0f) // Send, if all to send signals are batched in a cycle
            {
                foreach (var signal in signalstosend)
                {
                    SendSignal(signal);
                }
                lastsend = Time.fixedUnscaledTime;
                signalstosend.Clear();
            }
        }
#endif
    }
}