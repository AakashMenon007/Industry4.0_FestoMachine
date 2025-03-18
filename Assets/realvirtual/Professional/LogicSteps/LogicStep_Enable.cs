// realvirtual.io (formerly game4automation) (R) a Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 realvirtual GmbH - Usage of this source code only allowed based on License conditions see https://realvirtual.io/unternehmen/lizenz  

using UnityEngine;

namespace realvirtual
{
    [HelpURL("https://doc.realvirtual.io/components-and-scripts/defining-logic/logicsteps")]
    public class LogicStep_Enable: LogicStep
    {
        public GameObject Gameobject;
        public bool Enable;

 
        
        protected new bool NonBlocking()
        {
            return true;
        }
        
        protected override void OnStarted()
        {
            State = 50;
            if (Gameobject!=null)
                Gameobject.SetActive(Enable);
            NextStep();
        }

    }

}

