using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using System.Text.RegularExpressions;

namespace realvirtual
{
#pragma warning disable 0414
    [InitializeOnLoad]
    //! Class to handle the creation of the game4automation menu
    public class MaterialWindow : EditorWindow 
    {
        private static MaterialPalet materialPalet; 
        private static List<Material> currentmaterials=new List<Material>();
        private Vector2 scrollPos;
        private static Material selectedMaterial;
        private static int selectedMaterialIndexOld; 
        private int selectedGroupIndex = 0;
        private static int selectedGroupIndexOld;
        static List<Object> selectedobjs = new List<Object>();
        private static List<string> Groups;
        private string activegroup;
        private static object[] activegroupsel = new object[0];
        private bool layoutwithGroup;
        
        [InitializeOnLoadMethod]
        static void Initialize()
        {
            InitSettings();
        }
        private static void InitSettings()
        {
            GetMaterialPalet();
            UpdateMaterialPalette();
        }
        
        static void GetMaterialPalet()
        {
            string[] palet;
            var paletname=EditorPrefs.GetString("materialPalet");
            if (paletname != "")
            {
                palet = AssetDatabase.FindAssets(paletname);
            }
            else
            {
                palet = AssetDatabase.FindAssets("MaterialpaletDefault");
            }
            if (palet.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(palet[0]);
                materialPalet = AssetDatabase.LoadAssetAtPath<MaterialPalet>(path);
            }
        }
        private static void UpdateMaterialPalette()
        {
            if (materialPalet != null)
            {
                currentmaterials.Clear();
                foreach (var material in materialPalet.materiallist)
                {
                    currentmaterials.Add(material);
                }
                EditorPrefs.SetString("materialPalet", materialPalet.name);
            }
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
        
        [MenuItem("realvirtual/Material window (Pro)", false, 400)]
        static void Init()
        {
            MaterialWindow window =
                (MaterialWindow) EditorWindow.GetWindow(typeof(MaterialWindow));
            window.Show();
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
            Init();
        }

        void OnGUI()
        {
            if (materialPalet == null)
            {
                GetMaterialPalet();
            }

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, false, false);
            float width = position.width;
            GUILayout.BeginVertical();
            GUILayout.Width(10);
            GUILayout.EndVertical();

            var selected = Selection.objects.Count();
            EditorGUILayout.Separator();

            GUILayout.BeginVertical();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Current material list:", GUILayout.Width((width / 2) - (width/5)));
            materialPalet = (MaterialPalet)EditorGUILayout.ObjectField(materialPalet, typeof(MaterialPalet), false,
                GUILayout.Width((width / 3) ));
            UpdateMaterialPalette();
            if (GUILayout.Button("New material set", GUILayout.Width((width / 3) - 10)))
            {
                var newmaterialpalet = ScriptableObject.CreateInstance<MaterialPalet>();
                AssetDatabase.CreateAsset(newmaterialpalet, "Assets/MaterialPalet.asset");
                // open asset in inspector
                Selection.activeObject = newmaterialpalet;
            }
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Separator();
           
            for (int i = 0; i < currentmaterials.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                // align label vertically
                GUILayout.BeginVertical();
                GUILayout.FlexibleSpace();
                GUILayout.Label(currentmaterials[i].name, GUILayout.Width(width / 3 - 15));
                GUILayout.FlexibleSpace();
                GUILayout.EndVertical();
                
                GUILayout.BeginVertical();
                GUILayout.FlexibleSpace();
                var preview = AssetPreview.GetAssetPreview(currentmaterials[i]);
                // show the preview in the GUI window
                if (preview != null)
                {
                    GUILayout.Label(preview, GUILayout.Width(50), GUILayout.Height(50));
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndVertical();
                
                GUILayout.BeginVertical();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Select", GUILayout.Width(width / 4)))
                {
                    SelectMaterial(currentmaterials[i]);
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndVertical();
                
                GUILayout.BeginVertical();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Set", GUILayout.Width(width / 4)))
                {
                    AssignMaterial(currentmaterials[i]);
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndVertical();
                
                EditorGUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
            
    
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
        private void RecordUndo(ref List<GameObject> list)
        {
            foreach (var go in list)
            {
                Undo.RecordObject(go, "Selection Window Changes");
            }
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
        
        private void GroupToIUnitySelection(string group)
        {
            var objs = GetAllWithGroup(group);
            Selection.objects = new Object[0];
            Selection.objects = objs.ToArray();
            activegroupsel = Array.ConvertAll(Selection.objects, item => (Object) item);
            activegroup = group;
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
        private void AssignMaterialtoGroup(Material material)
        {
            var group = Groups[selectedGroupIndex];
            GroupToIUnitySelection(group);
            AssignMaterial(material);
            
        }
    }
}
