using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using NaughtyAttributes;
using UnityEngine.SceneManagement;


namespace realvirtual
{
    //! Updates old CAD data against new ones. Both (old and new) need to be included in one scene. 
    public class CADUpdater : MonoBehaviour
    {
#if UNITY_EDITOR
        public GameObject CADCurrent; //!< The current CAD data
        public GameObject CADUpdate; //!< The new CAD data

        public bool ChainIDs; //!< Chain IDs of all upper Parts to create an ID (only needed if no correct ID metadata is available)
        public bool ForceUniqueIDs;

        public bool UseJTVersionNumber; //!< Use JT Version Number (if correct JT Version number is exported)
        public bool CompareOnlyMeshLength = true; //!< Compare only the Lenght of the Mesh arrays not the content itself
        public bool UpdatePositions; //!< Update Positions if Position is moved
        public bool UpdateMeshes; //!< Update Meshes if Mesh is changed
        public bool DeleteParts; //!< Delete Parts which are not existing any more in the imported new CAD data
        public bool AddNewParts; //!< Add new Parts
        public List<CAD> ToDeleteWithScripts; //!< List with Objects which would be deleted and which are having Game4automation scripts on it
        [ReadOnly] public int numchanged; //!< Number of changed parts
        [ReadOnly] public int nummoved; //!< Number of moved parts
        [ReadOnly] public int numdeleted; //!< Number of deleted parts
        [ReadOnly] public int numadded; //!< Number of added parts
        [HideInInspector] public bool Silentmode = true;
        [HideInInspector] public List<CAD> Added;
        [HideInInspector] public List<CAD> Deleted;
        [HideInInspector] public List<CAD> Moved;
        [HideInInspector] public List<CAD> Unchanged;
        [HideInInspector] public List<CAD> Changed;
        private List<CAD> TopAdded;
        private List<CAD> TopExistingInAdded;
        private List<CAD> TopExistingInDeleted;
        private Hashtable ht;
        private Hashtable UpdatedIDs;
        private Hashtable CurrentIDs;
        private Dictionary<string, int>  Occurencies;
        [HideInInspector] public List<CAD> ToBechanged;
        

        public delegate void DelegateOnUpdateFinished();
        public DelegateOnUpdateFinished OnUpdateFinished;
        
        [Button("Set NX JT Preset")]
        public void SetNXJTPreset()
        {
            ChainIDs = false;
            UseJTVersionNumber = true;
            CompareOnlyMeshLength = true;
            UpdatePositions = true;
            UpdateMeshes = true;
            AddNewParts = true;
            DeleteParts = true;
        }

        [Button("Set CATIA JT Preset")]
        public void SetCATIAJTPreset()
        {
            ChainIDs = true;
            UseJTVersionNumber = false;
            CompareOnlyMeshLength = false;
            UpdatePositions = true;
            UpdateMeshes = true;
            AddNewParts = true;
            DeleteParts = true;
        }

        [Button("Set Metadata")]
        public void SetMetadata()
        {
            VisibleAll(true);
            if (CADCurrent != null)
            {
                SetMetadata(CADCurrent);
            }

            if (CADUpdate != null)
            {
                SetMetadata(CADUpdate);
            }
        }

        [Button("Compare Current with Update")]
        public void ButtonCheckStatus()
        {
            Silentmode = false;
            SetMetadata();
            CheckStatus();
        }

        public void CheckStatus()
        {
            VisibleAll(true);
            Added = new List<CAD>();
            Deleted = new List<CAD>();
            Moved = new List<CAD>();
            Changed = new List<CAD>();
            ToBechanged = new List<CAD>();
            Unchanged = new List<CAD>();
            TopAdded = new List<CAD>();
            TopExistingInAdded = new List<CAD>();
            TopExistingInDeleted = new List<CAD>();
            ToDeleteWithScripts = new List<CAD>();
            CurrentIDs = new Hashtable();

            var metadatascurrent = CADCurrent.GetComponentsInChildren<CAD>();
            foreach (var current in metadatascurrent)
            {
                var id = current.ID;

                if (!CurrentIDs.ContainsKey(current.ID))
                    CurrentIDs.Add(current.ID, current);
                else
                {
                    EditorUtility.DisplayDialog("Error",
                        "ID [" + current.ID + "] ist not unambiguously, can't check status", "OK");
                   
                    return;
                }
            }

            UpdatedIDs = new Hashtable();
            var metadatasupdated = CADUpdate.GetComponentsInChildren<CAD>();
            foreach (var updated in metadatasupdated)
            {
                UpdatedIDs.Add(updated.ID, updated);
            }

            var numparts = CurrentIDs.Count + UpdatedIDs.Count;
            ht = new Hashtable();
            var i = 0;
            numadded = 0;
            numchanged = 0;
            numdeleted = 0;
            nummoved = 0;

            foreach (DictionaryEntry de in CurrentIDs)
            {
                i++;
                CAD current = (CAD) de.Value;
                float progress = (float) i / (float) numparts;
                EditorUtility.DisplayProgressBar("Checking CAD Data", "Checked " + i + " of " + numparts + " parts",
                    progress);
                current.Status = CADStatus.Updated;

                var changed = false;
                var deleted = false;
                var moved = false;
                
                CAD updated = (CAD) UpdatedIDs[current.ID];

                if (!ReferenceEquals(updated, null))
                {
                    updated.Status = CADStatus.Updated;
                    ht.Add(current, updated);
                    current.RelatedUpdate = updated;
                    updated.RelatedCurrent = current;

                    if ((current.gameObject.transform.localPosition != updated.transform.localPosition) ||
                        (current.gameObject.transform.localRotation != updated.transform.localRotation))
                    {
                        current.Status = CADStatus.Moved;
                        updated.Status = CADStatus.Moved;
                        Moved.Add(current);
                        nummoved++;
                        moved = true;
                    }


                    // Compare Meshes
                    if (current.Version != updated.Version || !UseJTVersionNumber)
                        if (!ReferenceEquals(updated.IsMeshFor, null))
                        {
                            // Update Meshes for Mesh
                            var currmeshfilter = current.GetComponent<MeshFilter>();
                            bool verticesident = false;
                            bool normalsident = false;
                            bool trianglesident = false;
                            var updatemeshfilter = updated.GetComponent<MeshFilter>();
                            if (!CompareOnlyMeshLength)
                            {
                                verticesident =
                                    currmeshfilter.sharedMesh.vertices.SequenceEqual(updatemeshfilter.sharedMesh
                                        .vertices);
                                normalsident =
                                    currmeshfilter.sharedMesh.normals.SequenceEqual(updatemeshfilter.sharedMesh
                                        .normals);
                                trianglesident =
                                    currmeshfilter.sharedMesh.triangles.SequenceEqual(updatemeshfilter.sharedMesh
                                        .triangles);
                            }
                            else
                            {
                                verticesident = currmeshfilter.sharedMesh.vertices.Length ==
                                                updatemeshfilter.sharedMesh.vertices.Length;
                                normalsident = currmeshfilter.sharedMesh.normals.Length ==
                                               updatemeshfilter.sharedMesh.normals.Length;
                                trianglesident = currmeshfilter.sharedMesh.triangles.Length ==
                                                 updatemeshfilter.sharedMesh.triangles.Length;
                            }

                            if (!verticesident || !normalsident || !trianglesident)
                            {
                                current.Status = CADStatus.ToBeChanged;
                                ToBechanged.Add(current);
                                updated.Status = CADStatus.Changed;
                          
                                Changed.Add(updated);
                                changed = true;
                                if (!current.Keep)
                                    numchanged++;
                            }
                        }
                }

                if (ReferenceEquals(updated, null))
                {
                    current.Status = CADStatus.Deleted;
                    Deleted.Add(current);
                    if (!current.Keep)
                        numdeleted++;
                    deleted = true;
                }


                if (!ReferenceEquals(current.IsMeshFor, null) && !changed)
                {
                    current.Status = current.IsMeshFor.Status;
                }

                if (!changed && !deleted && !moved)
                {
                    Unchanged.Add(current);
                }
            }


            // Check components which are inside deleted and neeeds to be kept
            foreach (DictionaryEntry de in CurrentIDs)
            {
                CAD current = (CAD) de.Value;

                if (current.Status != CADStatus.Deleted)
                {
                    var upper = current.gameObject.transform.parent;
                    if (!ReferenceEquals(upper, null))
                    {
                        var uppercad = upper.GetComponent<CAD>();
                        if (uppercad != null)
                        {
                            if (uppercad.Status == CADStatus.Deleted)
                            {
                                TopExistingInDeleted.Add(uppercad);
                            }
                        }
                    }
                }
            }

            // Check for added components
            foreach (DictionaryEntry de in UpdatedIDs)
            {
                i++;
                CAD updated = (CAD) de.Value;

                float progress = (float) i / (float) numparts;
                EditorUtility.DisplayProgressBar("Checking CAD Data", "Checked " + i + " of parts " + numparts,
                    progress);


                if (!ht.ContainsValue(updated))
                {
                    Added.Add(updated);
                    updated.Status = CADStatus.Added;
                    if (!updated.Keep)
                        numadded++;
                }
            }


            // Check if component is top of Added
            foreach (var added in Added)
            {
                added.IsTopOfAdded = false;
                var upper = added.gameObject.transform.parent;
                if (!ReferenceEquals(upper, null))
                {
                    var uppercad = upper.GetComponent<CAD>();
                    if (uppercad != null)
                    {
                        if (uppercad.Status != CADStatus.Added)
                        {
                            added.IsTopOfAdded = true;
                            TopAdded.Add(added);
                            added.NewCurrentTop = uppercad.RelatedCurrent;
                        }
                    }
                    else
                    {
                        TopAdded.Add(added);
                        added.IsTopOfAdded = true;
                    }
                }
                else
                {
                    TopAdded.Add(added);
                    added.IsTopOfAdded = true;
                }
            }


            // Check for components which should remain but upper componen needs to be deleted
            foreach (DictionaryEntry de in UpdatedIDs)
            {
                CAD updated = (CAD) de.Value;

                if (updated.Status != CADStatus.Added)
                {
                    var upper = updated.gameObject.transform.parent;
                    if (!ReferenceEquals(upper, null))
                    {
                        var uppercad = upper.GetComponent<CAD>();
                        if (uppercad != null)
                        {
                            if (uppercad.Status == CADStatus.Added)
                            {
                                TopExistingInAdded.Add(updated.RelatedCurrent);
                                updated.RelatedCurrent.NewCurrentTop = uppercad;
                            }
                        }
                    }
                }
            }

            // Check for components which should be deleted, if Scripts are on it
            foreach (var todelete in Deleted)
            {
                var components = todelete.GetComponents(typeof(realvirtualBehavior));
                foreach (var component in components)
                {
                    if (component.GetType() != typeof(CAD) && component.GetType() != typeof(Group))
                    {
                        if (!todelete.Keep)
                           if (!ToDeleteWithScripts.Contains(todelete))
                                ToDeleteWithScripts.Add(todelete);
                    }
                }
            }


            EditorUtility.ClearProgressBar();
            if (!Silentmode)
            {
                EditorUtility.DisplayDialog("Update check finished",
                    numchanged + " geometrical changed parts, " + nummoved + " moved parts, " + numadded +
                    " added parts " + numdeleted + " deleted parts. Please check and press Update if you wish to update your old 3D data!", "OK",
                    "");

                if (ToDeleteWithScripts.Count > 0)
                {
                    EditorUtility.DisplayDialog("Warning",
                        ToDeleteWithScripts.Count +
                        " Objects with containing scripts will be deleted, see List ToDeleteWithScripts, please check",
                        "OK");
                }
            }
        }


        [Button("Isolate Current")]
        public void IsolateCurrent()
        {
            Global.SetVisible(CADCurrent, true);
            Global.SetVisible(CADUpdate, false);
        }

        [Button("Isolate Update")]
        public void IsolateUpdate()
        {
            Global.SetVisible(CADCurrent, false);
            Global.SetVisible(CADUpdate, true);
        }

        [Button("Isolate Added")]
        public void IsolateAdded()
        {
            Isolate(Added);
        }


        [Button("Isolate Deleted")]
        public void IsolateDeleted()
        {
            Isolate(Deleted);
        }

        [Button("Isolate Moved")]
        public void IsolateToBeCanged()
        {
            Isolate(Moved);
        }

        [Button("Isolate Changed")]
        public void IsolateChanges()
        {
            Isolate(Changed);
        }

        [Button("Isolate to be changed")]
        public void IsolateToBeChanged()
        {
            Isolate(ToBechanged);
        }

        [Button("Isolate Unchanged")]
        public void IsolateUnchanged()
        {
            Isolate(Unchanged);
            foreach (var tobechanged in ToBechanged)
            {
                Global.SetVisible(tobechanged.gameObject, false);
            }
        }

        [Button("Show all")]
        public void ShowAll()
        {
            VisibleAll(true);
        }

        [Button("Update")]
        public void UpdateCAD()
        {
            if ((EditorUtility.DisplayDialog("Are you sure?",
                     "This will update your current data to the imported CAD Design", "YES",
                     "CANCEL") == false))
                return;
            Silentmode = true;
            CheckStatus();
            var len = Moved.Count;
            var i = 0;
            var metadatas = CADCurrent.GetComponentsInChildren<CAD>();

            if (UpdateMeshes || UpdatePositions)
            {
                foreach (var current in ToBechanged)
                {
                    CAD updated = null;
                    updated = (CAD) ht[current];
                    if (UpdateMeshes)
                    {
                        // Update Meshes for Mesh
                        var destination = current.GetComponent<MeshFilter>();
                        var source = updated.GetComponent<MeshFilter>();

                        //destination.mesh = source.mesh;
                        CopyMesh(destination.sharedMesh, source.sharedMesh);
                    }
                }

                foreach (var current in Moved)
                {
                    if (!current.Keep)
                    {
                        i++;
                        float progress = (float) i / (float) len;
                        EditorUtility.DisplayProgressBar("Updating Moved And Changed", "Updated " + i + " of " + len,
                            progress);
                        CAD updated = null;
                        updated = (CAD) ht[current];
                        if (!ReferenceEquals(updated, null))
                        {
                            if (!ReferenceEquals(current.IsMeshFor, null))
                            {
                                if (UpdateMeshes)
                                {
                                    // Update Meshes for Mesh
                                    var destination = current.GetComponent<MeshFilter>();
                                    var source = updated.GetComponent<MeshFilter>();

                                    //destination.mesh = source.mesh;
                                    CopyMesh(destination.sharedMesh, source.sharedMesh);
                                }
                            }
                            else
                            {
                                if (UpdatePositions)
                                {
                                    current.transform.position = updated.transform.position;
                                    current.transform.rotation = updated.transform.rotation;
                                    current.transform.localScale = updated.transform.localScale;
                                }
                            }

                            // Update Metadata
                            current.Instance = updated.Instance;
                            current.Version = updated.Version;
                            current.ID = updated.ID;
                            current.Status = CADStatus.Updated;
                            current.ImportedDate = updated.ImportedDate;
#if REALVIRTUAL_PIXYZ

                            var updatemeta = updated.GetComponents<Pixyz.Import.Metadata>();
                            var currmeta = current.GetComponents<Pixyz.Import.Metadata>();
                            var a = 0;
                            for (int j = 0; j < updatemeta.Length; j++)
                            {
                                currmeta[j].setProperties(updatemeta[j].getPropertiesNative());

                                a++;
                            }
#endif
                        }
                    }
                }
            }

            if (DeleteParts)
            {
                len = Deleted.Count;
                i = 0;

                // first save parts which are need to be kept
                foreach (var indeleted in TopExistingInDeleted)
                {
                    if (!indeleted.Keep)
                       indeleted.gameObject.transform.parent = CADCurrent.transform;
                }

                foreach (var current in Deleted.ToArray())
                {
                    if (!current.Keep)
                    {
                        i++;
                        float progress = (float) i / (float) len;
                        EditorUtility.DisplayProgressBar("Deleting Deleted", "Updated " + i + " of " + len, progress);
                        if (current != null)
                            DestroyImmediate(current.gameObject);
                    }
                }
            }

            if (AddNewParts)
            {
                len = TopAdded.Count;
                i = 0;
                foreach (var added in TopAdded)
                {
                    if (!added.Keep)
                    {
                        float progress = (float) i / (float) len;
                        EditorUtility.DisplayProgressBar("Adding Added", "Updated " + i + " of " + len,
                            progress);

                        if (added.NewCurrentTop != null)
                        {
                            added.transform.parent = added.NewCurrentTop.transform;
                        }
                        else
                        {
                            added.transform.parent = CADCurrent.transform;
                        }
                    }
                }

                foreach (var existing in TopExistingInAdded)
                {
                    if (existing.NewCurrentTop != null)
                    {
                        existing.transform.parent = existing.NewCurrentTop.transform;
                    }
                    else
                    {
                        existing.transform.parent = CADCurrent.transform;
                    }
                }
            }


            EditorUtility.ClearProgressBar();
            DestroyImmediate(CADUpdate);
            ht.Clear();
            OnUpdateFinished.Invoke();
            
        }


        static void CopyMesh(Mesh destination, Mesh source)
        {
            //destination = (Mesh) Instantiate(source);
            destination.Clear();
            destination.vertices = source.vertices;
            destination.normals = source.normals;
            destination.uv = source.uv;
            destination.triangles = source.triangles;
            destination.tangents = source.tangents;
            destination.colors = source.colors;
            destination.colors32 = source.colors32;
        }

        public void DeleteAllMetadata(GameObject obj)
        {
            var metadatas = obj.GetComponentsInChildren<CAD>();
            foreach (var metadata in metadatas)
            {
                DestroyImmediate(metadata);
            }
        }

        public void SetChildMeta(GameObject parent)
        {
            foreach (Transform child in parent.transform)
            {
                var cad = child.gameObject.GetComponent<CAD>();
                if (ReferenceEquals(cad, null))
                    cad = child.gameObject.AddComponent<CAD>();
                cad.SetJTMetadata();
                cad.Status = CADStatus.Updated;
                if (ChainIDs)
                {
                    var parentcad = parent.GetComponent<CAD>();
                    if (!ReferenceEquals(parentcad, null))
                    {
                        cad.ID = parentcad.ID + "|" + cad.ID;
                    }
                }
                
                
                // Check if ID is unique
                if (ForceUniqueIDs)
                {
                    if (Occurencies.ContainsKey(cad.ID))
                    {
                        Occurencies[cad.ID] = Occurencies[cad.ID] + 1;
                        cad.ID = cad.ID + "-" + Occurencies[cad.ID];
                    }
                    else
                    {
                        Occurencies.Add(cad.ID, 0);
                    }
                }

                SetChildMeta(child.gameObject);
            }
        }

        public void SetMetadata(GameObject obj)
        {
            if (obj == null)
                return;
            Occurencies = new Dictionary<string, int>();
            SetChildMeta(obj);
            Occurencies.Clear();
        }

        private void VisibleAll(bool visible)
        {
            List<GameObject> rootObjects = new List<GameObject>();
            Scene scene = SceneManager.GetActiveScene();
            scene.GetRootGameObjects(rootObjects);
            foreach (var obj in rootObjects)
            {
                if (obj.GetComponents<realvirtualController>().Length == 0)
                {
                    var objs = Global.GatherObjects(obj);
                    foreach (var obje in objs)
                    {
                        var go = (GameObject) obje;
                        go.SetActive(visible);
                    }
                }
            }
        }

        private void Isolate(List<CAD> objs)
        {
            foreach (var unchanged in Unchanged.ToArray())
            {
                var go = unchanged.gameObject;
                go.SetActive(false);
            }


            VisibleAll(false);
            foreach (var obj in objs)
            {
                var go = obj.gameObject;
                Global.SetVisible(go, true);
                // set this object and everything above active
                do
                {
                    go.SetActive(true);
                    if (go.transform.parent != null)
                        go = go.transform.parent.gameObject;
                    else
                        go = null;
                } while (go != null);
            }
        }

        public CAD GetInstance(GameObject obj, string ID)
        {
            var metadatas = obj.GetComponentsInChildren<CAD>();
            foreach (var metadata in metadatas)
            {
                if (metadata.ID == ID)
                {
                    return metadata;
                }
            }

            return null;
        }
#endif
    }
}