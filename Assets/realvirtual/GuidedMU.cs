// realvirtual.io (formerly game4automation) (R) a Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 realvirtual GmbH - Usage of this source code only allowed based on License conditions see https://realvirtual.io/unternehmen/lizenz    


using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
    using UnityEditor;
#endif

namespace realvirtual
{

    [RequireComponent(typeof(MU))]
    [SelectionBase]
    [DisallowMultipleComponent]
    public class GuidedMU : realvirtualBehavior, ISourceCreated
    {
        
        [Header("Settings")] 
        public float RaycastLength = 0.3f;
        [SerializeField] public LayerMask RaycastLayer;
        [SerializeField]
        public bool DebugMode;
        [Header("State")]
        private  IGuidedSurface transportSurface;
        private MU mu;
        private Transform _transform;
        private IGuidedSurface lastTransport;
        private Rigidbody _rigidbody;
        private ConfigurableJoint _joint;
        private float _angleOffset;
        private readonly RaycastHit[] _raycastHits = new RaycastHit[2];
        private bool issource = false;
        private GameObject lasthitgo;
        private GameObject currenthitgo;
        

        private void OnEnable()
        {
            _transform = GetComponent<Transform>();
            _rigidbody = GetComponentInChildren<Rigidbody>();
            mu = GetComponent<MU>();
            issource = GetComponent<Source>() != null;
        }

        private void FixedUpdate()
        {
            if (issource) return;
            Raycast();
            Move();
        }

        private void Reset()
        {
            RaycastLayer = LayerMask.GetMask("rvTransport", "rvSimStatic");
        }

        private void Raycast()
        {
            var raycastPosition = transform.position + Vector3.up * 0.05f;;
            var hits = Physics.RaycastNonAlloc(raycastPosition, Vector3.down,
                _raycastHits, RaycastLength, RaycastLayer);

            if (hits == 0)
            {
                if (_joint != null) DestroyImmediate(_joint);
                transportSurface = null;
                return;
            }

            var hitIndex = 0;
            if (hits > 1)
            {
                hitIndex = GetClosestHitIndex(_raycastHits);
            }
            
            currenthitgo = _raycastHits[hitIndex].transform.gameObject;
            if (currenthitgo != lasthitgo)
            {
                transportSurface = currenthitgo.GetComponentInChildren<IGuidedSurface>();
                if (transportSurface == null)
                {
                    if (_joint != null) Destroy(_joint);
                }
                else
                {
                    if (transportSurface.IsSurfaceGuided())
                    {
                        _angleOffset = GetOffsetAngle(transportSurface);
                        CreateJoint();
                    }
                    else
                    {
                        if (_joint != null) DestroyImmediate(_joint);
                    }
                }
            }

            lastTransport = transportSurface;
            lasthitgo = currenthitgo;
        }

        private int GetClosestHitIndex(IReadOnlyList<RaycastHit> hits)
        {
            var distance = Mathf.Infinity;
            var result = 0; 
            for (var i = 0; i < hits.Count; i++)
            {
                if (distance < hits[i].distance) continue;
                distance = hits[i].distance;
                result = i;
            }

            return result;
        }

        private void CreateJoint()
        {
            _joint = TryGetComponent(out ConfigurableJoint joint) ? joint : gameObject.AddComponent<ConfigurableJoint>();
            _joint.anchor = new Vector3(0, 0, 0);
            _joint.autoConfigureConnectedAnchor = false;
            _joint.xMotion = ConfigurableJointMotion.Free;
            _joint.yMotion = ConfigurableJointMotion.Locked;
            _joint.zMotion = ConfigurableJointMotion.Locked;
            _joint.angularXMotion = ConfigurableJointMotion.Locked;
            _joint.angularYMotion = ConfigurableJointMotion.Locked;
            _joint.angularZMotion = ConfigurableJointMotion.Locked;
        }

        private void Move()
        {
            if (_joint == null) return;
            if (transportSurface == null) return;
            var normal = transportSurface.GetClosestDirection(_transform.position);
            _joint.connectedAnchor =  transportSurface.GetClosestPoint(_transform.position);
            _rigidbody.transform.rotation = Quaternion.LookRotation(normal, Vector3.up) * Quaternion.AngleAxis(_angleOffset, Vector3.up);
            _joint.axis = Quaternion.AngleAxis(_angleOffset, Vector3.up) * Vector3.forward;
            _rigidbody.velocity = normal * (transportSurface.GetSpeed()/realvirtualController.Scale);
        }

        private float GetOffsetAngle(IGuidedSurface transport)
        {
            var normal = transport.GetClosestDirection(_transform.position);
            var angle  = Vector3.SignedAngle(normal, transform.forward, Vector3.up);
            return Mathf.Round(angle / 90f) * 90f;
        }

        private void OnDrawGizmos()
        {
            if (!DebugMode) return;
            if (transportSurface == null) return;

            var point = transportSurface.GetClosestPoint(_transform.position);
            var normal = transportSurface.GetClosestDirection(_transform.position);
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(point, 0.02f);
            Gizmos.DrawLine(transform.position, transform.position + normal*0.2f);
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.down * RaycastLength);

        }

        public void OnSourceCreated()
        {
            issource = false;
        }
    }
}
