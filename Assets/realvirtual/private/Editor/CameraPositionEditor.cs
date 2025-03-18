// realvirtual (R) Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 realvirtual GmbH - Usage of this source code only allowed based on License conditions see https://realvirtual.io/en/company/license

using UnityEditor;
using UnityEngine;

namespace realvirtual
{
    [CustomEditor(typeof(CameraPosition))]
    //! Editor class for Get Position and SetPosition of CameraPosition
    public class CameraPositionEditor : UnityEditor.Editor {

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
        
            CameraPosition myScript = (CameraPosition)target;
            if(GUILayout.Button("Get Position"))
            {
                myScript.GetCameraPosition();
            }
            if(GUILayout.Button("Set Position"))
            {
                myScript.SetCameraPosition();
            }
        }

    }
}