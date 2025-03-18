// realvirtual (R) Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 realvirtual GmbH - Usage of this source code only allowed based on License conditions see https://realvirtual.io/en/company/license


using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using System.Text.RegularExpressions;

#if REALVIRTUAL_PLAYMAKER
using PlayMaker;
#endif

namespace realvirtual
{
    [InitializeOnLoad]
    [HelpURL("https://doc.realvirtual.io/basics/selection-window")]
    public class SelectionWindow : EditorWindow
    {
        private static bool groupEnabled;
        static List<Object> selectedobjs = new List<Object>();
        List<GameObject> list;
        private static Object selectedtonewparent;
        private static Object selectedpivottoorigin;
        private static LayerMask selectedlayer;
        private static int objectsingroup = 0;
        private static int selectedLayer2 = 0;
        private static Material selectedassingmaterial;
        private static Material selectedmaterial2;
        private static MaterialUpdateSettings selectedmaterialupdate;
        private static string selectedgroup = "New Group";
        private static List<string> Groups;
        private static SelectionWindowSettings settings;
        private Vector2 scrollPos;
        private string activegroup;
        private static object[] activegroupsel = new object[0];
        private static string isolategroup = "";
        private bool condensedgroupview;
        private string selectedshader;
        private string groupfilter;
        private static string namebakedmesh;
        static List<GameObject> BakeMeshesObjects= new List<GameObject>();

        [InitializeOnLoadMethod]
        static void Initialize()
        {
            UnityEditor.SceneManagement.EditorSceneManager.sceneOpened += OnEditorSceneManagerSceneOpened;
            GetGroups();
            InitSettings();
        }

        private static void InitSettings()
        {
            settings = UnityEditor.AssetDatabase.LoadAssetAtPath(
                "Assets/realvirtual/private/ProductiveTools/Editor/SelectionWindowSettings.asset",
                typeof(SelectionWindowSettings)) as SelectionWindowSettings;
            if (settings != null)
            {
                selectedassingmaterial = settings.selectedassingmaterial;
                selectedassingmaterial = settings.selectedmaterialupdate;
                selectedmaterialupdate = settings.materialupdatesettings;
                selectedgroup = settings.togroup;
                selectedtonewparent = settings.selectedtonewparent;
                selectedpivottoorigin = settings.selectedpivottoorigin;
            }
        }

        public void Awake()
        {
            GetGroups();
            if (Global.g4acontrollernotnull)
            {
                foreach (var group in Global.g4acontroller.HiddenGroups)
                {
                    VisibleGroup(false, group);
                }
            }

            InitSettings();
        }

        static void GetGroups()
        {
            object[] groups = UnityEngine.Resources.FindObjectsOfTypeAll(typeof(Group));
            Groups = new List<string>();
            foreach (Group group in groups)
            {
                if (EditorUtility.IsPersistent(group.transform.root.gameObject))
                    continue;
                if (!Groups.Contains(group.GroupName))
                    Groups.Add(group.GroupName);
            }

            Groups.Sort();
        }

        static void OnEditorSceneManagerSceneOpened(UnityEngine.SceneManagement.Scene scene,
            UnityEditor.SceneManagement.OpenSceneMode mode)
        {
            GetGroups();
        }
        
        static void AddObjectsToSelection()
        {
            foreach (var myobj in Selection.objects)
            {
                if (myobj is GameObject && selectedobjs.IndexOf(myobj) == -1)
                {
                    selectedobjs.Add(myobj);
                }
            }

            objectsingroup = selectedobjs.Count;
            Init();
        }


        static void AddObjectsAndChildrenToSelection()
        {
            foreach (var myobj in Selection.objects)
            {
                if (myobj is GameObject && selectedobjs.IndexOf(myobj) == -1)
                {
                    var objs = Global.GatherObjects((GameObject) myobj);
                    foreach (var theobj in objs)
                    {
                        selectedobjs.Add(theobj);
                    }
                }
            }

            AddToUnitySelection();
            objectsingroup = selectedobjs.Count;
            Init();
        }

        static void BakeSelectiontoMesh()
        {
            
            if (Selection.objects.Length == 0)
            {
                Debug.LogWarning("No objects selected");
                return;
            }
      
            if (namebakedmesh == "")
            {
                namebakedmesh = "OptimizedMesh";
            }
            
            GameObject newGO = Global.AddGameObjectIfNotExisting(namebakedmesh);
            var selectedMeshes= new List<MeshFilter>();
            foreach (var obj in Selection.objects)
            {
                if(obj is GameObject && ((GameObject) obj).GetComponent<MeshFilter>()!=null)
                {
                    selectedMeshes.Add(((GameObject) obj).GetComponent<MeshFilter>());
                }
                var children = ((GameObject) obj).GetComponentsInChildren<MeshFilter>();
                if(children.Length>0)
                {
                    foreach (var child in children)
                    {
                        selectedMeshes.Add(child);
                    }
                }
            }
            List<MeshRenderer> DeactivatedRenderers = new List<MeshRenderer>();
            // Combine the given Meshfilters to one gameobject and add it under "OptimizedMeshes"
            MeshCombine.CombineMeshesWithMutliMaterial(newGO, true, selectedMeshes.ToArray(),namebakedmesh,false,ref DeactivatedRenderers);
            BakeMeshesObjects.Add(newGO);
        }
        
        private static void RemoveObjectsFromSelection()
        {
            foreach (Object myobj in Selection.objects)
            {
                if (myobj is GameObject && selectedobjs.IndexOf(myobj) != -1)
                {
                    selectedobjs.Remove(myobj);
                }
            }

            objectsingroup = selectedobjs.Count;
            Init();
        }

        private void UnhideAll()
        {
            List<GameObject> rootObjects = new List<GameObject>();
            Scene scene = SceneManager.GetActiveScene();
            scene.GetRootGameObjects(rootObjects);
            foreach (var obj in rootObjects)
            {
                if (obj.GetComponents<realvirtualController>().Length == 0)
                {
                    var objs = Global.GatherObjects(obj);
                    Global.HideSubObjects(obj, false);
                }
            }
        }

        private void SupressChildren(bool supress)
        {
            var objs = GetAllSelectedObjects();
            foreach (var obj in objs)
            {
                Global.HideSubObjects(obj, supress);
            }
        }

        private static void AddToUnitySelection()
        {
            AddObjectsToSelection();
            Selection.objects = selectedobjs.ToArray();
        }

        public GameObject[] GatherObjects(GameObject root)
        {
            List<GameObject> objects = new List<GameObject>();
            Stack<GameObject> recurseStack = new Stack<GameObject>(new GameObject[] {root});

            while (recurseStack.Count > 0)
            {
                GameObject obj = recurseStack.Pop();
                objects.Add(obj);

                foreach (Transform childT in obj.transform)
                    recurseStack.Push(childT.gameObject);
            }

            return objects.ToArray();
        }

        public List<GameObject> GetAllWithGroup(string group)
        {
            List<GameObject> list = new List<GameObject>();
            var groupcomps = UnityEngine.Resources.FindObjectsOfTypeAll(typeof(Group));

            foreach (var comp in groupcomps)
            {
                var gr = (Group) comp;
                if (EditorUtility.IsPersistent(gr.transform.root.gameObject))
                    continue;
                if (gr.GroupName == group)
                {
                   
                        list.Add(gr.gameObject);
                }
            }

            return list;
        }

        private void SelectVisible(bool visible)
        {
            List<GameObject> list = new List<GameObject>();
            var groupcomps = UnityEngine.Resources.FindObjectsOfTypeAll(typeof(GameObject));
            foreach (var comp in groupcomps)
            {
                var gr = (GameObject) comp;
                if (EditorUtility.IsPersistent(gr.transform.root.gameObject))
                    continue;
                if (gr.transform.IsChildOf(Global.g4acontroller.transform))
                    continue;
         
                if (gr.activeSelf == visible)
                    list.Add(gr.gameObject);
            }

            Selection.objects = list.ToArray();
        }

        private void SelectLayer(LayerMask layer)
        {
            List<GameObject> rootObjects = new List<GameObject>();
            List<Object> result = new List<Object>();
            Scene scene = SceneManager.GetActiveScene();
            scene.GetRootGameObjects(rootObjects);
            foreach (var obj in rootObjects)
            {
                if (obj.GetComponents<realvirtualController>().Length == 0)
                {
                    var objs = Global.GatherObjects(obj);
                    foreach (var obje in objs)
                    {
                        var o = (GameObject) obje;
                        if (o.layer == layer)
                        {
                            result.Add(o);
                        }
                    }
                }
            }

            Selection.objects = result.ToArray();
        }

        private void SelectLocked(bool locked)
        {
            List<GameObject> rootObjects = new List<GameObject>();
            List<Object> result = new List<Object>();
            Scene scene = SceneManager.GetActiveScene();
            scene.GetRootGameObjects(rootObjects);
            foreach (var obj in rootObjects)
            {
                if (obj.GetComponents<realvirtualController>().Length == 0)
                {
                    var objs = Global.GatherObjects(obj);
                    foreach (var obje in objs)
                    {
                        var o = (GameObject) obje;
                        bool objectLockState = (o.hideFlags & HideFlags.NotEditable) > 0;
                        if (objectLockState == locked)
                        {
                            result.Add(o);
                        }
                    }
                }
            }

            Selection.objects = result.ToArray();
        }

        private void SelectMaterial(Material material)
        {
            if (material == null)
            {
                EditorUtility.DisplayDialog("No material selected", "Please select a material", "OK");
                return;
            }

            List<GameObject> list = new List<GameObject>();
            var groupcomps = UnityEngine.Resources.FindObjectsOfTypeAll(typeof(MeshRenderer));

            foreach (var comp in groupcomps)
            {
                var gr = (MeshRenderer) comp;
                if (EditorUtility.IsPersistent(gr.transform.root.gameObject))
                    continue;
                if (gr.sharedMaterial.name.Contains(material.name))
                    list.Add(gr.gameObject);
            }

            Selection.objects = list.ToArray();
        }

        private void AddGroup(string group)
        {
            var objs = GetAllWithGroup(group);
            foreach (var obj in objs)
            {
                selectedobjs.Add(obj);
            }
        }

        private void GroupToIUnitySelection(string group)
        {
            var objs = GetAllWithGroup(group);
            Selection.objects = new Object[0];
            Selection.objects = objs.ToArray();
            activegroupsel = Array.ConvertAll(Selection.objects, item => (Object) item);
            activegroup = group;
        }

        private void MoveObjectsToNewParent()
        {
            if (selectedtonewparent == null)
            {
                EditorUtility.DisplayDialog("No new parent selected", "Please select a new parent", "OK");
                return;
            }

            var objs = GetAllSelectedObjects();
            if (selectedtonewparent != null)
            {
                foreach (Object myobj in objs)
                {
                    ((GameObject) myobj).transform.parent = ((GameObject) selectedtonewparent).transform;
                }
            }
        }

        private void CopyObjectsToNewEmptyParent()
        {
            var objs = GetAllSelectedTopObjects();

            // Create new Object
            GameObject newobj = new GameObject();
            foreach (Object myobj in objs)
            {
                var copyobj = (GameObject) Object.Instantiate(myobj, newobj.transform);
                var origobj = (GameObject) myobj;
                copyobj.transform.position = origobj.transform.position;
                copyobj.transform.rotation = origobj.transform.rotation;
                copyobj.transform.localScale = origobj.transform.localScale;
            }

            Selection.activeObject = newobj;
            EditorGUIUtility.PingObject(Selection.activeObject);
        }

        private void MoveObjectsToLayer()
        {
            var objs = GetAllSelectedObjectsIncludingSub();
            foreach (var obj in objs)
            {
                obj.layer = selectedlayer;
            }
        }

        private void SetStatic(bool isstatic)
        {
            var objs = GetAllSelectedObjectsIncludingSub();
            foreach (var obj in objs)
            {
                obj.isStatic = isstatic;
            }
        }

        private void Lock(bool lockelement)
        {
            var objs = GetAllSelectedObjectsIncludingSub();
            foreach (var obj in objs)
            {
                Global.SetLockObject(obj, lockelement);
            }
        }


        private void ToGroup(string group, bool includingsub)
        {
            bool hidden = false;
            if (Global.g4acontrollernotnull)
                if (Global.g4acontroller.GroupIsHidden(group))
                    hidden = true;

            List<GameObject> objs;
            if (!includingsub)
                objs = GetAllSelectedObjects();
            else
                objs = GetAllSelectedObjectsIncludingSub();
            foreach (var obj in objs)
            {
                // Check if Group is already there
                var coms = obj.GetComponents<Group>();
                var existing = false;
                foreach (var com in coms)
                {
                    if (com.GroupName == group)
                        existing = true;
                }

                if (!existing)
                {
                    var groupcom = obj.AddComponent<Group>();
                    groupcom.GroupName = group;
                    if (hidden)
                        Global.SetVisible((GameObject) obj, false);
                }
            }

            if (!Groups.Contains(group))
                Groups.Add(group);
        }

        private void Visible(bool visible)
        {
            var objs = GetAllSelectedObjects();
            foreach (var obj in objs)
            {
                Global.SetVisible((GameObject) obj, visible);
            }
        }

        private void VisibleGroup(bool visible, string Group)
        {
            var objs = GetAllWithGroup(Group);
            foreach (var obj in objs)
            {
                Global.SetVisible((GameObject) obj, visible);
            }
        }


        private void VisibleAll(bool visible, bool considerhidden)
        {
            List<GameObject> rootObjects = new List<GameObject>();
            Scene scene = SceneManager.GetActiveScene();
            scene.GetRootGameObjects(rootObjects);
            foreach (var obj in rootObjects)
            {
                if (obj.GetComponents<realvirtualController>().Length == 0)
                {
                    var objs = Global.GatherObjects(obj);
                    foreach (var obje in objs)
                    {
                        var ishidden = false;
                        var go = (GameObject) obje;
                  
                            if (Global.g4acontrollernotnull && considerhidden)
                            {
                                var group = go.GetComponent<Group>();
                                if (group != null)
                                    ishidden = Global.g4acontroller.GroupIsHidden(group.GroupName);
                            }

                            if (visible && ishidden == false)
                                go.SetActive(visible);
                            if (!visible)
                                go.SetActive(visible);
                        
                    }
                }
            }
        }

        private void Isolate()
        {
            VisibleAll(false, false);
            var objs = GetAllSelectedObjects();
            foreach (var obj in objs)
            {
                var go = (GameObject) obj;
                Global.SetVisible(go, true);
                // set this object and everything above active
                do
                {
                    go.SetActive(true);
                    if (go.transform.parent != null)
                        go = go.transform.parent.gameObject;
                    else
                        go = null;
                } while (go != null);
            }
        }

        private void IsolateGroup(string group)
        {
            if (group == isolategroup)
            {
                VisibleAll(true, true);
                isolategroup = "";
                return;
            }

            isolategroup = group;
            VisibleAll(false, false);

            var objs = GetAllWithGroup(group);
            foreach (var obj in objs)
            {
                var go = (GameObject) obj;
                Global.SetVisible(go, true);
                // set this object and everything above active
                do
                {
                    go.SetActive(true);
                    if (go.transform.parent != null)
                        go = go.transform.parent.gameObject;
                    else
                        go = null;
                } while (go != null);
            }
        }


        private void RemoveMissingScripts()
        {
            var objs = GetAllSelectedObjectsIncludingSub();
            foreach (var obj in objs)
            {
                Undo.RegisterCompleteObjectUndo(obj, "Remove missing scripts");
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(obj);
            }
        }

        private void RemoveG4A()
        {
            var objs = GetAllSelectedObjectsIncludingSub();
            foreach (var obj in objs)
            {
                var behaviors = obj.GetComponents<realvirtualBehavior>().ToArray();
                foreach (var com in behaviors)
                {
                    DestroyImmediate(com);
                }
            }
        }

        private void RemoveGroup()
        {
            var objs = GetAllSelectedObjectsIncludingSub();
            foreach (var obj in objs)
            {
                var coms = obj.GetComponents<Group>().ToArray();
                foreach (var com in coms)
                {
                    DestroyImmediate(com);
                }
            }
        }

        private void RenameGroup()
        {
            if (activegroup == "" || selectedgroup == "")
                return;

            var objs = GetAllSelectedObjects();
            foreach (var obj in objs)
            {
                var coms = obj.GetComponents<Group>().ToArray();
                foreach (var com in coms)
                {
                    if (com.GroupName == activegroup)
                        com.GroupName = selectedgroup;
                }
            }

            activegroup = selectedgroup;
            GetGroups();
        }


        private void RemoveThisGroup(string group, bool includingsub)
        {
            List<GameObject> objs;
            if (!includingsub)
                objs = GetAllSelectedObjects();
            else
                objs = GetAllSelectedObjectsIncludingSub();

            foreach (var obj in objs)
            {
                var coms = obj.GetComponents<Group>().ToArray();
                foreach (var com in coms)
                {
                    if (com.GroupName == group)
                        DestroyImmediate(com);
                }
            }
        }

        private void CollapseSameLevel()
        {
            var objs = GetSameLevelObjects();
            foreach (var obj in objs)
            {
                Global.SetExpandedRecursive(obj, false);
            }
        }

        private void ExpandSameLevel()
        {
            var objs = GetSameLevelObjects();
            foreach (var obj in objs)
            {
                Global.SetExpandedRecursive(obj, true);
            }
        }

        private void AlignTo(GameObject to)
        {
            if (to == null)
            {
                EditorUtility.DisplayDialog("No object for alignment selected", "Please select an object", "OK");
                return;
            }

            List<GameObject> list = new List<GameObject>();
            var objs = GetAllSelectedObjects();
            foreach (var myobj in objs)
            {
                var go = (GameObject) myobj;
                Global.SetPositionKeepChildren(go, to.transform.position);
                Global.SetRotationKeepChildren(go, to.transform.rotation);
            }
        }

        private void AlignToCenter(GameObject to)
        {
            List<GameObject> list = new List<GameObject>();
            if (to == null)
                return;
            var center = Global.GetTotalCenter(to);

            var objs = GetAllSelectedObjects();
            foreach (var myobj in objs)
            {
                var go = (GameObject) myobj;
                Global.SetPositionKeepChildren(go, center);
            }
        }

        private void RecordUndo(ref List<GameObject> list)
        {
            foreach (var go in list)
            {
                Undo.RecordObject(go, "Selection Window Changes");
            }
        }

        private List<GameObject> GetAllSelectedObjects()
        {
            List<GameObject> list = new List<GameObject>();
            AddObjectsToSelection();
            foreach (Object myobj in selectedobjs)
            {
                GameObject go = (GameObject) myobj;
                list.Add(go);
            }

            selectedobjs.Clear();
            RecordUndo(ref list);
            return list;
        }

        private List<GameObject> GetAllSelectedTopObjects()
        {
            List<GameObject> list = new List<GameObject>();
            AddObjectsToSelection();
            foreach (Object myobj in selectedobjs)
            {
                GameObject go = (GameObject) myobj;
                // is also parent in list then delete this from list
                var parent = go.transform.parent;
                var notadd = false;
                if (!ReferenceEquals(parent, null))
                    if (selectedobjs.Contains(parent))
                        notadd = true;

                if (!notadd) list.Add(go);
            }

            selectedobjs.Clear();
            RecordUndo(ref list);
            return list;
        }

        private List<GameObject> GetAllSelectedObjectsIncludingSub()
        {
            List<GameObject> list = new List<GameObject>();
            AddObjectsToSelection();
            foreach (Object myobj in selectedobjs)
            {
                var objs = GatherObjects((GameObject) myobj);
                foreach (var obj in objs)
                {
                    list.Add(obj);
                }
            }

            selectedobjs.Clear();
            RecordUndo(ref list);
            return list;
        }

        private List<GameObject> GetSameLevelObjects()
        {
            List<GameObject> list = new List<GameObject>();
            AddObjectsToSelection();
            foreach (Object myobj in selectedobjs)
            {
                var obj = (GameObject) myobj;
                var parent = obj.transform.parent;
                // add all children
                if (parent != null)
                {
                    foreach (Transform child in parent.transform)
                    {
                        list.Add(child.gameObject);
                    }
                }
            }

            selectedobjs.Clear();
            return list;
        }

        private void AssignMaterial(Material material)
        {
            if (material == null)
            {
                EditorUtility.DisplayDialog("No new material for asignment selected", "Please select a new material",
                    "OK");
                return;
            }

            if (EditorUtility.DisplayCancelableProgressBar("Collecting objects", "Please wait",
                    0))
            {
                EditorUtility.ClearProgressBar();
                return;
            }

            var a = 0;
            List<GameObject> list = GetAllSelectedObjectsIncludingSub();
            foreach (var myobj in list)
            {
                a++;
                float progress = (float) a / (float) list.Count;
                var renderer = myobj.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    if (EditorUtility.DisplayCancelableProgressBar("Progressing objects",
                            $"Material update on object {a} of {list.Count}",
                            progress))
                    {
                        EditorUtility.ClearProgressBar();
                        return;
                    }

                    Material[] sharedMaterialsCopy = renderer.sharedMaterials;

                    for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                    {
                        sharedMaterialsCopy[i] = material;
                    }

                    renderer.sharedMaterials = sharedMaterialsCopy;
                }
            }

            EditorUtility.ClearProgressBar();
        }

        private void AssignShader(string shader)
        {
            if (shader == "null")
            {
                EditorUtility.DisplayDialog("No new shaderfor asignment selected", "Please select a new material",
                    "OK");
                return;
            }

            if (EditorUtility.DisplayCancelableProgressBar("Collecting objects", "Please wait",
                    0))
            {
                EditorUtility.ClearProgressBar();
                return;
            }

            var a = 0;
            List<GameObject> list = GetAllSelectedObjectsIncludingSub();
            foreach (var myobj in list)
            {
                a++;
                float progress = (float) a / (float) list.Count;
                var renderer = myobj.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    if (EditorUtility.DisplayCancelableProgressBar("Progressing objects",
                            $"Shader update on object {a} of {list.Count}",
                            progress))
                    {
                        EditorUtility.ClearProgressBar();
                        return;
                    }

                    Material[] sharedMaterialsCopy = renderer.sharedMaterials;


                    for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                    {
                        var color = sharedMaterialsCopy[i].color;
                        sharedMaterialsCopy[i].shader = Shader.Find(shader);
                        sharedMaterialsCopy[i].color = color;
                    }

                    renderer.sharedMaterials = sharedMaterialsCopy;
                }
            }

            EditorUtility.ClearProgressBar();
        }

        private void DoMaterialUpdate(MaterialUpdateSettings materialupdate)
        {
            if (materialupdate == null)
            {
                EditorUtility.DisplayDialog("No Material Update Definiton",
                    "Please select a material update definition", "OK");
                return;
            }

            if (EditorUtility.DisplayCancelableProgressBar("Collecting objects", "Please wait",
                    0))
            {
                EditorUtility.ClearProgressBar();
                return;
            }

            List<GameObject> list = GetAllSelectedObjectsIncludingSub();
            var i = 0;
            foreach (var myobj in list)
            {
                float progress = (float) i / (float) list.Count;
                if (EditorUtility.DisplayCancelableProgressBar("Progressing objects",
                        $"Material update on object {i} of {list.Count}",
                        progress))
                {
                    EditorUtility.ClearProgressBar();
                    return;
                }

                i++;
                materialupdate.UpdateMaterials(myobj);
            }

            EditorUtility.ClearProgressBar();
        }

        // Add menu named "My Window" to the Window menu
        [MenuItem("realvirtual/Selection window (Pro)", false, 400)]
        static void Init()
        {
            // Get existing open window or if none, make a new one:
            SelectionWindow window =
                (SelectionWindow) EditorWindow.GetWindow(typeof(SelectionWindow));
            window.Show();
        }

        void OnGUI()
        {
            // Still active group
            if (!Enumerable.SequenceEqual(Selection.objects, activegroupsel))
                activegroup = "";
            scrollPos =
                EditorGUILayout.BeginScrollView(scrollPos, false, false);
            float width = position.width;
            GUILayout.BeginVertical();
            GUILayout.Width(10);
            GUILayout.EndVertical();
            objectsingroup = selectedobjs.Count;
            var selected = Selection.objects.Count();
            EditorGUILayout.Separator();
            GUILayout.BeginVertical();

            /// Actions for All
            GUILayout.Label("Actions with ALL", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Stop suppression", GUILayout.Width(width / 3)))
            {
                UnhideAll();
            }

            if (GUILayout.Button("Show all", GUILayout.Width(width / 3)))
            {
                VisibleAll(true, true);
            }

            var textcond = "Small Group Window";
            if (condensedgroupview)
                textcond = "Full Window";
            if (GUILayout.Button(textcond, GUILayout.Width((width / 3) - 15)))
            {
                condensedgroupview = !condensedgroupview;
            }

            EditorGUILayout.EndHorizontal();

            GUILayout.Label("Selection", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Objects in Unity Selection: ", selected.ToString());
            EditorGUILayout.LabelField("Objects in g4a Selection: ", objectsingroup.ToString());

            if (!condensedgroupview)
            {
                /// Selections
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Add selected", GUILayout.Width(width / 2)))
                {
                    AddObjectsToSelection();
                }

                if (GUILayout.Button("To Unity selection", GUILayout.Width((width / 2) - 10)))
                {
                    AddToUnitySelection();
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Add selected and children to Unity selection", GUILayout.Width(width -8)))
                {
                    AddObjectsAndChildrenToSelection();
                }
                EditorGUILayout.EndHorizontal();
           
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Remove selected", GUILayout.Width(width / 2)))
                {
                    RemoveObjectsFromSelection();
                }

                if (GUILayout.Button("Remove all", GUILayout.Width((width / 2) - 10)))
                {
                    selectedobjs.Clear();
                    objectsingroup = selectedobjs.Count;
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Select Invisible", GUILayout.Width(width / 2)))
                {
                    SelectVisible(false);
                }

                if (GUILayout.Button("Select Visible", GUILayout.Width((width / 2) - 10)))
                {
                    SelectVisible(true);
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Select Unlocked", GUILayout.Width(width / 2)))
                {
                    SelectLocked(false);
                }

                if (GUILayout.Button("Select Locked", GUILayout.Width((width / 2) - 10)))
                {
                    SelectLocked(true);
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Select Layer", GUILayout.Width((width / 2))))
                {
                    SelectLayer(selectedLayer2);
                }

                selectedLayer2 = EditorGUILayout.LayerField("", selectedLayer2, GUILayout.Width((width / 2) - 10));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Select Material", GUILayout.Width(width / 2)))
                {
                    SelectMaterial(selectedmaterial2);
                }

                selectedmaterial2 = (Material) EditorGUILayout.ObjectField(selectedmaterial2, typeof(Material), false,
                    GUILayout.Width((width / 2) - 15));
                EditorGUILayout.EndHorizontal();


                /// Actions
                GUILayout.Label("Actions with Selection (Unity & g4a Selection)", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Assign Material", GUILayout.Width(width / 2)))
                {
                    AssignMaterial(selectedassingmaterial);
                }

                selectedassingmaterial = (Material) EditorGUILayout.ObjectField(selectedassingmaterial,
                    typeof(Material), false, GUILayout.Width((width / 2) - 15));
                settings.selectedassingmaterial = selectedassingmaterial;

                EditorGUILayout.EndHorizontal();


                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Change Shader", GUILayout.Width(width / 2)))
                {
                    AssignShader(selectedshader);
                }

                selectedshader = EditorGUILayout.TextField(selectedshader, GUILayout.Width((width / 2) - 10));

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Material Update", GUILayout.Width(width / 2)))
                {
                    DoMaterialUpdate(selectedmaterialupdate);
                }

                selectedmaterialupdate = (MaterialUpdateSettings) EditorGUILayout.ObjectField(selectedmaterialupdate,
                    typeof(MaterialUpdateSettings), false, GUILayout.Width((width / 2) - 15));
                settings.materialupdatesettings = selectedmaterialupdate;
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Supress children", GUILayout.Width(width / 2)))
                {
                    SupressChildren(true);
                }

                if (GUILayout.Button("Stop suppression", GUILayout.Width((width / 2) - 10)))
                {
                    SupressChildren(false);
                }

                EditorGUILayout.EndHorizontal();


                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("To new Parent", GUILayout.Width(width / 2)))
                {
                    MoveObjectsToNewParent();
                }

                selectedtonewparent = EditorGUILayout.ObjectField(selectedtonewparent, typeof(GameObject), true,
                    GUILayout.Width((width / 2) - 15));
                settings.selectedtonewparent = (GameObject) selectedtonewparent;
                EditorGUILayout.EndHorizontal();


                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Copy to empty Parent", GUILayout.Width((width / 2))))
                {
                    CopyObjectsToNewEmptyParent();
                }

                EditorGUILayout.EndHorizontal();


                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("To Layer", GUILayout.Width((width / 2))))
                {
                    MoveObjectsToLayer();
                }

                selectedlayer = EditorGUILayout.LayerField("", selectedlayer, GUILayout.Width((width / 2) - 10));
                EditorGUILayout.EndHorizontal();


                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Visible", GUILayout.Width(width / 2)))
                {
                    Visible(true);
                }

                if (GUILayout.Button("Invisible", GUILayout.Width((width / 2) - 10)))
                {
                    Visible(false);
                }

                EditorGUILayout.EndHorizontal();


                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Isolate", GUILayout.Width(width / 2)))
                {
                    Isolate();
                }

                if (GUILayout.Button("View all", GUILayout.Width((width / 2) - 10)))
                {
                    VisibleAll(true, true);
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Lock", GUILayout.Width(width / 2)))
                {
                    Lock(true);
                }

                if (GUILayout.Button("Unlock", GUILayout.Width((width / 2) - 10)))
                {
                    Lock(false);
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("To Group", GUILayout.Width(width / 2)))
            {
                ToGroup(selectedgroup, false);
            }

            selectedgroup = EditorGUILayout.TextField(selectedgroup, GUILayout.Width((width / 2) - 10));
            settings.togroup = selectedgroup;

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Remove Groups", GUILayout.Width((width / 2))))
            {
                RemoveGroup();
            }

            if (GUILayout.Button("Rename", GUILayout.Width((width / 2)-10)))
            {
                RenameGroup();
            }

            EditorGUILayout.EndHorizontal();

            if (!condensedgroupview)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Collapse same level", GUILayout.Width(width / 2)))
                {
                    CollapseSameLevel();
                }

                if (GUILayout.Button("Expand same level", GUILayout.Width((width / 2) - 10)))
                {
                    ExpandSameLevel();
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Pivot to origin", GUILayout.Width(width / 2)))
                {
                    AlignTo((GameObject) selectedpivottoorigin);
                }

                selectedpivottoorigin = EditorGUILayout.ObjectField(selectedpivottoorigin, typeof(GameObject), true,GUILayout.Width(((width / 2)-10)/2));
                if (GUILayout.Button("Set", GUILayout.Width(((width / 2)-10)/2-4)))
                {
                    EditorGUIUtility.PingObject(Selection.activeObject);
                    selectedpivottoorigin = Selection.activeObject;
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Pivot to center", GUILayout.Width(width / 2)))
                {
                    AlignToCenter((GameObject) selectedpivottoorigin);
                }

                EditorGUILayout.EndHorizontal();


                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Static", GUILayout.Width(width / 2)))
                {
                    SetStatic(true);
                }

                if (GUILayout.Button("Movable", GUILayout.Width((width / 2) - 10)))
                {
                    SetStatic(false);
                }

                EditorGUILayout.EndHorizontal();


                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Remove G4A scripts", GUILayout.Width(width / 2)))
                {
                    RemoveG4A();
                }

                if (GUILayout.Button("Remove missing scripts", GUILayout.Width((width / 2) - 10)))
                {
                    RemoveMissingScripts();
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Bake to single mesh", GUILayout.Width((width /2))))
                {
                    BakeSelectiontoMesh();
                }

                EditorGUILayout.LabelField("Name baked mesh:", EditorStyles.boldLabel,
                    GUILayout.Width(((width / 2) - 10) / 2));
                namebakedmesh = EditorGUILayout.TextField(namebakedmesh, GUILayout.Width(((width / 2)-10)/2-4));
                
                EditorGUILayout.EndHorizontal();
            }


            Color oldColor = GUI.backgroundColor;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Groups", EditorStyles.boldLabel, GUILayout.Width(width / 2));
            groupfilter = EditorGUILayout.TextField("Filter", groupfilter, GUILayout.Width((width / 2) - 10));

            settings.togroup = selectedgroup;
            EditorGUILayout.EndHorizontal();
            foreach (var group in Groups)
            {
                try
                {
                    if (!Regex.IsMatch(group, groupfilter))
                        continue;
                }
                catch
                {
                }


                EditorGUILayout.BeginHorizontal();

                var grouphidden = false;
                var hiddenstyle = new GUIStyle(GUI.skin.button);
                if (Global.g4acontrollernotnull)
                {
                    if (Global.g4acontroller.HiddenGroups.Contains(group))
                    {
                        grouphidden = true;
                    }
                }

                var groupstyle = new GUIStyle(GUI.skin.button);
                if (group == activegroup)
                {
                    groupstyle.normal.textColor = Color.green;
                }

                if (GUILayout.Button(group, groupstyle, GUILayout.Width(width / 2)))
                {
                    GroupToIUnitySelection(group);
                }

                if (GUILayout.Button("+", GUILayout.Width(width / 18)))
                {
                    ToGroup(group, false);
                }

                if (GUILayout.Button("-", GUILayout.Width(width / 18)))
                {
                    RemoveThisGroup(group, false);
                }

                if (GUILayout.Button("++", GUILayout.Width(width / 18)))
                {
                    ToGroup(group, true);
                }

                if (GUILayout.Button("--", GUILayout.Width(width / 18)))
                {
                    RemoveThisGroup(group, true);
                }


                var text = "Hide";
                GUI.backgroundColor = Color.green;
                if (grouphidden)
                {
                    GUI.backgroundColor = Color.red;
                    text = "Show";
                }

                if (GUILayout.Button(text, hiddenstyle, GUILayout.Width(width / 8)))
                {
                    if (Global.g4acontrollernotnull)
                    {
                        if (!grouphidden)
                            Global.g4acontroller.AddHideGroup(group);
                        else
                            Global.g4acontroller.RemoveHideGroup(group);
                    }

                    VisibleGroup(grouphidden, group);
                }

                GUI.backgroundColor = oldColor;
                if (isolategroup == group)
                    GUI.backgroundColor = Color.cyan;
                if (GUILayout.Button("Isol", GUILayout.Width((width / 8) - 10)))
                {
                    IsolateGroup(group);
                }

                GUI.backgroundColor = oldColor;
                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("Update Groups"))
                GetGroups();
            GUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }
    }
}