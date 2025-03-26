using UnityEngine;

namespace Kamgam.PolygonMaterialPainter
{
    public static class MeshExtensions
    {
        public static Material[] GetSharedMaterialsCopy(this Renderer renderer)
        {
            var materials = new Material[renderer.sharedMaterials.Length];
            System.Array.Copy(renderer.sharedMaterials, materials, renderer.sharedMaterials.Length);
            return materials;
        }

        public static void SetSharedMaterialsAsCopy(this Renderer renderer, Material[] materials)
        {
            var copy = new Material[materials.Length];
            System.Array.Copy(materials, copy, materials.Length);
            renderer.sharedMaterials = copy;
        }
	}
}

