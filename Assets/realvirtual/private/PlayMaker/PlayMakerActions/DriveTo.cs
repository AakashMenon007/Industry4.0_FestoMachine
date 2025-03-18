// realvirtual (R) Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 realvirtual GmbH - Usage of this source code only allowed based on License conditions see https://realvirtual.io/en/company/license

using UnityEngine;
using realvirtual;

#if REALVIRTUAL_PLAYMAKER

namespace HutongGames.PlayMaker.Actions
{
    [ActionTarget(typeof(Drive), "gameObject")]
    [ActionCategory("realvirtual")]
    [Tooltip("Drive Game4Automatopm Drive to a given destination")]
    public class DriveTo : FsmStateAction
    {
        public FsmOwnerDefault Drive;
        public FsmFloat Destination;
        public FsmBool Incremental=false;
        public FsmBool SetSpeed;
        public FsmFloat Speed;
        public FsmBool SetAcceleration;
        public FsmFloat Acceleration;
        public FsmBool NameAutomatic;

        private Drive _drive;
        // Code that runs on entering the state.

        public override void Reset()
        {
            base.Reset();
            if (this.State != null)
            this.State.ColorIndex = 3;

        }

        public virtual void OnActionTargetInvoked(object targetObject)
        {
            Debug.Log("Action Target INvoked");
        }

    


        public override string ErrorCheck()
        {
            string error = "";
      
            if (Fsm.GetOwnerDefaultTarget(Drive)==null)
            {
                error = "realvirtual no Drive component selected";
            }
            else
            {
                if (Fsm.GetOwnerDefaultTarget(Drive).GetComponent<Drive>()==null)
                {
                    error = "realvirtual Drive component missing at this GameObject";
                }
            }

            return error;

        }

        public void DriveAtPosition(Drive drive)
        {
            _drive.OnAtPosition -= DriveAtPosition;
            Finish();
        }

  

        public override void OnEnter()
        {
            _drive = Fsm.GetOwnerDefaultTarget(Drive).GetComponent<Drive>();
            if (_drive != null)
            {
                if (_drive.CurrentPosition != Destination.Value)
                {
                    _drive.OnAtPosition += DriveAtPosition;

                    if (SetAcceleration.Value)
                        _drive.Acceleration = Acceleration.Value;
                    if (SetSpeed.Value)
                        _drive.TargetSpeed = Speed.Value;
                    if (Incremental.Value)
                        _drive.DriveTo(_drive.CurrentPosition+ Destination.Value); 
                     else
               
                     _drive.DriveTo(Destination.Value);
                }
                else
                {
                    Finish();
                }
            }
        }

        // Code that runs when exiting the state.
        public override void OnExit()
        {
        }
    }
}
#endif