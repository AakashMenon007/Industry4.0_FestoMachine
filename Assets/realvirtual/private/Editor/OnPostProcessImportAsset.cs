// realvirtual (R) Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 realvirtual GmbH - Usage of this source code only allowed based on License conditions see https://realvirtual.io/en/company/license

using UnityEditor;
using UnityEngine;
using System.IO;

#if !UNITY_CLOUD_BUILD
namespace realvirtual
{
    class MyAllPostprocessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            bool Game4AutomationImport = false;

            foreach (string str in importedAssets)
            {
                if (str.Contains("Assets/realvirtual/private/Editor"))
                {

                    Game4AutomationImport = true;
                }

            }

#if !DEV
            if (Game4AutomationImport)
            {
                Debug.Log("Updating realvirtual");
                // Disable Interact
                string MenuName = "realvirtual/Enable Interact (Pro)";
                EditorPrefs.SetBool(MenuName, false);
                realvirtualToolbar.SetStandardSettings(false);

                var window = ScriptableObject.CreateInstance<HelloWindow>();
                window.Open();
                
                // Delete old QuickToggle Location if existant
                if (Directory.Exists("Assets/realvirtual/private/Editor/QuickToggle"))
                {
                    Directory.Delete("Assets/realvirtual/private/Editor/QuickToggle",true);
                }
            }
#endif

        }
    }
}
#endif