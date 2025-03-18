// realvirtual (R) Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 realvirtual GmbH - Usage of this source code only allowed based on License conditions see https://realvirtual.io/en/company/license

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

using UnityEngine.Events;
using LibUA.Core;
using NaughtyAttributes;
using UnityEditor;
using Types = LibUA.Core.Types;

#pragma warning disable 0219

namespace realvirtual
{
    //! OPC Node subscription class for active subscriptions
    public class OPCUANodeSubscription
    {
        public string NodeId; //!< The OPCUA NodeID
        public uint SubscriptionId;
        public uint ClientHandle;
        public List<NodeUpdateDelegate> UpdateDelegates;
    }
    
    public enum SignatureAlgorithm
    {
        Sha1,
        Sha256,
        Rsa15,
        RsaOaep,
        RsaOaep256,
        RsaPss256
    }
    
  
    public delegate void NodeUpdateDelegate(OPCUANodeSubscription sub, object value);

#if REALVIRTUAL
    [HelpURL("https://doc.realvirtual.io/components-and-scripts/interfaces/opcua")]
    public class OPCUA_Interface : realvirtualBehavior
#else
    public class OPCUA_Interface : MonoBehaviour
#endif
    {
        #region PublicVariables

        [Tooltip("The address of the OPC Server (default is localhost 127.0.0.1)")]
        public string ServerIP = "127.0.0.1";

        [Tooltip("The port number for the OPC Server (default is 4840)")]
        public int ServerPort = 4840;

        public SecurityPolicy SecurityPolicy = SecurityPolicy.None;
        [Tooltip("The path of the OPC Server (default is empty)")]
        public string Path = "";

        [Tooltip("The session timeout value in milliseconds (default is 60000ms)")]
        public int SessionTimeoutMs = 60000;

        [Tooltip("The top node identifier for the OPC Server")]
        public string TopNodeId = "Demo.Static.Scalar";

        [Tooltip("Toggles the Debug Mode for the OPC Client")]
        public bool DebugMode;

        private OPCUAClient client;

#if REALVIRTUAL
        [Tooltip("Read-only property indicating the connection status of the OPC Client")] [ReadOnly]
        public bool IsConnected;

        [Tooltip("Read-only property indicating if the OPC Client is in the process of reconnecting")] [ReadOnly]
        public bool IsReconnecting;
#else
[Tooltip("Property indicating the connection status of the OPC Client")]
public bool IsConnected;

[Tooltip("Property indicating if the OPC Client is in the process of reconnecting")]
public bool IsReconnecting;
#endif

        [Tooltip("The application name of the OPC Client")]
        public string ApplicationName = "realvirtual";

        [Tooltip("The appliction URN of the OPC Client")]
        public string ApplicationURN = "urn:realvirtual";

        [Tooltip("The appliction URI of the OPC Client")]
        public string ProductURI = "uri:realvirtual";

        [Tooltip("The SubPath of the Certificates inside StreamingAssets. If empty, no certificates are used")]
        public string ClientPrivateCertificate;

        [Tooltip("The SubPath of the client certificate inside StreamingAssets. If empty, no certificates are used")]
        public string ClientPublicCertificate;

        [Tooltip("The username for the OPC Client. If blank, anonymous user will be used")]
        public string UserName = "";

        [Tooltip("The password for the User")] public string Password = "";
        [Tooltip("The signature algorithm for the password")]  public SignatureAlgorithm PasswordSignatureAlgorithm =SignatureAlgorithm.RsaOaep;
        [Tooltip("Toggles the automatic disconnection of the OPC Client when a read error occurs")]
        public bool SetDisconnectedOnReadError = true;

        [Tooltip(
            "The time in milliseconds between reconnection attempts. If set to 0, no automatic reconnections are made")]
        public int ReconnectTime = 2000;

        [Tooltip("The maximum number of nodes allowed per subscription. Set to 0 if not limited")]
        public int MaxNumberOfNodesPerSubscription;

        [Tooltip("The publishing interval for subscriptions in milliseconds")]
        public float SubscriptionPublishingIntervall = 20;

        [Tooltip("The maximum number of notifications per publish")]
        public int MaxNotificationsPerPublish = 50;

        [Tooltip("List of regex expressions that define signals that need to be automatically defined as write to the OPC UA server")]
        public List<string> RegexWriteNodes;

        [Tooltip("Flag that indicates if automatically inputs on write signals")]
        public bool AutomaticallyInputOnWriteSignals;

#if REALVIRTUAL
        [Tooltip("Automatically creates realvirtual Signals")]
        public bool CreateSignals = true;
#endif

        [Tooltip("Flag that indicates if automatically subscribes when importing new nodes")]
        public bool AutomaticallySubscribeOnImport = true;

#if REALVIRTUAL
        [Tooltip("Number of subscriptions")]
        [ReadOnly] public int NumberSubsriptions;

        [Tooltip("Number of subscribing nodes")]
        [ReadOnly] public int NumberSubscribingNodes;
#else
        [Tooltip("Number of subscriptions")]
        public int NumberSubsriptions;

        [Tooltip("Number of subscribing nodes")]
        public int NumberSubscribingNodes;
#endif
        [HideInInspector] public int CurrentNodeInSubscription;
        
        [HideInInspector] public uint CurrentSubscriptionID;
        
        [HideInInspector] public uint CurrentClientHandle = 1;
        
        public UnityEvent EventOnConnected;
        
        public UnityEvent EventOnDisconnected;

        #endregion

        #region PrivateVariables

        private int numnodes;
        private int created;
        private string laststatus;
        private float lastconnecttime;
        private bool importnodes = false;

        private Dictionary<string, OPCUANodeSubscription> NodeSubscriptions =
            new Dictionary<string, OPCUANodeSubscription>();

        private Dictionary<uint, OPCUANodeSubscription> ClientHandlesSubscriptions =
            new Dictionary<uint, OPCUANodeSubscription>();

        private List<OPCUANodeSubscription> Subscriptions = new List<OPCUANodeSubscription>();

        #endregion

        #region PublicMethods

#if REALVIRTUAL
        protected new bool hidename()
        {
            return true;
        }
#endif
        //! Connects to the OPCUA Server
        public bool Connect()
        {
            StatusCode openRes, createRes, activateRes;
            lastconnecttime = Time.unscaledTime;
            IsReconnecting = true;
            IsConnected = false;
            try
            {
                var appDesc = new ApplicationDescription(
                    ApplicationURN, ProductURI, new LocalizedText(ApplicationName),
                    ApplicationType.Client, null, null, null);

                EndpointDescription[] endpointDescs = null;
                var pubpath = "";
                var privpath = "";
                if (ClientPublicCertificate != "" && ClientPrivateCertificate != "")
                {
                    pubpath = Application.streamingAssetsPath + "/" + ClientPublicCertificate;
                    privpath = Application.streamingAssetsPath + "/" + ClientPrivateCertificate;
                    Debug.Log(
                        $"OPCUA - Using public Certificate on path [{pubpath}] and private Certificate on path [{privpath}]");
                }
                else
                {
                    Debug.Log("OPCUA Interface - Certificates pathes are empty - using no Certificates");
                }

                if (Path == "")
                    client = new OPCUAClient(ServerIP, ServerPort, SessionTimeoutMs, pubpath, privpath);
                else
                    client = new OPCUAClient(ServerIP, ServerPort, Path, SessionTimeoutMs, pubpath, privpath);

                var connectRes = client.Connect();
                if (connectRes != StatusCode.Good)
                {
                    Debug.LogError(
                        $"OPCUA Interface - Error in connecting to opcua client [{connectRes}], please check if your OPCUA server is running and reachable!");
                    IsReconnecting = false;
                    return false;
                }
                
                ApplicationDescription[] appDescs = null;
                openRes = client.OpenSecureChannel(MessageSecurityMode.None, SecurityPolicy, null);
                client.FindServers(out appDescs, new[] {"en"});
                client.GetEndpoints(out endpointDescs, new[] {"en"});

                if (DebugMode) 
                {
                    var endpoints = "";
                    foreach (var endpoint in endpointDescs)
                    {
                       endpoints = endpoints += 
                            $"Found Endpoint Url:{endpoint.EndpointUrl} SecurityPolicyUri:${endpoint.SecurityPolicyUri} \n";
                        foreach (var tokenPolicy in endpoint.UserIdentityTokens)
                        {
                            endpoints = endpoints += 
                                $"  -Token PolicyUri: {tokenPolicy.SecurityPolicyUri} Token Type :{tokenPolicy.TokenType}\n";
                        }
                    }
                    Debug.Log(endpoints);
                }
                

                if (openRes != StatusCode.Good)
                {
                    Debug.LogError($"OPCUA Interface - Error in opening secure channel [{openRes}]");
                    IsReconnecting = false;
                    return false;
                }

                createRes = client.CreateSession(appDesc, ApplicationURN, 120);
                if (createRes != StatusCode.Good)
                {
                    Debug.LogError(
                        $"OPCUA Interface - Error in creating session [{createRes}] - please check IP adress and port of your OPCUA server");
                    IsReconnecting = false;
                    return false;
                }

                if (UserName == "")
                {
                    //client.GetEndpoints(out endpointDescs, new[] {"en"});
                    var usernamePolicyDesc = "0";
                    try
                    {
                        usernamePolicyDesc = endpointDescs
                            .First(e => e.UserIdentityTokens.Any(t => t.TokenType == UserTokenType.Anonymous))
                            .UserIdentityTokens.First(t => t.TokenType == UserTokenType.Anonymous)
                            .PolicyId;
                    }
                    catch 
                    {
                        Debug.Log("OPCUA Interface - No Anonymous Token policy found - setting 0 as default");
                        usernamePolicyDesc = "0";
                    }


                    Debug.Log("OPCUA Interface - Activating Session in Anonymous mode without Username and Password");
                    activateRes = client.ActivateSession(new UserIdentityAnonymousToken(usernamePolicyDesc),
                        new[] {"en"});
                }
                else
                {
                    Debug.Log($"OPCUA Interface - Activating Session with Username [{UserName}] and Password");
                    var serverCert = endpointDescs
                        .First(e => e.ServerCertificate != null && e.ServerCertificate.Length > 0)
                        .ServerCertificate;

                    var usernamePolicyDesc = endpointDescs
                        .First(e => e.UserIdentityTokens.Any(t => t.TokenType == UserTokenType.UserName))
                        .UserIdentityTokens.First(t => t.TokenType == UserTokenType.UserName)
                        .PolicyId;

                    string algorythm = GetSignatureAlgorythm(PasswordSignatureAlgorithm);
                    
                    Debug.Log($"Using Signature Algorythm {algorythm} for Password");

                    activateRes = client.ActivateSession(
                        new UserIdentityUsernameToken(usernamePolicyDesc, UserName,
                            new UTF8Encoding().GetBytes(Password),algorythm),
                        new[] {"en"});
             
                }

                if (activateRes != StatusCode.Good)
                {
                    Debug.LogError($"OPCUA Interface - Error in activating session [{activateRes}]  Error [{client.ErrorStatus}]");
                    IsReconnecting = false;
                    return false;
                }


                client.OnSubsriptionValueChanged += OnSubsriptionValueChanged;
            }
            catch (Exception e)
            {
                IsReconnecting = false;
                Debug.LogError($"OPCUA Interface - Connection Error {e.Message}");
                return false;
            }

            client.OnConnectionClosed += CLientOnConnectionClosed;

            // Initialize all Subscription variables
            NumberSubsriptions = 0;
            NumberSubscribingNodes = 0;
            CurrentNodeInSubscription = 0;
            CurrentClientHandle = 1;
            NodeSubscriptions = new Dictionary<string, OPCUANodeSubscription>();
            ClientHandlesSubscriptions =
                new Dictionary<uint, OPCUANodeSubscription>();
            Subscriptions = new List<OPCUANodeSubscription>();
            IsReconnecting = false;
            IsConnected = true;

            Debug.Log($"OPCUA Interface - connected to OPCUA server [{ServerIP}] on port [{ServerPort}]");
            if (Application.isPlaying)
                Invoke("OnConnected",
                    0); // Invoke connected a little bit later so that all subscriptions for Connect event have been made

            return true;
        }
        

        private void CLientOnConnectionClosed()
        {
            if (DebugMode)
                Debug.Log($"OPCUA Interface - Client Disconnected");
            IsConnected = false;
        }


        //! Gets an OPCUA_Node  with the NodeID in all the Childrens of the Interface 
        public OPCUA_Node GetOPCUANode(string nodeid)
        {
            OPCUA_Node[] children = transform.GetComponentsInChildren<OPCUA_Node>();

            foreach (var child in children)
            {
                if (child.NodeId == nodeid)
                {
                    return child;
                }
            }

            return null;
        }

        //! Imports all OPCUANodes under TopNodeId and creates GameObjects.
        //! If GameObject with NodeID is already existing the GameObject will be updated.
        //! Does not deletes any Nodes. If Realvirtual Framework is existent (Compiler Switch REALVIRTUAL) also Realvirtual
        //! PLCInputs and PLCOutputs are created or updated or all nodes with suitable data types.
        public void ImportNodes()
        {
            numnodes = 0;
            created = 0;
            ImportNodes(TopNodeId);
            Debug.Log($"OPCUA Interface - imported {numnodes} OPCUA nodes, created {created} new nodes");
            CreateG4ASignals();
        }

        public void EditorImportNodes()
        {
#if UNITY_EDITOR
            EditorUtility.DisplayProgressBar("Importing Nodes", "Please wait, this might take some time", 0.2f);
#endif
            importnodes = true;
            if (!Connect())
            {
#if UNITY_EDITOR
                EditorUtility.ClearProgressBar();
#endif
            }


            ImportNodes();
            Disconnect();
            importnodes = false;
        }

        //! Imports all nodes under one TopNodeID 
        public void ImportNodes(string nodeid)
        {
            if (IsConnected == false)
                return;

            BrowseResult[] browseResults;
            GameObject topobject = this.gameObject;

            if (nodeid != TopNodeId)
                topobject = GetOPCUANode(nodeid).gameObject;

            NodeId topnodeid = NodeId.TryParse(nodeid);

            if (nodeid == "")
                topnodeid = NodeId.TryParse("ns=0;i=84");
            string status = "";
            client.Browse(new BrowseDescription[]
            {
                new BrowseDescription(
                    topnodeid,
                    BrowseDirection.Forward,
                    NodeId.Zero,
                    true, 0xFFFFFFFFu, BrowseResultMask.All)
            }, 10000, out browseResults);
            List<OPCUA_Node> nodes = new List<OPCUA_Node>();

            var i = 0;
            foreach (var reference in browseResults[0].Refs)
            {
#if UNITY_EDITOR
                i++;
                var max = browseResults[0].Refs.Length;
                EditorUtility.DisplayProgressBar("Importing Nodes",
                    $"Creating Nodes {i} of {max} for {nodeid}, this might take some time", 0.8f);
#endif
                GameObject newnode;
                OPCUA_Node info = null;
                bool newcreated = false;
                var currnodeid = reference.TargetId;
                if (DebugMode)
                    Debug.Log(
                        $"OPCUA Interface - browse node [{reference.DisplayName.Text}], nodeclass [{reference.NodeClass.ToString()}]");
                if (reference.NodeClass == NodeClass.Object || (reference.NodeClass == NodeClass.Variable ||
                                                                (reference.NodeClass == NodeClass.ObjectType)))
                {
                    var name = reference.DisplayName.Text;

                    info = GetOPCUANode(reference.TargetId.ToString());

                    if (info == null)
                    {
                        newnode = new GameObject(name);
                        newnode.transform.parent = topobject.transform;
                        info = newnode.AddComponent<OPCUA_Node>();
                        newcreated = true;
                        created++;
                    }

                    info.NodeId = reference.TargetId.ToString();
                    info.Interface = this;
                    info.IdentifierType = reference.TargetId.IdType.ToString();
                    info.Identifier = reference.TargetId.StringIdentifier;
                    info.UserAccessLevel = ReadAccessLevel(currnodeid, ref status).ToString();
                    info.Status = status;

                    numnodes++;
                    if (DebugMode)
                        Debug.Log($"OPCUA Interface - node [{info.NodeId}] of type [{reference.NodeClass}] imported");
                }

                /// Folder
                if (reference.NodeClass == NodeClass.Object || reference.NodeClass == NodeClass.Variable)
                {
                    if (info.NodeId == "ns=3;s=urn:desktop-aofatq8:Siemens:Simit.OpcUaServer.OPC")
                        Debug.Log("Simit");
                    info.Type = "Object";
                    ImportNodes(info.NodeId);
                }

                /// Variable
                if (reference.NodeClass == NodeClass.Variable)
                {
                    info.Status = status;
                    info.Type = ReadType(currnodeid, ref status);
                    if (info.ReadNode() == false)
                    {
                        // If node cant be read dont subscribe
                        info.SubscribeValue = false;
                    }
                    else
                    {
                        if (newcreated)
                            SetNodeSubscriptionParameters(info);
                    }
                }
            }
#if UNITY_EDITOR
            EditorUtility.ClearProgressBar();
#endif
        }

        //! Subscribes to an OPCUA node, delegate function gets called when node value is updated on OPCUA server
        public OPCUANodeSubscription Subscribe(string nodeid, NodeUpdateDelegate del)
        {
            if (client == null)
            {
                Debug.LogError("OPCUA Interface - Error - please first connect before subscibing");
                return null;
            }

            OPCUANodeSubscription subscription = new OPCUANodeSubscription();

            // Check if already same node is subscribed
            if (NodeSubscriptions.ContainsKey(nodeid))
            {
                // Just add method to subscription
                subscription = NodeSubscriptions[nodeid];
                subscription.UpdateDelegates.Add(del);
                if (DebugMode)
                    Debug.Log(
                        $"OPCUA Interface, Subscription added to  Subsriction for Node {nodeid} created with SubscriptionID {subscription.SubscriptionId} and ClientHandle {subscription.ClientHandle}");
            }
            else
            {
                try
                {
                    // create new subscription
                    NumberSubscribingNodes++;
                    Subscriptions.Add(subscription);
                    NodeSubscriptions.Add(nodeid, subscription);
                    subscription.UpdateDelegates = new List<NodeUpdateDelegate>();
                    subscription.UpdateDelegates.Add(del);
                    subscription.NodeId = nodeid;
                    if (CurrentNodeInSubscription == 0)
                    {
                        uint maxnot = (uint) (int) MaxNotificationsPerPublish;
                        var status = client.CreateSubscription(SubscriptionPublishingIntervall, maxnot, true, 0,
                            out CurrentSubscriptionID);
                        if (DebugMode)
                            Debug.Log("Created new Subscription " + CurrentSubscriptionID + " with Status " +
                                      status.ToString());
                        if (status != StatusCode.Good)
                            Debug.LogError(
                                $"OPCUA Interface, Error in creating new Subscription {status}, maybe max number of subscriptions on your server is reached");
                        NumberSubsriptions++;
                    }

                    subscription.SubscriptionId = CurrentSubscriptionID;
                    CurrentNodeInSubscription++;

                    if (CurrentNodeInSubscription >= MaxNumberOfNodesPerSubscription)
                        CurrentNodeInSubscription = 0;
                    MonitoredItemCreateResult[] monitorCreateResults;
                    NodeId id = NodeId.TryParse(nodeid);
                    var statusmon = client.CreateMonitoredItems(CurrentSubscriptionID, TimestampsToReturn.Both,
                        new MonitoredItemCreateRequest[]
                        {
                            new MonitoredItemCreateRequest(
                                new ReadValueId(id, NodeAttribute.Value, null, new QualifiedName()),
                                MonitoringMode.Reporting,
                                new MonitoringParameters(CurrentClientHandle, 0, null, 100, false)),
                        }, out monitorCreateResults);
                    if (statusmon != StatusCode.Good)
                        Debug.LogError(
                            $"OPCUA Interface, Error in creating new Subscription for node {nodeid}, returned status {statusmon}, maybe max number of subscriptions on your server is reached");
                    subscription.ClientHandle = CurrentClientHandle;
                    ClientHandlesSubscriptions.Add(CurrentClientHandle, subscription);
                    if (DebugMode)
                        Debug.Log(
                            $"OPCUA Interface, Subscription Number {NumberSubsriptions} with NodeNumber {CurrentNodeInSubscription} for Node {nodeid} created with SubscriptionID {CurrentSubscriptionID} and ClientHandle {CurrentClientHandle} with Status {statusmon.ToString()}");
                    CurrentClientHandle++;
                }
                catch (Exception e)
                {
                    Debug.LogError("OPCUA Interface, Error in creating Subscripotion " + e.Message);
                    throw;
                }
            }

            return subscription;
        }
      


        //! Reads a Node value and returns it as object
        public object ReadNodeValue(OPCUA_Node node)
        {
            object value = null;
            var status = "";
            value = ReadNodeValue(node.NodeId, ref status);
            node.Status = status;
            if (!ReferenceEquals(value, null))
            {
                node.Value = value.ToString();
                node.SignalValue = value;
            }
            else
            {
                node.Value = "";
                node.SignalValue = null;
                node.Status = "Connection Error";
            }

            return value;
        }

        //! Reads a Node value based on its id and returns it as object
        public object ReadNodeValue(string nodeid)
        {
            string status = "";
            return ReadNodeValue(nodeid, ref status);
        }

        //! Reads a Node value based on its id and returns it as object, a status reference is passed 
        public object ReadNodeValue(string nodeid, ref string status)
        {
            try
            {
                object value = null;
                NodeId id = NodeId.TryParse(nodeid);
                value = ReadValue(id, ref status);
                return value;
            }
            catch (Exception e)
            {
                Debug.Log($"OPCUA Interface - Error reading node[{nodeid}] {e.Message}");
                return null;
            }
        }

        //! Writes a value to an OPCUA node with its nodeid
        public bool WriteNodeValue(string nodeid, object value)
        {
            string status = "";
            return WriteNodeValue(nodeid, value, ref status);
        }

        //! Writes a value to an OPCUA node with its node object
        public bool WriteNodeValue(OPCUA_Node node, object value)
        {
            string status = "";
            var success = WriteNodeValue(node.NodeId, value, ref status);
            node.Status = status;
            return success;
        }
        //! Writes a value to an OPCUA node with its node id and a status variable reference

        public bool WriteNodeValue(string nodeid, object value, ref string status)
        {
            try
            {
                var connected = false;
                if (!IsConnected)
                {
                    return false;
                }

                NodeId id = NodeId.TryParse(nodeid);
                var success = WriteValue(id, value, ref status);
                if (DebugMode)
                {
                    if (success)
                        Debug.Log($"OPCUA Interface - Writing node[{nodeid}] with value {value.ToString()} succesfull");
                    else
                        Debug.Log(
                            $"OPCUA Interface - Error writing node[{nodeid}] with value {value.ToString()} NOT SUCCESSFULL");
                }

                if (connected)
                    Disconnect();
                return success;
            }
            catch (Exception e)
            {
                Debug.Log($"OPCUA Interface - Error writing node[{nodeid}] {e.Message}");
                return false;
            }
        }

        public void UnSubscribe(OPCUANodeSubscription subscription)
        {
            if (subscription == null)
                return;

            NodeSubscriptions.Remove(subscription.NodeId);
            var clienthandle = subscription.ClientHandle;
            var handle = new uint[1];
            handle[0] = clienthandle;
            uint[] respStatuses;
            client.DeleteMonitoredItems(subscription.SubscriptionId, handle, out respStatuses);
            handle[0] = subscription.SubscriptionId;
            client.DeleteSubscription(handle, out respStatuses);
            Subscriptions.Remove(subscription);
            CurrentNodeInSubscription = 0;
        }

        //! Disconnects from the OPCUA server
        public void Disconnect()
        {
            if (!IsConnected)
                return;

            uint[] respStatuses;
            uint subscriptionid = 0;
            List<uint> clienthandles = new List<uint>();
            var i = 1;
            foreach (var subscription in Subscriptions)
            {
                if (subscriptionid == 0)
                    subscriptionid = subscription.SubscriptionId;
                if (subscription.SubscriptionId != subscriptionid || i == Subscriptions.Count)
                {
                    client.DeleteMonitoredItems(subscriptionid, clienthandles.ToArray(), out respStatuses);
                    client.DeleteSubscription(new[] {subscriptionid}, out respStatuses);
                    clienthandles.Clear();
                }

                i++;
                clienthandles.Add(subscription.ClientHandle);
            }

            NumberSubsriptions = 0;
            NumberSubscribingNodes = 0;
            CurrentClientHandle = 0;
            CurrentNodeInSubscription = 0;

            client.Dispose();
            IsConnected = false;
            OnDisconnected();
            Debug.Log($"OPCUA Interface - disconnected from OPCUA server [{ServerIP}]");
        }

        #endregion

        #region PrivateMethods

        private void OnConnected()
        {
            IsConnected = client.IsConnected;
            EventOnConnected.Invoke();
            if (DebugMode) Debug.Log("OPCUA - Connected (Method Connected) " + ServerIP);
#if REALVIRTUAL
            var signals = GetComponentsInChildren<Signal>();
            foreach (var signal in signals)
            {
                signal.SetStatusConnected(true);
            }

            if (realvirtualController != null)
                realvirtualController.OnConnectionOpened(this.gameObject);
#endif
        }

        private void OnConnectionClosedByServer()
        {
            Disconnect();
        }


        private void OnDisconnected()
        {
            EventOnDisconnected.Invoke();
#if REALVIRTUAL
            var signals = GetComponentsInChildren<Signal>();
            foreach (var signal in signals)
            {
                signal.SetStatusConnected(false);
            }

            if (realvirtualController != null)
                realvirtualController.OnConnectionClosed(this.gameObject);
#endif
        }


        private object ReadValue(NodeId nodeid, ref String status)
        {
            StatusCode readres = StatusCode.BadDisconnect;
            DataValue[] dvs = null;
            try
            {
                readres =
                    client.Read(
                        new ReadValueId[]
                            {new ReadValueId(nodeid, NodeAttribute.Value, null, new QualifiedName(0, null))},
                        out dvs);
                if (readres == StatusCode.BadConnectionClosed)
                {
                    OnConnectionClosedByServer();
                }

                status = readres.ToString();
                if (dvs[0] != null)
                {
                    if (ReferenceEquals(dvs[0].Value, null))
                    {
                        if (importnodes)
                        {
                            Debug.LogWarning(
                                $"OPCUA Interface - NodeID [{nodeid}] returns NULL value or is not existing");
                        }
                        else
                        {
                            if (SetDisconnectedOnReadError)
                            {
                                Debug.LogWarning(
                                    $"OPCUA Interface - NodeID [{nodeid}] returns NULL value or is not existing  - setting Interface to disconnected");
                                IsConnected = false;
                                client.Disconnect();
                            }
                            else
                            {
                                Debug.LogWarning(
                                    $"OPCUA Interface - NodeID [{nodeid}] returns NULL value or is not existing");
                            }
                        }
                    }

                    return dvs[0].Value;
                }
                else
                    return null;
            }
            catch
            {
                if (DebugMode)
                    Debug.LogWarning("OPCU Interface - Not able to read value " + nodeid + " Status " +
                                     readres.ToString());
                return null;
            }
        }

        private bool WriteValue(NodeId nodeid, object value, ref String status)
        {
            uint[] respStatuses;
            DataValue datavalue = new DataValue(value);
            client.Write(new WriteValue[]
            {
                new WriteValue(
                    nodeid, NodeAttribute.Value,
                    null, datavalue)
            }, out respStatuses);

            if (respStatuses != null)
            {
                StatusCode resultcode = (StatusCode) respStatuses[0];
                status = resultcode.ToString();
                if (resultcode == StatusCode.BadConnectionClosed)
                    OnConnectionClosedByServer();
                if (resultcode == StatusCode.Good)
                    return true;
                else
                {
                    if (DebugMode)
                    {
                        Debug.Log($"Error in writing {nodeid} with Statuscode {resultcode.ToString()}");
                    }

                    return false;
                }
            }
            else
            {
                if (DebugMode)
                {
                    Debug.Log($"Error in writing {nodeid} - no status code given back");
                }

                return true;
            }
        }

        private string ReadDisplayName(NodeId nodeid, ref String status)
        {
            DataValue[] dvs = null;
            var readRes =
                client.Read(
                    new ReadValueId[]
                        {new ReadValueId(nodeid, NodeAttribute.DisplayName, null, new QualifiedName(0, null))},
                    out dvs);
            status = readRes.ToString();
            LocalizedText text = (LocalizedText) (dvs[0].Value);
            if (text != null)
                return text.Text;
            else
                return "";
        }

        private string ReadAccessLevel(NodeId nodeid, ref String status)
        {
            DataValue[] dvs = null;
            var readRes =
                client.Read(
                    new ReadValueId[]
                        {new ReadValueId(nodeid, NodeAttribute.AccessLevel, null, new QualifiedName(0, null))},
                    out dvs);
            status = readRes.ToString();
            if (dvs == null)
                return "";
            if (dvs[0].Value != null)
            {
                string dvsval = dvs[0].Value.ToString();
                return Enum.Parse(typeof(AccessLevel), dvsval).ToString();
            }
            else
            {
                return "";
            }
        }

        private string ReadType(NodeId nodeid, ref String status)
        {
            DataValue[] dvs = null;
            var readRes =
                client.Read(
                    new ReadValueId[]
                        {new ReadValueId(nodeid, NodeAttribute.DataType, null, new QualifiedName(0, null))}, out dvs);
            status = readRes.ToString();
            if (dvs == null)
                return "";
            var typereference = (NodeId) (dvs[0].Value);
            if (typereference == null)
                return "";
            var readType = ReadDisplayName(typereference, ref status);
            if (readType == null)
                return "";

            return readType.ToString();
        }

        private void OnSubsriptionValueChanged(uint subid, uint clienthandle, object value)
        {
            var subscription = ClientHandlesSubscriptions[clienthandle];
            if (DebugMode)
            {
                if (value != null)
                    Debug.Log(
                        $"OPCUA Interface - Subscription with CLientHandle {clienthandle} Value Changed {subscription.NodeId} Value [{value.ToString()}]");
                else
                    Debug.Log(
                        $"OPCUA Interface - Subscription with CLientHandle {clienthandle} Value Changed {subscription.NodeId} Value [NULL]");
            }

            foreach (var updatedel in subscription.UpdateDelegates)
            {
                updatedel.Invoke(subscription, value);
            }
        }
        
        private string GetSignatureAlgorythm(SignatureAlgorithm algorithm)
        {
            switch (algorithm)
            {
                case SignatureAlgorithm.Sha1:
                    return "http://www.w3.org/2000/09/xmldsig#rsa-sha1";
                case SignatureAlgorithm.Sha256:
                    return "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";
                case SignatureAlgorithm.Rsa15:
                    return "http://www.w3.org/2001/04/xmlenc#rsa-1_5";
                case SignatureAlgorithm.RsaOaep:
                    return "http://www.w3.org/2001/04/xmlenc#rsa-oaep";
                case SignatureAlgorithm.RsaOaep256:
                    return "http://opcfoundation.org/UA/security/rsa-oaep-sha2-256";
                case SignatureAlgorithm.RsaPss256:
                    return "http://opcfoundation.org/UA/security/rsa-pss-sha2-256";
                default:
                    return "http://www.w3.org/2001/04/xmlenc#rsa-oaep";
            }
        }

        
        private void SetNodeSubscriptionParameters(OPCUA_Node node)
        {
            if (node == null)
                return;

            if (node.Status != "Good")
            {
                node.WriteValue = false;
                node.ReadValue = false;
                node.SubscribeValue = false;
                node.PollValue = false;
                return;
            }

            var IsWrite = false;
            if (RegexWriteNodes != null)
            {
                if (RegexWriteNodes.Count > 0)
                {
                    foreach (var regexstring in RegexWriteNodes)
                    {
                        Regex regex = new Regex(regexstring);
                        if (regex.IsMatch(node.NodeId))
                        {
                            IsWrite = true;
                        }
                    }
                }
            }

            if (AutomaticallyInputOnWriteSignals)
            {
                if (node.UserAccessLevel.Contains("CurrentWrite"))
                {
                    IsWrite = true;
                }
            }

            if (IsWrite)
            {
                node.ReadValue = false;
                node.WriteValue = true;
                node.SubscribeValue = false;
            }
            else
            {
                node.ReadValue = true;
                node.WriteValue = false;
                node.SubscribeValue = AutomaticallySubscribeOnImport;
            }
        }

        private void CreateG4ASignals()
        {
#if REALVIRTUAL
            OPCUA_Node[] opcuanodes = GetComponentsInChildren<OPCUA_Node>();

            if (CreateSignals == false)
                return;

            foreach (var node in opcuanodes)
            {
                node.UpdatePLCSignal();
                node.Awake();
            }
#endif
        }

        private void OnEnable()
        {
            importnodes = false;
            Connect();
        }

        private void OnDisable()
        {
            Disconnect();
        }

        private void OnApplicationQuit()
        {
            Disconnect();
        }

        private void Update()
        {
            if (client != null && client.IsConnected == false)
                IsConnected = false;

            if (!IsConnected && !IsReconnecting && ReconnectTime > 0)
            {
                var deltatime = Time.time - lastconnecttime;
                if (deltatime > ReconnectTime / 1000)
                    Connect();
            }
        }

        #endregion
    }
}