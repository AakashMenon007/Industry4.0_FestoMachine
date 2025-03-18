// realvirtual (R) Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 realvirtual GmbH - Usage of this source code only allowed based on License conditions see https://realvirtual.io/en/company/license

using realvirtual;

#if REALVIRTUAL_PLAYMAKER

namespace HutongGames.PlayMaker.Actions
{

	[ActionTarget(typeof(Drive), "gameObject")]              
	[ActionCategory("realvirtual")]
	[Tooltip("Wait for a sensor signal")]
public class WaitForSensor : FsmStateAction
{
		public FsmOwnerDefault Sensor;
		public FsmBool WaitForOccupied;

		private Sensor _sensor;
		
		public override void Reset()
		{
			base.Reset();
			if (this.State != null)
			this.State.ColorIndex = 4;
		}


		public override string ErrorCheck()
		{
			string error = "";
      
			if (Fsm.GetOwnerDefaultTarget(Sensor)==null)
			{
				error = "realvirtual no Sensor component selected";
			}
			else
			{
				if (Fsm.GetOwnerDefaultTarget(Sensor).GetComponent<Sensor>()==null)
				{
					error = "realvirtual Sensor component missing at this GameObject";
				}
			}

			return error;

		}
		
		public override void OnEnter()
		{
		
			_sensor = Fsm.GetOwnerDefaultTarget(Sensor).GetComponent<Sensor>();
		
		}

		public override void OnUpdate()
		{
			if (_sensor.Occupied && WaitForOccupied.Value)
				Finish();
			if (!_sensor.Occupied && !WaitForOccupied.Value)
				Finish();

		}
}

}
#endif
