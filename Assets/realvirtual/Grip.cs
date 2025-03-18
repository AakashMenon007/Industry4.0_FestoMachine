// realvirtual.io (formerly game4automation) (R) a Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 realvirtual GmbH - Usage of this source code only allowed based on License conditions see https://realvirtual.io/unternehmen/lizenz  

using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

namespace realvirtual
{
    [System.Serializable]
    public class EventMUGrip : UnityEvent<MU, bool>
    {
    }

    [SelectionBase]
    [RequireComponent(typeof(Rigidbody))]
    //! Grip is used for fixing MUs to components which are moved by Drives.
    //! The MUs can be gripped as Sub-Components or with Rigid Bodies.
    [HelpURL("https://doc.realvirtual.io/components-and-scripts/grip")]
    public class Grip : BaseGrip, IFix
    {
        [Header("Kinematic")] public Sensor PartToGrip; //!< Identifies the MU to be gripped.

        public bool
            DirectlyGrip = false; //!< If set to true the MU is directly gripped when Sensor PartToGrip detects a Part

        public GameObject PickAlignWithObject; //!<  If not null the MUs are aligned with this object before picking.
        public GameObject PlaceAlignWithObject; //!<  If not null the MUs are aligned with this object after placing.

        [Tooltip("Should be usually kept empty, for very special cases where joint should be used for gripping")] public UnityEngine.Joint
            ConnectToJoint; //< Should be usually kept empty, for very special cases where joint should be used for gripping

        public Sensor PickBasedOnSensor; //!< Picking is started when this sensor is occupied (optional)
        public Drive_Cylinder PickBasedOnCylinder; //!< Picking is stared when Cylinder is Max or Min (optional)
        public bool PickOnCylinderMax; //!< Picking is started when Cylinderis Max
        public bool NoPhysicsWhenPlaced = false; //!< Object remains kinematic (no phyisics) when placed

        public bool
            PlaceLoadOnMU = false; //!<  When placing the components they should be loaded onto an MU as subcomponent.

        public Sensor PlaceLoadOnMUSensor; //!<  Sensor defining the MU where the picked MUs should be loaded to.

        [Header("Pick & Place Control")]
        public bool PickObjects = false; //!< true for picking MUs identified by the sensor.

        public bool PlaceObjects = false; //!< //!< true for placing the loaded MUs.

        [Header("Events")]
        public EventMUGrip
            EventMUGrip; //!<  Unity event which is called for MU grip and ungrip. On grip it passes MU and true. On ungrip it passes MU and false.
        
        [Header("PLC IOs")] 
        public bool OneBitControl = false; //!< If true the grip is controlled by one bit. If false the grip is controlled by two bits.
        public PLCOutputBool SignalPick;
        [HideIf("OneBitControl")]public PLCOutputBool SignalPlace;
        
        [HideInInspector] public List<GameObject> PickedMUs;

        private bool _issignalpicknotnull;
        private bool _issignalplacenotnull;
        private bool Deactivated = false;
        private bool _pickobjectsbefore = false;
        private bool _placeobjectsbefore = false;
        private List<FixedJoint> _fixedjoints;
        
        //! Picks the GameObject obj
        public void DeActivate(bool activate)
        {
            Deactivated = activate;
        }

        //! Picks the GameObject obj
        public void Fix(MU mu)
        {
            if (Deactivated)
                return;
            
            var obj = mu.gameObject;
            if (PickedMUs.Contains(obj) == false)
            {
                if (mu == null)
                {
                    ErrorMessage("MUs which should be picked need to have the MU script attached!");
                    return;
                }

                if (ConnectToJoint == null)
                    mu.Fix(this.gameObject);
                
                if (PickAlignWithObject != null)
                {
                    obj.transform.position = PickAlignWithObject.transform.position;
                    obj.transform.rotation = PickAlignWithObject.transform.rotation;
                }

                if (ConnectToJoint != null)
                    ConnectToJoint.connectedBody = mu.Rigidbody;

                PickedMUs.Add(obj);
                if (EventMUGrip != null)
                    EventMUGrip.Invoke(mu, true);
            }
        }

        //! Places the GameObject obj
        public void Unfix(MU mu)
        {
            if (Deactivated)
                return;
            
            var obj = mu.gameObject;
            var tmpfixedjoints = _fixedjoints;
            var rb = mu.Rigidbody;
            if (EventMUGrip != null)
                EventMUGrip.Invoke(mu, false);

            if (PlaceAlignWithObject != null)
            {
                obj.transform.position = PlaceAlignWithObject.transform.position;
                obj.transform.rotation = PlaceAlignWithObject.transform.rotation;
            }

            if (ConnectToJoint == null)
              mu.Unfix();
            
            if (ConnectToJoint != null)
                ConnectToJoint.connectedBody = null;
            
            if (PlaceLoadOnMUSensor == null)
            {
                if (!NoPhysicsWhenPlaced)
                    if (rb!=null)
                       rb.isKinematic = false;
                    else
                        Warning("No Rigidbody for MU which is unfixed",this);
            }

            if (PlaceLoadOnMUSensor != null)
            {
                var loadmu = PlaceLoadOnMUSensor.LastTriggeredBy.GetComponent<MU>();
                if (loadmu == null)
                {
                    ErrorMessage("You can only load parts on parts which are of type MU, please add to part [" +
                                 PlaceLoadOnMUSensor.LastTriggeredBy.name + "] MU script");
                }

                loadmu.LoadMu(mu);
            }

            PickedMUs.Remove(obj);
        }

        //! Picks al objects collding with the Sensor
        public void Pick()
        {
            if (Deactivated)
                return;

            if (PartToGrip != null)
            {
                // Attach all objects with fixed joint - if not already attached
                foreach (GameObject obj in PartToGrip.CollidingObjects)
                {
                    var pickobj = GetTopOfMu(obj);
                    if (pickobj == null)
                        Warning("No MU on object for gripping detected", obj);
                    else
                        Fix(pickobj);
                }
            }
            else
            {
                ErrorMessage(
                    "Grip needs to define with a Sensor which parts to grip - no [Part to Grip] Sensor is defined");
            }
        }

        //! Places all objects
        public void Place()
        {
            if (Deactivated)
                return;
            
            var tmppicked = PickedMUs.ToArray();
            foreach (var mu in tmppicked)
            {
                Unfix(mu.GetComponent<MU>());
            }
        }

        private void Reset()
        {
            GetComponent<Rigidbody>().isKinematic = true;
        }

        // Use this for initialization
        private void Start()
        {
            PickedMUs = new List<GameObject>();
            _issignalpicknotnull = SignalPick != null;
            _issignalplacenotnull = SignalPlace != null;
            if (PartToGrip == null)
            {
                Error("Grip Object needs to be connected with a sensor to identify objects to pick", this);
            }

            _fixedjoints = new List<FixedJoint>();
            GetComponent<Rigidbody>().isKinematic = true;

            if (PickBasedOnSensor != null)
            {
                PickBasedOnSensor.EventEnter += PickBasedOnSensorOnEventEnter;
            }

            if (DirectlyGrip == true)
            {
                PartToGrip.EventEnter += PickBasedOnSensorOnEventEnter;
            }

            if (PickBasedOnSensor != null)
            {
                PickBasedOnSensor.EventExit += PickBasedOnSensorOnEventExit;
            }


            if (PickBasedOnCylinder != null)
            {
                if (PickOnCylinderMax)
                {
                    PickBasedOnCylinder.EventOnMin += Place;
                    PickBasedOnCylinder.EventOnMax += Pick;
                }
                else
                {
                    PickBasedOnCylinder.EventOnMin += Pick;
                    PickBasedOnCylinder.EventOnMax += Place;
                }
            }
        }

        private void PickBasedOnSensorOnEventExit(GameObject obj)
        {
            var mu = obj.GetComponent<MU>();
            if (mu != null)
                Unfix(mu);
        }

        private void PickBasedOnSensorOnEventEnter(GameObject obj)
        {
            var mu = obj.GetComponent<MU>();
            if (mu != null)
                Fix(mu);
        }


        private void FixedUpdate()
        {
            if (Deactivated)
                return;
            
            if (_issignalpicknotnull)
            {
                PickObjects = SignalPick.Value;
            }

            if (_issignalplacenotnull)
            {
                PlaceObjects = SignalPlace.Value;
            }
            
            if (OneBitControl)
                PlaceObjects = !PickObjects;
            
            if (_pickobjectsbefore == false && PickObjects)
            {
                Pick();
            }

            if (_placeobjectsbefore == false && PlaceObjects)
            {
                Place();
            }

            _pickobjectsbefore = PickObjects;
            _placeobjectsbefore = PlaceObjects;
        }
    }
}