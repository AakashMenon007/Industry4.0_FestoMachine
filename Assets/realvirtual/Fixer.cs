// realvirtual (R) Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 realvirtual GmbH - Usage of this source code only allowed based on License conditions see https://realvirtual.io/en/company/license


using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using UnityEngine.Serialization;


namespace realvirtual
{
    [HelpURL("https://doc.realvirtual.io/components-and-scripts/fixer")]
    [SelectionBase]
    //! The Fixer is able to fix Mus as Subcomponents to any Gameobject where the Fixer is attached. As soon as the free moving Mus are colliding or as soon as a Gripper is releasing
    //! the MUs the Fixer will fix the MU. MUs fixed by the Fixer have no Gravity and are purely kinematic.
    [ExecuteAlways]
    public class Fixer : BehaviorInterface,IFix
    {
        public bool UseRayCast; //!< Use Raycasts instead of Box Collider for detecting parts
        [FormerlySerializedAs("LimitFixToTags")] public List<string> LimitToTags; //< Limits all Fixing functions to objects with defined Tags
        [Tooltip("Raycast direction")] [ShowIf("UseRayCast")] public Vector3 RaycastDirection = new Vector3(1, 0, 0); //!< Raycast direction
        [Tooltip("Length of Raycast in mm, Scale is considered")] [ShowIf("UseRayCast")] public float RayCastLength = 100; //!<  Length of Raycast in mm
        [Tooltip("Raycast Layers")] [ShowIf("UseRayCast")] public List<string> RayCastLayers = new List<string>(new string[] {"rvMU","rvMUSensor",}); //!< Raycast Layers
        [Tooltip("MU should be fixed -will fix MU undependent from signal like a gluing surface")] public bool FixMU = true;
        [Tooltip("True if MU should be fixed or aligned when Distance between MU and Fixer is minimum (distance is increasing again)")] public bool AlignAndFixOnMinDistance;  //!< true if MU should be fixed or aligned when Distance between MU and Fixer is minimum (distance is increasing again)
        [Tooltip("Align pivot point of MU to Fixer pivot point")] public bool AlignMU; //!< true if pivot Points of MU and Fixer should be aligned
        [Tooltip("Offset to pivot point to align to in local coordinate system")][ShowIf("AlignMU")] public Vector3 DeltaAlign;
        [Tooltip("Tag which is set after Fix")]public string SetTagAfterFix;
        [Tooltip("Offset rotation in local coordinate system")] [ShowIf("AlignMU")]
        public Vector3 DeltaRot;
        [Tooltip("Display status of Raycast or BoxCollider")] public bool ShowStatus = true; //! true if Status of Collider or Raycast should be displayed
        [Tooltip("Opacity of Mesh in case of status display")] [ShowIf("ShowStatus")] [HideIf("UseRayCast")] public float StatusOpacity = 0.2f; //! Opacity of Mesh in case of status display
        [Tooltip("Disable handing over to onother fixer")] public bool BlockHandingOver; //! if true the fixer will not be able to hand over the MU to another fixer which is colliding with the MU
        [Tooltip("Only controlled by Signal FixerFix - with one bit")]public bool OneBitFix; //! Only controlled by Signal FixerFix - with one bit
        [Tooltip("PLCSignal for fixing current MUs and turning Fixer off")]  public PLCOutputBool FixerFix; 
        [Tooltip("PLCSignal for releasing current MUs and turning Fixer off")] [HideIf("OneBitFix")] public PLCOutputBool FixerRelease; //! PLCSignal for releasing current MUs and turning Fixer off
        [Tooltip("PLCSignal for blocking handing over to another fixer")] public PLCOutputBool SignalBlockHandingOver; // PLCOutpout for BlockHandingOver
        public bool DebugMode = false;    
        private bool nextmunotnull; 
        public List<MU> MUSEntered;
        public List<MU> MUSFixed;
 
        private MeshRenderer meshrenderer;
        private int layermask;
        private  RaycastHit[] hits;

        private bool lastfix;
        private bool lastfixmu;
        private bool lastrelease;
        private bool signalfixerreleasenotnull;
        private bool signalfixerfixnotnull;
        private bool signalbockhandingovernotnull;
        private bool meshrenderernotnull;
        private bool Deactivated = false;


        // Trigger Enter and Exit from Sensor
        public void OnTriggerEnter(Collider other)
        {
            var mu = other.gameObject.GetComponentInParent<MU>();
            
            if (LimitToTags.Count>0)
                if (!LimitToTags.Contains(mu.tag))
                {
                    if (DebugMode)
                        Debug.Log("DebugMode Fixer - MU not in LimitToTags " + mu.name);
                    MUSEntered.Remove(mu); 
                    return;
                }

            if (mu != null)
            {
                if (!MUSFixed.Contains(mu) && !MUSEntered.Contains(mu))
                {
                    if (DebugMode)
                        Debug.Log("DebugMode Fixer - MU entered " + mu.name);
                    MUSEntered.Add(mu);
                    mu.FixerLastDistance = -1;
                }
            }
        }
        
        public void OnTriggerExit(Collider other)
        {
       
            var mu = other.gameObject.GetComponentInParent<MU>();
            if (DebugMode && mu != null)
                Debug.Log("DebugMode Fixer - MU OnTriggerExit " + mu.name);
            if (MUSEntered.Contains(mu))
            {
                if (DebugMode)
                    Debug.Log("DebugMode Fixer - MUs in entered is leaving" + mu.name);
                MUSEntered.Remove(mu);
            }
        }
        
         void Reset()
         {
             if (GetComponent<BoxCollider>())
                 UseRayCast = false;
             else
                 UseRayCast = true;
         }
        
        public void CheckRelease()
        {
            var fix = false;
            if (signalfixerfixnotnull && FixerFix.Value == true && !OneBitFix)
                fix = true;
            
            var release = false;
            if (signalfixerreleasenotnull)
                release = FixerRelease.Value;

            if ((!FixMU && !fix) || release)
            {
                var mus = MUSFixed.ToArray();
                for (int i = 0; i < mus.Length; i++)
                {
                    Unfix(mus[i]);
                } 
            }
        }
        
        public void Unfix(MU mu)
        {
            if (Deactivated)
                return;

            if (!MUSFixed.Contains(mu))
                return;
            
            if (DebugMode)
                    Debug.Log("DebugMode Fixer - Unfix MU " + mu.name);
            mu.Unfix();
            MUSFixed.Remove(mu);
        }
        
        public void Fix()
        {
            var mus = MUSEntered.ToArray();
            if (mus.Length == 0)
                return;
            
            for (int i = 0; i < mus.Length; i++)
            {
                Fix(mus[i]);
            } 
        }

        public void DeActivate(bool activate)
        {
            Deactivated = activate;
        }
        
        public void Fix(MU mu)
        {
            if (SetTagAfterFix != "")
                mu.tag = SetTagAfterFix;
            if (Deactivated)
                return;
            
            var fix = false;
            if (signalfixerfixnotnull && FixerFix.Value == true)
                fix = true;
            
            if (!FixMU && !fix)
                return; 
            
            /// Check if currently fixed and if other Fixer is blocking Handing over
            if (mu.FixedBy != null)
            {
                var fixer = mu.FixedBy.GetComponent<Fixer>();
                if (fixer != null)
                {
                    if (fixer.BlockHandingOver)
                    {
                        return;
                    }
                }
                else
                {
                    return;
                }
            }

            MUSEntered.Remove(mu);
            if(!MUSFixed.Contains(mu))
                MUSFixed.Add(mu);
            if (AlignMU)
            {
                mu.transform.position = transform.position + transform.TransformDirection(DeltaAlign);
                mu.transform.rotation = transform.rotation;
                mu.transform.localRotation = mu.transform.localRotation * Quaternion.Euler(DeltaRot);
            }
            if (DebugMode)
                Debug.Log("DebugMode Fixer - Fix MU " + mu.name);
            mu.Fix(this.gameObject);
        }

        
        private void AtPosition(MU mu)
        {
            if (Deactivated)
                return;
            if (mu.LastFixedBy == this.gameObject)
            {
                return;
            }
            
            var release = false;
            if (signalfixerreleasenotnull)
                release = FixerRelease.Value;
            
            if (release)
                return;

            /// Only fix if another fixer element has fixed it - don't fix it if it is fixed by a gripper
            var fixedby = mu.FixedBy;
            Fixer fixedbyfixer = null;
            if (fixedby != null)
                fixedbyfixer = mu.FixedBy.GetComponent<Fixer>();

            if (mu.FixedBy == null || (fixedbyfixer != null && fixedbyfixer != this))
            {
                Fix(mu);
            }
        }

        private new void Awake()
        {
            if (!UseRayCast)
            {
                {
                    var collider = GetComponent<BoxCollider>();
                    if (collider == null)
                        Error("Fixer neeeds a Box Collider attached to if no Raycast is used!");
                    else
                    {
                        collider.isTrigger = true;
                    }
                }
            }
            base.Awake(); 
            layermask = LayerMask.GetMask(RayCastLayers.ToArray());
        }
        
        private void Start()
        {
            meshrenderer = GetComponent<MeshRenderer>();

            if (!Application.isPlaying)
                return;
            signalfixerreleasenotnull = FixerRelease != null;
            signalfixerfixnotnull = FixerFix != null;
            signalbockhandingovernotnull = SignalBlockHandingOver != null;
            meshrenderernotnull = meshrenderer != null;
            var mus = GetComponentsInChildren<MU>();
            foreach (MU mu in mus)
            {
                if (!MUSFixed.Contains(mu))
                    Fix(mu);
            }
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

        private float GetDistance(MU mu)
        {
            return Vector3.Distance(mu.gameObject.transform.position, this.transform.position);
        }


        void CheckEntered()
        {
            var entered = MUSEntered.ToArray();
            for (int i = 0; i < entered.Length; i++)
            {
                AtPosition(entered[i]);
            }
        }
        
        void Update()
        {
            if (Deactivated)
                return;
            if (!Application.isPlaying && ShowStatus && UseRayCast)
            {
                Raycast();
            }
        }
        
        void FixedUpdate()
        {
       
           if (Deactivated)
                return;

           if (signalbockhandingovernotnull)
               BlockHandingOver = SignalBlockHandingOver.Value;

           var checkrelease = false;
           if (signalfixerreleasenotnull)
               checkrelease = FixerRelease.Value;

           var checkfix = false;
           if (signalfixerfixnotnull)
               checkfix = FixerFix.Value;
           
           if (UseRayCast)
            {
                Raycast();
                if (hits.Length > 0)
                {
                    MUSEntered.Clear();
                    foreach (var hit in hits)
                    {
                        var mu = hit.collider.GetComponentInParent<MU>();
                        if (mu != null)
                        {
                            if (!MUSFixed.Contains(mu))
                            {
                                bool fix = true;
                                if (LimitToTags.Count>0)
                                    fix = LimitToTags.Contains(mu.tag);
                                if (fix)   
                                    MUSEntered.Add(mu);
                                
                            }
                        }
                    }
                }
                else
                {
                    MUSEntered.Clear();
                }
            }
            
            if (FixMU  && !checkrelease)
                Fix();
            if (!FixMU && !checkfix && MUSFixed.Count>0 && !signalfixerfixnotnull)
                CheckRelease();
      
            
            if (OneBitFix)
            {
                // One Bit fixer - Fix = true fixes and false = releases - only on signal change
                if (signalfixerfixnotnull)
                {
                    if (FixerFix.Value && !lastfix && !checkrelease)
                    {
                        Fix();
                    }
                     
                    if (!FixerFix.Value && lastfix)
                        CheckRelease();
                    lastfix = FixerFix.Value;
                }
            }
            else
            {
                // Two Bit fixer
                if (signalfixerreleasenotnull)
                {
                    if (FixerRelease.Value && lastrelease == false)
                        CheckRelease();
                    lastrelease = FixerRelease.Value;
                }
                
                if (signalfixerfixnotnull)
                {
                    if (FixerFix.Value  && lastfix == false && !checkrelease)
                        Fix();
                    lastfix = FixerFix.Value;
                }
            }

            if (AlignAndFixOnMinDistance)
            {
                foreach (var mu in MUSEntered.ToArray())
                {
                    var distance = GetDistance(mu);
                    if (distance > mu.FixerLastDistance && mu.FixerLastDistance != -1 )
                    {
                        if (DebugMode)
                            Debug.Log("DebugMode Fixer - AlignAndFixOnMinDistance - Mindistance reached " + mu.name);
                        AtPosition(mu);
                    }
                    mu.FixerLastDistance = distance;
                }
            }
            else
            {
                   CheckEntered();
            }

            if (meshrenderernotnull)
            {
                if (ShowStatus && !UseRayCast && MUSFixed.Count == 0)
                    meshrenderer.material.color = new Color(1,0,0,StatusOpacity);
                else
                    meshrenderer.material.color = new Color(0,1,0,StatusOpacity);
            }
        }
    }
}