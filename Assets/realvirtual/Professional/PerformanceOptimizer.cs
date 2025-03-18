using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using NaughtyAttributes;
using UnityEditor;

namespace realvirtual
{
    [HelpURL("https://doc.realvirtual.io/components-and-scripts/performance-optimizer")]
    //! The performance optimizer combines meshes into one mesh per kinematic group. This will improve performance of the simulation
    public class PerformanceOptimizer : MonoBehaviour
    {
        [ReadOnly] public bool IsOptimized = false; //! Is the gameobject already optimized
        [ReadOnly] [ShowIf("IsOptimized")] public int NumUnoptimizedMeshes; //! Number of unoptimized meshes     
        [ReadOnly] [ShowIf("IsOptimized")] public int NumOptimizedMeshes; //! Number of optimized meshes
        [ReadOnly] public  List<GameObject> OptimizedMeshes = new List<GameObject>(); //! List of all optimized meshes
        public bool IgnoreGroups = false;//! Ignore groups and combine everything into the combined mesh
        public bool IgnoreHiddenObjects = false; //! Ignore hidden objects and don't include them into the combined mesh
        [HideIf("IgnoreGroups")] public bool CombineNonKinematicGroups = false; //! Combine non kinematic groups into a separate mesh if true, if not they are part of the overall static mesh
        [HideIf("IgnoreGroups")] public List<string> IgnoredGroups = new List<string>(); //! List of groups which are not combined into the mesh and not considered - they remain active
       
       
        [HideInInspector] public List<MeshRenderer> DeactivatedRenderers = new List<MeshRenderer>();
       
        [HideInInspector] public List<string> DeactivatedOriginGroups = new List<string>();


        [Button("Optimize")]
        public void StartCombine()
        {
            #if UNITY_EDITOR
            // Display Warning Popup
            if (EditorUtility.DisplayDialog("Optimize Mehes",
                    "Optimizing the scene will deactivate all meshes under this component and combine them into one mesh per kinematic group. This will improve performance. The new meshes can be found under OptimizedMeshes Do you want to continue?",
                    "Yes", "No"))
            {
                if (IsOptimized)
                {
                    if (EditorUtility.DisplayDialog("Optimize Meshes",
                            "Meshes are already optimized - please first disaple this and then optimize again ", "Ok",
                            ""))
                        return;
                }
                else
                {
                    Optimize();
                }
            }
            #endif
        }

        // Extension:
        [Button("Undo Optimize")]
        public void StartUndoCmobine()
        {
            UndoOptimize();
            this.gameObject.SetActive(true);
        }
        public void Optimize()
        {
            NumOptimizedMeshes = 0;
            NumUnoptimizedMeshes = 0;
            
            DeactivatedOriginGroups.Clear();
            // Get everything what is "static" - evenn if not set
            // Get all Meshes which are not in a group and have not a drive on it
            var allmeshFilters = GetComponentsInChildren<MeshFilter>(true);
            var tocombine = new List<MeshFilter>();
            foreach (var meshFilter in allmeshFilters)
            {
                if (meshFilter.gameObject.GetComponentInParent<Drive>() == null &&
                    meshFilter.gameObject.GetComponentInParent<Group>() == null)
                {
                    tocombine.Add(meshFilter);
                }

                if (meshFilter.sharedMesh == null)
                {
                    Debug.LogError("Corrupt mesh detected "+meshFilter.transform.name+" in "+meshFilter.transform.parent.name+". Please check on the mesh. It was removed from the selextion.");
                    if (tocombine.Contains(meshFilter))
                    {
                        tocombine.Remove(meshFilter);
                    }
                }
                    
            }

            // First get all Groups included in the Hierarchy
            var groups = GetComponentsInChildren<Group>(true);

            // Now get all combination of groups in Hierarchy - for each combination one combined mesh is needed 
            // this will guarantee that group hiding and so on will still work as in detailled mode

            var allgroups = groups.Select(g => g.GetGroupName())
                .Distinct()
                .ToList();

            // Now get all groups attached to a Kinematic Script
            var kinematics = FindObjectsOfType<Kinematic>();
            var allkinematicgroups = kinematics.Select(k => k.GetGroupName())
                .Distinct()
                .ToList();

            // Now get all groups which are not attached to a Kinematic Script
            var allnonkinematicgroups = allgroups.Except(allkinematicgroups).ToList();


            // If not CombineNonKinematicGroups also groups which are not part of a kinematic needs to be added
            if (CombineNonKinematicGroups == false)
            {
                foreach (var group in allnonkinematicgroups)
                {
                    var allWithGroup = Global.GetAllWithGroupIncludingSub(group);
                    foreach (var go in allWithGroup)
                    {
                        if (go.GetComponent<MeshFilter>() != null)
                        {
                            DeactivatedOriginGroups.Add(group);
                            tocombine.Add(go.GetComponent<MeshFilter>());
                        }
                    }
                }
            }
            
            // Remove Hidden Objects if Ignore Hidden objects
            if (IgnoreHiddenObjects)
            {
                // Remove alll hidden gamobjects from tocombine
                var i = 0;
                while (i < tocombine.Count)
                {
                    if (tocombine[i].gameObject.activeInHierarchy == false)
                    {
                        tocombine.RemoveAt(i);
                    }
                    else
                    {
                        i++;
                    }
                }
            }
            DeactivatedRenderers.Clear();
            NumOptimizedMeshes++;
            NumUnoptimizedMeshes = NumUnoptimizedMeshes + tocombine.Count;
            var newgo = MeshCombine.CombineMeshes(this.transform,this.gameObject, this.name, true, tocombine.ToArray(), ref OptimizedMeshes,ref DeactivatedRenderers);
            newgo.isStatic = true;
            
            if (!IgnoreGroups)
            {
                // Now Process all groups
                foreach (var group in allgroups)
                {
                    if (IgnoredGroups.Contains(group))
                        continue;
                    tocombine = new List<MeshFilter>();
                    var allWithGroup = Global.GetAllWithGroupIncludingSub(group);
                    // add all MeshFilters off allWithGroup to a new list
                    foreach (var go in allWithGroup)
                    {
                        if (go.GetComponent<MeshFilter>() != null)
                        {
                            tocombine.Add(go.GetComponent<MeshFilter>());
                        }
                    }

                    // Remove Hidden Objects if Ignore Hidden objects
                    if (IgnoreHiddenObjects)
                    {
                        tocombine = tocombine.Where(mf => mf.gameObject.activeInHierarchy).ToList();
                    }

                    // Remove Hidden Objects if Ignore Hidden objects
                    if (IgnoreHiddenObjects)
                    {
                        // Remove alll hidden gamobjects from tocombine
                        var i = 0;
                        while (i < tocombine.Count)
                        {
                            if (tocombine[i].gameObject.activeInHierarchy == false)
                            {
                                tocombine.RemoveAt(i);
                            }
                            else
                            {
                                i++;
                            }
                        }
                    }


                    // In case of non kinematic groups
                    if (allkinematicgroups.Contains(group) == false)
                    {
                        if (CombineNonKinematicGroups)
                        {
                            NumOptimizedMeshes++;
                            NumUnoptimizedMeshes = NumUnoptimizedMeshes + tocombine.Count;
                            DeactivatedOriginGroups.Add(group);
                            // Create a new GameObject for the group
                            newgo = MeshCombine.CombineMeshes(this.transform,this.gameObject, group, true, tocombine.ToArray(),ref OptimizedMeshes,ref DeactivatedRenderers);
                            var newgroup = newgo.AddComponent<Group>();
                            newgroup.GroupName = group;
                            newgo.isStatic = true;
                        }
                    }
                    // and for kinematic groups
                    else
                    {
                        NumOptimizedMeshes++;
                        NumUnoptimizedMeshes = NumUnoptimizedMeshes + tocombine.Count;
                        DeactivatedOriginGroups.Add(group);
                        newgo =  MeshCombine.CombineMeshes(this.transform, this.gameObject, group, true, tocombine.ToArray(),ref OptimizedMeshes,ref DeactivatedRenderers);
                        // add to newgo Group with name of group
                        var newgroup = newgo.AddComponent<Group>();
                        newgroup.GroupName = group;
                        newgo.isStatic = false;
                    }
                }
            }

            ActivateGroups(false);
            this.IsOptimized = true;
        }

      

        private MeshFilter[] GetMeshFiltersToCombine()
        {
            // Get all MeshFilters belongs to this GameObject and its children:
            var allmeshFilters = GetComponentsInChildren<MeshFilter>(true);
            
            List<MeshFilter> MeshFiltersToUse = new List<MeshFilter>();
            foreach (var meshFilter in allmeshFilters)
            {
                bool skip = false;
                if (meshFilter.gameObject == this.gameObject)
                    skip = true;

                if (!skip)
                    MeshFiltersToUse.Add(meshFilter);
            }
            return MeshFiltersToUse.ToArray();
        }

        

        private void DeleteCombined(MeshFilter[] meshFilters, GameObject exclude)
        {
            foreach (var meshfilter in meshFilters)
            {
                // Check for empty upper gameobjects
                bool parentempty = true;
                List<GameObject> ParentsToDelete = new List<GameObject>();
                if (meshfilter != null)
                {
                    Transform currparent = meshfilter.gameObject.transform.parent;
                    while (parentempty && currparent != null)
                    {
                        if (currparent.GetComponent<MeshFilter>() == null && currparent.transform.childCount == 1)
                        {
                            ParentsToDelete.Add(currparent.gameObject);
                            currparent = currparent.transform.parent;
                        }
                        else
                        {
                            parentempty = false;
                        }
                    }

                    if (meshfilter.gameObject != exclude)
                        DestroyImmediate(meshfilter.gameObject);
                    foreach (var parent in ParentsToDelete.ToArray())
                    {
                        if (parent != exclude)
                            DestroyImmediate(parent);
                    }
                }
            }
        }

        void ActivateGroups(bool activate)
        {
            var groups = this.GetComponentsInChildren<Group>();
            foreach (var group in groups)
            {
                if (DeactivatedOriginGroups.Contains(group.name))
                    if (activate)
                        group.Active = realvirtualBehavior.ActiveOnly.Always;
                    else
                        group.Active = realvirtualBehavior.ActiveOnly.Never;
            }
        }

        private void UndoOptimize()
        {
            // Loop through all gameobjects in OptimizedMeshes and destroy them
            var i = 0;
            while (i < OptimizedMeshes.Count)
            {
                if (OptimizedMeshes[i] != null)
                {
                    DestroyImmediate(OptimizedMeshes[i]);
                    OptimizedMeshes.RemoveAt(i);
                }
                else
                    i++;
            }

            i = 0;
            while (i < OptimizedMeshes.Count)
            {
                if (OptimizedMeshes[i] == null)
                    OptimizedMeshes.RemoveAt(i);
                else
                    i++;
            }

            GameObject BaseGo = Global.GetGameObjectByName("OptimizedMeshes");
            if (BaseGo != null)
                if (BaseGo.transform.childCount == 0)
                    DestroyImmediate(BaseGo);
            foreach (var renderers in DeactivatedRenderers)
            {
                renderers.enabled = true;
            }

            ActivateGroups(true);
            this.IsOptimized = false;
        }


      
    }
}