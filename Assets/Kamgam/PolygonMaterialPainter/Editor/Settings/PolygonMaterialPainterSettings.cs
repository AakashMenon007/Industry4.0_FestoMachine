#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Kamgam.PolygonMaterialPainter
{
    // Create a new type of Settings Asset.
    public class PolygonMaterialPainterSettings : ScriptableObject
    {
        public const string Version = "1.4.0";
        public const string SettingsFilePath = "Assets/PolygonMaterialPainterSettings.asset";

        [SerializeField, Tooltip(_logLevelTooltip)]
        public Logger.LogLevel LogLevel;
        public const string _logLevelTooltip = "Any log above this log level will not be shown. To turn off all logs choose 'NoLogs'";

        [SerializeField, Tooltip(_scrollWheelSensitivityTooltip)]
        public float ScrollWheelSensitivity;
        public const string _scrollWheelSensitivityTooltip = "The sensitivity of the scroll wheel (determines how fast it will change the brush size).";

        [SerializeField, Tooltip(_disableOnHierarchyChangeTooltip)]
        public bool DisableOnHierarchyChange;
        public const string _disableOnHierarchyChangeTooltip = "Disable the tool if the hierarchy changes (convenience). Disable if it annoys you.";

        [SerializeField, Tooltip(_triggerSelectLinkedTooltip)]
        public KeyCode TriggerSelectLinked;
        public const string _triggerSelectLinkedTooltip = "Pressing this key while in 'Select Polygons' mode triggers the 'Select Linked' action.";

        [Range(0,1)]
        public float SelectionColorAlpha;

        [RuntimeInitializeOnLoadMethod]
        static void bindLoggerLevelToSetting()
        {
            // Notice: This does not yet create a setting instance!
            Logger.OnGetLogLevel = () => GetOrCreateSettings().LogLevel;
        }

        [InitializeOnLoadMethod]
        static void autoCreateSettings()
        {
            GetOrCreateSettings();
        }

        static PolygonMaterialPainterSettings cachedSettings;

        public static PolygonMaterialPainterSettings GetOrCreateSettings()
        {
            if (cachedSettings == null)
            {
                string typeName = typeof(PolygonMaterialPainterSettings).Name;

                cachedSettings = AssetDatabase.LoadAssetAtPath<PolygonMaterialPainterSettings>(SettingsFilePath);

                // Still not found? Then search for it.
                if (cachedSettings == null)
                {
                    string[] results = AssetDatabase.FindAssets("t:" + typeName);
                    if (results.Length > 0)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(results[0]);
                        cachedSettings = AssetDatabase.LoadAssetAtPath<PolygonMaterialPainterSettings>(path);
                    }
                }

                if (cachedSettings != null)
                {
                    SessionState.EraseBool(typeName + "WaitingForReload");
                }

                // Still not found? Then create settings.
                if (cachedSettings == null)
                {
                    CompilationPipeline.compilationStarted -= onCompilationStarted;
                    CompilationPipeline.compilationStarted += onCompilationStarted;

                    // Are the settings waiting for a recompile to finish? If yes then return null;
                    // This is important if an external script tries to access the settings before they
                    // are deserialized after a re-compile.
                    bool isWaitingForReloadAfterCompilation = SessionState.GetBool(typeName + "WaitingForReload", false);
                    if (isWaitingForReloadAfterCompilation)
                    {
                        Debug.LogWarning(typeName + " is waiting for assembly reload.");
                        return null;
                    }

                    cachedSettings = ScriptableObject.CreateInstance<PolygonMaterialPainterSettings>();
                    cachedSettings.LogLevel = Logger.LogLevel.Warning;
                    cachedSettings.ScrollWheelSensitivity = 1f;
                    cachedSettings.DisableOnHierarchyChange = false;
                    cachedSettings.TriggerSelectLinked = KeyCode.S;
                    cachedSettings.SelectionColorAlpha = 0.5f;

                    AssetDatabase.CreateAsset(cachedSettings, SettingsFilePath);
                    AssetDatabase.SaveAssets();

                    onSettingsCreated();

                    Logger.OnGetLogLevel = () => cachedSettings.LogLevel;
                }
            }

            return cachedSettings;
        }

        private static void onCompilationStarted(object obj)
        {
            string typeName = typeof(PolygonMaterialPainterSettings).Name;
            SessionState.SetBool(typeName + "WaitingForReload", true);
        }

        // We use this callback instead of CompilationPipeline.compilationFinished because
        // compilationFinished runs before the assemply has been reloaded but DidReloadScripts
        // runs after. And only after we can access the Settings asset.
        [UnityEditor.Callbacks.DidReloadScripts(999000)]
        public static void DidReloadScripts()
        {
            string typeName = typeof(PolygonMaterialPainterSettings).Name;
            SessionState.EraseBool(typeName + "WaitingForReload");
        }

        static void onSettingsCreated()
        {
            bool openManual = EditorUtility.DisplayDialog(
                    "Polygon Material Painter",
                    "Thank you for choosing Polygon Material Painter.\n\n" +
                    "You'll find the tool under Tools > Polygon Material Painter > Start\n\n" +
                    "Please start by reading the manual.\n\n" +
                    "It would be great if you could find the time to leave a review.",
                    "Open manual", "Cancel"
                    );

            if (openManual)
            {
                OpenManual();
            }
        }

        [MenuItem("Tools/Polygon Material Painter/Manual", priority = 101)]
        public static void OpenManual()
        {
            Application.OpenURL("https://kamgam.com/unity/PolygonMaterialPainterManual.pdf");
        }

        internal static SerializedObject GetSerializedSettings()
        {
            return new SerializedObject(GetOrCreateSettings());
        }

        [MenuItem("Tools/Polygon Material Painter/Settings", priority = 100)]
        public static void OpenSettings()
        {
            var settings = PolygonMaterialPainterSettings.GetOrCreateSettings();
            if (settings != null)
            {
                Selection.activeObject = settings;
                EditorGUIUtility.PingObject(settings);
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Polygon Material Painter Settings could not be found or created.", "Ok");
            }
        }

        [MenuItem("Tools/Polygon Material Painter/Please leave a review :-)", priority = 410)]
        public static void LeaveReview()
        {
            Application.OpenURL("https://assetstore.unity.com/packages/slug/246853?aid=1100lqC54&pubref=asset");
        }

        [MenuItem("Tools/Polygon Material Painter/More Asset by KAMGAM", priority = 420)]
        public static void MoreAssets()
        {
            Application.OpenURL("https://assetstore.unity.com/publishers/37829?aid=1100lqC54&pubref=asset");
        }

        [MenuItem("Tools/Polygon Material Painter/Version: " + Version, priority = 422)]
        public static void LogVersion()
        {
            Debug.Log("Polygon Material Painter Version: " + Version);
        }

        public void Save()
        {
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(this);
        }

    }

#if UNITY_EDITOR
    [CustomEditor(typeof(PolygonMaterialPainterSettings))]
    public class PolygonMaterialPainterSettingsEditor : Editor
    {
        public PolygonMaterialPainterSettings settings;

        public void OnEnable()
        {
            settings = target as PolygonMaterialPainterSettings;
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("Version: " + PolygonMaterialPainterSettings.Version);
            base.OnInspectorGUI();
        }
    }
#endif

    static class PolygonMaterialPainterSettingsProvider
    {
        [SettingsProvider]
        public static UnityEditor.SettingsProvider CreatePolygonMaterialPainterSettingsProvider()
        {
            var provider = new UnityEditor.SettingsProvider("Project/Polygon Material Painter", SettingsScope.Project)
            {
                label = "Polygon Material Painter",
                guiHandler = (searchContext) =>
                {
                    var settings = PolygonMaterialPainterSettings.GetSerializedSettings();

                    var style = new GUIStyle(GUI.skin.label);
                    style.wordWrap = true;

                    EditorGUILayout.LabelField("Version: " + PolygonMaterialPainterSettings.Version);
                    if (drawButton(" Open Manual ", icon: "_Help"))
                    {
                        PolygonMaterialPainterSettings.OpenManual();
                    }

                    drawField("LogLevel", "Log Level", PolygonMaterialPainterSettings._logLevelTooltip, settings, style);
                    drawField("ScrollWheelSensitivity", "Scroll Wheel Sensitivity", PolygonMaterialPainterSettings._scrollWheelSensitivityTooltip, settings, style);
                    drawField("DisableOnHierarchyChange", "Disable On Hierarchy Change", PolygonMaterialPainterSettings._disableOnHierarchyChangeTooltip, settings, style);
                    drawField("TriggerSelectLinked", "Trigger Select Linked", PolygonMaterialPainterSettings._triggerSelectLinkedTooltip, settings, style);
                    drawField("SelectionColorAlpha", "Selection Color Alpha", null, settings, style);

                    settings.ApplyModifiedProperties();
                },

                // Populate the search keywords to enable smart search filtering and label highlighting.
                keywords = new System.Collections.Generic.HashSet<string>(new[] { "shader", "triplanar", "rendering" })
            };

            return provider;
        }

        static void drawField(string propertyName, string label, string tooltip, SerializedObject settings, GUIStyle style)
        {
            EditorGUILayout.PropertyField(settings.FindProperty(propertyName), new GUIContent(label));
            if (!string.IsNullOrEmpty(tooltip))
            {
                GUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.Label(tooltip, style);
                GUILayout.EndVertical();
            }
            GUILayout.Space(10);
        }

        static bool drawButton(string text, string tooltip = null, string icon = null, params GUILayoutOption[] options)
        {
            GUIContent content;

            // icon
            if (!string.IsNullOrEmpty(icon))
                content = EditorGUIUtility.IconContent(icon);
            else
                content = new GUIContent();

            // text
            content.text = text;

            // tooltip
            if (!string.IsNullOrEmpty(tooltip))
                content.tooltip = tooltip;

            return GUILayout.Button(content, options);
        }
    }
}
#endif