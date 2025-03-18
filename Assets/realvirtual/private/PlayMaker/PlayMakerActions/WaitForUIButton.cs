// realvirtual (R) Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 realvirtual GmbH - Usage of this source code only allowed based on License conditions see https://realvirtual.io/en/company/license

using realvirtual;

#if REALVIRTUAL_PLAYMAKER

namespace HutongGames.PlayMaker.Actions
{

	[ActionTarget(typeof(Drive), "gameObject")]              
	[ActionCategory("realvirtual")]
	[Tooltip("Wait for UI Button")]
public class WaitForUIButton : FsmStateAction
{
		public FsmOwnerDefault Button;
		public FsmBool WaitForOn;

		private UIButton _button;
		
		public override void Reset()
		{
			base.Reset();
			if (this.State != null)
			this.State.ColorIndex = 4;
		}


		public override string ErrorCheck()
		{
			string error = "";
      
			if (Fsm.GetOwnerDefaultTarget(Button)==null)
			{
				error = "realvirtual no Button component selected";
			}
			else
			{
				if (Fsm.GetOwnerDefaultTarget(Button).GetComponent<UIButton>()==null)
				{
					error = "realvirtual UIButton component missing at this GameObject";
				}
			}

			return error;

		}
		
		public override void OnEnter()
		{
		
			_button = Fsm.GetOwnerDefaultTarget(Button).GetComponent<UIButton>();
		
		}

		public override void OnUpdate()
		{
			if (_button.IsOn && WaitForOn.Value)
				Finish();
			if (!_button.IsOn && !WaitForOn.Value)
				Finish();
		}
}

}
#endif
