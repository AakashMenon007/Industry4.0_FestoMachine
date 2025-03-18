// realvirtual.io (R) Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 realvirtual GmbH - Usage of this source code only allowed based on License conditions see https://realvirtual.io/unternehmen/lizenz

using System;
using System.IO;
using UnityEngine;
using NaughtyAttributes;
using ThreeMf;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.Rendering;

#if (UNITY_EDITOR && !UNITY_EDITOR_OSX && !UNITY_EDITOR_LINUX)
using Plugins.Importer.Scripts;
#endif

#pragma warning disable 0162
namespace realvirtual
{
    [ExecuteInEditMode]
    //! Interface for importing CAD Data (Step and 3MF)
    public class CADLink : MonoBehaviour
    {
        #region PublicVariables

        [BoxGroup("Import CAD")] public string File;
 
        [HideInInspector] public bool UpdateImport = false;

#if (UNITY_EDITOR && !UNITY_EDITOR_OSX && !UNITY_EDITOR_LINUX)
        [Header("Options")] [ShowIf("IsCad")] 
        public EQualitySetting Quality = EQualitySetting.Low;
#endif
        [ShowIf("Is3MF")] public float ImportScaleFactor = 0.001f;
        [ShowIf("IsCad")] public bool ZIsUpVector = true;
        [ShowIf("IsCad")] public bool AutomaticallyAddColliders = false;

        [Header("Import Options")]
        [ShowIf("IsCad")]
        [Tooltip("Moves the mesh in Triangle set to the upper component - simplifies the imported structure")]
        public bool MoveMeshToNamedComponent = true;
        [ShowIf("IsCad")] public Shader Shader;
        [ShowIf("Is3MF")] public bool NameClonesIdentical;
        [ShowIf("Is3MF")] public bool ImportDistinctVertices = true;
        [ShowIf("Is3MF")] public bool UnityRecalculateNormals = false;
        [ShowIf("Is3MF")] public float VertexTolerance = 10000;
        [ShowIf("Is3MF")] public bool AdvancedRecalculateNormals = true;

        [ShowIf("Is3MF")] [EnableIf("AdvancedRecalculateNormals")]
        public float AdvancedRecalcluateNormalsAngle = 30;

        [ShowIf("Is3MF")] [InfoBox("UV Mapping takes long - just use if needed")]
        public bool CalculateUVS = false;

        [ShowIf("Is3MF")] [EnableIf("CalculateUVS")]
        public float UVScalfactor = 1.0f;

        public bool SetAndCreateMaterials = false;
        public Material StandardMaterialIfNotMapped;

        [EnableIf("SetAndCreateMaterials")] [ShowIf("Is3MF")]
        public bool OverwriteExistingMaterials = false;

        [EnableIf("SetAndCreateMaterials")] public string NewMaterialPath = "Assets/realvirtual/CADLink/Materials";

        [EnableIf("SetAndCreateMaterials")] public MaterialMapping MaterialMapping;

        #endregion

        #region Private Variables

        private bool SetPositions = true;
        private GameObject upper;
        private float Progress = 0;
        private bool cancel = false;
#if REALVIRTUAL_PROFESSIONAL
        private CADUpdater cadupdater;

        // CMC Step-Importer
#if (UNITY_EDITOR && !UNITY_EDITOR_OSX && !UNITY_EDITOR_LINUX)
        private OccImport occimport;
#endif
        private bool m_build;
        private bool m_building;
        private bool m_createAsset;
        private Material m_mat;
        private FileInfo m_fileInfo;
#endif
        private List<Material> newmaterials = new List<Material>();
        private List<ThreeMFPart> nosubcomponent = new List<ThreeMFPart>();

        #endregion

        #region Buttons

        [Button("Create New Material Mapping")]
        private void CreateMeaterialMapping()
        {
#if UNITY_EDITOR
            var mapping = CreateAsset<MaterialMapping>();

            MaterialMapping = (MaterialMapping) mapping;
#endif
        }

        [Button("Update Materials")]
        private void UpdateMaterials()
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                SetMaterials(transform.GetChild(i).gameObject);
            }
        }

        [Button("Select CAD Import File")]
        void SelectFile()
        {
            try
            {
#if UNITY_EDITOR

                File = EditorUtility.OpenFilePanel("Select file to import", File, "3mf,stp,STEP");
#endif
            }
            catch (Exception e)
            {
                var error = e;
            }
        }

        [Button("Import CAD File")]
        public void ImportCad()
        {
#if REALVIRTUAL_PROFESSIONAL && UNITY_EDITOR
            UpdateImport = this.transform.childCount > 0;

            if (UpdateImport)
            {
#if UNITY_EDITOR
                if (EditorUtility.DisplayDialog("Update or new Import",
                        "Do you want to proceed with an update or a new import?", "Update", "New Import"))
                {
                    UpdateImport = true;
                }
                else
                {
                    UpdateImport = false;
                    DestroyImmediate(GetComponent<CADUpdater>());
                    DestroyImmediate(this.transform.GetChild(0).gameObject);
                }
#endif
            }
            
            if (UpdateImport)
            {
                // Check if Update folder available
                var updateTransform = transform.Find("Update");
                if (updateTransform != null)
                {
                    DestroyImmediate(updateTransform.gameObject);
                }

                // Update component availale
                CADUpdater cadupdater = GetComponent<CADUpdater>();
                if (cadupdater == null)
                {
                    cadupdater = gameObject.AddComponent<CADUpdater>();
                }

                cadupdater.CADCurrent = this.transform.GetChild(0).gameObject;
                cadupdater.ChainIDs = true;
                cadupdater.ForceUniqueIDs = true;
                cadupdater.CompareOnlyMeshLength = false;
                cadupdater.UpdateMeshes = true;
                cadupdater.UpdatePositions = true;
                cadupdater.DeleteParts = true;
                cadupdater.AddNewParts = true;
                cadupdater.OnUpdateFinished += OnUpdateFinished;
            }
#endif
#if !REALVIRTUAL_PROFESSIONAL
            if (this.transform.childCount > 0)
            {
#if UNITY_EDITOR
                if  (EditorUtility.DisplayDialog("Are you sure?",
                    "This seesm to be a reimport - this will delete all CADLink child object, are you sure to proceed?", "Yes", "No"))
#endif
                         DestroyImmediate(this.transform.GetChild(0).gameObject);
#if UNITY_EDITOR
                else
                    return;
#endif
            }

#endif

            cancel = false;
            if (!System.IO.File.Exists(File))
            {
#if UNITY_EDITOR
                EditorUtility.DisplayDialog("ERROR", "File at defined path [" + File + "] not existing.", "OK", "");
                return;
#endif
                Debug.LogError("File at defined path [" + File + "] not existing.");
            }

            if (!Directory.Exists(NewMaterialPath) && SetAndCreateMaterials && Is3MF())
            {
#if UNITY_EDITOR
                EditorUtility.DisplayDialog("ERROR", "New material path [" + NewMaterialPath + "] not existing.", "OK",
                    "");
                return;
#endif
                Debug.LogError("3MF File at defined path [" + File + "] not existing.");
            }

            bool success = false;
            GameObject destination = this.gameObject;

            if (UpdateImport)
            {
                destination = new GameObject("Update");
                destination.transform.parent = this.transform;
            }

            // check if non 3mf Import
            if (IsCad())
            {
#if UNITY_EDITOR
                var resultgo = StepImport(destination);
                CompleteCADData(resultgo);
#endif
            }
            else
            {
                success = Import3MF();
            }

#if UNITY_EDITOR
            if (UpdateImport && success)
            {
#if REALVIRTUAL_PROFESSIONAL
                // Check if Update folder available
                cadupdater = GetComponent<CADUpdater>();
                var updateTransform = this.transform.Find("Update");
                cadupdater.CADUpdate = upper;
                updateTransform.parent = this.transform;
                updateTransform.localPosition = Vector3.zero;
                updateTransform.localRotation = Quaternion.identity;
                upper.transform.parent = updateTransform;
                cadupdater.SetMetadata();
                cadupdater.Silentmode = false;
                cadupdater.CheckStatus();
#endif
            }
#endif
            if (SetAndCreateMaterials && success)
                SetMaterials(upper);

#if UNITY_EDITOR
            EditorUtility.ClearProgressBar();
#endif
        }

        #endregion

        #region Private Methods

        private void SetMaterials(GameObject upper)
        {
            newmaterials.Clear();
            // Go through all CAD parts with meshes and check if material is assigned
            // if not assign next material of upper levels
            if (Is3MF())
            {
                ThreeMFPart[] cadparts = upper.GetComponentsInChildren<ThreeMFPart>(this);
                foreach (ThreeMFPart cadpart in cadparts)
                {
                    MeshFilter meshfilter = cadpart.GetComponent<MeshFilter>();
                    Renderer meshrenderer = cadpart.GetComponent<Renderer>();

                    if (meshrenderer != null)
                    {
                        // Part with Mesh has material assigned
                        if (cadpart.Color != "" || cadpart.Material != "")
                        {
                            meshrenderer.sharedMaterial = Get3mfMaterial(cadpart.Color, cadpart.Material);
                        }
                        else
                            // if not find next CADpart upwards with Material
                        {
                            Component[] uppercadparts = cadpart.GetComponentsInParent(typeof(ThreeMFPart), true);
                            foreach (ThreeMFPart uppercadpart in uppercadparts)
                            {
                                if (uppercadpart.Color != "")
                                {
                                    meshrenderer.sharedMaterial =
                                        Get3mfMaterial(uppercadpart.Color, uppercadpart.Material);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            else
            {
#if UNITY_EDITOR
                // Go through all CD parts with meshes and check if material is an instantiated material, if not instantiate
                // new material if not already existing and assign it to the mesh - copy the color to the new instantiated material
                MeshRenderer[] meshrenderers = upper.GetComponentsInChildren<MeshRenderer>(this);
                foreach (var meshrenderer in meshrenderers)
                {
                    // check if material at renderer is available as asset
                    var path = AssetDatabase.GetAssetPath(meshrenderer.sharedMaterial);
                    if (string.IsNullOrEmpty(path))
                    {
                        Material material = null;
                        // Check if mateterial color is mapped
                        material = GetStepMaterial(meshrenderer.sharedMaterial.color);
                        if (meshrenderer.sharedMaterial != null)
                        {
                            // if not create a new material or map if color has been already created
                        if (material == null)
                        {
                            // get color of material
                            var color = meshrenderer.sharedMaterial.color;
                            // get hexcode of color
                            var hex = ColorUtility.ToHtmlStringRGB(color);
                            // check if material with hexnane is in materials asset folder
                            var name = "Col#" + hex;
                            material = AssetDatabase.LoadAssetAtPath<Material>(NewMaterialPath + "/" + name + ".mat");
                            // if no material is found create a new one
                            if (material == null)
                            {
                                material = new Material(Shader.Find("Standard"));
                                material.color = color;
                                AssetDatabase.CreateAsset(material, NewMaterialPath + "/" + name + ".mat");
                            }
                        }
                        // if material is found assign it to the renderer
                        meshrenderer.sharedMaterial = material;
                        }
                    }
                }
#endif
            }
        }
        

        private void Log(string message)
        {
            Debug.Log("Game4Automation CAD Import: " + message);
#if UNITY_EDITOR
            cancel = EditorUtility.DisplayCancelableProgressBar(
                "CAD Livelink - Import",
                message,
                Progress);
#endif
        }

        private bool IsCad()
        {
            if (Path.GetExtension(File) != null)
            {
                var ext = Path.GetExtension(File).ToUpper();
                return (ext == ".STP" || ext == ".STEP" || ext == ".JT" || ext == ".SLDASM" || ext == ".SLDPRT");
            }

            return true;
        }

        private bool Is3MF()
        {
            var ext = Path.GetExtension(File).ToUpper();
            return (ext == ".3MF");
        }

        private void CompleteCADData(GameObject resultgo)
        {
            GameObject aResultGameObject = resultgo.transform.GetChild(0).gameObject;
            ;
            // change transforms
            if (aResultGameObject != null)
            {
                GameObject go;

                if (MoveMeshToNamedComponent)
                {
                    var transforms = aResultGameObject.GetComponentsInChildren<Transform>();
                    foreach (var trans in transforms)
                    {
                        if (trans.name == "Triangle set")
                        {
                            trans.name = trans.parent.name;
                            trans.position = trans.parent.position;
                            trans.rotation = trans.parent.rotation;
                            trans.parent = trans.parent.parent;
                        }
                    }

                    for (int i = 0; i < transforms.Length; i++)
                    {
                        var trans = transforms[i];
                        if (trans.GetComponent<MeshFilter>() == null)
                            if (trans.childCount == 0)
                                DestroyImmediate(trans.gameObject);
                    }
                }

                var newRenderer = "";
                if (GraphicsSettings.currentRenderPipeline)
                {
                    if (GraphicsSettings.currentRenderPipeline.GetType().ToString().Contains("UniversalRenderPipeline"))
                    {
                        newRenderer = "Universal Render Pipeline/Simple Lit";
                    }

                    if (GraphicsSettings.currentRenderPipeline.GetType().ToString().Contains("HighDefinition"))
                    {
                        newRenderer = "HDRenderPipeline/Lit";
                    }
                }
                else
                {
                    // Standard RP
                }

                if (newRenderer != "")
                {
                    var newshader = Shader.Find(newRenderer);
                    if (newshader != null)
                    {
                        // Change of all Shaders after import is needed
                        var list = aResultGameObject.GetComponentsInChildren<MeshRenderer>();
                        foreach (var myobj in list)
                        {
                            var renderer = myobj;
                            if (renderer != null)
                            {
                                Material[] sharedMaterialsCopy = renderer.sharedMaterials;
                                for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                                {
                                    var color = sharedMaterialsCopy[i].color;
                                    sharedMaterialsCopy[i].shader = newshader;
                                    sharedMaterialsCopy[i].color = color;
                                }

                                renderer.sharedMaterials = sharedMaterialsCopy;
                            }
                        }
                    }
                }


                if (ZIsUpVector)
                {
                    aResultGameObject.transform.localRotation = Quaternion.Euler(-90, 0, 0);
                }
                else
                {
                    aResultGameObject.transform.localRotation = Quaternion.Euler(-0, 0, 0);
                }

                if (!AutomaticallyAddColliders)
                {
                    aResultGameObject.GetComponentsInChildren<MeshCollider>();
                    foreach (var meshCollider in aResultGameObject.GetComponentsInChildren<MeshCollider>())
                    {
                        DestroyImmediate(meshCollider);
                    }
                }

                aResultGameObject.transform.localPosition = new Vector3(0, 0, 0);
                go = aResultGameObject;

                upper = go;
#if UNITY_EDITOR
                if (UpdateImport)
                {
#if REALVIRTUAL_PROFESSIONAL
                    // Check if Update folder available
                    cadupdater = GetComponent<CADUpdater>();
                    var updateTransform = this.transform.Find("Update");
                    cadupdater.CADUpdate = upper;
                    updateTransform.parent = resultgo.transform;
                    updateTransform.localPosition = Vector3.zero;
                    updateTransform.localRotation = Quaternion.identity;
                    upper.transform.parent = updateTransform;
                    cadupdater.SetMetadata();
                    cadupdater.Silentmode = false;
                    cadupdater.CheckStatus();
#endif
                }
#endif
                if (SetAndCreateMaterials)
                    SetMaterials(upper);

#if UNITY_EDITOR
                EditorUtility.ClearProgressBar();
#endif
            }
        }

        private GameObject StepImport(GameObject root)
        {
#if (UNITY_EDITOR && !UNITY_EDITOR_LINUX && !UNITY_EDITOR_OSX)
            EditorUtility.ClearProgressBar();

            EditorUtility.DisplayProgressBar("Step Import",
                "Step 1 of 4 - Reading File. This might take some minutes. During import Unity is blocked.", 0.1f);

            var occimport = new OccImport();
            ColorSetting colorSetting = ColorSetting.AllColors;
            EQualitySetting qualitySetting = Quality;
            OccHierarchyMarshaller occHierarchyMarshaller = new OccHierarchyMarshaller(colorSetting, qualitySetting);
            IntPtr intPtr = occimport.ImportFile(File);

            EditorUtility.DisplayProgressBar("Step Import",
                "Step 2 of 4 - Creating Meshes. This might take some minutes. During import Unity is blocked.", 0.3f);
         
            if (Shader == null)
                Shader = Shader.Find("Standard");
            occHierarchyMarshaller.MarshallRootNode(intPtr, root, Shader);

            EditorUtility.DisplayProgressBar("Step Import",
                "Step 3 of 4 - Cleaning up. This might take some minutes. During import Unity is blocked.", 0.6f);

            NativeCoreImporter.DeleteNodeFromCacheNative(intPtr);

            EditorUtility.DisplayProgressBar("Step Import",
                "Step 4 of 4- Updating Materials. This might take some minutes. During import Unity is blocked.", 0.9f);


            EditorUtility.ClearProgressBar();
#endif
            return root;
        }

        #region 3MF

        private bool Import3MF()
        {
            var uppergo = this.gameObject;

            if (UpdateImport)
            {
                var go = transform.Find("Update");
                if (go != null)
                {
                    uppergo = go.gameObject;
                }
                else
                {
                    uppergo = new GameObject("Update");
                    uppergo.transform.parent = this.transform;
                    uppergo.transform.localPosition = Vector3.zero;
                    uppergo.transform.localRotation = Quaternion.identity;
                }
            }

            // 3mf Import
            Progress = 0.05f;
            var num = 0;
            nosubcomponent.Clear();
            Log("Load 3MF File");

            var test = ThreeMfFile.Load(File, 1);
            if (test == null)
                return false;
            Debug.Log("Import 3MF");
            // Import Positions and Meshes for all Ressources
            foreach (var model in test.Models)
            {
                foreach (var resource in model.Resources)
                {
                    num++;
                    Progress = (float) num / (float) model.Resources.Count;
                    if (cancel)
                    {
                        Debug.Log("Import Canceled by User");
#if UNITY_EDITOR
                        EditorUtility.ClearProgressBar();
#endif
                        return false;
                    }

                    if (resource.GetType() == typeof(ThreeMfObject))
                    {
                        var obj = (ThreeMfObject) resource;
                        Log("Import Part " + obj.Name + " " + obj.Id.ToString());

                        var part = CreateCadPart(uppergo, uppergo, obj.Name, obj.Id.ToString(), obj);
                        part.File = File;
                        part.LastUpdate = System.DateTime.Now.ToString();
                        // Mesh at Part
                        if (obj.Mesh != null)
                        {
                            CreateMesh(part, obj.Mesh);
                        }

                        // Get Porpertyindex

                        var propindex = obj.PropertyIndex;

                        // Properties at Part
                        if (obj.PropertyResource != null)
                        {
                            if (obj.PropertyResource.PropertyItems != null)
                            {
                                var currindex = 0;
                                foreach (var propertyItem in obj.PropertyResource.PropertyItems)
                                {
                                    if (currindex == propindex)
                                    {
                                        if (propertyItem.GetType() == typeof(ThreeMfColor))
                                        {
                                            var color = (ThreeMfColor) propertyItem;
                                            part.Color = color.Color.ToString();
                                            part.Material = "";
                                        }

                                        if (propertyItem.GetType() == typeof(ThreeMfBase))
                                        {
                                            var color = (ThreeMfBase) propertyItem;
                                            part.Color = color.Color.ToString();
                                            part.Material = color.Name;
                                        }
                                    }

                                    currindex = currindex + 1;
                                }
                            }
                        }

                        foreach (var component in obj.Components)
                        {
                            MakeAsSubComponent(uppergo, obj.Id.ToString(), component.Object.Id.ToString(), component);
                        }
                    }
                }

                // Set Positions for Build information
                foreach (var item in model.Items)
                {
                    SetBuildPosition(uppergo, item);
                }
            }

            if (nosubcomponent.Count > 0)
                upper = nosubcomponent[0].gameObject;
#if UNITY_EDITOR
            EditorUtility.ClearProgressBar();
#endif
            return true;
        }

        private void OnUpdateFinished()
        {
#if GAME4UTOMATIOREALVIRTUAL_PROFESSIONAL
            CADUpdater cadupdater = GetComponent<CADUpdater>();
            var updateTransform = transform.Find("Update");
            if (updateTransform != null)
            {
                DestroyImmediate(updateTransform.gameObject);
            }

            if (cadupdater != null)
            {
                DestroyImmediate(cadupdater);
            }
#endif
        }

        private Material GetStepMaterial(Color color)
        {
            if (MaterialMapping != null)
            {
                foreach (var mapping in MaterialMapping.Mappings)
                {
                    if (mapping.StepColor == color)
                        return mapping.Material;
                }
            }

            if (StandardMaterialIfNotMapped != null)
                return StandardMaterialIfNotMapped;

            return null;
        }

        private Material Get3mfMaterial(string colorname, string materialname)
        {
            Material newmaterial = null;
            //    colorname.Replace("#", "Hex");
            var tempMaterial = new Material(Shader.Find("Standard"));

            // check if material assignment in material mapping
            // 1st check if color and material combination are defined
            if (MaterialMapping != null)
            {
                foreach (var mapping in MaterialMapping.Mappings)
                {
                    if (mapping.ThreeMfColorname == colorname && mapping.ThreeMfMaterialname == materialname)
                        return mapping.Material;
                }
            }

            // 2nd check if material is defined 
            if (MaterialMapping != null)
            {
                foreach (var mapping in MaterialMapping.Mappings)
                {
                    if (mapping.ThreeMfMaterialname == materialname && mapping.ThreeMfMaterialname != "")
                        return mapping.Material;
                }
            }

            // 3rd check if color is defined 
            if (MaterialMapping != null)
            {
                foreach (var mapping in MaterialMapping.Mappings)
                {
                    if (mapping.ThreeMfColorname == colorname && mapping.ThreeMfColorname != "")
                        return mapping.Material;
                }
            }

            if (StandardMaterialIfNotMapped != null)
                return StandardMaterialIfNotMapped;

#if UNITY_EDITOR
            // create new material if not existing
            string[] assets = AssetDatabase.FindAssets(colorname + " t:material");

            bool createnewmaterial = true;

            if (assets.Length != 0 && OverwriteExistingMaterials == false)
            {
                createnewmaterial = false;
            }

            if (assets.Length != 0 && OverwriteExistingMaterials == true)
            {
                newmaterial = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(assets[0]));

                if (newmaterials.Contains(newmaterial))
                {
                    createnewmaterial = false;
                }
                else
                {
                    createnewmaterial = true;
                    AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(assets[0]));
                }
            }

            if (createnewmaterial)
            {
                var path = Path.Combine(NewMaterialPath, colorname + ".mat");
                AssetDatabase.CreateAsset(tempMaterial, path);
                newmaterial = AssetDatabase.LoadAssetAtPath<Material>(path);
                newmaterials.Add(newmaterial);
                var col = new Color();
                ColorUtility.TryParseHtmlString(colorname, out col);
                newmaterial.color = col;
            }
            else
            {
                newmaterial = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(assets[0]));
            }
#else
                newmaterial = new Material(Shader.Find("Standard"));   
                var col = new Color();
                ColorUtility.TryParseHtmlString(colorname, out col);
                newmaterial.color = col;
#endif


            return newmaterial;
        }

        private void CreateMesh(ThreeMFPart part, ThreeMfMesh mesh)
        {
            MeshFilter meshfilter = part.transform.GetComponent<MeshFilter>();
            if (meshfilter == null)
            {
                meshfilter = part.transform.gameObject.AddComponent<MeshFilter>();
            }

            MeshRenderer meshrenderer = part.transform.GetComponent<MeshRenderer>();
            if (meshrenderer == null)
            {
                meshrenderer = part.transform.gameObject.AddComponent<MeshRenderer>();
            }

            Mesh umesh = new Mesh();

            if (mesh.uTriangles.Count > 65535)
                umesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            umesh.name = part.gameObject.name;
            umesh.vertices = mesh.uVertices.ToArray();
            umesh.triangles = mesh.uTriangles.ToArray();
            if (ImportDistinctVertices)
                CADMeshTools.DistinctVertices(ref umesh);
            if (UnityRecalculateNormals)
                    umesh.RecalculateNormals();
            if (AdvancedRecalculateNormals)
                CADMeshTools.RecalculateNormals(umesh, AdvancedRecalcluateNormalsAngle);
            if (CalculateUVS)
                CADMeshTools.CalculateUVS(ref umesh, UVScalfactor);
            if (ImportScaleFactor != 1)
                CADMeshTools.ScaleMesh(ref umesh, ImportScaleFactor);

         
            umesh.RecalculateBounds();
            umesh.Optimize();
            meshfilter.mesh = umesh;
        }

        private ThreeMFPart CreateCadPart(GameObject toplevel, GameObject parent, string name, string Id,
            ThreeMfObject threemfobject)
        {
            var part = GetCADPart(toplevel, Id);

            if (part == null)
            {
                // Create new part
                var newobj = new GameObject(name);
                part = newobj.AddComponent<ThreeMFPart>();
                newobj.transform.parent = parent.transform;
                newobj.transform.localPosition = new Vector3(0, 0, 0);
                newobj.transform.localEulerAngles = new Vector3(0, 0, 0);
            }

            part.Id = Id;
            part.Name = name;
            part.LastUpdate = System.DateTime.Now.ToString();
            part.Set3MFfObject(threemfobject);
            nosubcomponent.Add(part);
            return part;
        }

        private void SetBuildPosition(GameObject toplevel, ThreeMfModelItem item)
        {
            var id = item.Object.Id.ToString();
            var comp = GetCADPart(toplevel, id);

            comp.ImportPos = item.Transform.GetPosition() * ImportScaleFactor;
            comp.ImportRotOriginal = item.Transform.Get3mfAngles();
            comp.ImportScale = item.Transform.GetScale();
            if (SetPositions)
            {
                comp.transform.localPosition = comp.ImportPos;
                comp.transform.localRotation = item.Transform.GetRotationQuaternion();
            }

            comp.ImportRot = comp.transform.localRotation.eulerAngles;
        }

        private void MakeAsSubComponent(GameObject toplevel, string idparent, string idchild,
            ThreeMfComponent component)
        {
            var parent = GetCADPart(toplevel, idparent);
            var child = GetCADPart(toplevel, idchild);
            nosubcomponent.Remove(child);
            ThreeMFPart comp;

            if (child.AssembledInto == null)
            {
                child.MakeAsSubCompponent(parent);
                comp = child;
            }
            else
            {
                // Is Allready assembled into another component - make copy
                var copy = child.CreateClone(NameClonesIdentical);
                copy.MakeAsSubCompponent(parent);
                comp = copy;
            }

            comp.ImportPos = component.Transform.GetPosition() * ImportScaleFactor;
            comp.ImportRotOriginal = component.Transform.Get3mfAngles();
            comp.ImportScale = component.Transform.GetScale();
            if (SetPositions)
            {
                comp.transform.localPosition = comp.ImportPos;
                comp.transform.localRotation = component.Transform.GetRotationQuaternion();
            }

            comp.ImportRot = comp.transform.localRotation.eulerAngles;
        }

        private ThreeMFPart GetCADPart(GameObject go, string id)
        {
            ThreeMFPart[] children = go.transform.GetComponentsInChildren<ThreeMFPart>();
            foreach (var child in children)
            {
                if (child.Id == id && child.IsClone == false)
                {
                    return child;
                }
            }

            return null;
        }

        #endregion

        private void Reset()
        {
            NewMaterialPath = "Assets/realvirtual/CADLink/Materials";
            #if UNITY_EDITOR
            Shader = Shader.Find("Standard");
            #endif
        }

        public static object CreateAsset<T>() where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
#if UNITY_EDITOR
            var find = AssetDatabase.FindAssets(
                "MaterialMapping t:ScriptableObject");

            string path = AssetDatabase.GUIDToAssetPath(find[0]);
            var theasset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (path == "")
            {
                path = "Assets";
            }
            else if (Path.GetExtension(path) != "")
            {
                path = path.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(theasset)), "");
            }

            string assetPathAndName =
                AssetDatabase.GenerateUniqueAssetPath(path + "/New " + typeof(T).ToString() + ".asset");

            AssetDatabase.CreateAsset(asset, assetPathAndName);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
#endif
            return asset;
        }

        #endregion
    }
}