// realvirtual (R) Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 realvirtual GmbH - Usage of this source code only allowed based on License conditions see https://realvirtual.io/en/company/license

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using NaughtyAttributes;
namespace realvirtual
{
    [System.Serializable]
    public class Game4AutomationEventMUSensor : UnityEvent<MU, bool>
    {
    }

    [System.Serializable]
    public class Game4AutomationEventGameobjectSensor : UnityEvent<GameObject, bool>
    {
    }


    [SelectionBase]
    //! The sensor is used for detecting MUs.
    //! Sensors are using Box Colliders for detecting MUs. The Sensor should be on Layer *g4aSensor* if the standard Game4Automation
    //! Layer settings are used. A behavior component (e.g. *Sensor_Standard*) must be added to the Sensor for providing connection to PLCs Input and 
    //! outputs.
    [HelpURL("https://doc.realvirtual.io/components-and-scripts/sensor")]
    [ExecuteInEditMode]
    public class Sensor : BaseSensor, ISignalInterface
    {
        // Public - UI Variables 
        [BoxGroup("Settings")]  public bool DisplayStatus = true; 
        [BoxGroup("Settings")] public string
            LimitSensorToTag; //!< Limits the function of the sensor to a certain MU tag - also MU names are working
        [BoxGroup("Settings")] public bool UseRaycast = false;

        [BoxGroup("Settings")] [ShowIf("UseRaycast")] public Vector3 RayCastDirection = new Vector3(1,0,0);
        [BoxGroup("Settings")] [ShowIf("UseRaycast")] public float RayCastLength=1000f;
        [BoxGroup("Settings")] [ShowIf("UseRaycast")] public float RayCastDisplayWidth = 0.01f;
        [BoxGroup("Settings")] [ShowIf("UseRaycast")] [ReorderableList] public List<string> AdditionalRayCastLayers;
        [BoxGroup("Settings")] [ShowIf("UseRaycast")]  public bool ShowSensorLinerenderer = true;
      

       //!<  Display the status of the sensor by changing the material (color).
        [BoxGroup("Settings")] public Material MaterialOccupied; //!<  Material for displaying the occupied status.
        [BoxGroup("Settings")] public Material MaterialNotOccupied; //!<  Material for displaying the not occupied status.
        [BoxGroup("Settings")] public bool PauseOnSensor = false; //!<  Pause simulation if sensor is getting high - can be used for debuging
        [BoxGroup("Interface Connection")] public PLCInputBool SensorOccupied; //! Boolean PLC input for the Sensor signal.

      
        private bool _isOccupiedNotNull;
        
        [Foldout("Events")] public Game4AutomationEventMUSensor
            EventMUSensor; //!<  Unity event which is called for MU enter and exit. On enter it passes MU and true. On exit it passes MU and false.
        [Foldout("Events")]  public Game4AutomationEventGameobjectSensor    
        EventNonMUGameObjectSensor; //!<  Unity event which is called for non MU objects enter and exit. On enter it passes gameobject (on which the collider was detected) and true. On exit it passes gameobject and false.

        [Foldout("Status")] public bool Occupied = false; //!<  True if sensor is occupied.
        [Foldout("Status")] public GameObject LastTriggeredBy; //!< Last MU which has triggered the sensor.
        [Foldout("Status")] [ShowIf("UseRaycast")] public float RayCastDistance; //!< Last RayCast Distance if Raycast is used
        [Foldout("Status")] public int LastTriggeredID; //!< Last MUID which has triggered the sensor.
        [Foldout("Status")] public int LastTriggeredGlobalID; //!<  Last GloabalID which has triggerd the sensor.
        [Foldout("Status")] public int Counter;
        [Foldout("Status")] public List<MU> CollidingMus; // Currently colliding MUs with the sensor.
        [Foldout("Status")] public List<GameObject>
            CollidingObjects; // Currently colliding GameObjects with the sensor (which can be more than MU because a MU can contain several GameObjects.


        public delegate void
            OnEnterDelegate(GameObject obj); //!< Delegate function for GameObjects entering the Sensor.

        public event OnEnterDelegate EventEnter;

        public delegate void OnExitDelegate(GameObject obj); //!< Delegate function for GameObjects leaving the Sensor.

        public event OnExitDelegate EventExit;


        // Private Variables
        private bool _occupied = false;
        private MeshRenderer _meshrenderer;
        private BoxCollider _boxcollider;
        private int layermask;
        [SerializeField] [HideInInspector] private float scale = 1000;
        private RaycastHit hit;
        private RaycastHit lasthit;
        private bool raycasthasthit;
        private bool lastraycasthasthit;
        private bool raycasthitchanged;
        private Vector3 startposraycast;
        private Vector3 endposraycast;
        
        private LineRenderer linerenderer;
        
        //! Delete all MUs in Sensor Area.
        public void DeleteMUs()
        {
            var tmpcolliding = CollidingObjects;
            foreach (var obj in tmpcolliding.ToArray())
            {
                var mu = GetTopOfMu(obj);
                if (mu != null)
                {
                    Destroy(mu.gameObject);
                }

                CollidingObjects.Remove(obj);
            }
        }


        // Use this when Script is inserted or Reset is pressed
        private void Reset()
        {
            AdditionalRayCastLayers = new List<string>();
            AdditionalRayCastLayers.Add("rvMU");
            AdditionalRayCastLayers.Add("rvMUSensor");
            if (MaterialOccupied == null)
            {
                MaterialOccupied = UnityEngine.Resources.Load("Materials/SensorOccupiedRed", typeof(Material)) as Material;
            }

            if (MaterialNotOccupied == null)
            {
                MaterialNotOccupied = UnityEngine.Resources.Load("Materials/SensorNotOccupied", typeof(Material)) as Material;
            }
    
            _boxcollider = GetComponent<BoxCollider>();
            if (_boxcollider != null)
                _boxcollider.isTrigger = true;
            else
                UseRaycast = true;
        }

        // Use this for initialization
        private void Start()
        {
            _isOccupiedNotNull = SensorOccupied != null;
            CollidingObjects = new List<GameObject>();
            CollidingMus = new List<MU>();
            if (LimitSensorToTag == null)
                LimitSensorToTag = "";
            _boxcollider = GetComponent<BoxCollider>();
            if (_boxcollider != null )
            {
                _meshrenderer = _boxcollider.gameObject.GetComponent<MeshRenderer>();
            }

            if (_boxcollider == null && !UseRaycast && Application.isPlaying)
            {
                ErrorMessage("Sensors which are not using a Raycast need to have a BoxCollider on the same Gameobject as this Sensor script is attached to");
            }
     
            if (Application.isPlaying)
            { 
                scale = realvirtualController.Scale;
                AdditionalRayCastLayers.Add(LayerMask.LayerToName(gameObject.layer));
                // create line renderer for raycast if not existing

                if (UseRaycast && ShowSensorLinerenderer)
                {
                    linerenderer = GetComponent<LineRenderer>();
                    if (linerenderer == null)
                        linerenderer = gameObject.AddComponent<LineRenderer>();
                }
                
            }

            if (AdditionalRayCastLayers == null)
                AdditionalRayCastLayers = new List<string>();
            layermask = LayerMask.GetMask(AdditionalRayCastLayers.ToArray());
            ShowStatus();
        }

        private void DrawLine()
        {
            if (ShowSensorLinerenderer)
            {
                List<Vector3> pos = new List<Vector3>();
                pos.Add(startposraycast);
                pos.Add(endposraycast);
                linerenderer.startWidth = RayCastDisplayWidth;
                linerenderer.endWidth = RayCastDisplayWidth;
                linerenderer.SetPositions(pos.ToArray());
                linerenderer.useWorldSpace = true;
                if (raycasthasthit)
                {
                    linerenderer.material = MaterialOccupied;
                }
                else
                {
                    linerenderer.material = MaterialNotOccupied;
                }
            }
        }
        
        private void Raycast()
        {
            if (!Application.isPlaying)
            {
                var list = new List<string>(AdditionalRayCastLayers);
                list.Add(LayerMask.LayerToName(gameObject.layer));
                layermask = LayerMask.GetMask(list.ToArray());
            }

            float scale = 1000;
            raycasthitchanged = false;
            var globaldir = transform.TransformDirection(RayCastDirection);
            var display = Vector3.Normalize(globaldir) * RayCastLength / scale;
            startposraycast = transform.position;
            if (Physics.Raycast(transform.position, globaldir, out hit, RayCastLength/scale, layermask))
            {
                var dir = Vector3.Normalize(globaldir) * hit.distance;
                if (Application.isPlaying)
                    scale = realvirtualController.Scale;

                RayCastDistance = hit.distance * scale;
                if (DisplayStatus) Debug.DrawRay(transform.position, dir, Color.red,0,true);
                raycasthasthit = true;
                if (hit.collider != lasthit.collider)
                    raycasthitchanged = true;
                endposraycast = startposraycast + dir;
            }
            else
            {
                if (DisplayStatus) Debug.DrawRay(transform.position, display, Color.yellow,0,true);
                raycasthasthit = false;
                endposraycast = startposraycast + display;
                RayCastDistance = 0;
            }

        }

        // Shows Status of Sensor
        private void ShowStatus()
        {
          
            if (CollidingObjects.Count == 0)
            {
                LastTriggeredBy = null;
                LastTriggeredID = 0;
                LastTriggeredGlobalID = 0;
            }
            else
            {
                GameObject obj = CollidingObjects[CollidingObjects.Count - 1];
                if (!ReferenceEquals(obj, null))
                {
                    var LastTriggeredByMU = GetTopOfMu(obj);
                    if (!ReferenceEquals(LastTriggeredByMU, null))
                        LastTriggeredBy = LastTriggeredByMU.gameObject;
                    else
                        LastTriggeredBy = obj;

                    if (LastTriggeredByMU != null)
                    {
                        LastTriggeredID = LastTriggeredByMU.ID;
                        LastTriggeredGlobalID = LastTriggeredByMU.GlobalID;
                    }
                }
            }

            if (CollidingObjects.Count > 0)
            {
                _occupied = true;
                if (DisplayStatus && _meshrenderer != null)
                {
                    _meshrenderer.material = MaterialOccupied;
                }
            }
            else
            {
                _occupied = false;
                if (DisplayStatus && _meshrenderer != null)
                {
                    _meshrenderer.material = MaterialNotOccupied;
                }
            }

            Occupied = _occupied;
        }

        // ON Collission Enter
        private void OnTriggerEnter(Collider other)
        {
            GameObject obj = other.gameObject;
            var tmpcolliding = CollidingObjects;
            var muobj = GetTopOfMu(obj);

            if ((LimitSensorToTag == "" || ((muobj.tag == LimitSensorToTag) || muobj.Name == LimitSensorToTag)))
            {
                if (PauseOnSensor)
                    Debug.Break();
                if (!CollidingObjects.Contains(obj))
                    CollidingObjects.Add(obj);
            
            
                ShowStatus();
                
                if (muobj != null)
                {
                    if (!CollidingMus.Contains(muobj))
                    {
                        if (EventEnter != null)
                            EventEnter(muobj.gameObject);
                        Counter++;
                        muobj.EventMUEnterSensor(this);
                        CollidingMus.Add(muobj);
                        if (EventMUSensor!=null)
                            EventMUSensor.Invoke(muobj, true);
                    }
                }
                else
                {
                    if (EventEnter != null)
                        EventEnter(obj);
                    if (EventNonMUGameObjectSensor!=null)
                      EventNonMUGameObjectSensor.Invoke(obj,true);
                }
            }
        }
        
        public void OnMUPartsDestroyed(GameObject obj)
        {
            CollidingObjects.Remove(obj);
        }

        public void OnMUDelete(MU muobj)
        {
            
            CollidingObjects.Remove(muobj.gameObject);

            // Check if remaining colliding objects belong to same mu
            var coolliding = CollidingObjects.ToArray();
            var i = 0;
            do
            {
                if (i < coolliding.Length)
                {
                    var thismuobj = GetTopOfMu(coolliding[i]);
                    if (thismuobj == muobj)
                    {
                        CollidingObjects.Remove(coolliding[i]);
                    }
                }

                i++;
            } while (i < coolliding.Length);
            CollidingMus.Remove(muobj);
            if (EventExit != null)
                EventExit(muobj.gameObject);
            if (EventMUSensor!= null)
                  EventMUSensor.Invoke(muobj, false);
            muobj.EventMUExitSensor(this);
            LastTriggeredBy = null;
            LastTriggeredID = 0;
            LastTriggeredGlobalID = 0;
            ShowStatus();
        }
        


        // ON Collission Exit
        private void OnTriggerExit(Collider other)
        {
            GameObject obj = other.gameObject;
            if (!ReferenceEquals(obj, null))
            {
                
                var muobj = GetTopOfMu(obj);
                var tmpcolliding = CollidingObjects;
                var dontdelete = false;
                CollidingObjects.Remove(obj);

                // Check if remaining colliding objects belong to same mu
                foreach (var thisobj in CollidingObjects)
                {
                    var thismuobj = GetTopOfMu(thisobj);
                    if (thismuobj == muobj)
                    {
                        dontdelete = true;
                    }
                }

                if (!dontdelete)
                {
               
                    if (muobj != null && CollidingMus.Contains(muobj))
                    {
                        CollidingMus.Remove(muobj);
                        if (EventExit != null)
                            EventExit(muobj.gameObject);
                        if (EventMUSensor!=null)
                             EventMUSensor.Invoke(muobj, false);
                        muobj.EventMUExitSensor(this);
                    }
                    else
                    {
                        if (EventNonMUGameObjectSensor!=null)
                            EventNonMUGameObjectSensor.Invoke(obj,false);
                        if (EventExit != null)
                            EventExit(obj);
                    }
                }
                ShowStatus();
            }
        }

        private void FixedUpdate()
        {
            if (Application.isPlaying && UseRaycast)
            {
                Raycast();
                
                // last raycast has left
                if ((lastraycasthasthit && !raycasthasthit)|| raycasthitchanged) 
                {
                    if (lasthit.collider!=null)
                       OnTriggerExit(lasthit.collider);
                }
                
                if ((raycasthasthit && !lastraycasthasthit) || raycasthitchanged)
                {
                    // new raycast hit
                    OnTriggerEnter(hit.collider);
                }

                lastraycasthasthit = raycasthasthit;
                lasthit = hit;

            }
            if (Application.isPlaying)
            // Set external PLC Outputs
               if (_isOccupiedNotNull)
                        SensorOccupied.Value = Occupied;
        }

        private void Update()
        {
            if (!Application.isPlaying && UseRaycast)
            {
                layermask = LayerMask.GetMask(AdditionalRayCastLayers.ToArray());
                Raycast();
            }

            if (Application.isPlaying && UseRaycast && DisplayStatus)
            {
                DrawLine();
            }
        }
    }
}