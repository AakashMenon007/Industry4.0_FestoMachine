using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Kamgam.PolygonMaterialPainter
{
    partial class MaterialPainterTool
    {
        public enum SelectionUpdateCause { Unknown, UndoPerformed, Add, Remove, ModeChanged }

        protected HashSet<SelectedTriangle> _selectedTriangles = new HashSet<SelectedTriangle>();
        protected SelectedTriangle _lastSelectedTriangle = null;
        public SelectedTriangle GetLastSelectedTriangle() { return _lastSelectedTriangle; }
        protected SelectedTriangle _lastClickedTriangle = null;

        // tmp list for raycast results
        protected List<RayCastTriangleResult> _hoverTriangles = new List<RayCastTriangleResult>();
        protected List<RayCastTriangleResult> _currentDragSelectionTriangles = new List<RayCastTriangleResult>();

        // tmp list for triangles selected while
        protected SelectedTriangle _tmpLastSelectedTriangle;
        protected SelectedTriangle _tmpLastDeselectedTriangle;

        protected float _selectBrushSize = 0f;
        protected float _selectBrushDepth = 0.5f;
        protected bool _selectCullBack = true;
        protected bool _limitLinkedSearchToSubMesh = false;

        public float GetBrushSize()
        {
            return _selectBrushSize;
        }

        public void SetBrushSize(float size)
        {
            _selectBrushSize = size;
        }

        public float GetBrushDepth()
        {
            return _selectBrushDepth;
        }

        public void SetBrushDepth(float depth)
        {
            _selectBrushDepth = depth;
        }

        public bool GetCullBack()
        {
            return _selectCullBack;
        }

        public void SetCullBack(bool cullBack)
        {
            _selectCullBack = cullBack;
        }

        public bool GetLimitLinkedSearchToSubMesh()
        {
            return _limitLinkedSearchToSubMesh;
        }

        public void SetLimitLinkedSearchToSubMesh(bool limit)
        {
            _limitLinkedSearchToSubMesh = limit;
        }

        protected bool _removeFromSelection = false;

        [System.NonSerialized]
        protected Vector3 _camPos = new Vector3(0f, 0f, -0.001f); // arbitrary start pos not 0/0/0 to trigger an update at first check.

        [System.NonSerialized]
        protected Quaternion _camRot = Quaternion.identity;

        [System.NonSerialized]
        protected float _camAspect = 0f;

        [System.NonSerialized]
        protected double _cameraLastMoveTime = -1;

        protected bool _scheduledMeshCacheUpdateDueToScrollWheel = false;

        [System.NonSerialized]
        protected bool _selectedTrianglesChangedSinceLastUndoRecording = false;

        static Material _selectionMaterialCulled;
        static Material _selectionMaterialNoCull;
        static Material createSelectionMaterial(Color color, bool cullBack)
        {
            if (cullBack)
                return createSelectionMaterial(ref _selectionMaterialCulled, color, cullBack: true);
            else
                return createSelectionMaterial(ref _selectionMaterialNoCull, color, cullBack: false);
        }

        static Material createSelectionMaterial(ref Material material, Color color, bool cullBack)
        {
            if (material == null)
            {
                // Unity has a built-in shader that is useful for drawing
                // simple colored things.
                Shader shader = Shader.Find("Hidden/Internal-Colored");
                material = new Material(shader);
                material.hideFlags = HideFlags.HideAndDontSave;
                // Turn on alpha blending
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                // Turn backface culling off
                material.SetInt("_Cull", (int)(cullBack ? UnityEngine.Rendering.CullMode.Back : UnityEngine.Rendering.CullMode.Off));
                // Turn off depth writes
                material.SetInt("_ZWrite", 0);
                // ZTest = "Always", according to https://docs.unity3d.com/ScriptReference/Rendering.CompareFunction.html
                material.SetInt("_ZTest", 8);
                material.SetColor("_Color", color);
            }
            else
            {
                material.SetColor("_Color", color);
            }

            return material;
        }

        protected void resetSelect()
        {
            _selectBrushSize = 0f;
            _selectBrushDepth = 0.5f;
            _selectCullBack = true;
            _limitLinkedSearchToSubMesh = false;

            _removeFromSelection = false;
            if(_selectionUndoStack != null)
                _selectionUndoStack.Clear();
        }

        public void DefragTriangles()
        {
            var trisToDelete = new List<SelectedTriangle>();
            foreach (var tri in _selectedTriangles)
            {
                if (tri.Transform == null || tri.Component == null)
                {
                    trisToDelete.Add(tri);
                }
            }

            foreach (var tri in trisToDelete)
            {
                _selectedTriangles.Remove(tri);
            }
        }

        protected void resetCameraMemory()
        {
            _camPos = new Vector3(0f, 0f, -0.001f);
            _camRot = Quaternion.identity;
            _camAspect = 0f;
            _cameraLastMoveTime = -1;
        }

        protected bool didCameraChange(Camera cam)
        {
            if (cam == null)
                return false;

            bool moved = cam.transform.position != _camPos || cam.transform.rotation != _camRot || _camAspect != cam.aspect;

            _camPos = cam.transform.position;
            _camRot = cam.transform.rotation;
            _camAspect = cam.aspect;

            return moved;
        }

        protected bool isMouseInSceneView()
        {
            return EditorWindow.mouseOverWindow != null && SceneView.sceneViews.Contains(EditorWindow.mouseOverWindow);
        }

        protected bool selectConsumesScrollWheelEvent(Event evt)
        {
            return evt.isScrollWheel && evt.shift;
        }

        public void ClearSelection()
        {
            _selectedTriangles.Clear();
            _lastSelectedTriangle = null;
            TriangleCache.RebuildBakedMeshesCache(SelectedObjects);
        }

        public bool HasSelectedTriangles()
        {
            return _selectedTriangles.Count > 0;
        }

        // Called from within the tools OnToolGUI method.
        protected void onSelectGUI(SceneView sceneView)
        {
            // Update cache if triggerd by scroll wheel.
            if (_scheduledMeshCacheUpdateDueToScrollWheel && EditorApplication.timeSinceStartup - _cameraLastMoveTime > 0.05f)
            {
                _scheduledMeshCacheUpdateDueToScrollWheel = false;
                TriangleCache.CacheTriangles(sceneView.camera, SelectedObjects);
                // Make sure the cached world space is in sync.
                foreach (var tri in _selectedTriangles)
                {
                    tri.UpdateWorldPos();
                }
            }

            var settings = PolygonMaterialPainterSettings.GetOrCreateSettings();

            // Clear if in view mode
            if (Tools.viewToolActive)
            {
                _hoverTriangles.Clear();
            }

            var evt = Event.current;
            if ((evt.isMouse || evt.isScrollWheel || evt.isKey || evt.type == EventType.Repaint) && !evt.alt)
            {
                // update brush size
                if (selectConsumesScrollWheelEvent(evt))
                {
                    float newSize = _selectBrushSize + evt.delta.y * -0.0035f * settings.ScrollWheelSensitivity;
                    _selectBrushSize = Mathf.Clamp(newSize, 0f, 1f);

                    if(MaterialPainterWindow.Instance != null)
                        MaterialPainterWindow.Instance.Repaint();
                }

                // Do raycasting only if the mouse is in the scene view
                if (isMouseInSceneView())
                {
                    if (!_mouseIsDown)
                    {
                        _tmpLastSelectedTriangle = null;
                        _tmpLastDeselectedTriangle = null;
                    }

                    // Raycast into scene.
                    _hoverTriangles.Clear();
                    if (!Tools.viewToolActive)
                    {
                        // Schedule cache update?
                        bool cameraChanged = sceneView != null && didCameraChange(sceneView.camera);
                        if (cameraChanged)
                        {
                            _cameraLastMoveTime = EditorApplication.timeSinceStartup;

                            // Update cache instantly after camera movement (this works because we do NOT execute this while Tools.viewToolActive is active).
                            if (EditorApplication.timeSinceStartup - _lastScrollWheelTime > 0.05f)
                            {
                                TriangleCache.CacheTriangles(sceneView.camera, SelectedObjects);
                                // Make sure the cached world space is in sync.
                                foreach (var tri in _selectedTriangles)
                                {
                                    tri.UpdateWorldPos();
                                }
                            }
                            else
                            {
                                // Schedule change due to scroll wheel.
                                // The scroll wheel does not trigger Tools.viewToolActive to be true,
                                // thus we need to keep track of it manually.
                                _scheduledMeshCacheUpdateDueToScrollWheel = true;
                            }
                        }

                        TriangleCache.GetTrianglesUnderMouse(
                            evt,
                            cullBack: _selectCullBack,
                            rayThickness: _selectBrushSize,
                            rayDepth: _selectCullBack ? _selectBrushDepth : float.MaxValue,
                            objects: SelectedObjects,
                            allowMultipleResults: _selectBrushSize > 0.001f || !_selectCullBack,
                            results: _hoverTriangles
                        );
                    }

                    if (_hoverTriangles.Count > 0)
                    {
                        _removeFromSelection = _controlPressed;

                        if (_leftMouseIsDown && !Tools.viewToolActive)
                        {
                            recordSelectUndoIfEmpty();

                            foreach (var result in _hoverTriangles)
                            {
                                var tri = new SelectedTriangle(result);
                                if (_selectedTriangles.Contains(tri))
                                {
                                    if (_removeFromSelection && !tri.Equals(_tmpLastSelectedTriangle))
                                    {
                                        _selectedTriangles.Remove(tri);
                                        _tmpLastDeselectedTriangle = tri;
                                        _selectedTrianglesChangedSinceLastUndoRecording = true;
                                    }
                                }
                                else
                                {
                                    if (!_removeFromSelection && !tri.Equals(_tmpLastDeselectedTriangle))
                                    {
                                        _selectedTriangles.Add(tri);
                                        _tmpLastSelectedTriangle = tri;
                                        _lastSelectedTriangle = tri;
                                        _selectedTrianglesChangedSinceLastUndoRecording = true;
                                    }
                                }
                                _lastClickedTriangle = tri;
                            }
                        }
                    }

                    // Register undo on mouse up
                    if (_leftMouseWasReleased)
                    {
                        if (SelectedObjects.Length > 0 && IsMouseInSceneView())
                        {
                            if (_selectedTrianglesChangedSinceLastUndoRecording)
                            {
                                if (_selectionUndoStack.HasUndoActions())
                                {
                                    RecordSelectionForUndo(0.2f, false);
                                }
                                else
                                {
                                    RecordSelectionForUndo(0f, true);
                                }
                            }
                        }
                    }
                }

                if(evt.isKey && evt.type == EventType.KeyUp && evt.keyCode == PolygonMaterialPainterSettings.GetOrCreateSettings().TriggerSelectLinked && !evt.control && !Tools.viewToolActive)
                {
                    AddLinkedToSelection( remove: evt.shift);
                }

                if (evt.isMouse || evt.isKey)
                {
                    sceneView.Repaint();
                }
            }
        }

        public void RecordSelectionForUndo(float minDeltaTime, bool forceNewGroup = false)
        {
            _selectedTrianglesChangedSinceLastUndoRecording = false;
            _selectionUndoStack.Record(_selectedTriangles, minDeltaTime, forceNewGroup);
            _activeUndoStack = _selectionUndoStack;
        }

        public void AddLinkedToSelection(bool remove = false)
        {
            List<SelectedTriangle> trisToRemove = null;
            if (remove)
            {
                trisToRemove = new List<SelectedTriangle>();
            }

            if (_lastClickedTriangle == null)
                return;

            var linkedTris = TriangleCache.AddLinked(SelectedObjects, _lastClickedTriangle, _limitLinkedSearchToSubMesh);
            foreach (var tri in linkedTris)
            {
                if (remove)
                {
                    if (_selectedTriangles.Contains(tri))
                    {
                        trisToRemove.Add(tri);
                    }
                }
                else
                {
                    if (!_selectedTriangles.Contains(tri))
                    {
                        _selectedTriangles.Add(tri);
                    }
                }
            }

            if (remove)
            {
                foreach (var tri in trisToRemove)
                {
                    _selectedTriangles.Remove(tri);
                }
            }
        }

        // Will be called after all regular rendering is done
        public void onSelectRenderMesh()
        {
            // Paint tris under mouse
            if (_mode == Mode.PaintSelection && _hoverTriangles.Count > 0)
            {
                var color = new Color(1f, 1f, 0f);
                if (_removeFromSelection)
                {
                    color = new Color(1f, 0f, 0f);
                }
                color.a = Mathf.Clamp01(PolygonMaterialPainterSettings.GetOrCreateSettings().SelectionColorAlpha + 0.1f);

                // Apply the line material
                var material = createSelectionMaterial(color, _selectCullBack);
                material.SetPass(0);

                GL.PushMatrix();
                GL.MultMatrix(Matrix4x4.identity);
                GL.Begin(GL.TRIANGLES);
                try
                {

                    foreach (var result in _hoverTriangles)
                    {
                        if (result.Transform == null)
                            continue;

                        // Draw triangles
                        Vector3 v0 = result.Transform.TransformPoint(result.VertexLocal0);
                        Vector3 v1 = result.Transform.TransformPoint(result.VertexLocal1);
                        Vector3 v2 = result.Transform.TransformPoint(result.VertexLocal2);

                        GL.Vertex3(v0.x, v0.y, v0.z);
                        GL.Vertex3(v1.x, v1.y, v1.z);
                        GL.Vertex3(v2.x, v2.y, v2.z);
                    }
                }
                finally
                {
                    GL.End();
                    GL.PopMatrix();
                }
            }

            // Paint selected tris
            if ((_mode == Mode.PaintSelection) && _selectedTriangles.Count > 0)
            {
                var color = new Color(0f, 1f, 0f);
                color.a = PolygonMaterialPainterSettings.GetOrCreateSettings().SelectionColorAlpha;

                // Apply the line material
                var material = createSelectionMaterial(color, _selectCullBack);
                material.SetPass(0);

                GL.PushMatrix();
                GL.MultMatrix(Matrix4x4.identity);
                GL.Begin(GL.TRIANGLES);
                try
                {
                    foreach (var result in _selectedTriangles)
                    {
                        if (result.Transform == null)
                            continue;

                        // Draw triangles
                        Vector3 v0 = result.Transform.TransformPoint(result.VertexLocal0);
                        Vector3 v1 = result.Transform.TransformPoint(result.VertexLocal1);
                        Vector3 v2 = result.Transform.TransformPoint(result.VertexLocal2);

                        GL.Vertex3(v0.x, v0.y, v0.z);
                        GL.Vertex3(v1.x, v1.y, v1.z);
                        GL.Vertex3(v2.x, v2.y, v2.z);
                    }
                }
                finally
                {
                    GL.End();
                    GL.PopMatrix();
                }
            }
        }



        #region Undo Stack
        protected UndoStack<HashSet<SelectedTriangle>,HashSet<SelectedTriangle>> _selectionUndoStack;

        protected void initSelectUndoStack()
        {
            if (_selectionUndoStack == null)
                _selectionUndoStack = new UndoStack<HashSet<SelectedTriangle>, HashSet<SelectedTriangle>>(createSelectionUndoState, restoreSelectionFromUndoState);
        }

        private void recordSelectUndoIfEmpty()
        {
            initSelectUndoStack();

            // Make sure there always is an empty selection at the bottom of the undo stack.
            if (_selectionUndoStack.IsEmpty())
            {
                _selectionUndoStack.Record(_selectedTriangles);
            }
        }

        protected HashSet<SelectedTriangle> createSelectionUndoState(HashSet<SelectedTriangle> tris)
        {
            return new HashSet<SelectedTriangle>(tris);
        }

        protected void restoreSelectionFromUndoState(HashSet<SelectedTriangle> selectedTriangles)
        {
            _selectedTriangles = new HashSet<SelectedTriangle>(selectedTriangles);
        }
        #endregion
    }
}
