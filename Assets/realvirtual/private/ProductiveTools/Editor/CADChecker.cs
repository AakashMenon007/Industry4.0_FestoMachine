// realvirtual (R) Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 realvirtual GmbH - Usage of this source code only allowed based on License conditions see https://realvirtual.io/en/company/license﻿

using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using Object = UnityEngine.Object;
using System.Reflection;

namespace realvirtual
{
    //! Checks the Mesh Data 
    public class CADChecker : EditorWindow
    {
        
        private static bool useupperpartnaming = true;
        private static bool showoccurencies = true;
        private static Object checkthis;
        private static Vector2 scrollPos;
        private static float totaltris;
        private static float totalvertices;
        private static float numduplicatemeshes;
        private static float trianglesinduplicatemeshes;
        private static int staticmeshes;
        private static int nonstaticmeshes;
        private static bool isinit = false;
        private static bool filteron = false;
        private static Object oldcheckthis;
        private static SearchableEditorWindow hierarchy { get; set; }

        public class MeshInfo : IComparable<MeshInfo>
        {
            public GameObject gameobject;
            public MeshFilter meshfilter;
            public string name;
            public float triangles;
            public int id;
            public float vertices;
            public MeshOccurence Occurence;

            public int CompareTo(MeshInfo other)
            {
                if (this.triangles.CompareTo(other.triangles) == 0)
                {
                    return this.name.CompareTo(other.name);
                }

                return this.triangles.CompareTo(other.triangles);
            }
        }

        public class MeshOccurence : IComparable<MeshOccurence>
        {
            public int id;
            public int number;
            public string name;
            public float triangles;
            public float vertices;
            public GameObject gameobject;
            public List<GameObject> gameobjects;
            public List<GameObject> duplicates;
            public int numduplicates;


            public int CompareTo(MeshOccurence other)
            {
                if (this.triangles.CompareTo(other.triangles) == 0)
                {
                    return this.name.CompareTo(other.name);
                }

                return this.triangles.CompareTo(other.triangles);
            }
        }

        public static List<MeshInfo> MeshInfos = new List<MeshInfo>();
        public static List<MeshOccurence> MeshOccurencies = new List<MeshOccurence>();


        static string GetName(GameObject obj)
        {
            if (useupperpartnaming && !ReferenceEquals(obj.transform.parent, null))
            {
                return obj.transform.parent.name;
            }

            return obj.name;
        }

        // Add menu named "My Window" to the Window menu
        [MenuItem("realvirtual/CAD Checker (Pro)",false,400)]
        static void Init()
        {
            // Get existing open window or if none, make a new one:
            CADChecker window = (CADChecker) EditorWindow.GetWindow(typeof(CADChecker));
            useupperpartnaming = EditorPrefs.GetBool( "realvirtual/CADChecker/UpperPartNaming",false);
            showoccurencies= EditorPrefs.GetBool( "realvirtual/CADChecker/ShowOccurencies",false);
            window.Show();
        }

        private void OnSelectionChange()
        {
            isinit = false;
        }

        static void GetMeshInfos()
        {
            isinit = true;
            MeshInfos.Clear();
            MeshOccurencies.Clear();
            totaltris = 0;
            totalvertices = 0;
            numduplicatemeshes = 0;
            staticmeshes = 0;
            nonstaticmeshes = 0;
            trianglesinduplicatemeshes = 0;
            Object[] objects = null;

            if (!ReferenceEquals(checkthis, null))
            {
                objects = new Object[1];
                objects[0] = (GameObject) checkthis;
            }
            else
            {
                objects = Selection.objects;

            }

            foreach (var obj in objects)
            {
                if (obj.GetType() == typeof(GameObject))
                {
                    var go = (GameObject) obj;

                    var meshfilters = go.GetComponentsInChildren<MeshFilter>();
                    foreach (var meshfilter in meshfilters)
                    {
                        var gameobject = meshfilter.gameObject;
                        var mesh = meshfilter.sharedMesh;
                        float numtriangles = mesh.triangles.Length;
                        float numvertices = mesh.vertexCount;
                        MeshInfo info = new MeshInfo();
                        info.gameobject = gameobject;
                        info.meshfilter = meshfilter;
                        info.triangles = numtriangles;
                        info.vertices = numvertices;
                        info.name = GetName(gameobject);
                        MeshInfos.Add(info);
                        totaltris += numtriangles;
                        totalvertices += numvertices;
                        if (gameobject.isStatic)
                            staticmeshes++;
                        else
                            nonstaticmeshes++;
                    }
                }
            }

            MeshInfos.Sort();
            MeshInfos.Reverse();

            var curtris = 0f;
            var curname = "";
            var id = 0;
            var sumtris = 0f;
            var sumverts = 0f;
            var number = 1;
            var occurence = new MeshOccurence();
            Vector3[] currmesh = new [] {new Vector3(0f, 0f, 0f)};
            // Get Occurrencies
            foreach (var meshInfo in MeshInfos)
            {

                if (!currmesh.SequenceEqual(meshInfo.meshfilter.sharedMesh.vertices))
                {
                    // new occurency
                    id++;
                    occurence = new MeshOccurence();
                    occurence.gameobjects = new List<GameObject>();
                    occurence.gameobject = meshInfo.gameobject;
                    occurence.name = meshInfo.name;
                    MeshOccurencies.Add(occurence);
                    curname = meshInfo.name;
                    curtris = meshInfo.triangles;
                    currmesh = meshInfo.meshfilter.sharedMesh.vertices;
                    sumtris = curtris;
                    sumverts = sumverts + meshInfo.vertices;
                    number = 1;
                }
                else
                {
                    sumtris = sumtris + curtris;
                    number++;

                }

                meshInfo.id = id;
                meshInfo.Occurence = occurence;
                occurence.number = number;
                occurence.triangles = sumtris;
                occurence.vertices = sumverts;
                occurence.gameobjects.Add(meshInfo.gameobject);
            }

            // Check same positions in Occurencies
            foreach (var meshOccurence in MeshOccurencies)
            {
                if (meshOccurence.number > 1)
                {
                    var samepos = CheckSamePositions(meshOccurence.gameobjects);
                    if (samepos.Count > 0)
                    {
                        meshOccurence.duplicates = samepos;
                        meshOccurence.numduplicates = samepos.Count;
                        numduplicatemeshes = numduplicatemeshes + samepos.Count;
                        trianglesinduplicatemeshes = trianglesinduplicatemeshes +
                                                     (meshOccurence.triangles / meshOccurence.number) * samepos.Count;
                    }

                }
            }

            MeshOccurencies.Sort();
            MeshOccurencies.Reverse();
        }

        static List<GameObject> CheckSamePositions(List<GameObject> gameobjects)
        {
            List<GameObject> res = new List<GameObject>();
            for (int i = 0; i < gameobjects.Count; i++)
            {
                var outerobj = gameobjects[i];
                for (int a = i; a < gameobjects.Count; a++)
                {
                    var innerobj = gameobjects[a];
                    if (innerobj != outerobj)
                    {
                        if ((innerobj.transform.position == outerobj.transform.position) &&
                            (innerobj.transform.rotation == outerobj.transform.rotation))
                        {
                            if (!res.Contains(innerobj))
                                res.Add(innerobj);
                        }

                    }
                }
            }

            return res;
        }

        void OnGUI()
        {
            float[] sizes = {40, 20, 10, 8, 8, 10, 20, 30};
            float[] w = new float[sizes.Length];
            // calc percent sizes
            float win = Screen.width;
            float total = 0;
            foreach (float size in sizes)
            {
                total += size;
            }

            for (int i = 0; i < sizes.Length; i++)
            {
                w[i] = sizes[i] / total * win;
            }

            if (!isinit)
                GetMeshInfos();

            GUILayout.Space(15);
            GUILayout.Label("Check this", GUILayout.Width(w[0]));
            checkthis = EditorGUILayout.ObjectField(checkthis, typeof(GameObject), true, GUILayout.Width(w[0]));
            GUILayout.Space(15);
            if (checkthis != oldcheckthis)
                isinit = false;
            useupperpartnaming = EditorGUILayout.Toggle("Upper part naming ", useupperpartnaming);
            showoccurencies = EditorGUILayout.Toggle("Show Occurencies ", showoccurencies);
            GUILayout.Space(15);
            EditorGUILayout.LabelField("Number of triangles: ", totaltris.ToString());
            EditorGUILayout.LabelField("Number of vertices: ", totalvertices.ToString());
            EditorGUILayout.LabelField("Meshes: ", MeshInfos.Count.ToString());
            float percstatic = (float) staticmeshes / (float) MeshInfos.Count * 100;
            EditorGUILayout.LabelField("Static Meshes: ",
                staticmeshes.ToString() + " (" + percstatic.ToString("0.00") + "%)");
            float pernonstatic = (float) nonstaticmeshes / (float) MeshInfos.Count * 100;
            EditorGUILayout.LabelField("Non static Meshes: ",
                nonstaticmeshes.ToString() + " (" + pernonstatic.ToString("0.00") + "%)");
            EditorGUILayout.LabelField("Mesh Occurencies: ", MeshOccurencies.Count.ToString());
            EditorGUILayout.LabelField("Mesh Duplicates: ", numduplicatemeshes.ToString());
            var percdup = trianglesinduplicatemeshes / totaltris * 100;
            EditorGUILayout.LabelField("Triangles Duplicates: ",
                trianglesinduplicatemeshes.ToString() + " (" + percdup.ToString("0.00") + "%)");

            GUILayout.Space(15);
            GUILayout.BeginHorizontal();
            GUILayout.Space(15);
            GUILayout.Label("Part", GUILayout.Width(w[0]));
            GUILayout.Label("Mesh", GUILayout.Width(w[1]));
            GUILayout.Label("Trias", GUILayout.Width(w[2]));
            GUILayout.Label("Vert", GUILayout.Width(w[2]));
            GUILayout.Label("Occ.", GUILayout.Width(w[3]));
            GUILayout.Label("%", GUILayout.Width(w[4]));
            GUILayout.Label("Dup", GUILayout.Width(w[5]));
            GUILayout.EndHorizontal();

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, true, false);

            if (!showoccurencies)
            {
                foreach (var meshinfo in MeshInfos)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(15);
                    GUILayout.Label(meshinfo.name, GUILayout.Width(w[0]));
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.ObjectField(meshinfo.gameobject, typeof(Object), false, GUILayout.Width(w[1]));
                    EditorGUI.EndDisabledGroup();
                    GUILayout.Label(meshinfo.triangles.ToString(), GUILayout.Width(w[2]));
                    GUILayout.Label(meshinfo.vertices.ToString(), GUILayout.Width(w[2]));
                    var percent = meshinfo.triangles / totaltris * 100f;
                    GUILayout.Label(meshinfo.Occurence.number.ToString(), GUILayout.Width(w[3]));
                    GUILayout.Label(percent.ToString("0.00"), GUILayout.Width(w[4]));
                    GUILayout.Label(meshinfo.Occurence.numduplicates.ToString(), GUILayout.Width(w[5]));
                    if (GUILayout.Button("Show"))
                    {
                        EditorGUIUtility.PingObject(meshinfo.gameobject);
                    }

                    if (!filteron)
                    {
                        if (GUILayout.Button("Filter"))
                        {
                            SetSearchFilter(meshinfo.name, FILTERMODE_NAME);
                            filteron = true;
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("Off"))
                        {
                            SetSearchFilter("", FILTERMODE_NAME);
                            filteron = false;
                        }
                    }

                    if (GUILayout.Button("Select"))
                    {
                        Selection.objects = new[] {meshinfo.gameobject};
                    }

                    if (GUILayout.Button("Dup."))
                    {
                        Selection.objects = new[] {meshinfo.gameobject};
                    }

                    GUILayout.Space(15);
                    GUILayout.EndHorizontal();
                }
            }
            else
            {
                foreach (var occurence in MeshOccurencies)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(15);
                    GUILayout.Label(occurence.name, GUILayout.Width(w[0]));
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.ObjectField(occurence.gameobject, typeof(Object), false, GUILayout.Width(w[1]));
                    EditorGUI.EndDisabledGroup();
                    GUILayout.Label(occurence.triangles.ToString(), GUILayout.Width(w[2]));
                    GUILayout.Label(occurence.vertices.ToString(), GUILayout.Width(w[2]));
                    var percent = occurence.triangles / totaltris * 100f;
                    GUILayout.Label(occurence.number.ToString(), GUILayout.Width(w[3]));
                    GUILayout.Label(percent.ToString("0.00"), GUILayout.Width(w[4]));
                    GUILayout.Label(occurence.numduplicates.ToString(), GUILayout.Width(w[5]));
                    if (GUILayout.Button("Show"))
                    {
                        EditorGUIUtility.PingObject(occurence.gameobject);
                    }

                    if (!filteron)
                    {
                        if (GUILayout.Button("Filter"))
                        {
                            SetSearchFilter(occurence.name, FILTERMODE_NAME);
                            filteron = true;
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("Off"))
                        {
                            SetSearchFilter("", FILTERMODE_NAME);
                            filteron = false;
                        }
                    }

                    if (GUILayout.Button("Select"))
                    {
                        Selection.objects = occurence.gameobjects.ToArray();
                    }

                    if (GUILayout.Button("Dup."))
                    {
                        Selection.objects = occurence.duplicates.ToArray();
                    }

                    GUILayout.Space(15);
                    GUILayout.EndHorizontal();
                }
            }


            GUILayout.EndScrollView();
            oldcheckthis = checkthis;
        }

        private void OnLostFocus()
        {
            EditorPrefs.SetBool( "realvirtual/CADChecker/UpperPartNaming",useupperpartnaming);
            EditorPrefs.SetBool( "realvirtual/CADChecker/ShowOccurencies",showoccurencies);
        }

        public const int FILTERMODE_ALL = 0;
        public const int FILTERMODE_NAME = 1;
        public const int FILTERMODE_TYPE = 2;

        public static void SetSearchFilter(string filter, int filterMode)
        {
            SearchableEditorWindow[] windows =
                (SearchableEditorWindow[]) UnityEngine.Resources.FindObjectsOfTypeAll(typeof(SearchableEditorWindow));

            foreach (SearchableEditorWindow window in windows)
            {
                if (window.GetType().ToString() == "UnityEditor.SceneHierarchyWindow")
                {
                    hierarchy = window;
                    break;
                }
            }

            if (hierarchy == null)
                return;

            MethodInfo setSearchType = typeof(SearchableEditorWindow).GetMethod("SetSearchFilter",
                BindingFlags.NonPublic | BindingFlags.Instance);
            object[] parameters = new object[] {filter, filterMode, true, false};

            setSearchType.Invoke(hierarchy, parameters);
        }
    }
}