
using NaughtyAttributes;
using UnityEngine;


namespace realvirtual
{
    public class RoboDKTarget : MonoBehaviour
    {
        public bool HideOnPlay = true;
        /* [Button("Move to Target")]
        public void MoveToTarget()
        {
            RoboDKInterface rdk = GetComponentInParent<RoboDKInterface>();
           // rdk.MoveToTarget(gameObject);
        } */

        public void Start()
        {
            
            var meshrenderer = GetComponentInChildren<MeshRenderer>();
            meshrenderer.enabled = !HideOnPlay;
        }
    }

}

