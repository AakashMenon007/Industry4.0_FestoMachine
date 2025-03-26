#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Kamgam.PolygonMaterialPainter
{
    public abstract class ExcludeFromBuildProcessor<T> : IProcessSceneWithReport where T : Component
    {
        public int callbackOrder => int.MinValue + 2;

        public void OnProcessScene(UnityEngine.SceneManagement.Scene scene, BuildReport report)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            var roots = scene.GetRootGameObjects();
            foreach (var root in roots)
            {
                var comps = root.GetComponentsInChildren<T>(includeInactive: true);
                foreach (var comp in comps)
                {
                    OnDestroy(comp);
                    GameObject.DestroyImmediate(comp);
                }
            }
        }

        public virtual void OnDestroy(T comp) {}
    }
}
#endif