// realvirtual.io (formerly game4automation) (R) a Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 realvirtual GmbH - Usage of this source code only allowed based on License conditions see https://realvirtual.io/unternehmen/lizenz  

using System.Collections.Generic;
using UnityEngine;
using System;
using System.Collections;
using System.IO;
using NaughtyAttributes;
using System.Text.RegularExpressions;
using ORiN2.bCAP;
using UnityEditor;

namespace realvirtual
{
     [ExecuteInEditMode]
    public class DensoInterface : InterfaceThreadedBaseClass
    {
        private const int NUM_JOINTS = 6;

        #region public variables

        // visualization
        [Header("Visualization")] public bool ShowGrid = false;
        [ShowIf("ShowGrid")] public int GridSize = 10;
        [OnValueChanged("GetSafetyAreas")] [ShowIf("IsConnected")]public bool ShowSafetyAreas = false;
        [Dropdown("getSafetyAreaDropdown")]  [OnValueChanged("ChangeSafetyAreas")] [ShowIf("ShowSafetyAreas")] 
        public string SafetyArea;
        [ShowIf("IsConnected")]public bool ShowAreas = false;
        [ShowIf("ShowAreas")][OnValueChanged("ChangeAreaNumber")] public int AreaNumber;
        [ShowIf("IsConnected")]public bool ShowTool = false;
        [ShowIf("ShowTool")][OnValueChanged("ChangeToolNumber")] public int ToolNumber = -1;
        [ShowIf("IsConnected")]public bool ShowWork = false;
        [ShowIf("ShowWork")][OnValueChanged("ChangeWorkNumber")]  public int WorkNumber = -1;

        public RobotArea Area = new RobotArea(-1);
        // robot connection
        [Header("Connection")] public string ControllerName="";
        public ControllerType RobotControllerType;
        public IPAddress ipAdress;
        public string WincapsProject;
        [OnValueChanged("RobotConnection")] public bool ConnectRealRobot = false;
        public bool DebugMode = false;
        [Header("Options")]
        public bool RoundSyncPositions = true;
        [HideInInspector] public int SafetyAreaNumber;
        #endregion
        
        #region private variables

        [Header("Kinematic")] public GameObject RobotFlange;
       
        [ReorderableList] public List<realvirtual.Drive> RobotAxis;

        [ReadOnly] public int NumberInputs;
        [ReadOnly] public int NumberOutputs;

        private string _safetyDataFile;
        protected double[] _curJnt = new double[NUM_JOINTS];
        protected double[] _curPos = new double[NUM_JOINTS];
        [HideInInspector] public List<RobotSafetyArea> SafetyAreasList = new List<RobotSafetyArea>();
        [HideInInspector] public List<RobotSafetyArea> SafetyFencesList = new List<RobotSafetyArea>();
        [HideInInspector] public Matrix4x4 _toolFrame = Matrix4x4.identity;
        [HideInInspector] public Matrix4x4 _workFrame = Matrix4x4.identity;

        // VRC variables
        protected bCAPClient _clientVRC = null;
     
        protected int _ctrlVRCDrives = 0;
        protected int _ctrlVRCIOs = 0;
        protected int _robVRC = 0;

        // real robot variables
        protected bCAPClient _clientRobot = null;
        protected int _ctrlDrives = 0;
        protected int _ctrlIOs = 0;
        protected int _rob = 0;
        
        private bool iohandlesready = false;
        private string[] safetyareanames = new string[0];

        [SerializeField] [HideInInspector] private Material _matLine = null;
        [SerializeField] [HideInInspector] private Material _matOverlay = null;
        [SerializeField] [HideInInspector]  private Material _matGrid = null;
        [HideInInspector] public static Color _colorGrid = Color.grey;
        protected static Color _colorSafetyAreas = new Color(0.98f, 0.82f, 0.56f, 1.0f); // orange
        protected static Color _colorRobotAreas = new Color(0.58f, 0.87f, 0.56f, 1.0f); // light green

        private static Vector3 _screenPos = Vector3.zero;

        private Hashtable iohandles;
        private List<string> threadmessages = new List<string>();
        private List<string> Messages = new List<string>();
        private bool firstioread = true;

        [HideInInspector] public Pose _robotWork;
        [HideInInspector] public Pose _robotTool;

        // colors for reference systems
        protected static Color _colorXAxis = Color.red;
        protected static Color _colorYAxis = Color.green;
        protected static Color _colorZAxis = Color.blue;
        
        #endregion
        
        #region customdatatypes
        
        // 2 types of robot controller : RC8 or R9
        public enum ControllerType
        {
            RC8 = 8,
            RC9 = 9
        }

        public struct RobotArea
        {
            public Matrix4x4 Transform;
            public Pose UnityPose;
            public Vector3 Size;
            public int Num;
            public int IOLine;
            public int PosVar;
            public int ErrorType;
            public bool Enabled;

            public RobotArea(int number)
            {
                this.Num = number;
                this.IOLine = -1;
                this.PosVar = -1;
                this.ErrorType = -1;
                this.Enabled = false;
                this.Transform = Matrix4x4.identity;
                this.UnityPose = new Pose();
                this.Size = Vector3.zero;
            }
        }

        #endregion

        #region ui update functions
        private List<string> getSafetyAreaDropdown()
        {
            if (safetyareanames == null)
            {    
                safetyareanames = new string[1];
            }
            if (safetyareanames.Length == 0)
            {
                safetyareanames = new string[1];
            }
            safetyareanames[0] = "None";
            return new List<string>(safetyareanames);
          
        }

        private void GetSafetyAreas()
        {
            if (ShowSafetyAreas)
             safetyareanames = UpdateSafetyAreas(true);
            SafetyAreaNumber = -2;
        }
        
        private void ChangeSafetyAreas()
        {
            if (SafetyArea == "Fences")
                SafetyAreaNumber = -1;
            else
                SafetyAreaNumber  =  Array.IndexOf(safetyareanames,SafetyArea)-2;
        }

        private void ChangeAreaNumber()
        {
            GetRobotArea(AreaNumber, true);
        }
        
        private void ChangeWorkNumber()
        {
            GetRobotWork(WorkNumber, true);
        }
        
        private void ChangeToolNumber()
        {
            GetRobotTool(ToolNumber, true);
        }

        private string GetControllerName()
        {
            int num = 1;
            string controllername = "Robot1";
            // try controllername "Robot1", check if it is existing and if it is existing add 1 and test again
            bool found = true;
            var densoInterfaces = FindObjectsOfType<DensoInterface>();
            while (found)
            {
                controllername = "Robot" + num;
                // check if name is already existing in one of the denso interfaces
                found = false;
                foreach (var densoInterface in densoInterfaces)
                {
                    if (densoInterface.ControllerName == controllername && densoInterface != this)
                    {
                        found = true;
                        break;
                    }
                }

                num++;
            }

            return controllername;
        }

        

        #endregion
        
        #region standardinterfacemethods

        public override void OpenInterface()
        {
           
            iohandlesready = false;
            firstioread = true;
            OpenConnection();
          
            UpdateInterfaceSignals(ref NumberInputs, ref NumberOutputs);
            if (Application.isPlaying)
            {
                // Start Thread
                base.OpenInterface();
            }
            SetIOHashtable();
        }

        public void SetGridSize(int gridSize)
        {
            GridSize = gridSize;
        }

        public override void CloseInterface()
        {
            iohandlesready = false;
            CloseConnection();
            base.CloseInterface();
        }

        protected override void CommunicationThreadUpdate()
        {
            UpdateRobotJoints();
            if (threadmessages.Count > 0)
                lock (threadmessages)
                {
                    Messages.AddRange(threadmessages);
                }
        }

        protected override void SecondCommunicationThreadUpdate()
        {
            SetRobotInputs();
            GetRobotOutputs();
            if (threadmessages.Count > 0)
                lock (threadmessages)
                {
                    Messages.AddRange(threadmessages);
                }
        }

        #endregion

        #region communication methods
        private void SetIOHashtable()
        {
            if (!IsConnected) return;

            iohandles = new Hashtable();
            foreach (var interfacesignal in InterfaceSignals)
            {
                var type = interfacesignal.Signal.GetType();
                var name = interfacesignal.Signal.Name;
                if (name == "")
                    name = interfacesignal.Signal.name;
                var numberstring = Regex.Match(name, @"\d+").Value;
                var symname = "";
                if (type == typeof(PLCOutputBool) || type == typeof(PLCInputBool))
                {
                    symname = "IO" + numberstring;
                }

                if (type == typeof(PLCOutputInt) || type == typeof(PLCInputInt))
                {
                    symname = "I" + numberstring;
                }

                if (type == typeof(PLCOutputFloat) || type == typeof(PLCInputFloat))
                {
                    symname = "F" + numberstring;
                }

                if (type == typeof(PLCOutputText) || type == typeof(PLCInputText))
                {
                    symname = "S" + numberstring;
                }

                if (type == typeof(PLCOutputTransform) || type == typeof(PLCInputTransform))
                {
                    symname = "P" + numberstring;
                }


                var handle = 0;
                if (!ConnectRealRobot)
                    handle = _clientVRC.Controller_GetVariable(_ctrlVRCIOs, symname, "@IfNotMember=true");
                else
                {
                    /// TODO Implement real robot IOS
                    throw new NotImplementedException();
                }

                iohandles.Add(interfacesignal, handle);
            }

            iohandlesready = true;
        }

        public object GetIOValue(InterfaceSignal interfacesignal)
        {
            try
            {
                var client = _clientVRC;
                if (!iohandles.ContainsKey(interfacesignal))
                {
                    lock (threadmessages)
                    {
                        threadmessages.Add(" No IO Handle for  signal [" + interfacesignal.Name +
                                           "]");
                    }
                    return null;
                }

                var handle = (int) iohandles[interfacesignal];
                if (ConnectRealRobot)
                {
                    client = _clientRobot;
                    throw new NotImplementedException();
                }

                if (client != null)
                {
                    return client.Variable_GetValue(handle);
                }
                else
                {
                    lock (threadmessages)
                    {
                        threadmessages.Add(" No active connection with VRC (robot controller)");
                    }

                    return null;
                }
            }
            catch (Exception ex)
            {
                // error when writing IOs
                lock (threadmessages)
                {
                    threadmessages.Add(" Exception for Signal [" + interfacesignal.Name + " ] " + ex);
                }

                return null;
            }
        }

        public void SetIOValue(InterfaceSignal interfacesignal, object value)
        {
            try
            {
                var client = _clientVRC;
                if (!iohandles.ContainsKey(interfacesignal))
                {
                    lock (threadmessages)
                    {
                        threadmessages.Add("No IO Handle for the signal [" + interfacesignal.Name +
                                           "]");
                    }

                    return;
                }

                var handle = (int) iohandles[interfacesignal];
                if (ConnectRealRobot)
                {
                    client = _clientRobot;
                    throw new NotImplementedException();
                }

                if (client != null)
                {
                    client.Variable_PutValue(handle, value);
                }
                else
                {
                    lock (threadmessages)
                    {
                        threadmessages.Add(" No active connection with VRC (robot controller)");
                    }
                }
            }
            catch (Exception ex)
            {
                // error when writing IOs
                lock (threadmessages)
                {
                    threadmessages.Add(" Exception for Signal [" + interfacesignal.Name + " ] " + ex);
                }
                return;
            }
        }

        void GetRobotOutputs(bool onlyalltranforms = false)
        {
            if (iohandlesready == false) return;
            foreach (var interfacesignal in InterfaceSignals)
            {

                if ((interfacesignal.Direction == InterfaceSignal.DIRECTION.OUTPUT || onlyalltranforms) &&
                     interfacesignal.Signal.Settings.Active )
                {
                    var value = GetIOValue(interfacesignal);
                    if (value != null)
                    {
                        var type = interfacesignal.Signal.GetType();
                        if (type == typeof(PLCInputTransform) || type == typeof(PLCOutputTransform))
                        {
                            float[] fl = new float[7];
                            fl = (float[]) value;
                            double[] doubleArray = Array.ConvertAll(fl, x => (double)x);
                            Pose pose = DensoTools.RobotBase2UnityPose(doubleArray,null,RoundSyncPositions);
                           
                            interfacesignal.Signal.SetValue(pose);
                        }
                        else
                        {
                            if (!onlyalltranforms)
                                interfacesignal.Signal.SetValue(value);
                        }
                    }
                }
            }

            if (firstioread) firstioread = false;
        }

        void SetRobotInputs(bool onlyalltranforms=false)
        {
            if (iohandlesready == false) return;
            foreach (var interfacesignal in InterfaceSignals)
            {
                if ((interfacesignal.Direction == InterfaceSignal.DIRECTION.INPUT || onlyalltranforms) &&
                    interfacesignal.Signal.Settings.Active)
                {
                    var currvalue = interfacesignal.Signal.GetValue();
                    if (!object.Equals(interfacesignal.LastValue,currvalue))
                    {
                        if (DebugMode)
                            Debug.Log("Signal Robot Input " + interfacesignal.Name + " changed!");
                        var type = interfacesignal.Signal.GetType();
                        if (type == typeof(PLCInputTransform) || type == typeof(PLCOutputTransform))
                        {
                            float[] fl = new float[7];
                            var sig = (Pose) (interfacesignal.Signal.GetValue());
                            var doubles = DensoTools.UnityPose2RobotBase(sig, null, RoundSyncPositions);
                            SetIOValue(interfacesignal, doubles);
                        }
                        else
                        {
                            if (!onlyalltranforms)
                                SetIOValue(interfacesignal, interfacesignal.Signal.GetValue());
                        }
                        interfacesignal.LastValue = currvalue;
                    }
                }
            }
        }

        private void SetToCurrentTransforms(bool inputs, bool outputs)
        {
            foreach (var interfacesignal in InterfaceSignals)
            {
                var type = interfacesignal.Signal.GetType();
                if (type == typeof(PLCInputTransform) && inputs)
                {
                    var signal = (PLCInputTransform) interfacesignal.Signal;
                    signal.SetValueToTransform();
                }
                ;
                if (type == typeof(PLCOutputTransform) && outputs)
                {
                    var signal = (PLCOutputTransform) interfacesignal.Signal;
                    signal.SetValueToTransoform();
                }
            }
        }

        private void SetToCurrentPoses(bool inputs, bool outputs)
        {
            foreach (var interfacesignal in InterfaceSignals)
            {
                var type = interfacesignal.Signal.GetType();
                if (type == typeof(PLCInputTransform) && inputs)
                {
                    var signal = (PLCInputTransform) interfacesignal.Signal;
                    signal.SetTransformFromValue();
                }
                type = interfacesignal.Signal.GetType();
                if (type == typeof(PLCOutputTransform) && outputs) 
                {
                    var signal = (PLCOutputTransform) interfacesignal.Signal;
                    signal.SetTransformFromValue();
                }
            }
        }

        [Button("Sync IOs")]
        public void SyncIOs()
        {
            OpenInterface();
            SetToCurrentTransforms(true,false);
            SetRobotInputs();
            GetRobotOutputs();
            SetToCurrentPoses(false,true);
            PrintThreadMessages();
            CloseInterface();
        
        }
        
        [Button("Sync all Transforms from Robot")]
        public void SyncFromRobot()
        {
            OpenInterface();
            GetRobotOutputs(true);
            SetToCurrentPoses(true,true);
            PrintThreadMessages();
            CloseInterface();
        
        }

        [Button("Sync all Transforms to Robot")]
        public void SyncToRobot()
        {
            OpenInterface();
            SetToCurrentTransforms(true,true);
            SetRobotInputs(true);
            PrintThreadMessages();
            CloseInterface();
        
        }

        [Button("Get Robot Drives")]
        public void GetRobotDrives()
        {
            /* Get reference to robot joints components */
            RobotAxis.Clear();
            int foundDrives = 0;
            GameObject robotJoint = gameObject;
            for (int i = 0; i < NUM_JOINTS; i++)
            {
                robotJoint = robotJoint.transform.Find("Axis" + (i + 1).ToString()).gameObject;
                if (robotJoint != null)
                {
                    RobotAxis.Add(robotJoint.GetComponent<realvirtual.Drive>());
                    foundDrives++;
                }
            }


            if (foundDrives != NUM_JOINTS)
            {
                Debug.LogError("[" + gameObject.name + "] : Found " + foundDrives +
                               " drive components, but expected robot joints are " + NUM_JOINTS);
            }
        }
        
        private void UpdateRobotJoints()
        {
            try
            {
                bool updateRobotDrives = false;
                if (ConnectRealRobot && (_clientRobot != null) && (_rob != 0))
                {
                    // read robot joints values
                    _curJnt = (double[]) _clientRobot.Robot_Execute(_rob, "CurJnt", "");
                    // read robot cartesian pose (disabled for now)
                    //_curPos = (double[])_clientRobot.Robot_Execute(_rob, "CurPos", "");
                    updateRobotDrives = true;
                }
                else
                {
                    if ((_clientVRC != null) && (_robVRC != 0))
                    {
                        // read robot joints values
                        _curJnt = (double[]) _clientVRC.Robot_Execute(_robVRC, "CurJnt", "");
                        // read robot cartesian pose (disabled for now)
                        //_curPos = (double[])_clientVRC.Robot_Execute(_robVRC, "CurPos", "");
                        updateRobotDrives = true;
                    }
                }

                if (updateRobotDrives)
                {
                    // TODO: "Drive.CurrentPosition" ONLY works in Play mode ...
                    for (int i = 0; i < RobotAxis.Count; i++)
                    {
                        RobotAxis[i].CurrentPosition = (float) _curJnt[i];
                    }
                }
            }
            catch (Exception ex)
            {
                // disconnected
                string errString = "Connection exception while connected to ";
                errString += ConnectRealRobot ? "real robot" : "VRC application";
                errString += " : " + ex.Message;
                lock (threadmessages)
                    threadmessages.Add(errString);
            

                ConnectRealRobot = false;
                _clientRobot = null;
            }
        }

        public void CloseConnection()
        {
            try
            {
                if (_robVRC != 0)
                {
                    /* Release robot handle */
                    _clientVRC.Robot_Release(_robVRC);
                    _robVRC = 0;
                }

                if (_ctrlVRCIOs != 0)
                {
                    /* Disconnect controller */
                    _clientVRC.Controller_Disconnect(_ctrlVRCIOs);
                    _ctrlVRCIOs = 0;
                    Debug.Log("[" + gameObject.name + "] : VRC IOs controller disconnected");
                }
                if (_ctrlVRCDrives != 0)
                {
                    /* Disconnect controller */
                    _clientVRC.Controller_Disconnect(_ctrlVRCDrives);
                    _ctrlVRCDrives = 0;
                    Debug.Log("[" + gameObject.name + "] : VRC drive controller disconnected");
                }

                if (_clientVRC != null)
                {
                    _clientVRC.Service_Stop();
                    _clientVRC = null;
                    Debug.Log("[" + gameObject.name + "] : VRC Service stopped");
                }

                if (_rob != 0)
                {
                    /* Release robot handle */
                    _clientRobot.Robot_Release(_rob);
                    _rob = 0;
                }

                if (_ctrlDrives != 0)
                {
                    /* Disconnect controller */
                    _clientRobot.Controller_Disconnect(_ctrlDrives);
                    _ctrlDrives = 0;
                    Debug.Log("[" + gameObject.name + "] : real robot controller disconnected");
                }

                if (_clientRobot != null)
                {
                    _clientRobot.Service_Stop();
                    _clientRobot = null;
                }

                Debug.Log("[" + gameObject.name + "] : Disconnected");
                IsConnected = false;
            }
            catch
            {
            }
        }

        public void OpenConnection()
        {
            if (_clientVRC != null)
            {
                Debug.LogWarning("[" + gameObject.name + "] : Resetting previous active connection ... ");
                CloseConnection();
            }

            if (_clientVRC == null)
            {
                try
                {
                    /* Get controller handle */
                    WincapsProject = Regex.Replace(WincapsProject, "/", @"\");

                    // Prepare connection to VRC
                    Debug.Log("[" + gameObject.name + "] : Connecting to VRC, ip address : " + ipAdress.GetAddress()
                              + " , controller type :  " + RobotControllerType
                              + " , controller name : '" + ControllerName + "'"
                              + " , project file : '" + WincapsProject + "'");
                    _clientVRC = new bCAPClient("TCP:" + ipAdress.GetAddress(), 3000, 3);
                    _clientVRC.Service_Start("WDT=400");
                    string provider = "";
                    switch (RobotControllerType)
                    {
                        case ControllerType.RC8:
                            //RC8 controller
                            provider = "CaoProv.DENSO.VRC"; 
                            break;
                        case ControllerType.RC9:
                            //RC9 controller
                            provider = "CaoProv.DENSO.VRC9";
                            break;
                    }
                    
                    string connectionOptions = "@IfNotMember=true, WPJ={" + WincapsProject + "}";
                    _ctrlVRCDrives = _clientVRC.Controller_Connect(ControllerName, provider, "localhost", connectionOptions);
                    _ctrlVRCIOs = _clientVRC.Controller_Connect(ControllerName, provider, "localhost", connectionOptions);
                    
                    /* Get robot handle */
                    _robVRC = _clientVRC.Controller_GetRobot(_ctrlVRCDrives, ControllerName, "@IfNotMember=true");
                    Debug.Log("[" + gameObject.name + "] : Connection to VRC successfully established");
                    GetRobotWork(0);
                    GetRobotTool(0);
                    GetRobotArea(0);
                    OnConnected();
                }
                catch (Exception ex)
                {
                    Debug.LogError("[" + gameObject.name + "] : Not able to connect to robot controller, Exception : "+ ex);
                    OnDisconnected();
                }
            }
        }
        
        private void RobotConnection()
        {
            if (ConnectRealRobot)
            {
                Debug.Log("[" + gameObject.name + "] : Connecting to real robot, ip address " + ipAdress.GetAddress());
                try
                {
                    // Prepare connection to Real Robot
                    _clientRobot = new bCAPClient("TCP:" + ipAdress.GetAddress(), 3000, 3);
                    _clientRobot.Service_Start("WDT=400");

                    string provider = "";
                    switch (RobotControllerType)
                    {
                        case ControllerType.RC8:
                            provider = "CaoProv.DENSO.VRC";
                            //provider = "CaoProv.DENSO.RC8";
                            break;
                        case ControllerType.RC9:
                            provider = "CaoProv.DENSO.VRC9";
                            //provider = "CaoProv.DENSO.RC9";
                            break;
                    }

                    /* Get controller handle */
                    _ctrlDrives = _clientRobot.Controller_Connect(ControllerName, provider, "localhost", "@IfNotMember = true");
                    _ctrlIOs = _clientRobot.Controller_Connect(ControllerName, provider, "localhost", "@IfNotMember = true");
                    /* Get robot handle */
                    _rob = _clientRobot.Controller_GetRobot(_ctrlDrives,  ControllerName, "");
                    Debug.Log("[" + gameObject.name + "] : Connection to real robot successfully established");
                    GetRobotWork(WorkNumber, true);
                    GetRobotTool(ToolNumber, true);
                    GetRobotArea(Area.Num, true);
                }
                catch (Exception ex)
                {
                    // disconnected
                    ConnectRealRobot = false;
                    Debug.LogError("[" + gameObject.name + "] : Connection exception while connecting to real robot : " + ex.Message);
                }
            }
            else
            {
                try
                {
                    // close connection
                    if (_rob != 0)
                    {
                        /* Release robot handle */
                        _clientRobot.Robot_Release(_rob);
                        _rob = 0;
                    }
                    if (_ctrlDrives != 0)
                    {
                        /* Disconnect controller */
                        _clientRobot.Controller_Disconnect(_ctrlDrives);
                        _ctrlDrives = 0;
                    }
                    _clientRobot.Service_Stop();
                    _clientRobot = null;
                    Debug.Log("[" + gameObject.name + "] : Disconnected from real robot");
                    GetRobotWork(WorkNumber, true);
                    GetRobotTool(ToolNumber, true);
                    GetRobotArea(Area.Num, true);
                }
                catch (Exception ex)
                {
                    // disconnected
                    ConnectRealRobot = false;
                    Debug.Log("[" + gameObject.name + "] : Connection exception while disconnecting from real robot : " + ex.Message);
                }
            }
        }

        #endregion

        #region ui functions


        void DrawText(string text, Vector3 worldPos, bool isEditor, Color? textColor = null)
        {
#if UNITY_EDITOR
            float height = 0;
            Vector2 size = GUI.skin.label.CalcSize(new GUIContent(text));

            if (isEditor)
            {
                UnityEditor.Handles.BeginGUI();
                if (textColor.HasValue) GUI.color = textColor.Value;
                var view = UnityEditor.SceneView.currentDrawingSceneView;
                _screenPos = view.camera.WorldToScreenPoint(worldPos);
                height = view.position.height;
                GUI.Label(new Rect(_screenPos.x - (size.x / 2), -_screenPos.y + height + 4, size.x, size.y), text);
                UnityEditor.Handles.EndGUI();
            }
#endif
        }

        protected void OnPreRender()
        {
            GL.Clear(true, true, new Color(0.19f, 0.19f, 0.19f, 1.0f));
            _matGrid.SetPass(0);
        }

        protected void CreateLineMaterial()
        {
            var shader = Shader.Find("DNWA/SimpleShader");
            _matLine = new Material(shader);
            _matOverlay = new Material(shader);
            _matGrid = new Material(shader);
        }

        void OnDrawGizmos()
        {
            if (ReferenceEquals(_matLine, null))
            {
                CreateLineMaterial();
            }
            UpdateUI(true);
        }

        void DrawWorldAxes()
        {
            float axisSize = 0.2f;
            Vector3 vAxisX = new Vector3(axisSize, 0, 0);
            Vector3 vAxisY = new Vector3(0, 0, axisSize);
            Vector3 vAxisZ = new Vector3(0, axisSize, 0);

            GL.Begin(GL.LINES);
            GL.Color(_colorXAxis);
            DensoTools.DrawLine(Vector3.zero, vAxisX);
            GL.Color(_colorYAxis);
            DensoTools.DrawLine(Vector3.zero, vAxisY);
            GL.Color(_colorZAxis);
            DensoTools.DrawLine(Vector3.zero, vAxisZ);
            GL.End();
            float rad = 0.01f;
            float dep = 0.05f;
            DensoTools.DrawArrowX(vAxisX.x, vAxisX.y, vAxisX.z, rad, dep, _colorXAxis,  DensoTools.ReferenceFrame.WorldFrame, new Pose(), null);
            DensoTools.DrawArrowY(vAxisY.x, vAxisY.y, vAxisY.z, rad, dep, _colorYAxis,  DensoTools.ReferenceFrame.WorldFrame, new Pose(), null);
            DensoTools.DrawArrowZ(vAxisZ.x, vAxisZ.y, vAxisZ.z, rad, dep, _colorZAxis,  DensoTools.ReferenceFrame.WorldFrame, new Pose(), null);
        }

        private GameObject CreateCsys(GameObject parent)
        {
            GameObject go = Instantiate(UnityEngine.Resources.Load("CSYS", typeof(GameObject))) as GameObject;

                if (go != null)
                {
                    go.transform.position = new Vector3(0, 0, 0);
                    if (parent != null)
                    {
                        go.transform.parent = parent.transform;
                    }
                    
                }
                return go;
        }

        void UpdateUI(bool isEditor)
        {
            if (_matLine == null)
            {
                CreateLineMaterial();
            }
            _matLine.SetPass(0);
            if (ShowGrid)
            {
                DensoTools.DrawGrid(GridSize, GridSize, _colorGrid);
                DrawWorldAxes();
            }

            _matOverlay.SetPass(0);
            if (ShowAreas)
            {
                // draw robot work areas            
                GL.Color(_colorRobotAreas);
                DensoTools.DrawBox(this,Area.Size, Area.UnityPose, _colorRobotAreas);
                DrawText("AREA " + Area.Num, gameObject.transform.TransformPoint(Area.UnityPose.position), isEditor);
            }

            if (ShowTool)
            {
                if (RobotFlange == null)
                { 
                    Debug.LogError("RobotFlange is null. Please assign a game object to RobotFlange property.");
                }
                else
                {
                    float dist = 0.2f;
                    float rad = 0.05f * dist;
                    float dep = 0.15f * dist;

                    // manage axes orientation according to the frame of "Flange" game object
                    Vector3 vRefOrigin = RobotFlange.transform.TransformPoint(_robotTool.position);
                    Vector3 vRefXPos =
                        RobotFlange.transform.TransformPoint(_robotTool.position +
                                                             _robotTool.rotation * (Vector3.right * dist));
                    Vector3 vRefXNeg =
                        RobotFlange.transform.TransformPoint(_robotTool.position +
                                                             _robotTool.rotation * (Vector3.left * dist));
                    Vector3 vRefYPos =
                        RobotFlange.transform.TransformPoint(_robotTool.position +
                                                             _robotTool.rotation * (Vector3.up * dist));
                    Vector3 vRefYNeg =
                        RobotFlange.transform.TransformPoint(_robotTool.position +
                                                             _robotTool.rotation * (Vector3.down * dist));
                    Vector3 vRefZPos =
                        RobotFlange.transform.TransformPoint(_robotTool.position +
                                                             _robotTool.rotation * (Vector3.back * dist));
                    Vector3 vRefZNeg =
                        RobotFlange.transform.TransformPoint(_robotTool.position +
                                                             _robotTool.rotation * (Vector3.forward * dist));

                    // draw reference frame on robot TOOL
                    GL.Begin(GL.LINES);
                    _matLine.SetPass(0);
                    GL.Color(_colorXAxis);
                    DensoTools.DrawLine(vRefOrigin, vRefXPos);
                    DensoTools.DrawLine(vRefOrigin, vRefXNeg);
                    GL.Color(_colorYAxis);
                    DensoTools.DrawLine(vRefOrigin, vRefYPos);
                    DensoTools.DrawLine(vRefOrigin, vRefYNeg);
                    GL.Color(_colorZAxis);
                    DensoTools.DrawLine(vRefOrigin, vRefZPos);
                    DensoTools.DrawLine(vRefOrigin, vRefZNeg);
                    GL.End();
                    DensoTools.DrawArrowX(vRefXPos.x, vRefXPos.y, vRefXPos.z, rad, dep, _colorXAxis,
                        DensoTools.ReferenceFrame.LocalFrame,
                        _robotTool, RobotFlange.transform);
                    DensoTools.DrawArrowY(vRefYPos.x, vRefYPos.y, vRefYPos.z, rad, dep, _colorYAxis,
                        DensoTools.ReferenceFrame.LocalFrame,
                        _robotTool, RobotFlange.transform);
                    DensoTools.DrawArrowZ(vRefZPos.x, vRefZPos.y, vRefZPos.z, rad, -dep, _colorZAxis,
                        DensoTools.ReferenceFrame.LocalFrame,
                        _robotTool, RobotFlange.transform);

                    DrawText("TOOL " + ToolNumber, vRefOrigin, isEditor);
                }
            }

            if (ShowWork)
            {
                float dist = 0.4f;
                float rad = 0.05f * dist;
                float dep = 0.15f * dist;

                // manage axes orientation according to the frame of the robot base game object
                Vector3 vRefOrigin = gameObject.transform.TransformPoint(_robotWork.position);
                Vector3 vRefX = gameObject.transform.TransformPoint(_robotWork.position + _robotWork.rotation * (Vector3.right * dist));
                Vector3 vRefY = gameObject.transform.TransformPoint(_robotWork.position + _robotWork.rotation * (Vector3.up * dist));
                Vector3 vRefZ = gameObject.transform.TransformPoint(_robotWork.position + _robotWork.rotation * (Vector3.back * dist));

                // draw reference frame on robot WORK
                GL.Begin(GL.LINES);
                _matLine.SetPass(0);
                GL.Color(_colorXAxis);
                DensoTools.DrawLine(vRefOrigin, vRefX);
                GL.Color(_colorYAxis);
                DensoTools.DrawLine(vRefOrigin, vRefY);
                GL.Color(_colorZAxis);
                DensoTools.DrawLine(vRefOrigin, vRefZ);
                GL.End();
                DensoTools.DrawArrowX(vRefX.x, vRefX.y, vRefX.z, rad, dep, _colorXAxis, DensoTools.ReferenceFrame.LocalFrame, _robotWork, gameObject.transform);
                DensoTools. DrawArrowY(vRefY.x, vRefY.y, vRefY.z, rad, dep, _colorYAxis, DensoTools.ReferenceFrame.LocalFrame, _robotWork, gameObject.transform);
                DensoTools.DrawArrowZ(vRefZ.x, vRefZ.y, vRefZ.z, rad, -dep, _colorZAxis, DensoTools.ReferenceFrame.LocalFrame, _robotWork, gameObject.transform);

                DrawText("WORK " + WorkNumber, vRefOrigin, isEditor);
            }
         

            if (ShowSafetyAreas && (SafetyAreaNumber >= -1))
            {
                
                GL.Color(_colorSafetyAreas);
                if (SafetyAreaNumber >= 0)
                {
                    
                    DensoTools.DrawBox(this,SafetyAreasList[SafetyAreaNumber].Size, SafetyAreasList[SafetyAreaNumber].UnityPose, _colorSafetyAreas);
                    DrawText(SafetyAreasList[SafetyAreaNumber].Name.ToUpper(),
                        gameObject.transform.TransformPoint(
                            SafetyAreasList[SafetyAreaNumber].UnityPose.position),
                        isEditor);
                }
                else
                {
                    if (SafetyFencesList.Count > 0)
                    {
                        foreach (RobotSafetyArea fenceArea in SafetyFencesList)
                        { 
                            DensoTools.DrawBox(this,fenceArea.Size, fenceArea.UnityPose, _colorSafetyAreas);
                        }

                        DrawText("VIRTUAL FENCE",
                            gameObject.transform.TransformPoint(
                                new Vector3(SafetyFencesList[0].Transform[0, 3],
                                    SafetyFencesList[0].Transform[1, 3],
                                    SafetyFencesList[0].Transform[2, 3])),
                            isEditor);
                    }
                }
            }
        }
        
        public void OnGUI()
        {
            if (ShowWork)
            {
                float height = Camera.main.scaledPixelHeight;
                Vector2 size =
                    GUI.skin.label.CalcSize(new GUIContent("<size=20><color=red>WORK " + WorkNumber.ToString() +
                                                           "</color></size>"));
                GUIStyle style = new GUIStyle();
                style.padding = new RectOffset(10, 0, 10, 0);
                style.richText = true;
                _screenPos = Camera.main.WorldToScreenPoint(gameObject.transform.TransformPoint(_robotWork.position));
                style.contentOffset = new Vector2(_screenPos.x - (size.x / 2), -_screenPos.y + height + 4);
                GUILayout.Label("<size=20><color=red>WORK " + WorkNumber.ToString() + "</color></size>", style);
            }

            if (ShowTool)
            {
                if (RobotFlange == null)
                {
                    Debug.LogError("[" + gameObject.name + "] : RobotFlange not set");
                   
                }
                else
                {
                    float height = Camera.main.scaledPixelHeight;
                    Vector2 size =
                        GUI.skin.label.CalcSize(new GUIContent("<size=20><color=red>TOOL " + ToolNumber.ToString() +
                                                               "</color></size>"));
                    GUIStyle style = new GUIStyle();
                    style.padding = new RectOffset(10, 0, 10, 0);
                    style.richText = true;
                    _screenPos = Camera.main.WorldToScreenPoint(RobotFlange.transform.TransformPoint(_robotTool.position));
                    style.contentOffset = new Vector2(_screenPos.x - (size.x / 2), -_screenPos.y + height + 4);
                    GUILayout.Label("<size=20><color=red>TOOL " + ToolNumber.ToString() + "</color></size>", style);
                }
             
            }

            if (ShowAreas)
            {
                float height = Camera.main.scaledPixelHeight;
                Vector2 size =
                    GUI.skin.label.CalcSize(new GUIContent("<size=20><color=red>AREA " + Area.Num.ToString() +
                                                           "</color></size>"));
                GUIStyle style = new GUIStyle();
                style.padding = new RectOffset(10, 0, 10, 0);
                style.richText = true;
                _screenPos =
                    Camera.main.WorldToScreenPoint(gameObject.transform.TransformPoint(Area.UnityPose.position));
                style.contentOffset = new Vector2(_screenPos.x - (size.x / 2), -_screenPos.y + height + 4);
                GUILayout.Label("<size=20><color=red>AREA " + Area.Num.ToString() + "</color></size>", style);
            }

            if (ShowSafetyAreas && (SafetyAreaNumber > -1))
            {
                float height = Camera.main.scaledPixelHeight;
                if (SafetyAreaNumber >= 0)
                {
                    Vector2 size = GUI.skin.label.CalcSize(new GUIContent("<size=20><color=red>" +
                                                                          SafetyAreasList[SafetyAreaNumber].Name
                                                                              .ToUpper() + "</color></size>"));
                    GUIStyle style = new GUIStyle();
                    style.padding = new RectOffset(10, 0, 10, 0);
                    style.richText = true;
                    _screenPos = Camera.main.WorldToScreenPoint(
                        gameObject.transform.TransformPoint(SafetyAreasList[SafetyAreaNumber].UnityPose.position));
                    style.contentOffset = new Vector2(_screenPos.x - (size.x / 2), -_screenPos.y + height + 4);
                    GUILayout.Label(
                        "<size=20><color=red>" + SafetyAreasList[SafetyAreaNumber].Name.ToUpper() + "</color></size>",
                        style);
                }
                else
                {
                    if (SafetyFencesList.Count > 0)
                    {
                        Vector2 size =
                            GUI.skin.label.CalcSize(
                                new GUIContent("<size=20><color=red>VIRTUAL FENCES</color></size>"));
                        GUIStyle style = new GUIStyle();
                        style.padding = new RectOffset(10, 0, 10, 0);
                        style.richText = true;
                        _screenPos =
                            Camera.main.WorldToScreenPoint(
                                gameObject.transform.TransformPoint(SafetyFencesList[0].UnityPose.position));
                        style.contentOffset = new Vector2(_screenPos.x - (size.x / 2), -_screenPos.y + height + 4);
                        GUILayout.Label("<size=20><color=red>VIRTUAL FENCES</color></size>", style);
                    }
                }
            }
        }

        public string[] UpdateSafetyAreas(bool readSafetyModel)
        {
            bool parseOk = true;
            List<string> safetyAreaNames = new List<string>();
            safetyAreaNames.Add("None");
            if (readSafetyModel)
            {
                parseOk = DensoTools.ParseXML(this);
            }

            if (parseOk)
            {
                if (SafetyFencesList.Count > 0)
                {
                    safetyAreaNames.Add("Fences");
                }

                foreach (RobotSafetyArea area in SafetyAreasList)
                {
                    safetyAreaNames.Add(area.Name);
                }
            }
            
            return safetyAreaNames.ToArray();
        }

        private bool IsFileLocked(FileInfo file)
        {
            try
            {
                using (FileStream stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    stream.Close();
                }
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //1. still being written to
                //2. or being processed by another thread
                //3. or does not exist (has already been processed)
                return true;
            }

            //file is not locked
            return false;
        }
        
        public double[] GetRobotTool(int toolNumber, bool forceRead = false)
        {
            double[] toolDef = new double[6];
            if ((_clientVRC != null) && (_robVRC != 0))
            {
                if (ConnectRealRobot && (_clientRobot != null) && (_rob != 0))
                {
                    try
                    {
                        if (forceRead || (toolNumber != ToolNumber))
                        {
                            toolDef = (double[]) _clientRobot.Robot_Execute(_rob, "GetToolDef", toolNumber);
                            DensoTools.UpdateTransformationTool(this, toolNumber, toolDef);
                        }
                    }
                    catch (Exception ex)
                    {
                        // disconnected
                        ConnectRealRobot = false;
                        Debug.LogError("[" + gameObject.name + "] : GetRobotTool() exception : " + ex);
                    }
                }
                else
                {
                    if (forceRead || (toolNumber != ToolNumber))
                    {
                        toolDef = (double[]) _clientVRC.Robot_Execute(_robVRC, "GetToolDef", toolNumber);
                        DensoTools.UpdateTransformationTool(this, toolNumber, toolDef);
                    }
                }
            }
            else
            {
                Debug.LogError("[" + gameObject.name + "] : GetRobotTool() exception : VRC not connected");
            }

            return toolDef;
        }

        public double[] GetRobotWork(int workNumber, bool forceRead = false)
        {
            double[] workDef = new double[6];
            if ((_clientVRC != null) && (_robVRC != 0))
            {
                if (ConnectRealRobot && (_clientRobot != null) && (_rob != 0))
                {
                    try
                    {
                        if (forceRead || (workNumber != WorkNumber))
                        {
                            workDef = (double[]) _clientRobot.Robot_Execute(_rob, "GetWorkDef", workNumber);
                            DensoTools.UpdateTransformationWork(this, workNumber, workDef);
                        }
                    }
                    catch (Exception ex)
                    {
                        // disconnected
                        ConnectRealRobot = false;
                        Debug.LogError("[" + gameObject.name + "] : GetRobotWork() exception : " + ex);
                    }
                }
                else
                {
                    if (forceRead || (workNumber != WorkNumber))
                    {
                        workDef = (double[]) _clientVRC.Robot_Execute(_robVRC, "GetWorkDef", workNumber);
                        DensoTools.UpdateTransformationWork(this, workNumber, workDef);
                    }
                }
            }
            else
            {
                Debug.LogError("[" + gameObject.name + "] : GetRobotWork() exception : VRC not connected");
            }

            return workDef;
        }

        public double[] GetRobotArea(int areaNumber, bool forceRead = false)
        {
            double[] areaDef = new double[34];
            if ((_clientVRC != null) && (_robVRC != 0))
            {
                if (ConnectRealRobot && (_clientRobot != null) && (_rob != 0))
                {
                    try
                    {
                        if (forceRead || (areaNumber != Area.Num))
                        {
                            areaDef = (double[]) _clientRobot.Robot_Execute(_rob, "GetAreaDef", areaNumber);
                            DensoTools.UpdateTransformationArea(this, areaNumber, areaDef);
                        }
                    }
                    catch (Exception ex)
                    {
                        // disconnected
                        ConnectRealRobot = false;
                        Debug.LogError("[" + gameObject.name + "] : GetRobotArea() exception : " + ex);
                    }
                }
                else
                {
                    if (forceRead || (areaNumber != Area.Num))
                    {
                        areaDef = (double[]) _clientVRC.Robot_Execute(_robVRC, "GetAreaDef", areaNumber);
                        DensoTools.UpdateTransformationArea(this, areaNumber, areaDef);
                    }
                }
            }
            else
            {
                Debug.LogError("[" + gameObject.name + "] : GetRobotArea() exception : VRC not connected");
            }

            return areaDef;
        }
        
        protected void OnPostRenderCallback(Camera cam)
        {
            if (cam != Camera.main)
            {
                return;
            }
            UpdateUI(false);
        }

        #endregion
        

        [Button("Connect")]
        public void Connect()
        {
            base.TwoThreads = true;
            OpenConnection();
            UpdateInterfaceSignals(ref NumberInputs, ref NumberOutputs);
            SetIOHashtable();
        }


        [Button("Disconnect")]
        public void Disconnect()
        {
            CloseInterface();
        }
        
        public void SelectController()
        {
#if UNITY_EDITOR
            // check if controllername is valid path
            // set directory to current project director
            
            var directory = Application.dataPath;
            if (!File.Exists(ControllerName))
            {
                ControllerName = "";
            }
            else
            {
                directory = Path.GetDirectoryName(ControllerName);
            }
            ControllerName = EditorUtility.OpenFilePanel("Select Wincaps Project file", ControllerName, "WPJ");
#endif
        }
        
        [Button("Select Project")]
        public void SelectProject()
        {
#if UNITY_EDITOR
            // check if controllername is valid path
            // set directory to current project director
            
            var directory = Application.dataPath;
            if (!File.Exists(WincapsProject))
            {
                WincapsProject = "";
            }
            else
            {
                directory = Path.GetDirectoryName(WincapsProject);
            }
            WincapsProject = EditorUtility.OpenFilePanel("Select Wincaps Project file", WincapsProject, "WPJ");
#endif
        }

        public  bool IsPrefab()
        {
#if UNITY_EDITOR

            var a = PrefabUtility.GetPrefabAssetType(this.gameObject);
            if (a == PrefabAssetType.NotAPrefab)
                return true;
            else
                return false;
#else
        return false;
#endif
        }
        
        void Reset()
        {
            IsConnected = false;
            MinUpdateCycle = 8;
            MinUpdateCycle2 = 8;
            TwoThreads = true;
            RobotControllerType = ControllerType.RC8;
            GetRobotDrives();
            ControllerName = GetControllerName();
            ipAdress = new IPAddress();
            ipAdress.Address1 = 127;
            ipAdress.Address2 = 0;
            ipAdress.Address3 = 0;
            ipAdress.Address4 = 1;
        }

        void Start()
        {
            Camera.onPostRender += OnPostRenderCallback;
            if (MinUpdateCycle < 8)
            {
                Debug.LogWarning("[" + gameObject.name + "] : MinUpdateCycle of communication thread must be greater than 8, automatically set to 8");
                MinUpdateCycle = 8;
            }
            if (MinUpdateCycle2 < 8)
            {
                Debug.LogWarning("[" + gameObject.name + "] : MinUpdateCycle2 of second communication thread must be greater than 8, automatically set to 8");
                MinUpdateCycle2 = 8;
            }
        }
        
   
        
        void PrintThreadMessages()
        {
         
                if (Messages.Count > 0)
                {
                    foreach (var message in threadmessages)
                    {
                        Debug.LogWarning("[" + gameObject.name + "] : " + message);
                    }
                }

                Messages.Clear();
            
        }

        new void Awake()
        {

            if (Application.isPlaying)
            {
                TwoThreads = true;
                base.Awake();
            }
            else
            {
                if (!IsPrefab())
                {
                    if (ControllerName == "")
                        ControllerName = GetControllerName();
                }
            }
      
        }

        void Update()
        {
            if (Application.isPlaying)
            {
                PrintThreadMessages();
            }
        }

    }
}