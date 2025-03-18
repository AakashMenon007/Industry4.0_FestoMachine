// realvirtual (R) Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 realvirtual GmbH - Usage of this source code only allowed based on License conditions see https://realvirtual.io/en/company/license

using UnityEditor;
using UnityEngine;


namespace realvirtual
{
   [CustomEditor(typeof(OPCUA_Interface))]
    //! OPCUAInterface Editor class for the Unity Inspector window
   public class OPCUAInterfaceEditor : UnityEditor.Editor {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            OPCUA_Interface myScript = (OPCUA_Interface) target;
            if (GUILayout.Button("Import nodes"))
            {
                myScript.EditorImportNodes();
            }
        }
    } 
}