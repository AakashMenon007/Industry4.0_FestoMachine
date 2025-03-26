using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Kamgam.PolygonMaterialPainter
{
    partial class MaterialPainterTool
    {
        public bool MergeSameMaterialMesh = true;
        public bool ApplyChangesToSelectedObject = true;
        protected bool _deleteNewMeshAfterResetToOriginal = true;
        public bool DeleteNewMeshAfterResetToOriginal
        {
            get => _deleteNewMeshAfterResetToOriginal;
            set
            {
                _deleteNewMeshAfterResetToOriginal = value;
                foreach (var modifier in _tmpAffectedModifiers)
                {
                    modifier.DeleteNewMeshAfterResetToOriginal = value;
                }
            }
        }

        protected Dictionary<Component, MeshModifier> _meshModifiers = new Dictionary<Component, MeshModifier>();
        protected List<MeshModifier> _tmpAffectedModifiers = new List<MeshModifier>();

        protected void resetExecute()
        {
            _meshModifiers.Clear();
            _tmpAffectedModifiers.Clear();
            initExecuteUndoStack();
            _executeUndoStack.Clear();
        }

        public void AssignMaterialToSelection(Material material)
        {
            Logger.Log("Assigning " + material);

            if (_selectedTriangles.Count > 0)
            {
                // Modify the mesh.
                try
                {
                    EditorUtility.DisplayProgressBar("Analyzing Mesh", "Analyzing mesh and assigning new material.", 0.1f);

                    fillAffectedModifiersListBasedOnSelectedTriangles();

                    // Rec first Undo
                    bool materialChangeOnly = false;
                    recordFirstUndoIfNecessary(_tmpAffectedModifiers, materialChangeOnly);

                    foreach (var modifier in _tmpAffectedModifiers)
                    {
                        modifier.AssignMaterial(_selectedTriangles, material, MergeSameMaterialMesh, ApplyChangesToSelectedObject);
                    }

                    // Rec Undo
                    _executeUndoStack.Record((_tmpAffectedModifiers, materialChangeOnly));
                    _activeUndoStack = _executeUndoStack;

                    TriangleCache.CacheTriangles();
                    ClearSelection();
                    _selectionUndoStack.Clear();
                }
                finally
                {
                    EditorUtility.ClearProgressBar();
                }
            }
            else
            {
                // No triangles are selected. Simply assign the material to the sub mesh index on the selected meshes.
                fillAffectedModifiersListBasedOnSelectedObjects();

                // Rec first Undo
                bool materialChangeOnly = true;
                recordFirstUndoIfNecessary(_tmpAffectedModifiers, materialChangeOnly);

                foreach (var modifier in _tmpAffectedModifiers)
                {
                    modifier.AssignMaterialToIndex(material, modifier.LastAssignedSubMeshIndex);
                }

                // Record undo
                _executeUndoStack.Record((_tmpAffectedModifiers, materialChangeOnly));
                _activeUndoStack = _executeUndoStack;
            }
        }

        public void ResetSelected()
        {
            try
            {
                EditorUtility.DisplayProgressBar("Analyzing Mesh", "Analyzing mesh and reverting selected areas.", 0.1f);

                fillAffectedModifiersListBasedOnSelectedTriangles();

                // Rec first Undo
                bool materialChangeOnly = false;
                recordFirstUndoIfNecessary(_tmpAffectedModifiers, materialChangeOnly);

                foreach (var modifier in _tmpAffectedModifiers)
                {
                    modifier.ResetSelected(_selectedTriangles);
                }

                // Record undo
                _executeUndoStack.Record((_tmpAffectedModifiers, materialChangeOnly));
                _activeUndoStack = _executeUndoStack;

                TriangleCache.CacheTriangles();
                ClearSelection();
                _selectionUndoStack.Clear();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        public void ResetAll()
        {
            fillAffectedModifiersListBasedOnSelectedObjects();

            // Rec first Undo
            bool materialChangeOnly = false;
            recordFirstUndoIfNecessary(_tmpAffectedModifiers, materialChangeOnly);

            foreach (var modifier in _tmpAffectedModifiers)
            {
                modifier.ResetAll();
            }

            // Record undo
            _executeUndoStack.Record((_tmpAffectedModifiers, materialChangeOnly));
            _activeUndoStack = _executeUndoStack;

            TriangleCache.CacheTriangles();
            ClearSelection();
            _selectionUndoStack.Clear();
        }

        public void ResetToOriginalMesh()
        {
            fillAffectedModifiersListBasedOnSelectedObjects();

            // Rec first Undo
            bool materialChangeOnly = false;
            recordFirstUndoIfNecessary(_tmpAffectedModifiers, materialChangeOnly);

            foreach (var modifier in _tmpAffectedModifiers)
            {
                modifier.ResetToOriginal();
            }

            // Record undo
            _executeUndoStack.Record((_tmpAffectedModifiers, materialChangeOnly));
            _activeUndoStack = _executeUndoStack;

            TriangleCache.CacheTriangles();
            ClearSelection();
            _selectionUndoStack.Clear();
        }

        public void MakeCurrentOriginal()
        {
            fillAffectedModifiersListBasedOnSelectedObjects();

            foreach (var modifier in _tmpAffectedModifiers)
            {
                if (modifier.HasOriginalMesh)
                {
                    modifier.RefreshOrignialState();
                }
            }
        }

        public bool HasOriginalMesh()
        {
            fillAffectedModifiersListBasedOnSelectedObjects();

            foreach (var modifier in _tmpAffectedModifiers)
            {
                if (!modifier.HasOriginalMesh)
                {
                    return false;
                }
            }

            return true;
        }

        public bool IsUsingEditedMesh()
        {
            fillAffectedModifiersListBasedOnSelectedObjects();

            foreach (var modifier in _tmpAffectedModifiers)
            {
                if (!modifier.IsUsingEditedMesh())
                {
                    return false;
                }
            }

            return true;
        }

        void fillAffectedModifiersListBasedOnSelectedTriangles()
        {
            _tmpAffectedModifiers.Clear();

            foreach (var tri in _selectedTriangles)
            {
                createAndCacheModifier(tri.Component);

                var modifier = _meshModifiers[tri.Component];
                if (!_tmpAffectedModifiers.Contains(modifier) && modifier != null)
                {
                    _tmpAffectedModifiers.Add(modifier);
                }
            }
        }

        void createAndCacheModifier(Component comp)
        {
            // New mesh
            if (!_meshModifiers.ContainsKey(comp))
            {
                var newModifier = new MeshModifier(comp);
                _meshModifiers.Add(comp, newModifier);
            }

            // If the mesh is null (after deletion for example)
            if (!_meshModifiers[comp].HasMesh())
            {
                _meshModifiers.Remove(comp);
                var newModifier = new MeshModifier(comp);
                _meshModifiers.Add(comp, newModifier);
            }
        }

        void fillAffectedModifiersListBasedOnSelectedObjects()
        {
            _tmpAffectedModifiers.Clear();

            foreach (var obj in Selection.gameObjects)
            {
                var skinnedMeshRenderer = obj.GetComponentInChildren<SkinnedMeshRenderer>();
                var meshFilter = obj.GetComponentInChildren<MeshFilter>();

                if (skinnedMeshRenderer != null)
                {
                    createAndCacheModifier(skinnedMeshRenderer);
                    if (_meshModifiers.ContainsKey(skinnedMeshRenderer))
                    {
                        _tmpAffectedModifiers.Add(_meshModifiers[skinnedMeshRenderer]);
                    }
                }

                if (meshFilter != null)
                {
                    createAndCacheModifier(meshFilter);
                    if (_meshModifiers.ContainsKey(meshFilter))
                    {
                        _tmpAffectedModifiers.Add(_meshModifiers[meshFilter]);
                    }
                }
            }
        }

        public void ExportAsObj()
        {
            fillAffectedModifiersListBasedOnSelectedObjects();

            foreach (var modifier in _tmpAffectedModifiers)
            {
                Mesh mesh = modifier.GetSharedMeshFromComponent();
                if (mesh != null)
                {
                    string path = "Assets/Mesh-" + mesh.name + ".obj";

                    // Use mesh path instead of static path if possible.
                    var meshPath = AssetDatabase.GetAssetPath(mesh);
                    if (!string.IsNullOrEmpty(meshPath))
                    {
                        path = System.IO.Path.GetDirectoryName(meshPath) + System.IO.Path.DirectorySeparatorChar + System.IO.Path.GetFileNameWithoutExtension(meshPath) + "-export.obj";
                    }
                    ObjExporter.SaveMeshAsObj(path, mesh.name, mesh, modifier.GetSharedMaterialsFromComponent(), logFilePaths: true, pingAsset: true);
                }
            }
        }

        #region Undo Stack
        public class ExecuteUndoState
        {
            // Usually it's either a material change or mesh change.
            public MaterialAssignUndoState MaterialState;
            public MeshModificationUndoState MeshState;

            public ExecuteUndoState(MaterialAssignUndoState materialState)
            {
                MaterialState = materialState;
            }

            public ExecuteUndoState(MeshModificationUndoState meshState)
            {
                MeshState = meshState;
            }

            public static ExecuteUndoState CreateFromModifiers(List<MeshModifier> modifiers, bool materialChangeOnly)
            {
                if (materialChangeOnly)
                {
                    var innerState = MaterialAssignUndoState.CreateFromModifiers(modifiers);
                    return new ExecuteUndoState(innerState);
                }
                else
                {
                    var innerState = MeshModificationUndoState.CreateFromModifiers(modifiers);
                    return new ExecuteUndoState(innerState);
                }
            }
        }


        public class MaterialAssignUndoState
        {
            public List<SubMeshMaterialState> SubMeshMaterialStates = new List<SubMeshMaterialState>();

            /// <summary>
            /// Undo state for when only the material of one sub mesh has been changed.
            /// </summary>
            public class SubMeshMaterialState
            {
                public int SubMeshIndex;
                public Component Component;
                public Material Material;

                public static SubMeshMaterialState CreateFromModifier(MeshModifier modifier)
                {
                    var state = new SubMeshMaterialState();
                    state.Component = modifier.Component;
                    state.SubMeshIndex = modifier.LastAssignedSubMeshIndex;
                    state.Material = modifier.LastAssignedMaterial;

                    return state;
                }

                public void ApplyTo(MeshModifier modifier)
                {
                    modifier.AssignMaterialToIndex(Material, SubMeshIndex);
                }
            }

            public static MaterialAssignUndoState CreateFromModifiers(List<MeshModifier> modifiers)
            {
                var state = new MaterialAssignUndoState();
                foreach (var modifier in modifiers)
                {
                    if (!modifier.HasMesh())
                        continue;

                    var s = SubMeshMaterialState.CreateFromModifier(modifier);
                    state.SubMeshMaterialStates.Add(s);
                }

                return state;
            }
        }

        public class MeshModificationUndoState
        {
            public List<ModifierState> ModifierStates = new List<ModifierState>();

            public class ModifierState
            {
                public Component Component;
                public SkinnedMeshRenderer SkinnedMeshRenderer;
                public MeshRenderer MeshRenderer;
                public MeshFilter MeshFilter;
                public Material[] Materials;
                public Mesh Mesh;
                public int LastAssignedSubMeshIndex;
                public Material LastAssignedMaterial;

                public static ModifierState CreateFromModifier(MeshModifier modifier)
                {
                    var state = new ModifierState();
                    state.Component = modifier.Component;
                    state.SkinnedMeshRenderer = modifier.SkinnedMeshRenderer;
                    state.MeshRenderer = modifier.MeshRenderer;
                    state.MeshFilter = modifier.MeshFilter;
                    state.LastAssignedSubMeshIndex = modifier.LastAssignedSubMeshIndex;
                    state.LastAssignedMaterial = modifier.LastAssignedMaterial;

                    if (modifier.SkinnedMeshRenderer != null)
                    {
                        state.Mesh = new Mesh();
                        state.Mesh.name = modifier.SkinnedMeshRenderer.sharedMesh.name;
                        MeshUtils.CopyMesh(modifier.SkinnedMeshRenderer.sharedMesh, state.Mesh);

                        state.Materials = new Material[modifier.SkinnedMeshRenderer.sharedMaterials.Length];
                        modifier.SkinnedMeshRenderer.sharedMaterials.CopyTo(state.Materials, 0);
                    }
                    else if (modifier.MeshRenderer != null && modifier.MeshFilter != null)
                    {
                        state.Mesh = new Mesh();
                        state.Mesh.name = modifier.MeshFilter.sharedMesh.name;
                        MeshUtils.CopyMesh(modifier.MeshFilter.sharedMesh, state.Mesh);

                        state.Materials = new Material[modifier.MeshRenderer.sharedMaterials.Length];
                        modifier.MeshRenderer.sharedMaterials.CopyTo(state.Materials, 0);
                    }

                    return state;
                }

                public void ApplyTo(MeshModifier modifier)
                {
                    if (SkinnedMeshRenderer != null && modifier.SkinnedMeshRenderer != null && modifier.IsUsingEditedMesh())
                    {
                        if (MeshModifier.IsNameOfEditedMesh(modifier.SkinnedMeshRenderer.sharedMesh.name) &&
                            MeshModifier.IsNameOfEditedMesh(Mesh.name))
                        {
                            // The actual undo
                            MeshUtils.CopyMesh(Mesh, modifier.SkinnedMeshRenderer.sharedMesh);
                        }
                        else
                        {
                            modifier.SkinnedMeshRenderer.sharedMesh = Mesh;
                        }
                        modifier.SkinnedMeshRenderer.SetSharedMaterialsAsCopy(Materials);
                        modifier.LastAssignedSubMeshIndex = LastAssignedSubMeshIndex;
                        modifier.LastAssignedMaterial = LastAssignedMaterial;
                    }
                    else if (MeshRenderer != null && modifier.MeshFilter != null)
                    {
                        // Only copy in the mesh data if the object already is an edited mesh asset.
                        // Otherwise NOT copy in the mesh data since that would change the mesh of an asset
                        // which was not created by this tool.
                        //
                        // Also if the source mesh in the undo state is NOT and edited mesh then do not copy
                        // the mesh data but simply assign the mesh (revert to original instead of creating a copy).

                        // TODO: Meshes are reverted by copying the data. This leads to mesh assets remaining which
                        // are not used yet they appear as if they were used since the copied mesh name is the same.
                        // Fix: Keep track of assets in undos too and recreate/delete them accordingly. 
                        if (MeshModifier.IsNameOfEditedMesh(modifier.MeshFilter.sharedMesh.name) &&
                            MeshModifier.IsNameOfEditedMesh(Mesh.name)
                            )
                        {
                            MeshUtils.CopyMesh(Mesh, modifier.MeshFilter.sharedMesh);
                        }
                        else
                        {
                            modifier.MeshFilter.sharedMesh = Mesh;
                        }
                        modifier.MeshRenderer.SetSharedMaterialsAsCopy(Materials);
                        modifier.LastAssignedSubMeshIndex = LastAssignedSubMeshIndex;
                        modifier.LastAssignedMaterial = LastAssignedMaterial;
                    }
                }
            }

            public static MeshModificationUndoState CreateFromModifiers(List<MeshModifier> modifiers)
            {
                var state = new MeshModificationUndoState();
                foreach (var modifier in modifiers)
                {
                    if (!modifier.HasMesh())
                        continue;

                    var s = ModifierState.CreateFromModifier(modifier);
                    state.ModifierStates.Add(s);
                }

                return state;
            }
        }

        protected UndoStack<(List<MeshModifier>, bool), ExecuteUndoState> _executeUndoStack;

        protected void initExecuteUndoStack()
        {
            if(_executeUndoStack == null)
                _executeUndoStack = new UndoStack<(List<MeshModifier>, bool), ExecuteUndoState>(createExecuteUndoState, revertToExecuteUndoState);
        }

        protected void recordFirstUndoIfNecessary(List<MeshModifier> modifiers, bool materialChangeOnly)
        {
            initExecuteUndoStack();
            if (_executeUndoStack.IsEmpty())
            {
                _executeUndoStack.Record((modifiers, materialChangeOnly));
            }
        }

        protected ExecuteUndoState createExecuteUndoState((List<MeshModifier>, bool) input)
        {
            (List<MeshModifier> modifiers, bool materialChangeOnly) = input;
            return ExecuteUndoState.CreateFromModifiers(modifiers, materialChangeOnly);
        }

        protected void revertToExecuteUndoState(ExecuteUndoState state)
        {
            foreach (var modifier in _tmpAffectedModifiers)
            {
                // Usually it's either a MeshStatr OR a MaterialState never both.
                // But just in case let's set the mesh first so that potential material
                // changes won't be overridden.
                if (state.MeshState != null)
                {
                    var list = state.MeshState.ModifierStates;
                    foreach (var modifierState in list)
                    {
                        if (modifier.Component != modifierState.Component)
                            continue;

                        modifierState.ApplyTo(modifier);
                    }
                }
                if (state.MaterialState != null)
                {
                    var list = state.MaterialState.SubMeshMaterialStates;
                    foreach (var materialState in list)
                    {
                        if (modifier.Component != materialState.Component)
                            continue;

                        materialState.ApplyTo(modifier);
                    }
                }
            }

            TriangleCache.CacheTriangles();
            _selectionUndoStack.Clear();
            ClearSelection();
        }
        #endregion
    }
}
