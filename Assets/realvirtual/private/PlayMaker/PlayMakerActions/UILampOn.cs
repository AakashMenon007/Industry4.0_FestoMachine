// realvirtual (R) Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 realvirtual GmbH - Usage of this source code only allowed based on License conditions see https://realvirtual.io/en/company/license

using realvirtual;

#if REALVIRTUAL_PLAYMAKER

namespace HutongGames.PlayMaker.Actions
{
    [ActionTarget(typeof(Drive), "gameObject")]
    [ActionCategory("realvirtual")]
    [Tooltip("Stop Game4Automatopm Drive")]
    public class UILampOn : FsmStateAction
    {
        public FsmOwnerDefault Lamp;
        public FsmBool TurnLampOn = true;
        private UILamp _lamp;
       

       
        public override string ErrorCheck()
        {
            string error = "";
      
            if (Fsm.GetOwnerDefaultTarget(Lamp)==null)
            {
                error = "realvirtual no Lamp component selected";
            }
            else
            {
                if (Fsm.GetOwnerDefaultTarget(Lamp).GetComponent<UILamp>()==null)
                {
                    error = "realvirtual UILamp component missing at this GameObject";
                }
            }

            return error;

        }
        
          
        public override void Reset()
        {
            base.Reset();
            if (this.State != null)
            this.State.ColorIndex = 1;

        }

        public override void OnEnter()
        {
            _lamp = Fsm.GetOwnerDefaultTarget(Lamp).GetComponent<UILamp>();
            if (_lamp != null)
            {
                _lamp.LampIsOn = TurnLampOn.Value;
            }

            Finish();
        }
    }
}
#endif