// realvirtual (R) Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 realvirtual GmbH - Usage of this source code only allowed based on License conditions see https://realvirtual.io/en/company/license

#if UNITY_STANDALONE_WIN
using UnityEditor;
using UnityEngine;

namespace realvirtual
{
    [CustomEditor(typeof(PLCSIMAdvancedInterface))]
    public class PLCSIMAdvanedInterfaceEditor : UnityEditor.Editor {

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
        
            PLCSIMAdvancedInterface myScript = (PLCSIMAdvancedInterface)target;
            if(GUILayout.Button("Import Signals"))
            {
                myScript.ImportSignals(false);
            }
        }

    }
}
#endif