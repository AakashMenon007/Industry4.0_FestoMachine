// realvirtual (R) Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 realvirtual GmbH - Usage of this source code only allowed based on License conditions see https://realvirtual.io/en/company/license

using UnityEngine;

namespace realvirtual
{
    //! Behavior model which is just connecting an PLCOutput to an PLCInput
    public class SignalChart : BehaviorInterface
    {
        public AnimationCurve Chart;
        public float RecordAfterSeconds = 2;
        public bool Record = true;
        private Signal thissignal;
        private bool signalnotnull;
        
        // Start is called before the first frame update
        void Start()
        {
           

            thissignal = GetComponent<Signal>();
            signalnotnull = thissignal != null;
        }

        // Update is called once per frame
        void FixedUpdate()
        {
            if (signalnotnull)
            {
                if (Record && Time.fixedTime>= RecordAfterSeconds )
                Chart.AddKey(Time.fixedTime, (float) thissignal.GetValue());
            }
            
        }
    }
}

