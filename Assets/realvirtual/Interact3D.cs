// realvirtual (R) Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 realvirtual GmbH - Usage of this source code only allowed based on License conditions see https://realvirtual.io/en/company/license

using UnityEngine;
using UnityEditor;
using NaughtyAttributes;

namespace realvirtual
   
{
#pragma warning disable 0108
    //! Adds buttons or Lights which can be connected to Signals tp a 3D scene
    public class Interact3D : BehaviorInterface
    {
        [Header("Status")] [OnValueChanged("UpdateVisual")]
        public bool On;  //!< Status On

        [OnValueChanged("UpdateVisual")] public bool MouseDown; //!< Mouse is down
        [OnValueChanged("UpdateVisual")] public bool PLCOn;  //!< PLC Signal is ON
        [OnValueChanged("UpdateVisual")]  public bool Blocked; //< true if interaction is blocked by a PLCSignal (e.g. for security doors)
        [Header("Settings")] public bool Switch; //!< true if interaction should work like a switch, if false it works like a button
        
        public Material MaterialOn;  //!< Material which should be used for the ON status of the Switch or Button
        public Material MaterialOnMouseDown; //!< Material which should be used on mouse down
        public Material MaterialOnBlocked; //!< Material which should be used if interaction is blocked by a PLCSignal
        public float DurationMaterialOnBlocked;  //!< Duration of visual feedback with MaterialOnBlocked. If user is pressing button and it is blocked by PLC it will be visualized
        public Material MaterialPLCOn;  //!< Material which should be used if PLC Output signal SignalOn is true
        public Light LightOn; //!< Optional light which is turned on on ON status
        public Light LightPLCOn; //!< Optional light which is turned on if PLC Output signal SignalOn is true


        [Header("PLC IOs")] public PLCInputBool SignalIsOn; //!< PLCInput if button or switch status is on
        public PLCOutputBool SignalOn; //!< PLCOutput to turn PLCOn Status on
        public PLCOutputBool SignalBlocked; //!< PLCOutput to block interaction with button


        private  Collider collider;
        private Material standardmaterial;

        private  MeshRenderer renderer;

        // Start is called before the first frame update
        private bool signalisonnotnull;
        private bool signalblockendnotnull;
        private bool signalonnutnull;
        private bool lastsginalon;
        
#if UNITY_EDITOR
        void OnScene(SceneView scene)
        {
            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 2)
            {

                Vector3 mousePos = e.mousePosition;

                Ray ray = scene.camera.ScreenPointToRay(mousePos);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit))
                {
                    if (hit.collider == collider)
                    {
                      
                        OnMouseDown();
                        e.Use();
                    }
                }
            }

            if (e.type == EventType.MouseUp && e.button == 2)
            {
            
                OnMouseUp();
            }

        }
#endif
        new void  Awake()
        {
            #if UNITY_EDITOR
            SceneView.duringSceneGui += OnScene;
            #endif
            collider = GetComponent<Collider>();
            if (collider == null)
                collider = gameObject.AddComponent<MeshCollider>();
            renderer = GetComponent<MeshRenderer>();
            if (renderer != null)
                standardmaterial = renderer.material;
            signalisonnotnull = SignalIsOn != null;
            signalblockendnotnull = SignalBlocked != null;
            signalonnutnull = SignalOn != null;
            base.Awake();
        }

        private void OnDestroy()
        {
#if UNITY_EDITOR
            SceneView.duringSceneGui -= OnScene;
#endif
        }

        private void ChangeMaterial(Material newmaterial)
        {
            if (renderer != null)
                if (standardmaterial != null)
                    if (newmaterial != null)
                        renderer.material = newmaterial;
        }

        private void UpdateVisual()
        {
            if (!On)
                ChangeMaterial(standardmaterial);
            if (On)
                ChangeMaterial(MaterialOn);
            if (MouseDown)
                ChangeMaterial(MaterialOnMouseDown);
            if (PLCOn)
                ChangeMaterial(MaterialPLCOn);

            if (LightOn != null)
                LightOn.enabled = On;
            if (LightPLCOn != null)
                LightPLCOn.enabled = PLCOn;
            var halo = GetComponent("Halo");
            if (halo != null)
            {
                if (On || PLCOn)
                    halo.GetType().GetProperty("enabled").SetValue(halo, true, null);
                else
                    halo.GetType().GetProperty("enabled").SetValue(halo, false, null);
            }

        }

        private void Start()
        {
            UpdateVisual();
        }

        private void OnMouseDown()
        {
            if (!On && Blocked)
            {
                ChangeMaterial(MaterialOnBlocked);
                Invoke("UpdateVisual", DurationMaterialOnBlocked);
                return;
            }

            if (Switch)
                On = !On;
            if (!Switch)
                On = true;
            MouseDown = true;
            UpdateVisual();
        }

        private void OnMouseUp()
        {
            if (!On && Blocked)
                return;

            if (!Switch)
                On = false;
            MouseDown = false;
            UpdateVisual();
        }

        private void FixedUpdate()
        {
            if (signalisonnotnull)
            {
                SignalIsOn.Value = On;
            }

            if (signalonnutnull)
            {
                PLCOn = SignalOn.Value;
                if (SignalOn.Value != lastsginalon)
                {
                    UpdateVisual();
                }

                lastsginalon = SignalOn.Value;
            }

            if (signalblockendnotnull)
            {
                if (SignalBlocked.Value != Blocked)
                {
                    Blocked = SignalBlocked.Value;
                    UpdateVisual();
                }
            }
            
        }
    }
}
