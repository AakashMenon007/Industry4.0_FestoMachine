#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace realvirtual
{
    public class AutoConnectExample : AutoConnectBase
    {
        
        public override bool AutoConnect(BehaviorInterface component)
        {
            var connected = false;
            var signalname = "";
            Debug.Log($" Checking AutoConnect for " +component.name);

            /// Here you can define your connection rules, create signals and assign them to the behavior models
            if (component.GetType() == typeof(Drive_Simple))
            {
                signalname = $"StartDrive-{component.name}";   // define Signal name based on custom rule
                var signal = GetOrCreateSignal<PLCOutputBool>(signalname);   // create the signal if not already there
                ((Drive_Simple) component).Forward = signal; // connect the signal to the behavior model
#if UNITY_EDITOR
                EditorUtility.SetDirty(component);
#endif
                connected = true;
            }
            
            return connected;
        }
    }
}

