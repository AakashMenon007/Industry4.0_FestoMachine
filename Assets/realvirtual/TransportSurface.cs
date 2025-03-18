// realvirtual (R) Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 realvirtual GmbH - Usage of this source code only allowed based on License conditions see https://realvirtual.io/en/company/license


using System;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnityEngine;
using Object = UnityEngine.Object;
#if REALVIRTUAL_AGX
using AGXUnity;
#endif

namespace realvirtual
{
#pragma warning disable 0214
#pragma warning disable 0414
    //! Transport Surface - this class is needed together with Drives to model conveyor systems. The transport surface is transporting
    //! rigid bodies which are colliding with its surface
    [HelpURL("https://doc.realvirtual.io/components-and-scripts/motion/transportsurface")]
    public class TransportSurface : BaseTransportSurface, IGuidedSurface
    {
        #region Public Variables

#if REALVIRTUAL_AGX
        public bool UseAGXPhysics;
#else
        [HideInInspector] public bool UseAGXPhysics = false;
#endif


        public Vector3
            TransportDirection; //!< The direction in local coordinate system of Transport surface - is initialized normally by the Drive

        public bool AnimateSurface = true;

        [ShowIf("AnimateSurface")] public float
            TextureScale = 10; //!< The texture scale what influcences the texture speed - needs to be set manually 

        public bool ChangeConstraintsOnEnter = false;
        [ShowIf("ChangeConstraintsOnEnter")] public RigidbodyConstraints ConstraintsEnter;
        public bool ChangeConstraintsOnExit = false;
        [ShowIf("ChangeConstraintsOnExit")] public RigidbodyConstraints ConstraintsExit;

        public bool Radial = false;
        [HideInInspector] public Drive UseThisDrive;
        [ReadOnly] public float speed = 0; //!< the current speed of the transport surface - is set by the drive 
        [ReadOnly] public bool IsGuided = true;

        [InfoBox("Standard Setting for layer is rvTransport")] [OnValueChanged("RefreshReferences")]
        public string Layer = "rvTransport";

        [InfoBox("For Best performance unselect UseMeshCollider, for good transfer between conveyors select this")]
        [OnValueChanged("RefreshReferences")]
        public bool UseMeshCollider = false;

        public bool DebugMode = false;

        public Drive
            ParentDrive; //!< needs to be set to true if transport surface moves based on another drive - transport surface and it's drive are not allowed to be a children of the parent drive.

        public delegate void
            OnEnterExitDelegate(Collision collission,
                TransportSurface surface); //!< Delegate function for GameObjects entering the Sensor.

        public event OnEnterExitDelegate OnEnter;
        public event OnEnterExitDelegate OnExit;

        #endregion

        #region Private Variables

        private MeshRenderer _meshrenderer;
        private Collider _collider;
        private Rigidbody _rigidbody;
        private bool _isMeshrendererNotNull;
        private bool parentdrivenotnull;
        private Transform _parent;
        [HideInInspector] public Vector3 parentposbefore;
        private Quaternion parentrotbefore;
        private Quaternion parenttextrotbefore;
        private Quaternion parentstartrot;
        private Quaternion startrot;
        private float lastmovZ = 0;
        private float lastmovX = 0;
        private float lastmovY = 0;
        private IGuide guide;

        [ReadOnly] public List<Rigidbody> LoadedPart = new List<Rigidbody>();

        [HideInInspector] public Vector3 startTransportDirection;

        #endregion

        #region Public Methods

        public bool IsSurfaceGuided()
        {
            if (guide != null)
                return guide.IsActive();
            else
                return false;
        }
        
        public float GetSpeed()
        {
            return speed;
        }

        public Vector3 GetClosestDirection(Vector3 position)
        {
            if (IsGuided)
                if (guide != null)
                    return guide.GetClosestDirection(position);
            return TransportDirection;
        }

        public Vector3 GetClosestPoint(Vector3 position)
        {
            if (IsGuided)
                return guide.GetClosestPoint(position);

            return position;
        }

        //! Gets a center point on top of the transport surface
        public Vector3 GetMiddleTopPoint()
        {
            if (gameObject == null)
                return Vector3.zero;
            ;
            var collider = gameObject.GetComponent<Collider>();
            if (collider != null)
            {
                var vec = new Vector3(collider.bounds.center.x, collider.bounds.center.y + collider.bounds.extents.y,
                    collider.bounds.center.z);
                return vec;
            }
            else
                return Vector3.zero;
        }

        //! Sets the speed of the transport surface (used by the drive)
        public void SetSpeed(float _speed)
        {
            speed = _speed;
        }

        public void OnEnterSurface(Collision other)
        {
            if (OnEnter != null)
                OnEnter.Invoke(other, this);
        }

        public void OnExitSurface(Collision other)
        {
            if (OnExit != null)
                OnExit.Invoke(other, this);
        }

        #endregion


        #region Private Methods

        private void RefreshReferences()
        {
            if (!UseAGXPhysics)
            {
                var _mesh = GetComponent<MeshCollider>();
                var _box = GetComponent<BoxCollider>();
                if (UseMeshCollider)
                {
                    if (_box != null)

                        DestroyImmediate(_box);
                    if (_mesh == null)
                    {
                        _mesh = gameObject.AddComponent<MeshCollider>();
                    }
                }
                else
                {
                    if (_mesh != null)
                        DestroyImmediate(_mesh);
                    if (_box == null)
                    {
                        _box = gameObject.AddComponent<BoxCollider>();
                    }
                }

                _rigidbody = gameObject.GetComponent<Rigidbody>();
                if (_rigidbody != null)
                {
                    _rigidbody.isKinematic = true;
                    _rigidbody.useGravity = false;
                }
                else
                {
                    Error("Transport Surface needs a Rigidbody component attached to it");
                }

                _collider = gameObject.GetComponent<Collider>();

            }

            _meshrenderer = gameObject.GetComponent<MeshRenderer>();
            if (UseAGXPhysics)
            {
                _rigidbody = gameObject.GetComponent<Rigidbody>();
                if (_rigidbody != null)
                    DestroyImmediate(_rigidbody);
            }
        }


        private void Reset()
        {
            gameObject.layer = LayerMask.NameToLayer(Layer);
            RefreshReferences();

            // Add transport surface to drive if a drive is existing in this or an upper object
            if (UseThisDrive != null)
            {
                UseThisDrive.AddTransportSurface(this);
                return;
            }

            var drive = gameObject.GetComponentInParent<Drive>();
            if (drive != null)
                drive.AddTransportSurface(this);
        }

        [Button("Destroy Transport Surface")]
        private void DestroyTransportSurface()
        {
            var drive = gameObject.GetComponentInParent<Drive>();
            if (drive != null)
                drive.RemoveTransportSurface(this);
            Object.DestroyImmediate(this);
        }

        new void Awake()
        {
            if (ParentDrive != null || IsGuided || OnEnter != null || OnExit != null || ChangeConstraintsOnEnter ||
                ChangeConstraintsOnExit)
            {
                Global.AddComponentIfNotExisting<TransportsurfaceCollider>(this.gameObject);
                LoadedPart.Clear();
            }

            guide = GetComponentInChildren<IGuide>();
            IsGuided = guide != null;
            base.Awake();
        }


        void Start()
        {
            Reset();
            SetSpeed(speed);
            parentposbefore = Vector3.zero;
            parentrotbefore = Quaternion.identity;
            parenttextrotbefore = Quaternion.identity;
            parentdrivenotnull = ParentDrive != null;
            _isMeshrendererNotNull = _meshrenderer != null;
            if (ParentDrive != null)
            {
                parentstartrot = ParentDrive.transform.localRotation;
            }
            else
                parentstartrot = Quaternion.identity;

            startrot = transform.localRotation;

#if REALVIRTUAL_AGX
            if (UseAGXPhysics)
            {
                var rb = GetComponent<RigidBody>();
                if (rb == null)
                {
                    Debug.LogWarning("Transportsurface using AGX: Expecting an AGX RigidBody component.", this);
                    return;
                }

                if (GetComponent<AGXUnity.Collide.Box>() == null && GetComponent<AGXUnity.Collide.Mesh>() == null &&
                    GetComponent<AGXUnity.Collide.Sphere>() == null &&
                    GetComponent<AGXUnity.Collide.Cylinder>() == null && GetComponent<AGXUnity.Collide.Plane>() == null)
                {
                    Debug.LogWarning("Transportsurface using AGX: Expecting an AGX Shape Collider component.", this);
                    return;
                }

                Simulation.Instance.ContactCallbacks.OnContact(OnContact, rb);
            }
#else
            UseAGXPhysics = false;
#endif
        }

#if REALVIRTUAL_AGX
        private bool OnContact(ref ContactData data)
        {
            if (Radial)
            {
                Error("Radial AGX Transport Surfaces are not yet supported");
            }
            else
            {
                var global = TransportDirection;
                foreach (ref var point in data.Points)
                {
                    
                    point.SurfaceVelocity =
 -global * speed *  realvirtualController.SpeedOverride / realvirtualController.Scale;;
                }
            }

            return true;
        }
#endif

        void Update()
        {

            if (speed != 0)
            {
                Vector3 mov = Vector3.zero;
                var globalrot = this.transform.rotation.eulerAngles;

                if (parentdrivenotnull && !ParentDrive.IsRotation)
                {
                    if (parenttextrotbefore == Quaternion.identity)
                    {
                        parenttextrotbefore = ParentDrive.transform.rotation;
                    }

                    var parentrot = ParentDrive.transform.rotation;
                    var deltarot = parentrot * Quaternion.Inverse(parentstartrot);
                    var newrot = deltarot * _rigidbody.rotation;
                    mov = newrot * TransportDirection * TextureScale * Time.deltaTime * speed *
                        realvirtualController.SpeedOverride / realvirtualController.Scale;
                }
                else
                {
                    var currentTD = Vector3.zero;
                    if (!Radial)
                        currentTD = startTransportDirection;
                    else
                    {
                        currentTD = TransportDirection;
                    }

                    mov = currentTD * TextureScale * Time.deltaTime * speed *
                        realvirtualController.SpeedOverride / realvirtualController.Scale;
                }

                Vector2 vector2 = new Vector2();
                var x = mov.x + lastmovX;
                float y = 0;

                if (!Radial && (!parentdrivenotnull || !ParentDrive.IsRotation))
                {
                    y = mov.z + lastmovY;
                    var localrot = this.transform.localRotation.eulerAngles;
                    Vector3 vector3 = new Vector3(x, y, 0);

                    var textdir = Quaternion.Euler(0, 0, localrot.y) * vector3;

                    vector2 = new Vector2(textdir.x, textdir.y);
                }
                else
                {
                    y = mov.y + lastmovY;
                    vector2 = new Vector2(x, y);
                }

                lastmovX = x;
                lastmovY = y;

                if (parentdrivenotnull)
                    parenttextrotbefore = ParentDrive.transform.rotation;

                if (_isMeshrendererNotNull)
                {
                    _meshrenderer.material.mainTextureOffset = vector2;
                }
            }
        }


        void FixedUpdate()
        {
            if (UseAGXPhysics)
                return;
            if (IsGuided)
                return;

            if (!Radial)
            {
                Vector3 newpos, mov;
                newpos = _rigidbody.position;

                // Linear Conveyor
                if (parentdrivenotnull)
                {
                    if (parentposbefore == Vector3.zero)
                    {
                        parentposbefore = ParentDrive.transform.position;
                    }

                    if (parentrotbefore == Quaternion.identity)
                    {
                        parentrotbefore = ParentDrive.transform.localRotation;
                    }

                    var dir = TransportDirection;

                    var deltarot = parentrotbefore * Quaternion.Inverse(parentstartrot);

                    mov = deltarot * dir * Time.fixedDeltaTime * speed *
                          realvirtualController.SpeedOverride /
                          realvirtualController.Scale;

                    var parentpos = ParentDrive.transform.position;
                    var deltaparent = parentpos - parentposbefore;
                    var deltaUp = GetVertikalMov(deltaparent);
                    var deltaArea = deltaparent - deltaUp;
                    var dirtotal = mov + deltaArea; // ParentDrive separate
                    var dirback = -mov; // ParentDrive separate

                    if (DebugMode)
                    {
                        Global.DebugDrawArrow(transform.position + new Vector3(0, 0.5f, 0), deltaUp * 1000, Color.cyan);
                        Global.DebugDrawArrow(transform.position + new Vector3(0, 0.5f, 0), deltaArea * 1000,
                            Color.green);
                        Global.DebugDrawArrow(transform.position + new Vector3(0, 0.5f, 0), dirtotal * 1000, Color.red);
                    }

                    _rigidbody.position = (_rigidbody.position + dirback);
                    Physics.SyncTransforms();
                    _rigidbody.MovePosition(_rigidbody.position + dirtotal + deltaUp);
                    _rigidbody.MoveRotation(startrot * deltarot.normalized);

                    parentposbefore = ParentDrive.transform.position;
                    parentrotbefore = ParentDrive.transform.localRotation;
                }
                else
                {
                    if (speed != 0)
                    {
                        mov = TransportDirection * Time.fixedDeltaTime * speed *
                              realvirtualController.SpeedOverride /
                              realvirtualController.Scale;
                        _rigidbody.position = (_rigidbody.position - mov);
                        Physics.SyncTransforms();
                        _rigidbody.MovePosition(_rigidbody.position + mov);
                    }
                }
            }
            else
            {
                Quaternion nextrot;
                // Radial Conveyor
                if (ParentDrive != null)
                {
                    Error("Not implemented!");
                }
                else
                {
                    if (speed != 0)
                    {
                        _rigidbody.rotation = _rigidbody.rotation * Quaternion.AngleAxis(
                            -speed * Time.fixedDeltaTime *
                            realvirtualController.SpeedOverride,
                            transform.InverseTransformVector(TransportDirection));
                        nextrot = _rigidbody.rotation * Quaternion.AngleAxis(
                            +speed * Time.fixedDeltaTime * realvirtualController.SpeedOverride,
                            transform.InverseTransformVector(TransportDirection));
                        _rigidbody.MoveRotation(nextrot);
                    }
                }
            }
        }

        private Vector3 GetVertikalMov(Vector3 deltacomplete)
        {
            Vector3 deltaUp = Vector3.zero;
            // prevents the influence of rounding errors in for the up-vector
            if (Vector3.Angle(deltacomplete, Vector3.up) == 0)
            {
                deltaUp = new Vector3(0, deltacomplete.y, 0);
            }
            else if (Vector3.Angle(deltacomplete, Vector3.right) == 0)
            {
                deltaUp = new Vector3(deltacomplete.x, 0, 0);
            }
            else
            {
                deltaUp = new Vector3(0, 0, deltacomplete.z);
            }

            return deltaUp;
        }

        #endregion
    }
}