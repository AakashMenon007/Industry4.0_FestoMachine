// realvirtual (R) Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 realvirtual GmbH - Usage of this source code only allowed based on License conditions see https://realvirtual.io/en/company/license

using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
#if REALVIRTUAL_AGX
using Mesh = AGXUnity.Collide.Mesh;
using AGXUnity;
#endif

namespace realvirtual
{
    [HelpURL("https://doc.realvirtual.io/components-and-scripts/sink")]
    [SelectionBase]
    #if !REALVIRTUAL_AGX
    [RequireComponent(typeof(BoxCollider))]
    #endif
    //! Sink to destroy objects in the scene
    public class Sink : realvirtualBehavior
    {
#if REALVIRTUAL_AGX
        public bool UseAGXPhysics;
#else
        [HideInInspector] public bool UseAGXPhysics = false;
#endif
        // Public - UI Variables 
        [Header("Settings")] public bool DeleteMus = true; //!< Delete MUs
        [ShowIf("DeleteMus")] public bool Dissolve = true; //!< Dissolve MUs
        [ShowIf("DeleteMus")] public string DeleteOnlyTag; //!< Delete only MUs with defined Tag
        [ShowIf("DeleteMus")] public float DestroyFadeTime=0.5f; //!< Time to fade out MU
        [Header("Sink IO's")] public PLCOutputBool Delete; //!< PLC output for deleting MUs
        private bool _lastdeletemus = false;
    
        [Header("Status")] 
        [ReadOnly] public float SumDestroyed; //!< Sum of destroyed objects
        [ReadOnly] public float DestroyedPerHour; //!< Sum of destroyed objects per Hour
        [ReadOnly] public List<GameObject> CollidingObjects; //!< Currently colliding objects

        public SinkEventOnDestroy OnMUDelete;
        
        private bool _isDeleteNotNull;

        // Use this when Script is inserted or Reset is pressed
        private void Reset()
        {
            GetComponent<BoxCollider>().isTrigger = true;
        }    
    
        // Use this for initialization
        private void Start()
        {
            _isDeleteNotNull = Delete != null;
#if REALVIRTUAL_AGX
            if (UseAGXPhysics)
            {
                var body = this.GetComponent<AGXUnity.Collide.Box>();
                if (body == null)
                {
                    Error ("Sink using AGX: Expecting an AGX Box Shape Collider component with Collissions Enabled and IsSensor", this);
                    return;
                }
                body.CollisionsEnabled = true;
                body.IsSensor = true;
                Simulation.Instance.ContactCallbacks.OnContact(OnContact,body);
            }
#endif
        }

        public void DeleteMUs()
        {
            
            var tmpcolliding = CollidingObjects;
            foreach (var obj in tmpcolliding.ToArray())
            {
                var mu = GetTopOfMu(obj);
                if (mu != null)
                {
                    if (DeleteOnlyTag == "" || (mu.gameObject.tag == DeleteOnlyTag))
                    {

                        OnMUDelete.Invoke(mu);
                        if (!Dissolve)
                             Destroy(mu.gameObject);
                        else
                            mu.Dissolve(DestroyFadeTime);
                        SumDestroyed++;
                    }
                }

                CollidingObjects.Remove(obj);
            }
        }
    
        // ON Collission Enter
        private void OnTriggerEnter(Collider other)
        {
            GameObject obj = other.gameObject;
            SensorEnter(obj);
        }

        private void SensorEnter(GameObject obj)
        {
            CollidingObjects.Add(obj);
            if (DeleteMus==true)
            {
                // Act as Sink
                DeleteMUs();
            }
        }
    
        // ON Collission Exit
        private void OnTriggerExit(Collider other)
        {
            GameObject obj = other.gameObject;
            CollidingObjects.Remove(obj);
        }
        
        #if REALVIRTUAL_AGX
        private bool OnContact(ref ContactData data)
        {
            var obj = data.Component1.gameObject;
            SensorEnter(obj);
            return false;
        }
        #endif
        private void Update()
        {
            DestroyedPerHour = SumDestroyed / (Time.time / 3600);
            if (_isDeleteNotNull)
            {
                DeleteMus = Delete.Value;
            }
        
            if (DeleteMus && !_lastdeletemus)
            {
                DeleteMUs();
            }
            _lastdeletemus = DeleteMus;

        }
    }
}