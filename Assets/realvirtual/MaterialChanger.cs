using UnityEngine;
using NaughtyAttributes;

namespace realvirtual
{
    [HelpURL("https://doc.realvirtual.io/components-and-scripts/materialchanger")]
    public class MaterialChanger : realvirtualBehavior,ISignalInterface
    {
        
        public Material MaterialOff;
        public Material MaterialOn;

        [OnValueChanged("ChangeDisplay")]public bool StatusOn;
        public bool ChangeOnCollission;
        public Sensor ChangeOnSensor;
        public PLCOutputBool ChangeOnPLCOutput;

        private MeshRenderer meshrenderer;
        
        void Start()
        {
            meshrenderer = GetComponent<MeshRenderer>();
            if (MaterialOff == null)
                MaterialOff = meshrenderer.material;
            
            if (ChangeOnPLCOutput != null)
                    ChangeOnPLCOutput.EventSignalChanged.AddListener(OnPLCOutputOnSignalChanged);
            
            if (ChangeOnSensor != null)
                ChangeOnSensor.EventNonMUGameObjectSensor.AddListener(OnSensor);
        }

        private void OnCollisionEnter(Collision other)
        {
            StatusOn = true;
            ChangeDisplay();
        }

        private void OnCollisionExit(Collision other)
        {
            StatusOn = false;
            ChangeDisplay();
        }

        private void OnSensor(GameObject obj, bool occupied)
        {
            StatusOn = occupied;
            ChangeDisplay();
        }

        private void OnPLCOutputOnSignalChanged(Signal obj)
        {
            StatusOn = ((PLCOutputBool) obj).Value;
            ChangeDisplay();
        }

        // Update is called once per frame
        void ChangeDisplay()
        {
            if (StatusOn)
                meshrenderer.material = MaterialOn;
            else
                meshrenderer.material = MaterialOff;
        }
    }
}
