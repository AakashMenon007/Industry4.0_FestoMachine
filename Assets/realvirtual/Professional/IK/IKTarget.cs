// realvirtual.io (formerly game4automation) (R) a Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 realvirtual GmbH - Usage of this source code only allowed based on License conditions see https://realvirtual.io/unternehmen/lizenz  

using NaughtyAttributes;
using UnityEditor;
using UnityEngine;

namespace realvirtual
{
    [HelpURL("https://doc.realvirtual.io/components-and-scripts/motion/robot-inverse-kinematics")]
    [ExecuteInEditMode]
    //! Robot Inverse Kinematics - a target of the robot - the position and rotation of this gameobject transform
    public class IKTarget : BehaviorInterface
    {
        [System.Serializable]
        public enum Interploation 
        {
            PointToPoint,
            PointToPointUnsynced,
            Linear
        };

        [OnValueChanged("ChangedFollowInEditMode")]
        public bool FollowInEditMode=true; //!< If true the target will follow the robot in edit mode

        [Header("Right Handed Robot Coordinates")] [realvirtual.ReadOnly]
        public Vector3 PosRH; //!< Position of the target in right handed robot coordinates
        
        [realvirtual.ReadOnly] public Vector3 RotRH; //!< Rotation of the target in right handed robot coordinates

        [Header("Path Parameters")] [Range(0, 1)]
        public float SpeedToTarget = 1;//!< Speed to target in percent
        public float LinearAcceleration = 100;  //!< Linear acceleration in mm/s^2
        public Interploation InterpolationToTarget;//!< Interpolation to target
        public float LinearSpeedToTarget = 500; //!< Linear speed to target in mm/s
        public bool TurnCorrection; //!< If true the robot will do the 180 degree turn correction of axis 4 and 6
        public float[] AxisCorrection = new float[6]; //!< Correction of the axis in degrees
        [realvirtual.ReadOnly]public Vector3 PositionCorrection = new Vector3(0, 0, 0); //!< Correction of the position (because of inaccuracies of the robotik)
        [realvirtual.ReadOnly]public float PositionFailure = 0;//!< Position failure in m
        [Header("On At Target")] public PLCInputBool SetSignal; //!< Set Signal if target is reached
        public float SetSignalDuration = 0.5f; //!< Duration of the set signal in seconds
        public float WaitForSeconds = 0; //!< Wait for seconds after target is reached
        public PLCOutputBool WaitForSignal; //!< Wait for signal after target is reached
        [Header("Control")] [Range(0, 7)] public int Solution; //!< Solution to use of the IK for this target
        [realvirtual.ReadOnly] public string[] Solutions = new string[8]; //!< Solutions of the IK for this target
        [realvirtual.ReadOnly] public float[] AxisPos = new float[6];//!< Axis positions of the IK for this target
        [realvirtual.ReadOnly] public RobotIK RobotIK; //!< Robot IK of the robot
        [realvirtual.ReadOnly] public bool Reachable; //!< If true the target is reachable by the robot


        private bool waitforsignalnotnull;
        private bool setsignalnotnull;


        public void OnAtTarget()
        {
            if (setsignalnotnull)
                SetSignal.Value = true;
        }
        public Vector3 GetCorrectedGlobalPosition()
        {
            var newpos = this.transform.position + RobotIK.transform.TransformVector(PositionCorrection);
            return newpos;
        }

        public void OnLeaveTarget()
        {
            if (setsignalnotnull)
                Invoke("ResetSignal",SetSignalDuration);
        }

        private void ResetSignal()
        {
            SetSignal.Value = false;
        }
        new void Awake()
        {
            waitforsignalnotnull = WaitForSignal != null;
            setsignalnotnull = SetSignal != null;
            base.Awake();
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
        
        void ChangedFollowInEditMode()
        {
            if (FollowInEditMode == false)
                RobotIK.TargetMoveEditMode(false);
        }

        private void OnDrawGizmosSelected()
        {
            if (!Reachable)
            {
                Gizmos.color = new Color(1, 0, 0, 0.6f);
                Gizmos.DrawSphere(transform.position, 0.05f);
            }
        }

        [ExecuteInEditMode]
        private void OnDestroy()
        {
            if (!Application.isPlaying)
            {
                var ikpath = GetComponentInParent<IKPath>();
            
                if (ikpath != null)
                {
                    ikpath.OnTargetDelete(this);
                    
                }
            }
        }

        public void SetAsTarget()
        {
           
            if (SetRobotIK())
            {
                RobotIK.Target = this;
                RobotIK.FollowTarget = true;
                RobotIK.TargetMoveEditMode(true);
            }
        }

        [Button("Drive to target")]
        public void DriveTo()
        {
            var ikpath = GetComponentInParent<IKPath>();
            
            if (ikpath != null)
            {
                ikpath.DriveToTarget(this);
            }
        }

        [Button("Set Correction")]
        public void SetCorrection()
        {
            var delta =  RobotIK.transform.InverseTransformPoint(this.transform.position) - RobotIK.transform.InverseTransformPoint(RobotIK.GetTCPPosGlobal());
            PositionCorrection = PositionCorrection +delta;
            PositionFailure = PositionCorrection.magnitude;
        }
        
        [Button("Clear Correction")]
        public void ClearCorrection()
        {
            PositionCorrection = Vector3.zero;
        }



        // Update is called once per frame
        void Update()
        {
            if (!Application.isPlaying)
                if (transform.hasChanged)
                    if (FollowInEditMode)
                    {
                        #if UNITY_EDITOR
                        if (Selection.activeGameObject == this.gameObject)
                        {
                            PositionFailure = (this.transform.position - RobotIK.GetTCPPosGlobal()).magnitude;
                            SetAsTarget();
                        }
                           
                        #endif
                    }
        }
    }
}