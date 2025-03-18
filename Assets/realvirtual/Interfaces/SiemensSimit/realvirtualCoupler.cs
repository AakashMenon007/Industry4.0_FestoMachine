// realvirtual (R) Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 realvirtual GmbH - Usage of this source code only allowed based on License conditions see https://realvirtual.io/en/company/license


#if REALVIRTUAL_SIEMENSSIMIT

using CouplingToolbox;

using UnityEngine;

namespace realvirtual
{

    public class realvirtualCoupler : SignalComponent
    {
        // Start is called before the first frame update
        [ReadOnly] public realvirtual.Signal[] signals;

        public void ConnectSignals()
        {
            signals = GetComponentsInChildren<realvirtual.Signal>();

            foreach (var signal in signals)
            {
                var name = signal.name;
                if (signal.Name != null)
                    if (signal.Name != "")
                        name = signal.Name;
                var type = signal.GetType();
                if (signal.IsInput())
                {
                    if (type == typeof(PLCInputBool))
                        AddOutputSignal(name, SignalType.Binary);
                    if (type == typeof(PLCInputFloat))
                        AddOutputSignal(name, SignalType.Analog);
                    if (type == typeof(PLCInputInt))
                        AddOutputSignal(name, SignalType.Integer);
                }

                if (!signal.IsInput())
                {
                    if (type == typeof(PLCOutputBool))
                        AddInputSignal(name, SignalType.Binary);
                    if (type == typeof(PLCOutputFloat))
                        AddInputSignal(name, SignalType.Analog);
                    if (type == typeof(PLCOutputInt))
                        AddInputSignal(name, SignalType.Integer);
                }
            }
        }

        public void UpdateSignals()
        {


            if (signals == null)
                Debug.LogError("No Signals defined in realvirtual.io Simit Interface");
            foreach (var signal in signals)
            {
                var name = signal.name;
                if (signal.Name != "")
                    name = signal.Name;

                var type = signal.GetType();
                if (!signal.IsInput())
                {
                    if (type == typeof(PLCOutputBool))
                        ((PLCOutputBool) signal).Value = GetInputSignal(name).BinaryValue;
                    if (type == typeof(PLCOutputFloat))
                        ((PLCOutputFloat) signal).Value = (float) (GetInputSignal(name).AnalogValue);
                    if (type == typeof(PLCOutputInt))
                        ((PLCOutputInt) signal).Value = (int) (GetInputSignal(name).IntegerValue);
                }

                if (signal.IsInput())
                {
                    if (type == typeof(PLCInputBool))
                        GetOutputSignal(signal.Name).BinaryValue = ((PLCInputBool) signal).Value;
                    if (type == typeof(PLCInputFloat))
                        GetOutputSignal(signal.Name).AnalogValue = ((PLCInputFloat) signal).Value;
                    if (type == typeof(PLCInputInt))
                        GetOutputSignal(signal.Name).IntegerValue = ((PLCInputInt) signal).Value;
                }
            }
        }



        public override void InitializeSignals()
        {
            Debug.Log("realvirtual.io - Initialize Signals Siemens Simit Coupler");
            base.InitializeSignals();
            ConnectSignals();
        }

        public void Init()
        {
            InitializeSignals();
        }


        public void PreStep(float timeStep)
        {

        }
    }
}
#else
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SiemensSimitCoupler : MonoBehaviour
{
   
}

#endif

