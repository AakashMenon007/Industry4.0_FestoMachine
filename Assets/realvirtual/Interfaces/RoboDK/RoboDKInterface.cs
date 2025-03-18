

using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NaughtyAttributes;
using UnityEditor;

namespace realvirtual
{
    
//! This is the first version of the RoboDK integration. Still in Beta and some interactions from Unity side
//! like showing Targets, following in Editmode... are missing.
    [HelpURL("https://doc.realvirtual.io/components-and-scripts/interfaces/robodk")]
    public class RoboDKInterface : InterfaceThreadedBaseClass
    {
        public string RoboDKApplication = "C:/RoboDK/bin/RoboDK.exe";
        public string RobotDKFile;
        public string RobotName;
        public string RobotDKIP = "localhost";
        public bool OpenRDKOnStart = true;

        [OnValueChanged("SyncInEditModeChanged")]
        /* public bool SyncInEditMode = true; */
        [Range(0, 10)]
        public float RDKSimulationSpeed = 1f;

        public string RobotProgramm;
        public bool RunProgrammOnStart;
        public bool LoopProgramm;
        [NaughtyAttributes.ReadOnly] public bool ProgrammIsRunning;

        public bool StopProgrammOnUnityStop;

        /* public string TeachPositionName = "Position1";
        public GameObject Target;
        public bool FollowTarget = false; */
        public GameObject TargetPrefab;
        [HideInInspector] public GameObject DebugEndEffector;
        private float _oldspeed = 1f;
        [HideInInspector] public Vector3 EffectorPos;
        [HideInInspector] public Vector3 EffectorRot;
        [HideInInspector] public Vector3 OrigRot;
        [ReorderableList] public List<Drive> Axis;
        [NaughtyAttributes.ReadOnly] public List<float> CurrentAxisPosition;

        private RoboDK.Item Robot;
        private RoboDK RDK;
        private List<Signal> Signals;
        private Vector3 targetpos;

        private bool RDKIsConnected = false;

        //private Quaternion targetrot;
        //private Vector3 oldtargetpos;
        //private Quaternion oldtargetrot;
        private string _startprogramm = "";
        private RoboDK.Item runningprogramm;
        private bool _stopprogramm = false;
        private bool _movetotarget = false;
        // private Mat target;
        // private bool _synineditmodebeforestart = false;


        [Button("Get Targets")]
        public void GetTargets()
        {
            if (RDK == null)
                Connect();
            var Targets = RDK.getItemList(RoboDK.ITEM_TYPE_TARGET);
            var rdktargets = GetComponentsInChildren<RoboDKTarget>();
            foreach (var target in rdktargets.ToArray())
            {
                DestroyImmediate(target.gameObject);
            }

            foreach (var target in Targets)
            {
                var pose = target.Pose();
                var rot = pose.ToTxyzRxyz();
                Quaternion quatrdk = Quaternion.Euler((float) rot[3], (float) rot[4], (float) rot[5]);
                Vector3 EulerRDK = new Vector3((float) rot[3], (float) rot[4], (float) rot[5]);
                Quaternion quatunity = GetRotationQuaternion(pose);
                Vector3 EulerUnity2 = quatunity.eulerAngles;
                var newtarget = Instantiate(TargetPrefab, gameObject.transform);
                newtarget.name = target.Name();

                newtarget.transform.localPosition = RDKToUnityPos(pose);
                newtarget.transform.rotation = quatunity;
                Debug.Log(target.Name() + "/" + target.Pose());
            }
        }

        private Vector3 RDKToUnityPos(Mat mat)
        {
            var pos = mat.Pos();
            float x = -(float) pos[0];
            float y = (float) pos[1];
            float z = (float) pos[2];
            float ux = x / 1000;
            float uy = z / 1000;
            float uz = -y / 1000;
            return new Vector3(ux, uy, uz);
        }

        private Quaternion RDKToUnityRot(Mat mat)
        {
            var rot = mat.ToXYZRPW();
            var rx = (float) rot[3];
            var ry = (float) rot[4];
            var rz = (float) rot[5];

            return Quaternion.Euler(rx, rz, ry);
        }
        //x=-x
        // y=z
        // z=-y


        public Quaternion GetRotationQuaternion(Mat mat)
        {
            var angle = GetAngles(mat);
            Quaternion qx = Quaternion.AngleAxis(angle.x, new Vector3(1, 0, 0));
            Quaternion qy = Quaternion.AngleAxis(-angle.z, new Vector3(0, 1, 0));
            Quaternion qz = Quaternion.AngleAxis(angle.y, new Vector3(0, 0, 1));
            Quaternion unityRotationQuaternion = qy * qx * qz; // exact order!
            return unityRotationQuaternion;
        }

        public static Vector3 GetAngles(Mat source)
        {
            double thetaX, thetaY, thetaZ = 0.0;
            thetaX = System.Math.Asin(source[2, 1]);
            var pi = System.Math.PI;
            if (thetaX < (pi / 2))
            {
                if (thetaX > (-pi / 2))
                {
                    thetaZ = System.Math.Atan2(-source[0, 1], source[1, 1]);
                    thetaY = System.Math.Atan2(-source[2, 0], source[2, 2]);
                }
                else
                {
                    thetaZ = -System.Math.Atan2(-source[0, 2], source[0, 0]);
                    thetaY = 0;
                }
            }
            else
            {
                thetaZ = System.Math.Atan2(source[0, 2], source[0, 0]);
                thetaY = 0;
            }

            float gradx = (float) ((180 / pi) * thetaX);
            float grady = (float) ((180 / pi) * thetaY);
            float gradz = (float) ((180 / pi) * thetaZ);
            return new Vector3(gradx, grady, gradz);
        }

        // Start is called before the first frame update
        public Matrix4x4 GetTRSFlipped(Mat rdkmat)
        {
            Matrix4x4 mat = new Matrix4x4(
                new Vector4((float) rdkmat[0, 0], (float) rdkmat[1, 0], (float) rdkmat[2, 0], (float) rdkmat[3, 0]),
                new Vector4((float) rdkmat[0, 1], (float) rdkmat[1, 1], (float) rdkmat[2, 1], (float) rdkmat[3, 1]),
                new Vector4((float) rdkmat[0, 2], (float) rdkmat[1, 2], (float) rdkmat[2, 2], (float) rdkmat[3, 2]),
                new Vector4(0, 0, 0, 1));

            Matrix4x4 invert = new Matrix4x4(new Vector4(-1, 0, 0, 0), new Vector4(0, 1, 0, 0), new Vector4(0, 0, 1, 0),
                new Vector4(0, 0, 0, 1));
            var convert = invert * mat * invert;
            return convert;
        }

        public Vector3 GetPosition(Mat rdkmat)
        {
            var mat = GetTRSFlipped(rdkmat);
            Vector3 vect = new Vector3(mat.m30, mat.m31, mat.m32);
            return vect;
        }

        void Connect()
        {
            var path = RobotDKFile;
            if (!Path.IsPathRooted(path))
            {
                path = Path.Combine(Application.dataPath, path);
            }

            if (RDK == null)
                RDK = new RoboDK(RobotDKIP, !OpenRDKOnStart, -1, RobotDKFile, RoboDKApplication);
            if (OpenRDKOnStart)
                RDK.ShowRoboDK();
            RDK.Finish();
        }

        void StartSimulation()
        {
            Connect();
            // Set to simulation mode:
            RDK.setRunMode(RoboDK.RUNMODE_SIMULATE);
            Robot = RDK.getItem(RobotName, RoboDK.ITEM_TYPE_ROBOT);
        }

        [Button("Select RoboDK File")]
        void SelectFile()
        {
#if UNITY_EDITOR
            RobotDKFile = EditorUtility.OpenFilePanel("Select RoboDK File", RobotDKFile, "rdk");
#endif
        }

        [Button("Open RoboDK File")]
        void OpenRDKFile()
        {
            var path = RobotDKFile;
            if (!Path.IsPathRooted(path))
            {
                path = Path.Combine(Application.dataPath, path);
            }

            RDK = new RoboDK(RobotDKIP, !OpenRDKOnStart, -1, RobotDKFile, RoboDKApplication);
            RDK.Disconnect();
        }

        [Button("Import Robot IOs")]
        public void GetParams()
        {
            if (!Application.isPlaying)
                Connect();

            var parameters = RDK.getParams();

            foreach (var param in parameters)
            {
                var name = param[0];
                var value = param[1];
                var mysignal = GetSignal(name);
                if (mysignal == null)
                {
                    InterfaceSignal signal = new InterfaceSignal();
                    signal.Name = name;
                    signal.SymbolName = name;
                    signal.Type = InterfaceSignal.TYPE.INT;
                    signal.Direction = InterfaceSignal.DIRECTION.OUTPUT;
                    var tmp = (AddSignal(signal)).gameObject;
                    mysignal = tmp;
                }

                mysignal.GetComponent<Signal>().SetValue(value);
                ;
            }
        }

        public void InitSignals()
        {
            Signal[] signals = GetComponentsInChildren<Signal>();
            DeleteSignals();
            foreach (Signal signal in signals)
            {
                InterfaceSignal interfacesignal = signal.GetInterfaceSignal();

                AddSignal(interfacesignal);
            }
        }

/* 
    private void SyncInEditModeChanged()
    {
        foreach (var axis in Axis)
        {
            if (SyncInEditMode)
                axis.StartEditorMoveMode();
            else
                axis.EndEditorMoveMode();
        }

        if (SyncInEditMode)
        {
            EditorApplication.update += EditorUpdate;
            EditorSceneManager.sceneSaving += OnSceneSave;
        }
        else
        {

            EditorApplication.update -= EditorUpdate;
            EditorSceneManager.sceneSaving -= OnSceneSave;
        }
    }

    private void OnSceneSave(Scene scene, string path)
    {
        OnDestroy();
    }
    
        
    private void OnDestroy()
    {
        if (Application.isPlaying)
        {
            SyncInEditMode = _synineditmodebeforestart;
            if (SyncInEditMode)
            {
                SyncInEditModeChanged();
            }
        }
     
    }
*/
        void UpdateSignals()
        {
            foreach (var signal in InterfaceSignals)
            {
                if (signal.Direction == InterfaceSignal.DIRECTION.OUTPUT)
                {
                    var value = RDK.getParam(signal.Name);
                    signal.Signal.SetValue(value);
                }

                if (signal.Direction == InterfaceSignal.DIRECTION.INPUT)
                {
                    string signalvalue = signal.Signal.GetValue().ToString();
                    RDK.setParam(signal.Name, signalvalue);
                }
            }
        }

        void Start()
        {
            /* _synineditmodebeforestart = SyncInEditMode;
            SyncInEditMode = false;
            SyncInEditModeChanged(); */
            if (Application.isPlaying)
            {
                /* if (Target != null)
                {
                    targetpos = Target.transform.localPosition;
                    targetrot = Target.transform.localRotation;
                }
                else
                {
                    targetpos = Vector3.zero;
                    targetrot = Quaternion.identity;
                }*/

                _oldspeed = RDKSimulationSpeed;
                StartSimulation();
                InitAxis();
                SetSpeed();
                InitSignals();
                if (RobotProgramm != "" && RunProgrammOnStart)
                    StartProgramm();
            }
            else
            {
                /* if (SyncInEditMode)
                   SyncInEditModeChanged(); */
            }
        }


        private void EditorUpdate()
        {
            if (!Application.isPlaying)
                // if (!Application.isPlaying && SyncInEditMode)
            {
                if (RDK == null)
                {
                    StartSimulation();
                    InitAxis();
                }

                UpdateAxisPositionEditMode();
            }
        }

        void InitAxis()
        {
            CurrentAxisPosition = new List<float>();
            RoboDK.Item[] robots = new[] {Robot};
            var joints = RDK.Joints(robots);
            for (int i = 0; i < joints[0].Length; i++)
            {
                CurrentAxisPosition.Add((float) joints[0][i]);
            }
        }



        void UpdateAxisPosition()
        {
            if (RDK == null || Robot == null)
                return;
            RoboDK.Item[] robots = new[] {Robot};
            var joints = RDK.Joints(robots);
            for (int i = 0; i < joints[0].Length; i++)
            {
                CurrentAxisPosition[i] = (float) joints[0][i];
                Axis[i].CurrentPosition = CurrentAxisPosition[i];
            }
        }

        void UpdateAxisPositionEditMode()
        {
            /*  if (SyncInEditMode)
              {
                  RoboDK.Item[] robots = new[] {Robot};
                  var joints = RDK.Joints(robots);
                  for (int i = 0; i < joints[0].Length; i++)
                  {
                      CurrentAxisPosition[i] = (float) joints[0][i];
                      if (Axis[i]!=null)
                         Axis[i].SetPositionEditorMoveMode(CurrentAxisPosition[i]);
                  }
              } */
        }


        private void StartProg(string programm)
        {
            if (RDK == null)
                Connect();
            if (RDK.Connected() == false)
            {
                Connect();
            }

            var prog = RDK.getItem(programm, RoboDK.ITEM_TYPE_PROGRAM);
            prog.RunProgram();
            runningprogramm = prog;
        }

        [Button("Start Programm")]
        public void StartProgramm()
        {

            _startprogramm = RobotProgramm;
            if (!Application.isPlaying)
                StartProg(_startprogramm);

        }

        [Button("Stop Programm")]
        public void StopProgramm()
        {
            LoopProgramm = false;
            _stopprogramm = true;
            if (!Application.isPlaying)
                StopProg();

        }

        private void StopProg()
        {
            if (RDK == null)
                Connect();
            if (RDK.Connected() == false)
            {
                Connect();
            }

            runningprogramm.Stop();
            _stopprogramm = false;
        }

        public void StartProgramm(string programm)
        {
            _startprogramm = programm;

        }


        public void SetSpeed()
        {
            RDK.setSimulationSpeed(RDKSimulationSpeed);
        }

        /* [Button("Teach current Position")]
         public void TeachCurrentPosition()
         {
          
             if (!Application.isPlaying)
             {
     #if UNITY_EDITOR
                 EditorUtility.DisplayDialog("Error",
                     "You need to be in Play mode to teach a position", "OK");
     #endif
                 return;
             }
             RDK.AddTarget(TeachPositionName);
         } */

        /*  [Button("Move to Target")]
          public void MoveToTarget()
          {
      
              if (!Application.isPlaying)
              {
      #if UNITY_EDITOR
                  EditorUtility.DisplayDialog("Error",
                      "You need to be in Play mode to move the robot to a Unity target", "OK");
      #endif
                  return;
              }
              var trans = new Vector3(targetpos.x * 1000, targetpos.y * 1000, targetpos.z * 1000);
              var rot = targetrot.eulerAngles;
              target = Mat.FromTxyzRxyz(-trans.x, -trans.z, trans.y, rot.x, rot.z, rot.y);
              _movetotarget = true;
              oldtargetpos = targetpos;
              oldtargetrot = targetrot;
          }
      */

        protected override void CommunicationThreadUpdate()
        {
            if (!RDKIsConnected)
                return;

            lock (InterfaceSignals)
            {
                if (_oldspeed != RDKSimulationSpeed)
                {
                    _oldspeed = RDKSimulationSpeed;
                    SetSpeed();
                }

                UpdateAxisPosition();
                UpdateSignals();
            }

            if (_startprogramm != "")
            {
                StartProg(_startprogramm);
                _startprogramm = "";
            }

            if (_stopprogramm)
            {
                StopProg();
                _stopprogramm = false;
            }

            if (_movetotarget)
            {
                //Robot.setPose(target);
                _movetotarget = false;
            }

            if (runningprogramm != null)
            {
                ProgrammIsRunning = true;
                if (!runningprogramm.Busy())
                {
                    if (LoopProgramm)
                    {
                        StartProg(RobotProgramm);
                    }
                    else
                    {
                        runningprogramm = null;
                        ProgrammIsRunning = false;
                    }
                }
            }
        }

        private void OnApplicationQuit()
        {
            CloseInterface();
        }

        protected override void CommunicationThreadClose()
        {
            if (StopProgrammOnUnityStop && RobotProgramm != "")
            {
                if (RDK != null)
                {
                    var prog = RDK.getItem(RobotProgramm, RoboDK.ITEM_TYPE_PROGRAM);
                    prog.Stop();
                }
            }

            RDKIsConnected = false;
            if (RDK != null)
                RDK.Disconnect();
        }

        // Update is called once per frame
         void Update()
        {
            if (!RDKIsConnected)
                if (RDK != null)
                    if (RDK.Connected())
                        RDKIsConnected = true;
/*
        if (Target != null && FollowTarget)
        {
            if (oldtargetpos != Target.transform.localPosition || oldtargetrot != Target.transform.localRotation)
            {
                targetpos = Target.transform.localPosition;
                targetrot = Target.transform.localRotation;
                MoveToTarget();
            }
        } */
        }
    }
}