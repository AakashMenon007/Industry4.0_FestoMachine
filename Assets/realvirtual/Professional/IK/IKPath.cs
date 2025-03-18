// realvirtual.io (formerly game4automation) (R) a Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 realvirtual GmbH - Usage of this source code only allowed based on License conditions see https://realvirtual.io/unternehmen/lizenz  

using System.Collections.Generic;
using UnityEngine;
using realvirtual;
using NaughtyAttributes;
using UnityEditor;

namespace realvirtual
{
#pragma warning disable 0414
    [HelpURL("https://doc.realvirtual.io/components-and-scripts/motion/robot-inverse-kinematics")]
    //! Robot Inverse Kinematics - the path interpolation for driving the robot along different targets which are forming a path    
    public class IKPath : BehaviorInterface
    {

        [Header("Path Parameters")] [Range(0, 1)]
        public float SpeedOverride = 1; //!< Speed override for this path

        public bool SetNewTCP; //!< Set new TCP for this path
        public bool DebugPath = false; //!< Debug the path by drawing points in scene mode - red is tcp position, green is corrected position by PositionCorrection value in target 
        [ShowIf("SetNewTCP")] public GameObject TCP; //! New TCP for this path
        public bool DrawPath = true; //!< Draw the path in the scene view
        public bool DrawTargets = true; //!< Draw the targets in the scene view

        [Header("Targets")] [ReorderableList] public List<IKTarget> Path = new List<IKTarget>(); //!< List of targets for the path
        [Header("Start Conditions")] public PLCOutputBool SignalStart; //!< Signal to start the path
        public bool StartPath; //!< Start the path on simulation start
        public bool LoopPath; //!< Loop the path after this path is ended
        [Header("On Path End")] public PLCInputBool SignalIsStarted; //!<  Signal that the path is started
        public PLCInputBool SignalEnded; //!< Signal that the path is ended
        public IKPath StartNextPath; //!< Start this path after this path is ended
    
        [Header("Status")] [realvirtual.ReadOnly]
        public bool PathIsActive = false; //!< Is the path active?
        [realvirtual.ReadOnly] public bool PathIsFinished = false; //!< Is the path finished?
        [realvirtual.ReadOnly] public bool LinearPathActive = false; //!< Is the linear path active?
        [realvirtual.ReadOnly] public float LinearPathPos = 0; //!< Position of the linear path
        [realvirtual.ReadOnly] public IKTarget CurrentTarget; //!<  Current target of the path
        [realvirtual.ReadOnly] public IKTarget LastTarget; //!< Last target of the path
        [realvirtual.ReadOnly] public int NumTarget; //!< Number of the current target
        [realvirtual.ReadOnly] public bool WaitForSignal; //!< Is the path waiting for a signal?
        [realvirtual.ReadOnly] public RobotIK RobotIK; //!<  RobotIK component - assigned robot to this path
        [realvirtual.ReadOnly] public Vector3 LastPlannedPosition; //!< Last planned position of the robot
     
        private List<Drive> drivesatposition = new List<Drive>();

        private float linearpathspeed = 0;
        private float linearpathacceleration = 0;
        private bool linearacceleration = false;
        private bool lineardeceleration = false;

        private float LinearPathStartTime = 0;
        private Vector3 PositionOnPath;
        private Vector3 LinearPathStartPos;
        private Quaternion LinearPathStartRot;
        private bool startbefore = false;
        private bool signalendednotnull, signalisstartednotnull, signalstartnotnull;
        private PLCOutputBool waitforsignal;
        private bool waitforstart;

        [Button("Start Path")]
        //! Start the path
        public void startPath()
        {
            if (PathIsActive)
            {
                Debug.Log("Start not possible because path is currently active!");
                return;
            }

            if (SetNewTCP)
                RobotIK.SetTCP(TCP);

            if (SetRobotIK())
            {
                RobotIK.FollowTarget = false;
                drivesatposition.Clear();
                NumTarget = 0;
                PathIsActive = true;
                if (signalisstartednotnull)
                    SignalIsStarted.Value = true;
                if (signalendednotnull)
                    SignalEnded.Value = false;
                CheckNextTarget();
            }
        }
        
        //! Starts to drive PTP to the target
        public void StartDrivePTP(IKTarget target)
        {
            // Get All Times
            // Check max Times 
            float maxtime = 0;
            var i = 0;
            var maxdrive = 0;
            if (target.InterpolationToTarget == IKTarget.Interploation.PointToPoint)
            {
                foreach (var drive in RobotIK.Axis)
                {
                    drive.SpeedOverride = SpeedOverride * target.SpeedToTarget;
                    var driveteime = drive.GetTimeTo(target.AxisPos[i]);
                    if (driveteime > maxtime)
                    {
                        maxtime = driveteime;
                        maxdrive = i;
                    }

                    i++;
                }

                // Start Drives
                i = 0;
                foreach (var drive in RobotIK.Axis)
                {
                    var pos = target.AxisPos[i];
                     pos = pos + target.AxisCorrection[i];
                    drive.OnAtPosition += DriveOnOnAtPosition;
                    if (i != maxdrive)
                        drive.DriveTo(pos, maxtime);
                    else
                        drive.DriveTo(pos);
                    
                    i++;
                }
            }

            if (target.InterpolationToTarget == IKTarget.Interploation.PointToPointUnsynced)
            {
                // Start Drives
                i = 0;
                foreach (var drive in RobotIK.Axis)
                {
                    drive.OnAtPosition += DriveOnOnAtPosition;
                    drive.DriveTo(target.AxisPos[i]);
                    i++;
                }
            }
        }
        
        public void OnTargetDelete(IKTarget target)
        {
            Path.Remove(target);
        }

        //! Starts to drive linear to the target
        void StartDriveLinear(IKTarget target)
        {
            LinearPathStartPos = LastPlannedPosition;
            if (LastPlannedPosition == Vector3.zero)
                LinearPathStartPos = RobotIK.GetTCPPosGlobal();
            LinearPathStartRot = RobotIK.GetTCPRotGlobal();
            PositionOnPath = LinearPathStartPos;
            linearpathspeed = 0;
            linearpathacceleration = 0;
            linearacceleration = false;
            lineardeceleration = false;
            LinearPathActive = true;
            LinearPathPos = 0;
        }

        //! Starts to drive to the target
        public bool DriveToTarget(IKTarget target)
        {
            if (LastTarget != null)
                LastTarget.OnLeaveTarget();
            LastTarget = null;
            RobotIK.Solution = target.Solution;
            RobotIK.SolveIK(target);
            CurrentTarget = target;
            if (target.Reachable)
            {
                if (target.InterpolationToTarget == IKTarget.Interploation.PointToPoint)
                    StartDrivePTP(target);
                if (target.InterpolationToTarget == IKTarget.Interploation.PointToPointUnsynced)
                    StartDrivePTP(target);
                if (target.InterpolationToTarget == IKTarget.Interploation.Linear)
                    StartDriveLinear(target);
                return true;
            }
            else
            {
                Error("Target " + target + "is not reachable");
                return false;
            }
        }

        
        [Button("Add new Target to Path")]
        public void AddTargetToPath()
        {
            var newgo = new GameObject();
            newgo.transform.parent = this.transform;
            // Same Position and Rotation as last Target
            if (Path != null && Path.Count > 0)
            {
                var last = Path[Path.Count - 1];
                newgo.transform.position = last.transform.position;
                newgo.transform.rotation = last.transform.rotation;
            }
            else
            {
                if (SetRobotIK())
                {
                    newgo.transform.position = RobotIK.GetTCPPosGlobal();
                    newgo.transform.rotation = RobotIK.GetTCPRotGlobal();
                }
            }

            newgo.name = "Target" + Path.Count;
            var target = newgo.AddComponent<IKTarget>();
            Path.Add(target);
            target.RobotIK= RobotIK;
#if UNITY_EDITOR
            Selection.activeGameObject = newgo;
#endif
        }
        
        //! Check if the path is ended or if next target can be selected
        private void CheckNextTarget()
        {
            if (!PathIsActive)
                return;
            if (NumTarget < Path.Count)
            {
                DriveToTarget(Path[NumTarget]);
            }
            else
            {
                // Path Ended
                CurrentTarget = null;
                NumTarget = 0;
                PathIsActive = false;
                if (signalendednotnull)
                    SignalEnded.Value = true;
                if (signalisstartednotnull)
                    SignalIsStarted.Value = false;
                if (StartNextPath != null)
                    StartNextPath.startPath();
                else if (LoopPath)
                    startPath();
            }
        }

        
        private void AtTarget()
        {
            NumTarget++;
            // Reset all Values for all motion types
            drivesatposition.Clear();
            linearpathspeed = 0;
            linearpathacceleration = 0;
            linearacceleration = false;
            lineardeceleration = false;
            LinearPathActive = false;
            LinearPathPos = 0;
            CurrentTarget.OnAtTarget();

            if (CurrentTarget.WaitForSignal != null)
            {
                waitforsignal = CurrentTarget.WaitForSignal;
                WaitForSignal = true;
            }
            else
            {
                ReadyForCheckNextTarget();
            }
        }

        private void ReadyForCheckNextTarget()
        {
            WaitForSignal = false;
            Invoke("CheckNextTarget", CurrentTarget.WaitForSeconds);
            LastTarget = CurrentTarget;
            CurrentTarget = null;
        }

        private void DriveOnOnAtPosition(Drive drive)
        {
            drive.OnAtPosition -= DriveOnOnAtPosition;
            drive.SpeedOverride = 1;
            drivesatposition.Add(drive);
            if (drivesatposition.Count == 6)
            {
                LastPlannedPosition = CurrentTarget.GetCorrectedGlobalPosition();
                AtTarget();
            }
        }

        void Reset()
        {
            CurrentTarget = null;
            NumTarget = 0;
        }

        // Update is called once per frame
        new void Awake()
        {
            NumTarget = 0;
            PathIsActive = false;
            CurrentTarget = null;
            signalendednotnull = SignalEnded != null;
            signalisstartednotnull = SignalIsStarted != null;
            signalstartnotnull = SignalStart != null;
            base.Awake();
            waitforstart = true;
        }

        void Start()
        {
            LastPlannedPosition = Vector3.zero;
            Invoke("EndWaitForStart", 0.1f);
        }

        void EndWaitForStart()
        {
            waitforstart = false;
        }

        private void PositionOnLinearPath()
        {
            // Calculate Path Position
            // Slow down needed
            bool slowdownneeded = false;
            var vectortoend = CurrentTarget.GetCorrectedGlobalPosition() - PositionOnPath;
            if (DebugPath) Debug.DrawLine(PositionOnPath,PositionOnPath+vectortoend,Color.green);
            var distancetoend = vectortoend.magnitude * realvirtualController.Scale;
            var availslowdowntime = Mathf.Sqrt(2 * distancetoend / CurrentTarget.LinearAcceleration);
            var needslowdowntime = linearpathspeed * SpeedOverride * realvirtualController.SpeedOverride /
                                   CurrentTarget.LinearAcceleration;
            if (needslowdowntime >= availslowdowntime)
                slowdownneeded = true;

            // Accelereation needed
            if (!linearacceleration && !lineardeceleration && !slowdownneeded && linearpathspeed <=
                CurrentTarget.LinearSpeedToTarget * SpeedOverride * realvirtualController.SpeedOverride)
            {
                linearpathacceleration = CurrentTarget.LinearAcceleration;
                linearacceleration = true;
            }

            // Deceleration needed
            if (slowdownneeded && !lineardeceleration)
            {
                linearpathacceleration = -CurrentTarget.LinearAcceleration;
                lineardeceleration = true;
            }

            // Limit Speed
            if (!lineardeceleration && linearpathspeed > CurrentTarget.LinearSpeedToTarget * SpeedOverride *
                realvirtualController.SpeedOverride * realvirtualController.SpeedOverride)
            {
                linearpathacceleration = 0;
                linearpathspeed = CurrentTarget.LinearSpeedToTarget * SpeedOverride *
                                  realvirtualController.SpeedOverride;
            }


            // Calculate Speed
            linearpathspeed = linearpathspeed + linearpathacceleration * Time.fixedDeltaTime;

            // Callculate new position
            LinearPathPos = LinearPathPos + Time.fixedDeltaTime * linearpathspeed * SpeedOverride *
                realvirtualController.SpeedOverride;


            var path = CurrentTarget.GetCorrectedGlobalPosition() - LinearPathStartPos;
            var pathpercent = (LinearPathPos / realvirtualController.Scale) / path.magnitude;

            var endpath = false;

            // At destination
            if (lineardeceleration && pathpercent >= 1 || lineardeceleration && linearpathspeed < 0)
            {
                pathpercent = 1;
                endpath = true;
            }
         
            PositionOnPath = LinearPathStartPos + path * pathpercent;
          
            var rotation = Quaternion.Lerp(LinearPathStartRot, CurrentTarget.transform.rotation, pathpercent);

            var ispossible = RobotIK.PositionRobotGlobal(PositionOnPath, rotation, CurrentTarget.Solution,CurrentTarget.TurnCorrection);

            if (!ispossible)
            {
                Error("Solution on the linear path [" + this.name + "] to Target [" + CurrentTarget.name +
                      "]is not possible");
            }

            if (DebugPath)
            {
                 Global.DebugGlobalPoint(PositionOnPath, Color.green, 200f);
                 Global.DebugGlobalPoint(RobotIK.GetTCPPosGlobal(), Color.red, 200f);
            }

            if (endpath)
            {
                LastPlannedPosition = CurrentTarget.GetCorrectedGlobalPosition();
                AtTarget();
            }
        }

        private bool SetRobotIK()
        {
            if (RobotIK == null)
                RobotIK = GetComponentInParent<RobotIK>();
            if (RobotIK == null)
            {
                Error("Path needs to be a child of a RobotIK component");
                return false;
            }

            return true;
        }


        private void FixedUpdate()
        {
            if (WaitForSignal)
                if (waitforsignal.Value == true)
                    ReadyForCheckNextTarget();

            if (signalstartnotnull)
                StartPath = SignalStart.Value;

            if (!startbefore && StartPath && !PathIsActive && !waitforstart)
                startPath();

            if (LinearPathActive)
            {
                PositionOnLinearPath();
              
            }
       
            if (!waitforstart)
                startbefore = StartPath;
        }
    }
}