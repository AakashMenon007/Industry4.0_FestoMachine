// realvirtual.io (formerly game4automation) (R) a Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 realvirtual GmbH - Usage of this source code only allowed based on License conditions see https://realvirtual.io/unternehmen/lizenz  

using UnityEngine;

namespace realvirtual
{
    [HelpURL("https://doc.realvirtual.io/components-and-scripts/defining-logic/logicsteps")]
    public class LogicStep_SetSignalBool : LogicStep
    {
        public Signal Signal;
        public bool SetToTrue;

        private bool signalnotnull = false;
        
        protected new bool NonBlocking()
        {
            return true;
        }
        
        protected override void OnStarted()
        {
            State = 50;
            if (signalnotnull)
                Signal.SetValue((bool)SetToTrue);
            NextStep();
        }
        
        protected new void Start()
        {
            signalnotnull = Signal != null;
            base.Start();
        }
        
      
    }

}

