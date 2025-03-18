// realvirtual (R) Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 realvirtual GmbH - Usage of this source code only allowed based on License conditions see https://realvirtual.io/en/company/license

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace realvirtual
{
    [InitializeOnLoad]
    //! Quick Edit Overlay Window with Buttons in Scene view
    public static class QuickEdit
    {
        public static string selected = "";
        public static GameObject obj = null;
        public static bool noselection;
        public static float timescale = 1;
        public static float speedoverride = 1;
        public static bool stylesbuild = false;
        public static GUILayoutOption w3, w;
        private static GameObject PivotReference;
        private static SceneView thisscene;
        private static bool isShiftKeyDown;
        private static List<Drive> drives;

        private static Texture icondrive,
            iconkinematic,
            icondrivekin,
            iconinfloat,
            iconinbool,
            iconinint,
            iconoutint,
            iconoutbool,
            iconoutfloat,
            iconpivot,
            icon0local,
            icon0global,
            iconrotxplus,
            iconrotyplus,
            iconrotzplus,
            iconrotxminus,
            iconrotyminus,
            iconrotzminus,
            icontoempty,
            iconempty;

        public static GUILayoutOption w2;
        public static GUILayoutOption w6;
        private static Drive joggingdrive;
        private static Drive lastclickdrive;
        private static GUIStyle buttonstyle;
        private static bool drivesnotnull;
 
        public const string MenuName = "realvirtual/Quick Edit Overlay";
        private static string drivefilter;
        
        public delegate void OnQuickEditDrawDelegate(); //!< Delegate function for GameObjects leaving the Sensor.

        public static event OnQuickEditDrawDelegate OnQuickEditDraw;
        
        static QuickEdit()
        {
            Buildstyles();
            SceneView.duringSceneGui += OnSceneGUI;
            EditorApplication.playModeStateChanged += EditorApplicationOnplayModeStateChanged;
            Global.QuickEditDisplay = EditorPrefs.GetBool(MenuName, true);
   
        }

        private static void EditorApplicationOnplayModeStateChanged(PlayModeStateChange playModeStateChange)
        {
            if (playModeStateChange == PlayModeStateChange.EnteredPlayMode)
            {
                drives = Global.GetAllSceneComponents<Drive>();
            }

            drivesnotnull = drives != null;
        }

        static void Buildstyles()
        {
            icon0local = UnityEngine.Resources.Load("Icons/button-0local") as Texture;
            icondrive = UnityEngine.Resources.Load("Icons/button-drive") as Texture;
            iconkinematic = UnityEngine.Resources.Load("Icons/button-kinematic") as Texture;
            icondrivekin = UnityEngine.Resources.Load("Icons/button-drivekin") as Texture;
            iconinbool = UnityEngine.Resources.Load("Icons/button-inputbool") as Texture;
            iconinfloat = UnityEngine.Resources.Load("Icons/button-inputfloat") as Texture;
            iconinint = UnityEngine.Resources.Load("Icons/button-inputint") as Texture;

            iconoutbool = UnityEngine.Resources.Load("Icons/button-outputbool") as Texture;
            iconoutfloat = UnityEngine.Resources.Load("Icons/button-outputfloat") as Texture;
            iconoutint = UnityEngine.Resources.Load("Icons/button-outputint") as Texture;

            iconpivot = UnityEngine.Resources.Load("Icons/button-pivot") as Texture;
            icon0local = UnityEngine.Resources.Load("Icons/button-0local") as Texture;
            icon0global = UnityEngine.Resources.Load("Icons/button-0global") as Texture;

            iconrotxplus = UnityEngine.Resources.Load("Icons/button-xplus") as Texture;
            iconrotyplus = UnityEngine.Resources.Load("Icons/button-yplus") as Texture;
            iconrotzplus = UnityEngine.Resources.Load("Icons/button-zplus") as Texture;

            iconrotxminus = UnityEngine.Resources.Load("Icons/button-xminus") as Texture;
            iconrotyminus = UnityEngine.Resources.Load("Icons/button-yminus") as Texture;
            iconrotzminus = UnityEngine.Resources.Load("Icons/button-zminus") as Texture;

            iconempty = UnityEngine.Resources.Load("Icons/button-empty") as Texture;
            icontoempty = UnityEngine.Resources.Load("Icons/button-toempty") as Texture;

            w = GUILayout.Width(270);
            w2 = GUILayout.Width(135);
            w6 = GUILayout.Width(44);
            w3 = GUILayout.Width(88);
        }

     

        static void ZeroLocal()
        {
            var sel = Selection.activeGameObject;
            Undo.RecordObject ( sel.transform, "Transform to local zero");
            sel.transform.localPosition = Vector3.zero; 
        
        }

        static void ZeroGlobal()
        {
            var sel = Selection.activeGameObject;
            Undo.RecordObject ( sel.transform, "Transform to global zero");
            sel.transform.position = Vector3.zero;
        }
        
        
        static void NewEmpty()
        {
            var sel = Selection.activeGameObject;
            var go = new GameObject();
            Undo.RegisterCreatedObjectUndo (go, "Created GameObject");
             if (sel != null)
            go.transform.parent = sel.transform;
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            Selection.activeGameObject = go;
        }
        
        static void IntoEmpty()
        {
    
            var sel = Selection.activeGameObject;
            var go = new GameObject();
         
            go.transform.parent = sel.transform.parent;
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            var sels = Selection.gameObjects;
            for (int i = 0; i < sels.Length; i++)
            {
                sels[i].transform.parent = go.transform;

            }
            Global.SetExpandedRecursive(go,true);
            Selection.activeGameObject = go;
            EditorApplication.DirtyHierarchyWindowSorting();
        }

        
        static void AlignPivot()
        {


            if (isShiftKeyDown)
            {
                PivotReference = Selection.activeGameObject;
                return;
            }

            if (PivotReference == null)
                return;
            
            var sel = Selection.gameObjects;
            for (int i = 0; i < sel.Length; i++)
            {
                Undo.RecordObject (sel[i].transform, "Align Pivot");
                sel[i].transform.position = PivotReference.transform.position;
                sel[i].transform.rotation = PivotReference.transform.rotation;
            }

            PivotReference = null;
        }

        static void Rotation(Vector3 rotation)
        {
            Undo.RecordObject (obj.transform, "Rotation");
            obj.transform.rotation = obj.transform.rotation * Quaternion.Euler(rotation);
        }

        static List<Component> AddComponent(System.Type com)
        {
            var res = new List<Component>();
            var sel = Selection.gameObjects;
            for (int i = 0; i < sel.Length; i++)
            {
                res.Add(Undo.AddComponent(sel[i],com));
            }

            return res;
        }
        
        static void CreateSignal(System.Type com)
        {
            var sel = Selection.activeGameObject;
            var go = new GameObject();
            Undo.RegisterCreatedObjectUndo (go, "Create Signal");
            if (!noselection)
            {
                go.transform.parent = sel.transform;
                if (sel.GetComponent<Signal>() != null)
                    go.transform.parent = sel.transform.parent;
            }

            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.name = com.Name;
            go.AddComponent(com);
            Selection.activeGameObject = go;
        }

        static void ChangeSignal(string type)
        {
            var sel = Selection.activeGameObject;
            Undo.RegisterCreatedObjectUndo (sel, "Change Siglan Signal");
            var sig = sel.GetComponent<Signal>();
            if (sig.IsInput())
            {
                switch (type)
                {
                    case "int" :
                        sel.AddComponent<PLCOutputInt>();
                        break;
                    case "float" :
                        sel.AddComponent<PLCOutputFloat>();
                        break;
                    case "bool" :
                        sel.AddComponent<PLCOutputBool>();
                        break;
                }
            }
            else
            {
                switch (type)
                {
                    case "int" :
                        sel.AddComponent<PLCInputInt>();
                        break;
                    case "float" :
                        sel.AddComponent<PLCInputFloat>();
                        break;
                    case "bool" :
                        sel.AddComponent<PLCInputFloat>();
                        break;
                }
            }
            Object.DestroyImmediate(sig);
            
        }
        

        public static void EditModeGUI()
        {
        
            int controlID = GUIUtility.GetControlID(FocusType.Passive);
            var e = Event.current.GetTypeForControl(controlID);
           
                if (Event.current.GetTypeForControl(controlID) == EventType.KeyDown)
                {
                    if (Event.current.keyCode == KeyCode.LeftControl || (Event.current.keyCode == KeyCode.RightControl))
                    {
                        isShiftKeyDown = true;
                    } 
                }
                
                if (Event.current.GetTypeForControl(controlID)  == EventType.KeyUp)
                {
                    if (Event.current.keyCode == KeyCode.LeftControl || (Event.current.keyCode  == KeyCode.RightControl))
                    {
                        isShiftKeyDown = false;
                    }
                }


            
            noselection = false;

            if (Selection.objects == null)
                noselection = true;
            else
            {
                try
                {
                    if (Selection.objects[0] == null)
                        noselection = true;
                }
                catch
                {
                    noselection = true;
                }
                
            }
                
            var w3 = GUILayout.Width(88);

            var drive = obj.GetComponent<Drive>() != null;
            var signal = obj.GetComponent<Signal>() != null;
            var kinematic = obj.GetComponent<Kinematic>() != null;
            var transportsurface = false;
            if (obj.GetComponentInParent<TransportSurface>() != null)
                    transportsurface = true;
            var behavior = obj.GetComponent<BehaviorInterface>() != null;
#if REALVIRTUAL_PROFESSIONAL
            var logicsteps = obj.GetComponent<LogicStep>() != null;
#else
            var logicsteps = false;
#endif

            if (!signal && !noselection)
            {
                EditorGUILayout.LabelField(selected);
                selected = "";
                EditorGUILayout.BeginHorizontal(w);
                if (GUILayout.Button(icon0local))
                    ZeroLocal();

                if (GUILayout.Button(icon0global))
                    ZeroGlobal();

                if (PivotReference==null)
                    GUI.enabled = false;

                if (isShiftKeyDown)
                    GUI.enabled = true;
                
                var oldcol = GUI.backgroundColor;
                
                if (PivotReference!=null)
                       GUI.backgroundColor = Color.green;
                
                if (GUILayout.Button(iconpivot))
                    AlignPivot();
                GUI.backgroundColor = oldcol;
                
                EditorGUILayout.EndHorizontal();
                GUI.enabled = true;

                if (Selection.objects.Length == 1 && !noselection && !logicsteps)
                {
                    EditorGUILayout.BeginHorizontal(w);


                    if (GUILayout.Button(iconrotxplus))
                        Rotation(new Vector3(90, 0, 0));

                    if (GUILayout.Button(iconrotyplus))
                        Rotation(new Vector3(0, 90, 0));

                    if (GUILayout.Button(iconrotzplus))
                        Rotation(new Vector3(0, 0, 90));
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal(w);

                    if (GUILayout.Button(iconrotxminus))
                        Rotation(new Vector3(-90, 0, 0));

                    if (GUILayout.Button(iconrotyminus))
                        Rotation(new Vector3(0, -90, 0));

                    if (GUILayout.Button(iconrotzminus))
                        Rotation(new Vector3(0, 0, -90));

                    EditorGUILayout.EndHorizontal();
                }
            }

            //// Creating Gameobjects
            EditorGUILayout.BeginHorizontal(w);
            if (GUILayout.Button(iconempty))
                NewEmpty();

            if (noselection)
                GUI.enabled = false;
            if (GUILayout.Button(icontoempty))
                IntoEmpty();
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        
            

            if (!signal  && !noselection && !logicsteps)
            {
               
                EditorGUILayout.BeginHorizontal(w);
                if (!drive)
                    if (GUILayout.Button(icondrive))
                        AddComponent(typeof(Drive));
                if (!kinematic)
                    if (GUILayout.Button(iconkinematic))
                        AddComponent(typeof(Kinematic));
                if (!drive && !kinematic)
                    if (GUILayout.Button(icondrivekin))
                    {
                        AddComponent(typeof(Drive));
                        AddComponent(typeof(Kinematic));
                    }
                   
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal(w);
                if (GUILayout.Button("Transport Surface", w2))
                    AddComponent(typeof(TransportSurface));
                if (GUILayout.Button("Sensor", w2))
                {
                    AddComponent(typeof(Sensor));
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal(w);
                if (GUILayout.Button("Transport Guided", w2))
                {
                    var comps = AddComponent(typeof(Drive));
                    foreach (Drive comp in comps)
                    {
                        comp.Direction = DIRECTION.Virtual;
                        var transportGuided = comp.gameObject.AddComponent<TransportGuided>();
                        transportGuided.Init();
                    } 
                }
                
              
                   
                if (GUILayout.Button("Grip", w2))
                    AddComponent(typeof(Grip));
                EditorGUILayout.EndHorizontal();

                if (transportsurface)
                {
                    EditorGUILayout.BeginHorizontal(w);
                    if (GUILayout.Button("Guide Line", w2))
                        AddComponent(typeof(GuideLine));
                    if (GUILayout.Button("Guide Circle", w2))
                        AddComponent(typeof(GuideCircle));
                    EditorGUILayout.EndHorizontal();
                    
                }

                EditorGUILayout.BeginHorizontal(w);
                if (GUILayout.Button("Fixer", w2))
                    AddComponent(typeof(Fixer));
                if (GUILayout.Button("Joint", w2))
                    AddComponent(typeof(SimpleJoint));
                EditorGUILayout.EndHorizontal();
            
                if (drive)
                {
                     var oldcol = GUI.backgroundColor;
                    GUI.backgroundColor = Color.yellow;
                    EditorGUILayout.BeginHorizontal(w);
                    if (GUILayout.Button("Simple Drive", w2))
                        AddComponent(typeof(Drive_Simple));
                    if (GUILayout.Button("Cylinder", w2))
                        AddComponent(typeof(Drive_Cylinder));
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUILayout.BeginHorizontal(w);
                    if (GUILayout.Button("Gear", w2))
                        AddComponent(typeof(Drive_Gear));
                    if (GUILayout.Button("CAM", w2))
                        AddComponent(typeof(CAM));
                    EditorGUILayout.EndHorizontal();
                    
                    
                    EditorGUILayout.BeginHorizontal(w);
                    if (GUILayout.Button("Follow Position", w2))
                        AddComponent(typeof(Drive_FollowPosition));

                    if (GUILayout.Button("Destination Drive", w2))
                        AddComponent(typeof(Drive_DestinationMotor));

              
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUILayout.BeginHorizontal(w);
                    if (GUILayout.Button("Drive Erratic", w2))
                        AddComponent(typeof(Drive_ErraticPosition));
                    if (GUILayout.Button("Drive Speed", w2))
                        AddComponent(typeof(Drive_Speed));
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal(w);
                    if (GUILayout.Button("Gear", w2))
                        AddComponent(typeof(Drive_Gear));

                    if (GUILayout.Button("CAM", w2))
                        AddComponent(typeof(CAM));

                  
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUILayout.BeginHorizontal(w);
                    if (GUILayout.Button("Drive Sequence", w2))
                        AddComponent(typeof(Drive_Sequence));

                     GUI.backgroundColor = oldcol;
                 
                    EditorGUILayout.EndHorizontal();
                }
            }
            
#if REALVIRTUAL_PROFESSIONAL
            if (logicsteps)
            {
                var oldcol = GUI.backgroundColor;
                GUI.backgroundColor = Color.yellow;
                    
                EditorGUILayout.BeginHorizontal(w);
                if (GUILayout.Button("Drive to", w2))
                    AddComponent(typeof(LogicStep_DriveTo));
                if (GUILayout.Button("Start Drive", w2))
                    AddComponent(typeof(LogicStep_StartDriveTo));
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal(w);
                if (GUILayout.Button("Drive Speed", w2))
                    AddComponent(typeof(Drive_Speed));
             
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal(w);
                if (GUILayout.Button("Set Signal", w2))
                    AddComponent(typeof(LogicStep_SetSignalBool));
                if (GUILayout.Button("Junp", w2))
                    AddComponent(typeof(LogicStep_JumpOnSignal));
                EditorGUILayout.EndHorizontal();
                    
                EditorGUILayout.BeginHorizontal(w);
                if (GUILayout.Button("Wait Sensor", w2))
                    AddComponent(typeof(LogicStep_WaitForSensor));
                if (GUILayout.Button("Wait Signal", w2))
                    AddComponent(typeof(LogicStep_WaitForSignalBool));
                EditorGUILayout.EndHorizontal();
                    
                EditorGUILayout.BeginHorizontal(w);
                if (GUILayout.Button("Wait Drives", w2))
                    AddComponent(typeof(LogicStep_WaitForDrivesAtTarget));
                if (GUILayout.Button("Delay", w2))
                    AddComponent(typeof(LogicStep_Delay));
                EditorGUILayout.EndHorizontal();
                GUI.backgroundColor = oldcol;
            }
#endif

            /// Signals
            if (signal && obj.transform.parent != null)
            {
                obj = obj.transform.parent.gameObject;
            }

            if (!drive && !kinematic &&!logicsteps)
            {
                EditorGUILayout.BeginHorizontal(w);
                if (GUILayout.Button(iconoutbool))
                    CreateSignal(typeof(PLCOutputBool));

                if (GUILayout.Button(iconoutint))
                    CreateSignal(typeof(PLCOutputInt));

                if (GUILayout.Button(iconoutfloat))
                    CreateSignal(typeof(PLCOutputFloat));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal(w);

                if (GUILayout.Button(iconinbool))
                    CreateSignal(typeof(PLCInputBool));

                if (GUILayout.Button(iconinint))
                    CreateSignal(typeof(PLCInputInt));
                if (GUILayout.Button(iconinfloat))
                    CreateSignal(typeof(PLCInputFloat));
                EditorGUILayout.EndHorizontal();
                
                if (signal)
                {
                    EditorGUILayout.BeginHorizontal(w);
                    if (GUILayout.Button("To Bool"))
                        ChangeSignal("bool");

                    if (GUILayout.Button("To Int"))
                        ChangeSignal("int");

                    if (GUILayout.Button("To Float"))
                        ChangeSignal("float");
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal(w);
                    if (GUILayout.Button("Change Signal Direction"))
                        SignalHierarchyContextMenu.HierarchyChangeSignalDirection();
                    EditorGUILayout.EndHorizontal();
                }
                
            }
            if (OnQuickEditDraw != null)
                OnQuickEditDraw.Invoke();
          
            
        }

        public static void PlayModeGUI()
        {
            /// Playmode
      
            var newvaluetscale = Time.timeScale;
            EditorGUILayout.BeginHorizontal(w);
            GUILayout.Label("Timescale",w2);
            var stime = Time.time.ToString("0.0");
            GUILayout.Label("" ,w3);
            GUILayout.Label( stime,w3);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal(w);
            newvaluetscale = GUILayout.HorizontalSlider(newvaluetscale, 0f, 100f,w2);
            GUILayout.Label(newvaluetscale.ToString("0.0"), EditorStyles.boldLabel);
            if (GUILayout.Button("0.1",w6))
            {
                newvaluetscale = 0.1f;
            }
            if (GUILayout.Button("1",w6))
            {
                newvaluetscale = 1.0f;
            }
            if (GUILayout.Button("4",w6))
            {
                newvaluetscale = 4;
            }
            if (GUILayout.Button("max",w6))
            {
                newvaluetscale = 100;
            }
            EditorGUILayout.EndHorizontal();
            if (newvaluetscale != Time.timeScale)
            {
                Time.timeScale = newvaluetscale;
            }
            
            var newvaluespped = speedoverride;
            GUILayout.Label("Drive Speed Override");
            EditorGUILayout.BeginHorizontal(w);
            newvaluespped = GUILayout.HorizontalSlider(newvaluespped, 0f, 10f,w2);
            
            GUILayout.Label(newvaluespped.ToString("0.0"), EditorStyles.boldLabel);
            if (GUILayout.Button("0",w6))
            {
                newvaluespped = 0.1f;
            }
            if (GUILayout.Button("0.1",w6))
            {
                newvaluespped = 0.1f;
            }
            if (GUILayout.Button("1",w6))
            {
                newvaluespped = 1;
            }
            if (GUILayout.Button("4",w6))
            {
                newvaluespped = 4;
            }
            EditorGUILayout.EndHorizontal();
            if (newvaluespped != speedoverride)
            {
                speedoverride = newvaluespped;
                
                 List<GameObject> rootObjects = new List<GameObject>();
                 Scene scene = SceneManager.GetActiveScene();
                 scene.GetRootGameObjects(rootObjects);
                 foreach (var obj in rootObjects)
                 {
                     if (obj.GetComponent<realvirtualController>() != null)
                     {
                         obj.GetComponent<realvirtualController>().SpeedOverride = speedoverride;
                         break;
                     }
                 }
            }
            
            // Drives
            if (drivesnotnull)
            {
                if (drives.Count > 0)
                {
                    EditorGUILayout.BeginHorizontal(w2);
                    drivefilter = EditorGUILayout.TextField("", drivefilter, w2);
                    EditorGUILayout.EndHorizontal();
                }

                foreach (var drive in drives)
                {
                    
                    try
                    {
                        if (!Regex.IsMatch(drive.name,drivefilter))
                            continue;
                    }
                    catch 
                    {
                   
                    }
                    
                    EditorGUILayout.BeginHorizontal(w);
                    var oldcol = GUI.backgroundColor;
                    if (drive.CurrentSpeed != 0)
                        GUI.backgroundColor = Color.green;


                    if (GUILayout.Button(drive.name, w2))
                    {
                        Selection.objects = new Object[] {drive.gameObject};
                        if (lastclickdrive == drive)
                            SceneView.lastActiveSceneView.FrameSelected();
                        lastclickdrive = drive;
                    }

                    GUI.backgroundColor = oldcol;

                    GUIContent buttonText = new GUIContent("<");
                    Rect buttonRect = GUILayoutUtility.GetRect(buttonText, buttonstyle, w6);
                    Event e = Event.current;
                    if (e.isMouse && e.type == EventType.MouseDown && buttonRect.Contains(e.mousePosition))
                    {
                        drive.JogBackward = true;
                        drive.JogForward = false;
                        joggingdrive = drive;

                    }

                    if (e.isMouse && e.type == EventType.MouseUp && joggingdrive == drive)
                    {

                        drive.JogBackward = false;
                        drive.JogForward = false;
                        joggingdrive = null;
                    }

                    GUI.Button(buttonRect, buttonText);

                    buttonText = new GUIContent(">");
                    buttonRect = GUILayoutUtility.GetRect(buttonText, buttonstyle, w6);
                    if (e.isMouse && e.type == EventType.MouseDown && buttonRect.Contains(e.mousePosition))
                    {
                        drive.JogBackward = false;
                        drive.JogForward = true;
                        joggingdrive = drive;

                    }

                    if (e.isMouse && e.type == EventType.MouseUp && joggingdrive == drive)
                    {

                        drive.JogBackward = false;
                        drive.JogForward = false;
                        joggingdrive = null;
                    }

                    GUI.Button(buttonRect, buttonText);

                    /* if (GUILayout.Button("<",w6))
                    {
                       
                    }
                    if (GUILayout.Button(">",w6))
                    {
                        drive.JogBackward = false;
                        drive.JogForward = true;
                    } */
                    EditorGUILayout.EndHorizontal();
                }

            }
        }


        public static void OnSceneGUI(SceneView scene)
        {
            buttonstyle = new GUIStyle("Button");
            if (!Global.QuickEditDisplay)
            {
                stylesbuild = false;
                return;
            }


            if (!stylesbuild)
            {
                Buildstyles();
                stylesbuild = true;
            }

            thisscene = scene;
            

            if (Selection.activeObject != null)
                if (Selection.activeObject.GetType() == typeof(GameObject))
                    obj = (GameObject) Selection.activeObject;
                else
                    obj = null;

            if (obj != null)
            {
                Handles.BeginGUI();

                if (!Application.isPlaying)
                {
                    EditModeGUI();
                }
                else
                {
                    PlayModeGUI();
                }
                Handles.EndGUI();
            }

        }
    }
}
