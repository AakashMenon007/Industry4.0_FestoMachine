// realvirtual (R) Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 realvirtual GmbH - Usage of this source code only allowed based on License conditions see https://realvirtual.io/en/company/license


using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using UnityEngine.Serialization;


namespace realvirtual
{
    [SelectionBase]
    //! The Fixer is able to fix Mus as Subcomponents to any Gameobject where the Fixer is attached. As soon as the free moving Mus are colliding or as soon as a Gripper is releasing
    //! the MUs the Fixer will fix the MU. MUs fixed by the Fixer have no Gravity and are purely kinematic.
    [ExecuteAlways]
    public class UnFixer : BehaviorInterface
    {
        [FormerlySerializedAs("LimitFixToTags")] public List<string> LimitToTags; //< Limits all Fixing functions to objects with defined Tags
        [Tooltip("Raycast direction")] [ShowIf("UseRayCast")] public Vector3 RaycastDirection = new Vector3(1, 0, 0); //!< Raycast direction
        [Tooltip("Length of Raycast in mm, Scale is considered")] [ShowIf("UseRayCast")] public float RayCastLength = 100; //!<  Length of Raycast in mm
        [Tooltip("Raycast Layers")] [ShowIf("UseRayCast")] public List<string> RayCastLayers = new List<string>(new string[] {"g4a MU","g4A SensorMU",}); //!< Raycast Layers
        [Tooltip("MU should be und fixed")] public bool UnFixMU = true;
        [Tooltip("Tag which is set after UnFix")]public string SetTagAfterUnfix;
        [Tooltip("Display status of Raycast or BoxCollider")] public bool ShowStatus = true; //! true if Status of Collider or Raycast should be displayed
        [Tooltip("PLCSignal for unfixing current MUs")]  public PLCOutputBool SignalUnfix; 
     
     
        private int layermask;
        private  RaycastHit[] hits;
        private bool signalunfixnotnull;
        
        public void Unfix(MU mu)
        {
            mu.Unfix();
        }

        private new void Awake()
        {
     
            base.Awake(); 
            layermask = LayerMask.GetMask(RayCastLayers.ToArray());
        }
        
        private void Start()
        {
        
            if (!Application.isPlaying)
                return;
            signalunfixnotnull = SignalUnfix != null;
        }
        
        private void Raycast()
        {
            float scale = 1000;
            if (!Application.isPlaying)
            {
                if (Global.g4acontroller != null) scale = Global.g4acontroller.Scale;
            }
            else
            {
                scale = realvirtualController.Scale;
            }

            var globaldir = transform.TransformDirection(RaycastDirection);
            var display = Vector3.Normalize(globaldir) * RayCastLength / scale;
            hits = Physics.RaycastAll(transform.position, globaldir, RayCastLength/scale, layermask,
                QueryTriggerInteraction.UseGlobal);
            if (hits.Length>0)
            {
             
                if (ShowStatus) Debug.DrawRay(transform.position, display ,Color.red,0,true);
            }
            else
            {
                if (ShowStatus) Debug.DrawRay(transform.position, display, Color.yellow,0,true);
            
            }
    
        }
        
        void Update()
        {
            if (!Application.isPlaying && ShowStatus )
            {
                Raycast();
            }
        }
        
        void FixedUpdate()
        {
            if (signalunfixnotnull)
                UnFixMU = SignalUnfix.Value;
                Raycast();
                if (hits.Length > 0)
                {
                 
                    foreach (var hit in hits)
                    {
                        var mu = hit.collider.GetComponentInParent<MU>();
                        if (mu != null)
                        {
                            if (UnFixMU)
                            {
                                if (SetTagAfterUnfix != "")
                                    mu.tag = SetTagAfterUnfix;
                                Unfix(mu);
                            }
                        }
                    }
                }
        }
        
    }
}