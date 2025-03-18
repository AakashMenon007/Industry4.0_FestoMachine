// realvirtual (R) Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 realvirtual GmbH - Usage of this source code only allowed based on License conditions see https://realvirtual.io/en/company/license

using realvirtual;


#if REALVIRTUAL_PLAYMAKER

namespace HutongGames.PlayMaker.Actions
{
    [ActionTarget(typeof(Drive), "gameObject")]
    [ActionCategory("realvirtual")]
    [Tooltip("Stop realvirtual Drive")]
    public class DriveStop : FsmStateAction
    {
        public FsmOwnerDefault Drive;
        private Drive _drive;

       
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
        
          
        public override void Reset()
        {
            base.Reset();
            if (this.State != null)
            this.State.ColorIndex = 4;

        }

        public override void OnEnter()
        {
            _drive = Fsm.GetOwnerDefaultTarget(Drive).GetComponent<Drive>();
            if (_drive != null)
            {
                _drive.JogForward = false;
                _drive.JogBackward = false;
            }

            Finish();
        }
    }
}
#endif