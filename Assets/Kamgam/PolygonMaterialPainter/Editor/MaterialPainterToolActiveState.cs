using System;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

namespace Kamgam.PolygonMaterialPainter
{
    [InitializeOnLoad]
    public static class MaterialPainterToolActiveState
    {
        static bool _initialized = false;
        public static bool IsActive = false;

        static MaterialPainterToolActiveState()
        {
            init();
        }

        static void init()
        {
            _initialized = true;

            bool active = false;

            // EditorTools available until Unity 2020.1 (2020.2+ does not longer have this class)
#if UNITY_2020_2_OR_NEWER
            ToolManager.activeToolChanged -= onToolChanged;
            ToolManager.activeToolChanged += onToolChanged;

            // active right from the start?
            if (ToolManager.activeToolType == typeof(MaterialPainterTool))
            {
                // Remember: MaterialPainterTool.Instance is still null here! 
                active = true;
                waitForInstance(active);
            }
#else
            EditorTools.activeToolChanged -= onToolChanged;
            EditorTools.activeToolChanged += onToolChanged;

            // active right from the start?
            if (EditorTools.activeToolType == typeof(MaterialPainterTool))
            {
                // Remember: MaterialPainterTool.Instance is still null here! 
                active = true;
                waitForInstance(active);
            }
#endif
        }

        static async void waitForInstance(bool active) 
        {
            float totalWaitTime = 0f; // precaution against endlessly running task
            while (MaterialPainterTool.Instance == null && totalWaitTime < 3000)
            {
                await System.Threading.Tasks.Task.Delay(50);
                totalWaitTime += 50;
            }

            if (totalWaitTime >= 3000)
                return;

            setActive(active);
            MaterialPainterTool.Instance.OnToolChanged();
        }

        static void onToolChanged()
        {
#if UNITY_2020_2_OR_NEWER
            setActive(ToolManager.activeToolType == typeof(MaterialPainterTool));
#else
            setActive(EditorTools.activeToolType == typeof(MaterialPainterTool));
#endif

            if (MaterialPainterTool.Instance != null)
                MaterialPainterTool.Instance.OnToolChanged();
        }

        static void setActive(bool active)
        {
            // We do this to ensure the active state does not change between layout and repaint events!
            EditorApplication.delayCall += () =>
            {
                IsActive = active;
            };
        }

        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            if(!_initialized)
                init();
        }
    }
}
