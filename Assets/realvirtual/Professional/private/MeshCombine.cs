using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace realvirtual
{
    public static class MeshCombine 
    {
        private const int Mesh16BitBufferVertexLimit = 65535;
        private static Vector3 oldScale;
        public static GameObject CombineMeshes(Transform parent, GameObject gobject, string name, bool showCreatedMeshInfo,
            MeshFilter[] meshfilters, ref List<GameObject>OptimizedMeshes, ref List<MeshRenderer> DeactivatedRenderers)
        {
           
            GameObject BaseGo = Global.AddGameObjectIfNotExisting("OptimizedMeshes");
            // create a new gameobject
            GameObject newGo = new GameObject();
            newGo.transform.position = parent.position;
            newGo.transform.parent = BaseGo.transform;
            newGo.name = name;


            #region Save our parent scale and our Transform and reset it temporarily:

            // When we are unparenting and get parent again then sometimes scale is a little bit different so save scale before unparenting:
            Vector3 oldScaleAsChild = gobject.transform.localScale;

            // If we have parent then his scale will affect to our new combined Mesh scale so unparent us:
            int positionInParentHierarchy = gobject.transform.GetSiblingIndex();
            parent = gobject.transform.parent;
            if(parent != null)
                parent.transform.parent = null;
           

            // Thanks to this the new combined Mesh will have same position and scale in the world space like its children:
            Quaternion oldRotation = gobject.transform.rotation;
            Vector3 oldPosition =gobject.transform.position;
            oldScale = gobject.transform.localScale;
            gobject.transform.rotation = Quaternion.identity;
            gobject.transform.position = Vector3.zero;
            gobject.transform.localScale = Vector3.one;

            #endregion Save Transform and reset it temporarily.

            #region Combine Meshes into one Mesh:

            // Combine the given Meshfilters to one gameobject and add it under "OptimizedMeshes"
            CombineMeshesWithMutliMaterial(newGo, showCreatedMeshInfo, meshfilters,name,true, ref DeactivatedRenderers);
            OptimizedMeshes.Add(newGo);

            #endregion Combine Meshes into one Mesh.

            #region Set old Transform values:

            // Bring back the Transform values:
            gobject.transform.rotation = oldRotation;
            gobject.transform.position = oldPosition;
            gobject.transform.localScale = oldScale;
            newGo.transform.localScale = oldScaleAsChild;
            newGo.transform.rotation = oldRotation;
            newGo.transform.position = oldPosition;

            // Get back parent and same hierarchy position:
            gobject.transform.parent = parent;
            gobject.transform.SetSiblingIndex(positionInParentHierarchy);

            // Set back the scale value as child:
            gobject.transform.localScale = oldScaleAsChild;

            #endregion Set old Transform values.

            return newGo;
        }

        public static void CombineMeshesWithMutliMaterial(GameObject newgo, bool showCreatedMeshInfo,
            MeshFilter[] meshFilters, string name, bool deactivate, ref List<MeshRenderer> DeactivatedRenderers )
        {
            if (meshFilters.Length == 0)
                return;
            MeshRenderer[] meshRenderers = new MeshRenderer[meshFilters.Length + 1];
            if (meshFilters.Length == 0)
                Debug.Log("Nothing to combine!");

            MeshFilter filter = null;
            filter = newgo.GetComponent<MeshFilter>();
            if (filter == null)
                filter = newgo.AddComponent<MeshFilter>();
            meshRenderers[0] = Global.AddComponentIfNotExisting<MeshRenderer>(newgo);
            List<Material> uniqueMaterialsList = new List<Material>();
            for (int i = 0; i < meshFilters.Length; i++)
            {
                meshRenderers[i + 1] = meshFilters[i].GetComponent<MeshRenderer>();
                if (meshRenderers[i + 1] != null)
                {
                    Material[] materials = meshRenderers[i + 1].sharedMaterials; // Get all Materials from child Mesh.
                    for (int j = 0; j < materials.Length; j++)
                    {
                        if (!uniqueMaterialsList
                                .Contains(materials[j])) // If Material doesn't exists in the list then add it.
                        {
                            uniqueMaterialsList.Add(materials[j]);
                        }
                    }
                }

                var render = meshFilters[i].GetComponent<MeshRenderer>();
                DeactivatedRenderers.Add(render);
                if (deactivate)
                    render.enabled = false;
            }

            List<CombineInstance> finalMeshCombineInstancesList = new List<CombineInstance>();

            // If it will be over 65535 then use the 32 bit index buffer:
            long verticesLength = 0;

            for (int i = 0;
                 i < uniqueMaterialsList.Count;
                 i++) // Create each Mesh (submesh) from Meshes with the same Material.
            {
                List<CombineInstance> submeshCombineInstancesList = new List<CombineInstance>();

                for (int j = 0; j < meshFilters.Length; j++) // Get only childeren Meshes (skip our Mesh).
                {
                    if (meshRenderers[j + 1] != null)
                    {
                        Material[] submeshMaterials =
                            meshRenderers[j + 1].sharedMaterials; // Get all Materials from child Mesh.

                        for (int k = 0; k < submeshMaterials.Length; k++)
                        {
                            // If Materials are equal, combine Mesh from this child:
                            if (uniqueMaterialsList[i] == submeshMaterials[k])
                            {
                                CombineInstance combineInstance = new CombineInstance();
                                combineInstance.subMeshIndex = k; // Mesh may consist of smaller parts - submeshes.
                                // Every part have different index. If there are 3 submeshes
                                // in Mesh then MeshRender needs 3 Materials to render them.
                                combineInstance.mesh = meshFilters[j].sharedMesh;
                                combineInstance.transform = meshFilters[j].transform.localToWorldMatrix;
                                submeshCombineInstancesList.Add(combineInstance);
                                verticesLength += combineInstance.mesh.vertices.Length;
                                // loop over all vertices and multiply the scale
                              
                            }
                        }
                    }
                }

                // Create new Mesh (submesh) from Meshes with the same Material:
                Mesh submesh = new Mesh();

                if (verticesLength > Mesh16BitBufferVertexLimit)
                {
                    submesh.indexFormat =
                        UnityEngine.Rendering.IndexFormat.UInt32; // Only works on Unity 2017.3 or higher.
                }

                submesh.CombineMeshes(submeshCombineInstancesList.ToArray(), true);
                CombineInstance finalCombineInstance = new CombineInstance();
                finalCombineInstance.subMeshIndex = 0;
                finalCombineInstance.mesh = submesh;
                finalCombineInstance.transform = Matrix4x4.identity;
                finalMeshCombineInstancesList.Add(finalCombineInstance);
            }

            meshRenderers[0].sharedMaterials = uniqueMaterialsList.ToArray();

            Mesh combinedMesh = new Mesh();
            combinedMesh.name = name;
            if (verticesLength > Mesh16BitBufferVertexLimit)
            {
                combinedMesh.indexFormat =
                    UnityEngine.Rendering.IndexFormat.UInt32;
            }

            combinedMesh.CombineMeshes(finalMeshCombineInstancesList.ToArray(), false);
            MeshFilter Base = newgo.GetComponent<MeshFilter>();
            Base.sharedMesh = combinedMesh;


            if (showCreatedMeshInfo)
            {
                if (verticesLength <= Mesh16BitBufferVertexLimit)
                {
                    Debug.Log("Mesh \"" + name + "\" was created from " + (meshFilters.Length - 1) +
                              " children meshes and has "
                              + finalMeshCombineInstancesList.Count + " submeshes, and " + verticesLength +
                              " vertices.</b></color>");
                }
                else
                {
                    Debug.Log("Mesh \"" + name + "\" was created from " + (meshFilters.Length - 1) +
                              " children meshes and has "
                              + finalMeshCombineInstancesList.Count + " submeshes, and " + verticesLength
                              + " vertices. Some old devices, like Android with Mali-400 GPU, do not support over 65535 vertices.</b></color>");
                }
            }
        }
        public static void activateCombinedGameObjects(MeshFilter[] meshFilters)
        {
            for (int i = 0;
                 i < meshFilters.Length;
                 i++)
            {
                meshFilters[i].gameObject.SetActive(true);
                MeshRenderer meshRenderer = meshFilters[i].gameObject.GetComponent<MeshRenderer>();
                if (meshRenderer != null)
                {
                    meshRenderer.enabled = true;
                }
            }
        }
    }
}
