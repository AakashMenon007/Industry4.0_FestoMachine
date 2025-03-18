using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;


namespace realvirtual
{
#pragma warning disable 0414
    [InitializeOnLoad]
    //! Class to handle the creation of the realvirtual menu
    public class MeasureTool : EditorWindow 
    {
        private static Vector3 startpoint; 
        private static Vector3 endpoint;
        private static Vector3 thirdpoint;
        private static List<Vector3> points = new List<Vector3>();
        private static Vector3 centerpoint=Vector3.zero;
        private static float radius=0;
        private static float diameter=0;
        private static Vector3 normal=Vector3.zero;
        private float currentDistance=0f;
        private Vector2 scrollPos;
        private static string currentSelection;
        private string drawGizmo = "";
        private static bool startpointSelected = false;
        private static bool endpointSelected = false;
        private static bool thirdpointSelected = false;
        private static Color activeButtonColor = new Color(0.87f, 0.3f, 0.49f, 1f);
        private static Color ButtonColor = new Color(0.35f,0.35f,0.35f,1f);
        private static int scaleUnit = 1;
        private static bool useScaleUnit = false;
        
        public static bool select = false;
        
        private static float handleScaleFactor = 0.05f;
        
        [MenuItem("realvirtual/Measurement (Pro)", false, 400)]
        static void Init()
        {
            currentSelection = "Select first point";
            select = true;
            if (points.Contains(startpoint))
                points.Remove(startpoint);
            SceneView.duringSceneGui += OnSceneGUI;
            MeasureTool tool =
             (MeasureTool) EditorWindow.GetWindow(typeof(MeasureTool));
            tool.titleContent = new GUIContent("Measure");
            tool.minSize = new Vector2(230, 250);
            tool.Show();
        }
        void OnGUI()
        {
#if UNITY_EDITOR
            GUIStyle ButtonPoint1Style=new GUIStyle(GUI.skin.button);
            GUIStyle ButtonPoint2Style=new GUIStyle(GUI.skin.button);
            GUIStyle ButtonPoint3Style=new GUIStyle(GUI.skin.button);
            Texture2D activeTex = CreateTexture(activeButtonColor);
            Texture2D inactiveTex = CreateTexture(ButtonColor);

            switch (currentSelection)
            {
                case "Select first point":
                    ButtonPoint1Style.normal.background = activeTex;
                    ButtonPoint2Style.normal.background = inactiveTex;
                    ButtonPoint3Style.normal.background = inactiveTex;
                    break;
                case "Select second point":
                    ButtonPoint1Style.normal.background = inactiveTex;
                    ButtonPoint2Style.normal.background = activeTex;
                    ButtonPoint3Style.normal.background = inactiveTex;
                    break;
                case "Select third point":
                    ButtonPoint1Style.normal.background = inactiveTex;
                    ButtonPoint2Style.normal.background = inactiveTex;
                    ButtonPoint3Style.normal.background = activeTex;
                    break;
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
                if (GUILayout.Button("Point 1", ButtonPoint1Style, GUILayout.Width((width / 3) - 10)))
                {
                    // selection by click
                    currentSelection = "Select first point";
                    select = true;
                    if (points.Contains(startpoint))
                        points.Remove(startpoint);
                    SceneView.duringSceneGui += OnSceneGUI;
                }
                // field to show a vector3
                EditorGUILayout.Vector3Field("", startpoint);
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Separator();

                EditorGUILayout.BeginHorizontal();
            
                if (GUILayout.Button("Point 2",ButtonPoint2Style, GUILayout.Width((width / 3) - 10)))
                {
                    currentSelection = "Select second point";
                    select = true;
                    endpointSelected = true;
                    if (points.Contains(endpoint))
                        points.Remove(endpoint);
                    SceneView.duringSceneGui += OnSceneGUI;
                }
                EditorGUILayout.Vector3Field("", endpoint);
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Separator();
                
                EditorGUILayout.BeginHorizontal();
                useScaleUnit = EditorGUILayout.Toggle("Unit mm", useScaleUnit, GUILayout.Width((width / 3) - 10));
                if (useScaleUnit)
                    scaleUnit = 1000;
                else
                {
                    scaleUnit = 1;
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Separator();
                
                EditorGUILayout.BeginHorizontal();
                currentDistance = (Vector3.Distance(startpoint, endpoint))*scaleUnit;
                string formDist = currentDistance.ToString("F3");
                EditorGUILayout.TextField("Distance:", formDist);
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Separator();
                
                EditorGUILayout.BeginHorizontal();
                string formX = ((Math.Abs(endpoint.x - startpoint.x))*scaleUnit).ToString("F3");
                EditorGUILayout.TextField("Distance X:",formX );
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Separator();
                
                EditorGUILayout.BeginHorizontal();
                string formY = ((Math.Abs(endpoint.y - startpoint.y))*scaleUnit).ToString("F3");
                EditorGUILayout.TextField("Distance Y:", formY);
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Separator();
                
                EditorGUILayout.BeginHorizontal();
                string formZ = ((Math.Abs(endpoint.z - startpoint.z))*scaleUnit).ToString("F3");
                EditorGUILayout.TextField("Distance Z:", formZ);
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Separator();
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Measure centerpoint");
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Separator();
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Point 3",ButtonPoint3Style, GUILayout.Width((width / 3) - 10)))
                {
                    currentSelection = "Select third point";
                    select = true;
                    if (points.Contains(thirdpoint))
                        points.Remove(thirdpoint);
                    if (points.Contains(centerpoint))
                        points.Remove(centerpoint);
                    
                    SceneView.duringSceneGui += OnSceneGUI;
                }
                EditorGUILayout.Vector3Field("", thirdpoint);
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Separator();
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Centerpoint:",GUILayout.Width((width / 3) - 10));
                if (startpointSelected && endpointSelected && thirdpointSelected)
                {
                    centerpoint = CalculateCenterpoint(startpoint, endpoint, thirdpoint);
                    normal = Vector3.Cross(startpoint - centerpoint, endpoint - centerpoint);
                    points.Add(centerpoint);
                }
                EditorGUILayout.Vector3Field("", centerpoint);
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Separator();
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Radius:",GUILayout.Width((width / 4) - 10));
                EditorGUILayout.TextField(radius.ToString("F3"),GUILayout.Width((width / 4) - 10));
                EditorGUILayout.LabelField("Diameter:",GUILayout.Width((width / 4) - 10));
                EditorGUILayout.TextField(diameter.ToString("F3"),GUILayout.Width((width / 4) - 10));

                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Separator();
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Reset", GUILayout.Width((width / 3) - 10)))
                {
                    startpoint = Vector3.zero;
                    endpoint = Vector3.zero;
                    thirdpoint = Vector3.zero;
                    centerpoint = Vector3.zero;
                    startpointSelected = false;
                    endpointSelected = false;
                    thirdpointSelected = false;
                    currentDistance = 0f;
                    points.Clear();
                    currentSelection = "Select first point";
                    select = true;
                    SceneView.duringSceneGui += OnSceneGUI;
                    var window = GetWindow<MeasureTool>();
                    window.Repaint();
                }
            
                
                EditorGUILayout.EndHorizontal();
            
            GUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
#endif
        }

        
        static void OnSceneGUI(SceneView sceneView)
        {

            Event current = Event.current;
            float handleSize = 0.05f;
            if (select)
            {
                Handles.color = Color.red;
                Vector2 mousePos = current.mousePosition;
                bool found = HandleUtility.FindNearestVertex(mousePos, out Vector3 vertex);
                if (found)
                {
                    handleSize = CalculateHandleSize(vertex);
                    Handles.SphereHandleCap(0, vertex, Quaternion.identity, handleSize, EventType.Repaint);
                    Handles.Label(vertex, vertex.ToString());
                }
                if (current.type == EventType.MouseDown && current.button == 0 && found)
                {
                    if (currentSelection == "Select first point")
                    {
                        startpoint = RoundVector(vertex,3);
                        points.Add(startpoint);
                        startpointSelected = true;
                        currentSelection = "Select second point";
                        select = true;
                        current.Use();
                    }
                    else if (currentSelection == "Select second point")
                    {
                        endpoint = RoundVector(vertex,3);;
                        points.Add(endpoint);
                        endpointSelected = true;
                        currentSelection = "";
                        select = false;
                    }
                    else if(currentSelection == "Select third point")
                    {
                        thirdpoint = RoundVector(vertex,3);;
                        points.Add(thirdpoint);
                        thirdpointSelected = true;
                        currentSelection = "";
                        select = false;
                    }
                }
            }
            for (int i = 0; i < points.Count; i++)
            {
               
                Handles.color = Color.green;
                handleSize=CalculateHandleSize(points[i]);
                Handles.SphereHandleCap(0, points[i], Quaternion.identity, handleSize, EventType.Repaint);
                Handles.Label(points[i], points[i].ToString());
                if (current.type == EventType.Layout)
                {
                    HandleUtility.Repaint();
                }
            }
            if(startpointSelected && endpointSelected)
            {
                Handles.color = Color.green;
                Handles.DrawLine(startpoint, endpoint);
            }
            if (startpointSelected && endpointSelected && thirdpointSelected)
            {
                Handles.color = Color.blue;
                Handles.DrawLine(startpoint, centerpoint);
                Handles.DrawLine(thirdpoint, centerpoint);
                Handles.DrawLine(endpoint, centerpoint);
                Handles.DrawWireDisc(centerpoint, normal, Vector3.Distance(startpoint, centerpoint));
            }
            if (current.type == EventType.Layout)
            {
                HandleUtility.Repaint();
            }
        }

        private void OnEnable()
        {
            startpoint = Vector3.zero;
            endpoint= Vector3.zero;
            currentDistance = 0f;
            points.Clear();
        }

        void OnDisable()
        {
           SceneView.duringSceneGui -= OnSceneGUI;
           currentSelection = "";
           startpoint = Vector3.zero;
           endpoint= Vector3.zero;
           currentDistance = 0f;
           points.Clear();
           startpointSelected = false;
           endpointSelected = false;
           thirdpointSelected = false;
           select = false;
        }

        private static float CalculateHandleSize(Vector3 vertex)
        {
            Camera sceneViewCamera = SceneView.lastActiveSceneView.camera;
            float zoomFactor;

            if (sceneViewCamera.orthographic)
            {
                // For orthographic cameras, zoom factor is the orthographic size
                zoomFactor = sceneViewCamera.orthographicSize;
            }
            else
            {
                // For perspective cameras, calculate zoom factor based on field of view
                float fovRadians = sceneViewCamera.fieldOfView * Mathf.Deg2Rad;
                float distance = Vector3.Distance(sceneViewCamera.transform.position, vertex);
                zoomFactor = distance * Mathf.Tan(0.5f * fovRadians);
            }

            float handleSize = zoomFactor*handleScaleFactor;
            return handleSize;
        }
        private Vector3 CalculateCenterpoint(Vector3 A, Vector3 B, Vector3 C)
        {
           Vector3 center = Vector3.zero;
           
           Vector3 u = (B - A).normalized;
           Vector3 w = Vector3.Cross(C - A, u).normalized;
           Vector3 v = Vector3.Cross(w, u).normalized;

           // Calculate the 2D coordinates of B and C in the u, v plane
           float bx = Vector3.Dot(B - A, u);
           float by = Vector3.Dot(B - A, v);

           float cx = Vector3.Dot(C - A, u);
           float cy = Vector3.Dot(C - A, v);

           // Calculate the center of the circle in 2D coordinates
           float h = ((cx - bx / 2) * (cx - bx / 2) + cy * cy - (bx / 2) * (bx / 2)) / (2 * cy);

           // Calculate the center of the circle in 3D coordinates
          center = A + (bx / 2) * u + h * v;
          
          radius = Vector3.Distance(center, A)*scaleUnit;
          diameter = (radius * 2);
          
           return RoundVector(center,3);;
        }

        private static Texture2D CreateTexture(Color color)
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0,0,color);
            tex.Apply();
            return tex;
        }
        private static Vector3 RoundVector(Vector3 vector, int decimalPlaces)
        {
            float multiplier = Mathf.Pow(10, decimalPlaces);

            float x = Mathf.Round(vector.x * multiplier) / multiplier;
            float y = Mathf.Round(vector.y * multiplier) / multiplier;
            float z = Mathf.Round(vector.z * multiplier) / multiplier;

            return new Vector3(x, y, z);
        }
    }
}
