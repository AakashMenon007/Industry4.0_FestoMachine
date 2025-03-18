// realvirtual (R) Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 realvirtual GmbH - Usage of this source code only allowed based on License conditions see https://realvirtual.io/en/company/license
#if (UNITY_EDITOR && !UNITY_EDITOR_OSX && !UNITY_EDITOR_LINUX && !UNITY_ANDROID && !UNITY_IOS && !UNITY_WEBGL)
using UnityEditor;
using UnityEngine;

namespace realvirtual
{
    [CustomEditor(typeof(SharedMemoryInterface))]
    public class SharedMemoryInterfaceEditor : UnityEditor.Editor {

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
        
            SharedMemoryInterface myScript = (SharedMemoryInterface)target;
            if(GUILayout.Button("Import Signals"))
            {
                myScript.ImportSignals(false);;
            }
        }
    }
}
#endif