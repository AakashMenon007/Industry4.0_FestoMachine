using UnityEngine;
using NaughtyAttributes;

namespace realvirtual
{
    [RequireComponent(typeof(Drive))]
    public class TransportGuided : realvirtualBehavior, IGuidedSurface
    {
        [OnValueChanged("Init")] public bool Circular = false;

        [HideIf("Circular")] [OnValueChanged("Init")]
        public float Length = 1.0f;
        
        [ShowIf("Circular")] [OnValueChanged("Init")]
        public float Radius = 1.0f;

        [ShowIf("Circular")] [OnValueChanged("Init")]
        public float Angle = 90;

        [ShowIf("Circular")] [OnValueChanged("Init")]
        public float ColliderBorderZ = 0.2f;
        [ShowIf("Circular")] [OnValueChanged("Init")]
        public float ColliderBorderX = 0.2f;
        
        [OnValueChanged("Init")] public float Height = 0.5f;
        [OnValueChanged("Init")] [HideIf("Circular")]public float Width = 0.5f;

        [OnValueChanged("Init")] public bool UseStandardLayer = true;
        [OnValueChanged("Init")] public float DistanceCollider = 0.005f;
        [OnValueChanged("Init")] public bool ShowGizmos = true;
        private Vector3[] pathpoints;
        private int pathpointsnumber;
        [HideInInspector] [SerializeField] private Drive drive;
        [HideInInspector] [SerializeField] BoxCollider boxcollider;
        private IGuide guide;

        public void Init()
        {
            if (UseStandardLayer)
                this.gameObject.layer = LayerMask.NameToLayer("rvSimStatic");
            drive = GetComponent<Drive>();
            drive.Direction = DIRECTION.Virtual;
            if (!Circular)
            {
                DestroyImmediate(GetComponent<GuideCircle>());
                guide = (Global.AddComponentIfNotExisting<GuideLine>(this.gameObject));
                // set length in GuideLine
                var guideLine = (GuideLine) guide;
                guideLine.Length = Length;
               
                boxcollider = Global.AddComponentIfNotExisting<BoxCollider>(this.gameObject);
                boxcollider.size = new Vector3(Length, Height, Width);
                boxcollider.center = new Vector3(Length / 2, -Height / 2 - DistanceCollider, 0);
                guideLine.ShowGizmos = ShowGizmos;
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(guideLine);
#endif
                
            }
            else
            {
                DestroyImmediate(GetComponent<GuideLine>());
                guide = (Global.AddComponentIfNotExisting<GuideCircle>(this.gameObject));
                // set length in GuideLine
                var guidecircle = (GuideCircle) guide;
                guidecircle.Radius = Radius;
                guidecircle.Angle = Angle;
                guidecircle.CreatePath();
                guidecircle.ShowGizmos = ShowGizmos;
                boxcollider = Global.AddComponentIfNotExisting<BoxCollider>(this.gameObject);
                SetColliderSizeArc();
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(guidecircle);
#endif
            }
        }

        private void SetColliderSizeArc()
        {
            var guidecircle = (GuideCircle) guide;
            if (Mathf.Approximately(Angle, 0)) return ;
            var sizex = guidecircle.maxx +ColliderBorderX;
            var sizez = guidecircle.maxy + ColliderBorderZ;
            var borz = ColliderBorderZ;
            if (Angle< 0)
            {
                sizez = -sizez;
                borz = -borz;
                
            }
            boxcollider.size = new Vector3(sizex, Height, sizez);
            boxcollider.center = new Vector3(sizex*0.5f, -Height * 0.5f, -sizez*0.5f+borz);
        }

        public void OnDrawGizmos()
        {
            if (!ShowGizmos) return;
            Gizmos.color = Color.green;
            // Draw a Gimzo for BoxCollider
            if (boxcollider != null)
            {
                var center = boxcollider.center;
                // to global coordinates
                center = this.transform.TransformPoint(center);
                var size = boxcollider.size;
                // to global size
                size = this.transform.TransformVector(size);
                Gizmos.DrawWireCube(center, size);
            }
        }

        private Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Vector3 angles)
        {
            var dir = point - pivot;
            dir = Quaternion.Euler(angles) * dir;
            point = dir + pivot;
            return point;
        }


        private void Reset()
        {
            Init();
        }

        private new void Awake()
        {
            base.Awake();
            Init();
        }


        public bool IsSurfaceGuided()
        {
            return this.enabled;
        }

        public Vector3 GetClosestDirection(Vector3 position)
        {
            return guide.GetClosestDirection(position);
        }

        public Vector3 GetClosestPoint(Vector3 position)
        {
            return guide.GetClosestPoint(position);
        }

        public float GetSpeed()
        {
            return drive.CurrentSpeed;
        }
    }
}