using UnityEngine;

namespace realvirtual
{
    public interface IGuidedSurface
    {
        public bool IsSurfaceGuided();
        public Vector3 GetClosestDirection(Vector3 position);
        
        public Vector3 GetClosestPoint(Vector3 position);

        public float GetSpeed();
    }
}