﻿﻿

using UnityEngine;
using NaughtyAttributes;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace realvirtual
{
    //! Helper to import CAD Data with PIXYZ intorealvirtual. Needs to be used together with CADUpdater
    public class PixyzImporter : realvirtualBehavior
    {
        #if UNITY_EDITOR
        #if REALVIRTUAL_PIXYZ

        public Pixyz.Import.ImportSettings importSettings;

        public string filePath;

        public string ImportedName = "Import";

        public bool UdateImport = true;

        private bool CheckForUpdates = true;



        [Button("Select CAD file")]
        void SelectFile()
        {

            filePath = EditorUtility.OpenFilePanel("Select file to import", filePath, "*.*");

        }
        

        [Button("Import")]
        void Import()
        {
            CheckForUpdates = false;
            importcad();
        }
        
        void importcad()
        {
            var updatepathtrans = gameObject.transform.Find(ImportedName);
            if (updatepathtrans != null)
            {
                DestroyImmediate(updatepathtrans.gameObject);
            }
     
            EditorUtility.DisplayProgressBar("Reading CAD Data", "This may take a while" , 0f);
            
            var importer = new Pixyz.Import.Importer(filePath, importSettings);
    
            importer.isAsynchronous = false;
            importer.progressed += onProgressChanged;
        
            importer.completed += onImportEnded;

            importer.run();
        }


        void onProgressChanged(float progress, string message)
        {
            Debug.Log("Progress : " + 100f * progress + "%");
            EditorUtility.DisplayProgressBar("Reading CAD Data", "This may take a while" , progress);
        }


        void onImportEnded(GameObject imported)
        {
            var update = false;
            update = UdateImport;
            EditorUtility.ClearProgressBar();

            var child = imported.transform.GetChild(0);
            child.transform.parent = this.transform;
            child.transform.localPosition = new Vector3(0, 0, 0);
            child.transform.localRotation = Quaternion.Euler(new Vector3(0, 0, 0));
            child.name = ImportedName;
            

            var meta = child.GetComponent<Pixyz.Import.Metadata>();
            DestroyImmediate(meta);
            DestroyImmediate(imported);

            var cadupdater = GetComponent<CADUpdater>();

            if (cadupdater == null)
                return;
            if (!update)
            {
                cadupdater.CADCurrent = child.gameObject;
                cadupdater.SetMetadata();
            }

            if (update)
            {
                cadupdater.CADUpdate = child.gameObject;
            }
                
            // On Update
            if (update)
            {
                cadupdater.SetMetadata();
                if (update)
                {
                    cadupdater.Silentmode = false;
                    cadupdater.CheckStatus();
                    cadupdater.Silentmode = true;
                
                }
             
            }
        }
#endif
#endif
    } 


}