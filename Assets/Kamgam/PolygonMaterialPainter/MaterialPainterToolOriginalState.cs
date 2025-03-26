using UnityEngine;

namespace Kamgam.PolygonMaterialPainter
{
#if UNITY_EDITOR
    internal class MaterialPainterToolOriginalStateProcessor : ExcludeFromBuildProcessor<MaterialPainterToolOriginalState> {}
#endif

    [AddComponentMenu("")] // Hide from component menu
    public class MaterialPainterToolOriginalState : MonoBehaviour
    {
        public static void Create(GameObject go)
        {
            var state = go.AddComponent<MaterialPainterToolOriginalState>();
            state.Init();
        }

        protected MeshRenderer _meshRenderer;
        public MeshRenderer MeshRenderer
        {
            get
            {
                if (_meshRenderer == null)
                {
                    _meshRenderer = this.GetComponent<MeshRenderer>();
                }
                return _meshRenderer;
            }
        }

        protected MeshFilter _meshFilter;
        public MeshFilter MeshFilter
        {
            get
            {
                if (_meshFilter == null)
                {
                    _meshFilter = this.GetComponent<MeshFilter>();
                }
                return _meshFilter;
            }
        }

        protected SkinnedMeshRenderer _skinnedMeshRenderer;
        public SkinnedMeshRenderer SkinnedMeshRenderer
        {
            get
            {
                if (_skinnedMeshRenderer == null)
                {
                    _skinnedMeshRenderer = this.GetComponent<SkinnedMeshRenderer>();
                }
                return _skinnedMeshRenderer;
            }
        }

        public Mesh Mesh;
        public Material[] SharedMaterials;

        public void Init()
        {
            hideFlags = HideFlags.NotEditable;

            Mesh sharedMesh = null;
            if (MeshRenderer && MeshFilter)
            {
                sharedMesh = MeshFilter.sharedMesh;
                SharedMaterials = MeshRenderer.GetSharedMaterialsCopy();
            }
            else if (SkinnedMeshRenderer)
            {
                sharedMesh = SkinnedMeshRenderer.sharedMesh;
                SharedMaterials = SkinnedMeshRenderer.GetSharedMaterialsCopy();
            }

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this.gameObject);

            if (sharedMesh != null)
            {
                var path = UnityEditor.AssetDatabase.GetAssetPath(sharedMesh);
                bool meshIsThirdPartyAsset = !string.IsNullOrEmpty(path) && !sharedMesh.name.EndsWith(PolygonMaterialPainterConfig.FILE_ENDING);

                if (meshIsThirdPartyAsset)
                {
                    Mesh = sharedMesh;
                }
                else
                {
                    Mesh = new Mesh();
                    MeshUtils.CopyMesh(sharedMesh, Mesh);
                }
            }
#endif
        }

        public void ResetToOriginal()
        {
            if (MeshRenderer && MeshFilter)
            {
                MeshFilter.sharedMesh = Mesh;
                MeshRenderer.SetSharedMaterialsAsCopy(SharedMaterials);
            }
            else if (SkinnedMeshRenderer)
            {
                SkinnedMeshRenderer.sharedMesh = Mesh;
                SkinnedMeshRenderer.SetSharedMaterialsAsCopy(SharedMaterials);
            }
        }
    }

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(MaterialPainterToolOriginalState))]
    public class MaterialPainterToolOriginalStateEditor : UnityEditor.Editor
    {
        MaterialPainterToolOriginalState obj;

        public void OnEnable()
        {
            obj = target as MaterialPainterToolOriginalState;
        }

        public override void OnInspectorGUI()
        {
            UnityEditor.EditorGUILayout.HelpBox("This component is temporary.\nIt will be removed in builds.\nIt stores the original state of your model you can easily restore it.", UnityEditor.MessageType.Info);

            base.OnInspectorGUI();

            GUI.enabled = true;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Reset to Original")))
            {
                obj.ResetToOriginal();
            }
            if (GUILayout.Button(new GUIContent("Delete","Deleting the original state componnt will force the remembered original state to the current state.\n\n" +
                "I.e. the old state will be erased and the model's current state will become the new original state.")))
            {
                GameObject.DestroyImmediate(obj);
            }
            GUILayout.EndHorizontal();
            GUI.enabled = false;
        }
    }
#endif
}
