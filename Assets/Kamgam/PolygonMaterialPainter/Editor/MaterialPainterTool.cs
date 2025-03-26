using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;
using System.Collections.Generic;
using System;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
#if UNITY_2021_2_OR_NEWER
using PrefabStage = UnityEditor.SceneManagement.PrefabStage;
#else
using PrefabStage = UnityEditor.Experimental.SceneManagement.PrefabStage;
#endif

namespace Kamgam.PolygonMaterialPainter
{
    [EditorTool("Polygon Material Painter")]
    partial class MaterialPainterTool : EditorTool
    {
        public enum Mode { PickObject, PaintSelection }

        public static MaterialPainterTool Instance;

        public GameObject[] SelectedObjects = new GameObject[] { };

        public PersistentAssetReferenceList<Material> MaterialReferences = new PersistentAssetReferenceList<Material>("Kamgam.PolygonMaterialPainter.MaterialReferences");

        protected Mode _mode = Mode.PickObject;
        protected Tool _toolBefore;
        protected IUndoStack _activeUndoStack = null;

        // flags & temp
        protected bool _selectionChanged;
        protected bool _mouseIsDown;
        protected bool _mouseIsInSceneView;
        protected bool _mouseEnteredSceneView;
        protected bool _leftMouseIsDown;
        protected bool _leftMouseWasPressed;
        protected bool _leftMouseWasReleased;
        protected bool _shiftPressed;
        protected bool _altPressed;
        protected bool _controlPressed;
        protected bool _scrollWheelTurned;
        protected double _lastMouseDragTime;
        protected double _lastScrollWheelTime;
        protected int _toolActiveFrameCount;

        public override void OnActivated()
        {
            Instance = this;
            PolygonMaterialPainterSettings.GetOrCreateSettings();

            SetMode(Mode.PickObject);
            if (Selection.gameObjects.Length > 0)
            {
                updateSelected();
                if (SelectedObjects.Length > 0)
                {
                    SetMode(Mode.PaintSelection);
                }
            }

            _toolActiveFrameCount = 0;

            Selection.selectionChanged -= onSelectionChanged;
            Selection.selectionChanged += onSelectionChanged;

            var sceneViewDrawer = SceneViewDrawer.Instance();
            sceneViewDrawer.OnRender -= onRenderMesh;
            sceneViewDrawer.OnRender += onRenderMesh;

            EditorApplication.hierarchyChanged -= onHierarchyChanged;
            EditorApplication.hierarchyChanged += onHierarchyChanged;

            EditorSceneManager.sceneOpened -= onSceneOpened;
            EditorSceneManager.sceneOpened += onSceneOpened;

            PrefabStage.prefabStageOpened -= onClearDueToPrefabStage;
            PrefabStage.prefabStageOpened += onClearDueToPrefabStage;

            PrefabStage.prefabStageClosing -= onClearDueToPrefabStage;
            PrefabStage.prefabStageClosing += onClearDueToPrefabStage;

            MaterialReferences.AddNullValues = true;
            MaterialReferences.Load();
            int numOfMaterialsToChooseFrom = 5;
            for (int i = 0; i < numOfMaterialsToChooseFrom - MaterialReferences.References.Count; i++)
            {
                MaterialReferences.AddAsset(null);
            }
            MaterialReferences.Save();
            MaterialReferences.AddNullValues = false;
            MaterialReferences.HasChanged = false;

            MaterialPainterWindow.ShowWindow();

            TriangleCache.CacheTriangles();

            initSelectUndoStack();
            _selectionUndoStack.Clear();

            initExecuteUndoStack();
            _executeUndoStack.Clear();
        }

        void onClearDueToPrefabStage(PrefabStage obj)
        {
            ClearSelection();
        }

        void onSceneOpened(Scene scene, OpenSceneMode mode)
        {
            ClearSelection();
        }

        public override void OnWillBeDeactivated()
        {
            SetMode(Mode.PickObject);
        }

        /// <summary>
        /// A method to activate the tool. Though remember, this is not always called.
        /// The tool is actually a ScriptableObject which is instantiated and deserialized by Unity.
        /// </summary>
        [MenuItem("Tools/Polygon Material Painter/Start", priority = 1)]
        public static void Activate()
        {
#if UNITY_2020_2_OR_NEWER
            ToolManager.SetActiveTool(typeof(MaterialPainterTool));
#else
            EditorTools.SetActiveTool(typeof(MeshExtractorTool));
#endif

            SceneView.lastActiveSceneView.Focus();
        }

        public void OnToolChanged()
        {
            if (MaterialPainterToolActiveState.IsActive)
            {
                SceneView.lastActiveSceneView.Focus();

                _mouseIsDown = false;
                _leftMouseIsDown = false;
            }
        }

        private void onHierarchyChanged()
        {
            // For some reason the hierarch changes if a tool gets activated for the very first time. Thus we add this delay.
            if (_toolActiveFrameCount > 100 && PolygonMaterialPainterSettings.GetOrCreateSettings().DisableOnHierarchyChange)
            {
                ExitTool();
            }
        }

        // Will be called after all regular rendering is done
        public void onRenderMesh()
        {
            onSelectRenderMesh();
        }

        // Equivalent to Editor.OnSceneGUI.
        public override void OnToolGUI(EditorWindow window)
        {
            var current = Event.current;
            OnGUIWithEvent(current, window);
        }

        bool isDocked(EditorWindow window)
        {
            if (window == null)
                return false;

#if UNITY_2020_2_OR_NEWER
            return window.docked;
#else
            return false;
#endif
        }

        public void OnGUIWithEvent(Event current, EditorWindow window = null)
        {
            _toolActiveFrameCount++;

            SceneView sceneView = null;
            if (window != null)
            {
                sceneView = window as SceneView;
            }
            if (sceneView == null)
            {
                sceneView = SceneView.lastActiveSceneView;
            }

            // Detect if the mouse returns into the scene view
            if (current.type == EventType.Repaint)
            {
                bool mouseIsInSceneView = IsMouseInSceneView();
                if (!_mouseIsInSceneView && mouseIsInSceneView)
                {
                    _mouseEnteredSceneView = true;
                }
                _mouseIsInSceneView = mouseIsInSceneView;
            }

            // handle key presses & draw handles
            int passiveControlID = GUIUtility.GetControlID(FocusType.Passive);

            // Refocus scene view
            if (IsMouseInSceneView() && !SceneViewIsActive())
            {
                sceneView.Focus();
            }

            // Key events
            bool keyEvent = false;
            bool useKey = false;
            _shiftPressed = current.shift;
            _controlPressed = current.control;
            // Each key down trigger two KeyDown event, one with keyCode set and
            // another with keyCode = None (but event.character set). We are only
            // interested in the keyCode.
            if (current.type == EventType.KeyDown && current.keyCode != KeyCode.None)
            {
                keyEvent = true;

                // undo redo
                if ((SceneViewIsActive() || IsMouseInSceneView() || IsMouseInWindow())
                    && current.isKey && (current.control || current.command)
                    )
                {
                    if (current.keyCode == KeyCode.Z)
                    {
                        Undo();
                        useKey = true;
                    }
                    else if (current.keyCode == KeyCode.Y)
                    {
                        Redo();
                        useKey = true;
                    }
                }

                if (current.keyCode == KeyCode.Escape)
                {
                    useKey = true;
                    if (_mode == Mode.PaintSelection)
                    {
                        SetMode(Mode.PickObject);
                    }
                    else
                    {
                        ExitTool();
                    }
                }

                if (current.keyCode == KeyCode.LeftAlt)
                {
                    _altPressed = true;
                }

                if (useKey)
                {
                    current.Use();
                }
            }

            if (current.isKey && current.keyCode == PolygonMaterialPainterSettings.GetOrCreateSettings().TriggerSelectLinked && !Tools.viewToolActive )
            {
                keyEvent = true;
                useKey = true;
            }


            // Mouse events
            bool mouseEvent = false;
            if (current.type == EventType.MouseDown)
            {
                _mouseIsDown = true;
                if (current.button == 0)
                {
                    _leftMouseIsDown = true;
                    _leftMouseWasPressed = true;
                }
                mouseEvent = true;
            }
            else if (current.type == EventType.MouseUp)
            {
                _mouseIsDown = false;
                _leftMouseIsDown = false;
                _leftMouseWasReleased = true;
                mouseEvent = true;
            }
            else if (current.type == EventType.MouseDrag)
            {
                _mouseIsDown = true;
                mouseEvent = true;
                _lastMouseDragTime = EditorApplication.timeSinceStartup;
                if (current.button == 0)
                {
                    _leftMouseIsDown = true;
                }
            }
            else if (current.type == EventType.MouseMove)
            {
                // fixing mouse down
                bool timedOut = EditorApplication.timeSinceStartup - _lastMouseDragTime > 0.05f;
                if (_mouseIsDown && timedOut)
                {
                    _mouseIsDown = false;
                }
                if (_leftMouseIsDown && timedOut)
                {
                    _leftMouseIsDown = false;
                }
                mouseEvent = true;
            }

            bool scrollWheelEvent = false;
            if (current.type == EventType.ScrollWheel)
            {
                _scrollWheelTurned = true;
                _lastScrollWheelTime = EditorApplication.timeSinceStartup;
                scrollWheelEvent = true;
            }

            switch (_mode)
            {
                case Mode.PickObject:
                    break;

                case Mode.PaintSelection:
                    onSelectGUI(sceneView);
                    break;

                default:
                    break;
            }

            // Restore selection after the mouse entered the scene view again.
            if (_mouseEnteredSceneView && SelectedObjects.Length > 0)
            {
                restoreSelected();
            }

            if (mouseEvent
                // Don't consume mouse events if a view tool is active.
                && !Tools.viewToolActive && !current.alt
                // Consume only left mouse button down or drag events.
                && current.button == 0
                && (current.type == EventType.MouseDown || current.type == EventType.MouseDrag)
                )
            {
                if (_mode == Mode.PaintSelection && IsMouseInSceneView()
                    // Do not consume if CTRL is pressed AND no triangles are hovered.
                    && !(current.control && _hoverTriangles.Count == 0)
                    )
                {
                    GUIUtility.hotControl = passiveControlID;
                    Event.current.Use();
                }
            }

            // Consume scroll wheel events.
            if (scrollWheelEvent)
            {
                if (selectConsumesScrollWheelEvent(current))
                {
                    // Do not set the hot control to inactive if the scroll wheel was used (otherwise it would prohibit key press detection afterwards).
                    Event.current.Use();
                }
            }
            else if (keyEvent && useKey)
            {
                Event.current.Use();
            }

            resetFlags();

            // Reset mode to object selection if no object is selected.
            if (_mode == Mode.PaintSelection && SelectedObjects.Length == 0)
            {
                SetMode(Mode.PickObject);
            }
        }

        public void Redo()
        {
            if (_activeUndoStack != null && _activeUndoStack.HasRedoActions())
            {
                _activeUndoStack.Redo();
            }
        }

        public void Undo()
        {
            if (_activeUndoStack != null && _activeUndoStack.HasUndoActions())
            {
                _activeUndoStack.Undo();
            }
        }

        void updateSelected()
        {
            List<GameObject> selectedObjectsInScene = getValidSelection();

            if (selectedObjectsInScene.Count > 0)
            {
                SelectedObjects = selectedObjectsInScene.ToArray();
                Selection.objects = SelectedObjects;
            }

            resetToPickObjectModeIfNecessary();
        }

        void restoreSelected()
        {
            List<GameObject> selectedObjectsInScene = getValidSelection();

            if (selectedObjectsInScene.Count == 0)
            {
                var newSelectedObjects = new List<GameObject>(SelectedObjects);

                // Add the object from the selected triangles
                foreach (var tri in _selectedTriangles)
                {
                    if(tri.Component == null)
                    {
                        continue;
                    }

                    if (!selectedObjectsInScene.Contains(tri.Component.gameObject))
                    {
                        bool isChild = false;
                        foreach (var selectedObj in SelectedObjects)
                        {
                            if (tri.Component.transform.IsChildOf(selectedObj.transform))
                            {
                                isChild = true;
                                break;
                            }
                        }

                        if (!isChild && UtilsEditor.IsInScene(tri.Component.gameObject))
                        {
                            newSelectedObjects.Add(tri.Component.gameObject);
                        }
                    }
                }

                Selection.objects = newSelectedObjects.ToArray();
            }

            resetToPickObjectModeIfNecessary();
        }

        void resetToPickObjectModeIfNecessary()
        {
            if (_mode != Mode.PickObject && SelectedObjects.Length == 0)
            {
                SetMode(Mode.PickObject);
            }
        }

        List<GameObject> getValidSelection()
        {
            var selectedObjects = Selection.gameObjects;

            // Put all the objects which are in the scene view into a list (ignore the others).
            List<GameObject> selectedObjectsInScene = new List<GameObject>();
            if (selectedObjects.Length > 0)
            {
                for (int i = 0; i < selectedObjects.Length; i++)
                {
                    if (UtilsEditor.IsInScene(selectedObjects[i].gameObject))
                    {
                        // Add only if the object has a valid mesh on it.
                        var meshRenderer = selectedObjects[i].gameObject.GetComponentInChildren<MeshRenderer>();
                        var meshFilter = selectedObjects[i].gameObject.GetComponentInChildren<MeshFilter>();
                        var skinnedRenderer = selectedObjects[i].gameObject.GetComponentInChildren<SkinnedMeshRenderer>();
                        if ((meshFilter != null && meshFilter.sharedMesh != null) || (skinnedRenderer != null && skinnedRenderer.sharedMesh != null))
                        {
                            selectedObjectsInScene.Add(selectedObjects[i]);
                        }
                    }
                }
            }

            return selectedObjectsInScene;
        }

        private void resetFlags()
        {
            _selectionChanged = false;
            _leftMouseWasPressed = false;
            _leftMouseWasReleased = false;
            _scrollWheelTurned = false;
            _altPressed = false;
            _mouseEnteredSceneView = false;
        }

        public Mode GetMode()
        {
            return _mode;
        }

        public void SetMode(Mode mode)
        {
            if (_mode == mode)
                return;

            switch (mode)
            {
                case Mode.PickObject:
                    DefragTriangles();
                    break;

                case Mode.PaintSelection:
                    DefragTriangles();

                    autoClearOldSelection();

                    if (didCameraChange(SceneView.lastActiveSceneView.camera))
                    {
                        TriangleCache.CacheTriangles(SceneView.lastActiveSceneView.camera, SelectedObjects);
                        // Make sure the cached world space is in sync.
                        foreach (var tri in _selectedTriangles)
                        {
                            tri.UpdateWorldPos();
                        }
                    }
                    break;

                default:
                    break;
            }

            _mode = mode;
        }

        void autoClearOldSelection()
        {
            // Check if there are selected triangles in object which are currently not selected.
            if (_mode == Mode.PickObject && _selectedTriangles.Count > 0)
            {
                bool oldSelectionFound = false;
                foreach (var tri in _selectedTriangles)
                {
                    bool foundInSelection = false;
                    foreach (var obj in SelectedObjects)
                    {
                        if (tri.Transform.IsChildOf(obj.transform))
                        {
                            foundInSelection = true;
                            break;
                        }
                    }
                    if (!foundInSelection)
                    {
                        oldSelectionFound = true;
                        break;
                    }
                }
                if (oldSelectionFound)
                {
                    var trisToRemove = new List<SelectedTriangle>();
                    foreach (var tri in _selectedTriangles)
                    {
                        bool foundInSelection = false;
                        foreach (var obj in SelectedObjects)
                        {
                            if (tri.Transform.IsChildOf(obj.transform))
                            {
                                foundInSelection = true;
                                break;
                            }
                        }
                        if (!foundInSelection)
                        {
                            trisToRemove.Add(tri);
                        }
                    }
                    foreach (var tri in trisToRemove)
                    {
                        _selectedTriangles.Remove(tri);
                    }
                }

            }
        }

        public static Camera GetValidSceneViewCamera()
        {
            var cam = SceneView.lastActiveSceneView.camera;
            if (cam != null && cam.transform.position != Vector3.zero)
            {
                return cam;
            }

            return null;
        }


        public static bool SceneViewIsActive()
        {
            return EditorWindow.focusedWindow == SceneView.lastActiveSceneView;
        }

        public static bool IsMouseInSceneView()
        {
            return EditorWindow.mouseOverWindow != null && SceneView.sceneViews.Contains(EditorWindow.mouseOverWindow);
        }

        public static bool IsMouseInWindow()
        {
            return EditorWindow.mouseOverWindow is MaterialPainterWindow;
        }

        public void ExitTool()
        {
            Tools.current = Tool.Move;
            SelectedObjects = new GameObject[] { };

            Selection.selectionChanged -= onSelectionChanged;
            SceneViewDrawer.Instance().OnRender -= onRenderMesh;
            EditorApplication.hierarchyChanged -= onHierarchyChanged;

            TriangleCache.Clear();
        }

        public void Reset()
        {
            ClearSelection();
            Selection.objects = new GameObject[] { };
            resetSelect();
            resetExecute();

        }

        public bool HasSelectedObjects()
        {
            return SelectedObjects.Length > 0;
        }

        void onSelectionChanged()
        {
            _selectionChanged = true;

            updateSelected();

            // We do this to trigger a cache renewal after the selectin changed.
            resetCameraMemory();

            _selectionUndoStack.Clear();
            _executeUndoStack.Clear();

            if (MaterialPainterWindow.Instance != null)
            {
                MaterialPainterWindow.Instance.Repaint();
            }
        }
    }
}
