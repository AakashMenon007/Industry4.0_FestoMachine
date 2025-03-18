// realvirtual.io (formerly game4automation) (R) a Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 realvirtual GmbH - Usage of this source code only allowed based on License conditions see https://realvirtual.io/unternehmen/lizenz  

using UnityEngine;

namespace realvirtual
{
    [HelpURL("https://doc.realvirtual.io/components-and-scripts/defining-logic/logicsteps")]
    public class LogicStep_Delay : LogicStep
    {

        public float Duration;

        private float starttime;
        
        protected override void OnStarted()
        {
            State = 0;
            Invoke("Finished",Duration);
            starttime = Time.time;
        }
        

        private void Finished()
        {
            NextStep();
        }

        public void FixedUpdate()
        {
            if (StepActive)
            {
                var delta = ((Time.time - starttime) / Duration*100);
                State = delta;
            }
        }

   
    }

}

