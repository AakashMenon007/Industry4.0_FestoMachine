using UnityEngine;

namespace realvirtual
{
    public class MeasurePLCCycleTime : MonoBehaviour
    {
        public float PLCCycleTimeMs;
        public float CommCycleTimeMs;

        public float NumMeasures;
    
        private PLCOutputFloat cyclenum;

        private float lastcyclenum;
        private float startmeasure;
        private float lastchangecyclenum;
        private float startmeasuretime;
        // Start is called before the first frame update
        void Start()
        {
            cyclenum = GetComponent<PLCOutputFloat>();
        }

        // Update is called once per frame
        void FixedUpdate()
        {
            var currycle = cyclenum.Value;
            if (startmeasure == 0)
            {
                startmeasure = currycle;
                startmeasuretime = Time.unscaledTime;
            }

            if (currycle - startmeasure > NumMeasures)
            {
                var deltatime = Time.unscaledTime - startmeasuretime;
                var numcycles = currycle - startmeasure;
                PLCCycleTimeMs = deltatime / numcycles * 1000;
                startmeasure = 0;

            }
        
            // check value changed
            if (currycle != lastcyclenum)
            {
                var deltatime = Time.unscaledTime - lastchangecyclenum;
                CommCycleTimeMs = deltatime * 1000;
                lastchangecyclenum = Time.unscaledTime;
            }
            
            lastcyclenum = currycle;
        }
    }
}

