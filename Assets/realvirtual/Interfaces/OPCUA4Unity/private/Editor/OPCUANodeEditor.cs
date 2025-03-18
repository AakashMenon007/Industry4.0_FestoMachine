
using UnityEngine;
using UnityEditor;


namespace realvirtual
{
    [CustomEditor(typeof(OPCUA_Node))]
    [CanEditMultipleObjects]
    //! OPCUA_Node  Editor class for the Unity Inspector window
    public class OPCUANodeEditor : UnityEditor.Editor {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            OPCUA_Node myScript = (OPCUA_Node)target;
            if(GUILayout.Button("Read Node"))
            {
                if (myScript.Interface != null)
                {
                    myScript.Interface.Connect();
                    myScript.ReadNode();
                    myScript.Interface.Disconnect();
                }
            }
        }
    } 
}
