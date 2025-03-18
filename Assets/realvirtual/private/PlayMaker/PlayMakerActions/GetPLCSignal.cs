// realvirtual (R) Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 realvirtual GmbH - Usage of this source code only allowed based on License conditions see https://realvirtual.io/en/company/license

using realvirtual;

#if REALVIRTUAL_PLAYMAKER

namespace HutongGames.PlayMaker.Actions
{

	[ActionTarget(typeof(Drive), "gameObject")]              
	[ActionCategory("realvirtual")]
	[Tooltip("Gets a PLC signal")]
public class GetPLCSignal : FsmStateAction
{
		public FsmOwnerDefault Signal;
		public FsmBool GetBool;
		public FsmFloat GetFloat;
		public FsmInt GetInt;

		private Signal _signal;
		
		public override void Reset()
		{
			base.Reset();
			if (this.State != null)
			this.State.ColorIndex = 4;
		}


		public override string ErrorCheck()
		{
			string error = "";
      
			if (Fsm.GetOwnerDefaultTarget(Signal)==null)
			{
				error = "realvirtual no Signal component selected";
			}
			else
			{
				if (Fsm.GetOwnerDefaultTarget(Signal).GetComponent<Signal>()==null)
				{
					error = "realvirtual Signal component missing at this GameObject";
				}
			}

			return error;

		}
		
		public override void OnEnter()
		{
		
			_signal = Fsm.GetOwnerDefaultTarget(Signal).GetComponent<Signal>();
			
			if (_signal is PLCOutputInt || _signal is PLCInputInt)
				GetInt.Value = (int)_signal.GetValue();


			if (_signal is PLCOutputFloat || _signal is PLCInputFloat)
				GetFloat.Value = (float)_signal.GetValue();

			if (_signal is PLCOutputBool || _signal is PLCInputBool)
				GetBool.Value = (bool)_signal.GetValue();
		
			Finish();
		}

	
}

}
#endif
