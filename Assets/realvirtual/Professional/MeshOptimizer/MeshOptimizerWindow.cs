using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using System.Text.RegularExpressions;
using UnityMeshSimplifier;
#if UNITY_EDITOR
namespace realvirtual
{
#pragma warning disable 0414
    [InitializeOnLoad]
    //! This class is used to create the menu items for the game4automation menu
    //! It also contains the functions for the menu items
    public class MeshOptimizerWindow : EditorWindow
    {
        
        static List<Object> selectedobjs = new List<Object>();
        private Vector2 scrollPos;
        private static bool LossLess = false;
        private static float Quality = 0.7f;
        private static  bool EnableSmartLink = true;
        private static  bool PreserveBorderEdges = false;
        private static  bool PreserveUVSeamEdges = false;
        private static  bool PreserveUVFoldoverEdges = false;
        private static  double Agressivness = 2.0;
        
        private static MeshSimplifier simplifier;
        private static Hashtable oldMeshes = new Hashtable();
        private static Hashtable newMeshes= new Hashtable();
        private static List<GameObject> lastSelection = new List<GameObject>();

        [InitializeOnLoadMethod]
        static void Initialize()
        {
           simplifier = new MeshSimplifier();
        }

        [MenuItem("realvirtual/Mesh Optimizer (Pro)", false, 400)]
        static void Init()
        {
            MeshOptimizerWindow window =
                (MeshOptimizerWindow) EditorWindow.GetWindow(typeof(MeshOptimizerWindow));
            window.Show();
        }
        static void Optimize()
        {
            List<GameObject> list =GetAllSelectedObjectsIncludingSub();
            foreach (var obj in list)
            {
                if (oldMeshes.Contains(obj))
                {
                    UndoSimplifyMeshFilter(obj.GetComponent<MeshFilter>(), (Mesh)oldMeshes[obj]);
                }
                MeshFilter meshFilter = obj.GetComponent<MeshFilter>();  
                Mesh sourceMesh= meshFilter.sharedMesh;
                if (sourceMesh != null)
                {
                    if(oldMeshes.Contains(obj))
                        oldMeshes[obj]= sourceMesh;
                    else
                    {
                        oldMeshes.Add(obj,sourceMesh);
                    }
                   simplifyMesh(ref meshFilter,sourceMesh);
                   
                   if(newMeshes.Contains(obj))
                       newMeshes[obj]= meshFilter.sharedMesh;
                   else
                   {
                       newMeshes.Add(obj,meshFilter.sharedMesh);
                   }
                   lastSelection.Add(obj);
                }
            }
        }
        private static void simplifyMesh( ref MeshFilter meshFilter, Mesh sourceMesh)
        {
            var meshSimplifier = new MeshSimplifier();
            meshSimplifier.Initialize(sourceMesh);
            meshSimplifier.EnableSmartLink = EnableSmartLink;
            meshSimplifier.PreserveBorderEdges = PreserveBorderEdges;
            meshSimplifier.PreserveUVSeamEdges = PreserveUVSeamEdges;
            meshSimplifier.PreserveUVFoldoverEdges = PreserveUVFoldoverEdges;
            meshSimplifier.Agressiveness = Agressivness;
            // This is where the magic happens, lets simplify!
            if (LossLess)
                meshSimplifier.SimplifyMeshLossless();
            else
                meshSimplifier.SimplifyMesh(Quality);

            // Create our final mesh and apply it back to our mesh filter
            meshFilter.sharedMesh = meshSimplifier.ToMesh();
        }
        static void UndoOptimize()
        {
            List<GameObject> list =GetAllSelectedObjectsIncludingSub();
            foreach (var obj in lastSelection)
            {
                if (newMeshes.Contains(obj) && oldMeshes.Contains(obj))
                {
                    UndoSimplifyMeshFilter(obj.GetComponent<MeshFilter>(), (Mesh)oldMeshes[obj]);
                    newMeshes.Remove(obj);
                    oldMeshes.Remove(obj);
                }
                
            }
        }
        private static void UndoSimplifyMeshFilter( MeshFilter meshFilter, Mesh oldMesh)
        {
            if (oldMesh == null || meshFilter == null)
                return;
            meshFilter.sharedMesh = oldMesh;

        }

        private void OnEnable()
        {
            EditorPrefs.GetString("realvirtual-MeshOptimizerWindow-lastSelection", string.Join(",", lastSelection.Select(x => x.name).ToArray()));
            EditorPrefs.GetString("realvirtual-MeshOptimizerWindow-oldMeshes", string.Join(",", oldMeshes.Keys.Cast<GameObject>().Select(x => x.name).ToArray()));
            EditorPrefs.GetString("realvirtual-MeshOptimizerWindow-newMeshes", string.Join(",", newMeshes.Keys.Cast<GameObject>().Select(x => x.name).ToArray()));
            EditorPrefs.GetBool("realvirtual-MeshOptimizerWindow-LossLess", LossLess);
            EditorPrefs.GetFloat("realvirtual-MeshOptimizerWindow-Quality", Quality);
            EditorPrefs.GetBool("realvirtual-MeshOptimizerWindow-EnableSmartLink", EnableSmartLink);
            EditorPrefs.GetBool("realvirtual-MeshOptimizerWindow-PreserveBorderEdges", PreserveBorderEdges);
            EditorPrefs.GetBool("realvirtual-MeshOptimizerWindow-PreserveUVSeamEdges", PreserveUVSeamEdges);
            EditorPrefs.GetBool("realvirtual-MeshOptimizerWindow-PreserveUVFoldoverEdges", PreserveUVFoldoverEdges);
            EditorPrefs.GetFloat("realvirtual-MeshOptimizerWindow-Agressivness", (float)Agressivness);
        }

        void OnDisable()
        {
            EditorPrefs.SetBool("realvirtual-MeshOptimizerWindow-LossLess", LossLess);
            EditorPrefs.SetFloat("realvirtual-MeshOptimizerWindow-Quality", Quality);
            EditorPrefs.SetBool("realvirtual-MeshOptimizerWindow-EnableSmartLink", EnableSmartLink);
            EditorPrefs.SetBool("realvirtual-MeshOptimizerWindow-PreserveBorderEdges", PreserveBorderEdges);
            EditorPrefs.SetBool("realvirtual-MeshOptimizerWindow-PreserveUVSeamEdges", PreserveUVSeamEdges);
            EditorPrefs.SetBool("realvirtual-MeshOptimizerWindow-PreserveUVFoldoverEdges", PreserveUVFoldoverEdges);
            EditorPrefs.SetFloat("realvirtual-MeshOptimizerWindow-Agressivness", (float)Agressivness);
        }
       
        void OnDestroy()
        {
            EditorPrefs.SetString("realvirtual-MeshOptimizerWindow-lastSelection", string.Join(",", lastSelection.Select(x => x.name).ToArray()));
            EditorPrefs.SetString("realvirtual-MeshOptimizerWindow-oldMeshes", string.Join(",", oldMeshes.Keys.Cast<GameObject>().Select(x => x.name).ToArray()));
            EditorPrefs.SetString("realvirtual-MeshOptimizerWindow-newMeshes", string.Join(",", newMeshes.Keys.Cast<GameObject>().Select(x => x.name).ToArray()));
        }
       
        private void OnGUI()
        {
            
            float width = position.width;
            
            GUILayout.BeginVertical();
            GUILayout.Width(10);
            GUILayout.EndVertical();
            
            var selected = Selection.objects.Count();
            EditorGUILayout.Separator();
            
            GUILayout.BeginVertical();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Loss Less:",EditorStyles.boldLabel, GUILayout.Width(width/2));
                LossLess = EditorGUILayout.Toggle(LossLess, GUILayout.Width(width/2));
                EditorGUILayout.EndHorizontal();
            
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Quality:",EditorStyles.boldLabel, GUILayout.Width(width/2));
                Quality = EditorGUILayout.Slider(Quality,0.0f,1.0f, GUILayout.Width((width/2)-18));
                EditorGUILayout.EndHorizontal();
            
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Enable SmartLink:",EditorStyles.boldLabel, GUILayout.Width(width/2));
                EnableSmartLink = EditorGUILayout.Toggle(EnableSmartLink, GUILayout.Width(width/2));
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Preserve BorderEdges:",EditorStyles.boldLabel, GUILayout.Width(width/2));
                PreserveBorderEdges = EditorGUILayout.Toggle(PreserveBorderEdges, GUILayout.Width(width/2));
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Preserve UV SeamEdges:",EditorStyles.boldLabel, GUILayout.Width(width/2));
                PreserveUVSeamEdges = EditorGUILayout.Toggle(PreserveUVSeamEdges, GUILayout.Width(width/2));
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Preserve UV FoldoverEdges:",EditorStyles.boldLabel, GUILayout.Width(width/2));
                PreserveUVFoldoverEdges = EditorGUILayout.Toggle(PreserveUVFoldoverEdges, GUILayout.Width(width/2));
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Agressivness:",EditorStyles.boldLabel, GUILayout.Width(width/2));
                Agressivness=EditorGUILayout.DoubleField(Agressivness, GUILayout.Width((width/2)-16));
                EditorGUILayout.EndHorizontal();
            
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Optimize", GUILayout.Width(width-12)))
                {
                    Optimize();
                }
                EditorGUILayout.EndHorizontal();
            
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Undo Optimize", GUILayout.Width(width-12)))
                {
                    UndoOptimize();
                }
                EditorGUILayout.EndHorizontal();
            
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Finalize Meshes", GUILayout.Width(width-12)))
                {
                    newMeshes.Clear();
                    oldMeshes.Clear();
                    lastSelection.Clear();
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Reset Settings", GUILayout.Width(width-12)))
                { 
                    LossLess = false;
                    Quality = 0.7f;
                    EnableSmartLink = true;
                    PreserveBorderEdges = false;
                    PreserveUVSeamEdges = false;
                    PreserveUVFoldoverEdges = false;
                    Agressivness = 2.0;
                }
                EditorGUILayout.EndHorizontal();
            
            GUILayout.EndVertical();
            
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
        }
        private static List<GameObject> GetAllSelectedObjectsIncludingSub()
        {
            List<GameObject> list = new List<GameObject>();
            AddObjectsToSelection();
            foreach (Object myobj in selectedobjs)
            { 
                var objs = Global.GatherObjects((GameObject) myobj);
                foreach (GameObject obj in objs)
                {
                    list.Add(obj);
                }
            }

            selectedobjs.Clear();
            RecordUndo(ref list);
            return list;
        }
        private static void RecordUndo(ref List<GameObject> list)
        {
            foreach (var go in list)
            {
                Undo.RecordObject(go, "Selection Window Changes");
            }
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
    }
}
#endif
