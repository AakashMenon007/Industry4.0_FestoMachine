using UnityEngine;

using realvirtual;
using NaughtyAttributes;


namespace realvirtual
{
    [HelpURL("https://doc.realvirtual.io/components-and-scripts/motion/chain-element")]
    [SelectionBase]
//! An element which is transported by a chain (moving along the spline on the chain)
    public class ChainElement : realvirtualBehavior,IChainElement
    {
        [Header(("Settings"))]
     
        public bool
            AlignWithChain = true; //!< true if the chainelement needs to align with the chain tangent while moving

        public bool
            MoveRigidBody = true; //!< needs to be set to true if chainelements has colliders which should make parts move physically
        
        [ShowIf("AlignWithChain")]
        [InfoBox("Z of object to tangent, AlignVector or AlignObjectZ = up")]
        public Vector3 AlignVector = new Vector3(1, 0, 0); //!< additinal rotation for the alignment
        [ShowIf("AlignWithChain")]
        public GameObject AlignObjectLocalZUp;
        [ShowIf("AlignWithChain")][InfoBox("Debug Green = Tangent, Red = Up")]
        public bool DebugDirections;
        
        public Drive ConnectedDrive{ get; set; } //!< Drive where the chain is connected to
        public float StartPosition{ get; set; } //!< Start position of this chain element
       
        public Chain Chain{ get; set; } //!< Chain where this chainelement belongs to
        public bool UsePath { get; set; }

        public void InitPos(float pos)
        {
            
        }

        public float Position{ get; set; } //!< Current position of this chain element
        public float RelativePosition{ get; set; } //!< Relative position of this chain element

        private Vector3 _targetpos;
        private Quaternion targetrotation;
        private Vector3 tangentforward;
        private realvirtualController realvirtualcontroller;
        private bool chainnotnull = false;
        private bool alignobjectnotnull = false;
        
        private Rigidbody _rigidbody;
        
        public void SetPosition()
        {
            RelativePosition = Position / Chain.Length;
            var positon  = Chain.GetPosition(RelativePosition);

            if (MoveRigidBody)
                _rigidbody.MovePosition(positon);
            else
                transform.position = positon;
            _targetpos = transform.position;
            if (AlignWithChain)
            {
                Quaternion rotation = new Quaternion();
                var globaltangent = Chain.GetTangent(RelativePosition);
                Vector3 align = AlignVector;
                
                if (alignobjectnotnull)
                {
                    align = AlignObjectLocalZUp.transform.forward;
                }

                if (DebugDirections)
                {
                    Debug.DrawRay(transform.position, globaltangent, Color.green);
                    Debug.DrawRay(transform.position, align, Color.red);
                }
                var globaldir = transform.TransformDirection(align);
                rotation = Quaternion.LookRotation(globaltangent, globaldir);
                if (MoveRigidBody)
                    _rigidbody.MoveRotation(rotation);
                else
                    transform.rotation = rotation;
            }
        }

        public void UpdatePosition(float deltaTime)
        {
            Position = ConnectedDrive.CurrentPosition + StartPosition;

            if (Position > Chain.Length)
            {
                var rounds = Position / Chain.Length;
                Position = (Position % Chain.Length);
            }

            SetPosition();
        }

     

        private void Start()
        {
            _rigidbody = GetComponent<Rigidbody>();
            alignobjectnotnull = AlignObjectLocalZUp != null;
           
            if (Chain != null)
            {
                chainnotnull = true;
                SetPosition();
            }
            else
                chainnotnull = false;

        }
        private void FixedUpdate()
        {
            if (chainnotnull)
                UpdatePosition(Time.fixedDeltaTime);
        }

    }
}