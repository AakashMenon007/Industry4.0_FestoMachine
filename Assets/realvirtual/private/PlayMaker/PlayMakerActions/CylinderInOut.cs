// realvirtual (R) Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 realvirtual GmbH - Usage of this source code only allowed based on License conditions see https://realvirtual.io/en/company/license


using realvirtual;


#if REALVIRTUAL_PLAYMAKER

namespace HutongGames.PlayMaker.Actions
{
    [ActionTarget(typeof(Drive), "gameObject")]
    [ActionCategory("realvirtual")]
    [Tooltip("Stop Game4Automatopm Motor ")]
    public class CylinderInOut : FsmStateAction
    {
        public FsmOwnerDefault Cylinder;
        public FsmBool MoveOut;
        private Drive_Cylinder _cylinder;
       

        public override void Reset()
        {
            base.Reset();
            if (this.State != null)
            this.State.ColorIndex = 2;
        }

        public override string ErrorCheck()
        {
            string error = "";
      
            if (Fsm.GetOwnerDefaultTarget(Cylinder)==null)
            {
                error = "realvirtual no Cylinder component selected";
            }
            else
            {
                if (Fsm.GetOwnerDefaultTarget(Cylinder).GetComponent<Drive_Cylinder>()==null)
                {
                    error = "realvirtual Cylinder component missing at this GameObject";
                }
            }

            return error;

        }

        public override void OnEnter()
        {
            _cylinder = Fsm.GetOwnerDefaultTarget(Cylinder).GetComponent<Drive_Cylinder>();
            if (_cylinder != null)
            {
                if (MoveOut.Value)
                {
                    _cylinder._out=true;
                    _cylinder._in = false;
                }
                else
                {
                    _cylinder._out=false;
                    _cylinder._in = true;
                }
            }
        }

        public override void OnUpdate()
        {
            if (MoveOut.Value && _cylinder._isOut)
            {
                Finish();
            }

            if (MoveOut.Value == false && _cylinder._isIn)
            {
                Finish();
            }
        }
    }
}
#endif