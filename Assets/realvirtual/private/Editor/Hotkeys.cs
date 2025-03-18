// realvirtual (R) Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 realvirtual GmbH - Usage of this source code only allowed based on License conditions see https://realvirtual.io/en/company/license

using System.Configuration;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace realvirtual
{
    [InitializeOnLoad]
    public static class Hotkeys
    {
        static GameObject active;
        private static realvirtualController g4a;


        public static void KeyUpEvent(KeyCode key)
        {
          
        }
        
        
        public static void KeyDownEvent(KeyCode key)
        {
            active = Selection.activeGameObject;
            g4a = UnityEngine.Object.FindObjectOfType<realvirtualController>();
            if (g4a == null)
                return;
            if (!g4a.EnableHotkeys)
                return;
            if (key == g4a.HotkeySource)
            {
                var source = g4a.StandardSource;
                    Debug.Log("Source created");
                    var newsource = (GameObject) PrefabUtility.InstantiatePrefab(source);
                    // check if drive or transportsurface is selected
                    if (active != null)
                    {
                        var drive = active.GetComponentInChildren<Drive>();
                        var surface = active.GetComponentInChildren<TransportSurface>();
                        if (!ReferenceEquals(surface, null))
                        {
                            var surfacemiddle = surface.GetMiddleTopPoint();
                            if (!ReferenceEquals(newsource, null))
                            {
                                var MU = newsource.GetComponent<MU>();
                                if (!ReferenceEquals(MU, null))
                                    MU.PlaceMUOnTopOfPosition(surfacemiddle);
                            }
                        }
                    }
                    return;
            }

            /// Turn on Ortho Views or Turn off if already on
            if (key == g4a.HotKeyOrthoViews)
            {
                var cont = Global.GetAllSceneComponents<OrthoViewController>();
                if (cont.Count == 1)
                {
                    cont[0].OrthoEnabled = !cont[0].OrthoEnabled;
                    if (active != null)
                        cont[0].transform.position = active.transform.position; 
                    cont[0].UpdateViews();
                }
                
            }
            
            // Align Ortho View to current object
            
            
            // Align Game Camera with current view
            
            

            if (key == g4a.HotkeyQuickEdit)
            {
                Global.QuickEditDisplay = !Global.QuickEditDisplay;
                EditorPrefs.SetBool( "realvirtual/Quick Edit Overlay", Global.QuickEditDisplay);
            } 
        }


        static Hotkeys()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

      

        private static void OnSceneGUI(SceneView sceneView)
        {
            Event current = Event.current;
          

            int controlID = GUIUtility.GetControlID(FocusType.Passive);

            if (Event.current.GetTypeForControl(controlID) == EventType.KeyDown)
            {
                KeyDownEvent(Event.current.keyCode);
            }
            
            if (Event.current.GetTypeForControl(controlID) == EventType.KeyUp)
            {
                KeyUpEvent(Event.current.keyCode);
            }
        }
    }


    [InitializeOnLoad]
    public class CustomHierarchyView
    {
        private static bool keydown = false;

        static CustomHierarchyView()
        {
            EditorApplication.hierarchyWindowItemOnGUI += HierarchyWindowItemOnGUI;
        }

        static void HierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
        {
            int controlID = GUIUtility.GetControlID(FocusType.Passive);

            if (keydown == false)
            {
                if (Event.current.GetTypeForControl(instanceID) == EventType.KeyDown)
                {
                    Hotkeys.KeyDownEvent(Event.current.keyCode);
                    keydown = true;
                }
            }

            if (keydown == true)
            {
                if (Event.current.GetTypeForControl(instanceID) == EventType.KeyUp)
                {
                    keydown = false;
                }
            }
        }
    }
}