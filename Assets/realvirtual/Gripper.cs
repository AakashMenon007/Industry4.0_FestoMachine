// realvirtual (R) Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 realvirtual GmbH - Usage of this source code only allowed based on License conditions see https://realvirtual.io/en/company/license

using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

namespace realvirtual
{
    [HelpURL("https://doc.realvirtual.io/components-and-scripts/gripper")]
    //! The Gripper is a full Gripper controll. Movements of fingers, Gripping and connection to PLCSignals is included. It is a more "all in on" Gripping function instead
    //! of moving the Fingers with Drives and defining manually Grip and needed Sensors. The fingers are stopping automaticlly closing as soon as the MU is touched. Detection
    //! of MU and close position is done with a RayCast Sensor
    public class Gripper : BehaviorInterface
    {
        [Tooltip("Keep empty if it is named Left")]
        public GameObject LeftFinger;

        [Tooltip("Keep empty if it is named Right")]
        public GameObject RightFinger;

        public float TimeOpening;
        public float TimeClosing;
        
        public float GripperWidth;
        public float OpenPosOffset;
        public Vector3 DirectionFinger = new Vector3(1, 0, 0);
        public Vector3 DirectionClosing = new Vector3(1, 0, 0);

        [Header("Status")] [ReadOnly] public bool FullyClosed;
        public bool Close;
        [ReadOnly] public bool Closing;
        [ReadOnly] public bool FullyOpened;
        public bool Open;
        [ReadOnly] public bool Opening;
        [ReadOnly] public bool MUIsGripped;
        [ReadOnly] public MU GrippedMU;


        [Header("PLC IOs")] public PLCOutputBool CloseGripper;
        public PLCOutputBool OpenGripper;
        public PLCInputBool IsClosing;
        public PLCInputBool IsOpening;
        public PLCInputBool IsFullyOpened;
        public PLCInputBool IsFullyClosed;


        private GameObject leftfinger;
        private GameObject rightfinger;
        private Sensor RayCastSensor;
        private bool nofingers;
        private Rigidbody rbright, rbleft;
        private bool isclosingnotnull;
        private bool isopeningnotnull;
        private bool isfullyopenednotnull;
        private bool isfullyclosednotnull;
        private bool isclosegrippernotnull;
        private bool isopengrippernotnull;
        private bool grippedonclosing;
        private Vector3 leftstartpos;
        private Vector3 rightstartpos;
        private float posrel;
        private float posabs;
        private Grip grip;
        private float gripdistancerel;
        public bool usefingers;
        private float fingerposright, fingerposleft;
        private MU muingripper;
    

        new void Awake()
        {
            // Create and Reference Gripper Components
            if (LeftFinger == null)
            {
                leftfinger = GetChildByName("Left");
            }
            else
            {
                leftfinger = LeftFinger;
            }

            if (RightFinger == null)
            {
                rightfinger = GetChildByName("Right");
            }
            else
            {
                rightfinger = RightFinger;
            }

            nofingers = leftfinger == null;
            this.gameObject.layer = LayerMask.NameToLayer("rvMU");
            if (nofingers)
            {
                RayCastSensor = gameObject.AddComponent<Sensor>();
                RayCastSensor.UseRaycast = true;
                RayCastSensor.RayCastDirection = DirectionFinger;
                RayCastSensor.RayCastLength = GripperWidth;
            }
            else
            {
                RayCastSensor = leftfinger.AddComponent<Sensor>();
                RayCastSensor.UseRaycast = true;
                var globdir = transform.TransformDirection(DirectionClosing);
                RayCastSensor.RayCastDirection = RayCastSensor.transform.InverseTransformDirection(globdir);
                RayCastSensor.RayCastLength = GripperWidth / 5;
            }

            if (rightfinger != null)
            {
                rbright = Global.AddComponentIfNotExisting<Rigidbody>(rightfinger);
                rbright.isKinematic = true;
                rbright.useGravity = false;
            }

            if (leftfinger != null)
            {
                rbleft = Global.AddComponentIfNotExisting<Rigidbody>(leftfinger);
                rbleft.isKinematic = true;
                rbleft.useGravity = false;
            }
            
            RayCastSensor.ShowSensorLinerenderer = false;
            RayCastSensor.AdditionalRayCastLayers = new List<string>();
            RayCastSensor.AdditionalRayCastLayers.Add("rvMUSensor");
            RayCastSensor.AdditionalRayCastLayers.Add("rvMU");
            isclosingnotnull = IsClosing != null;
            isopeningnotnull = IsOpening != null;
            isfullyopenednotnull = IsFullyOpened != null;
            isfullyclosednotnull = IsFullyClosed != null;
            isclosegrippernotnull = CloseGripper != null;
            isopengrippernotnull = OpenGripper != null;

            if (leftfinger != null && rightfinger != null)
            {
                leftstartpos = leftfinger.transform.localPosition;
                rightstartpos = rightfinger.transform.localPosition;
                usefingers = true;
            }
            else
            {
                usefingers = false;
            }

            DirectionClosing = Vector3.Normalize(DirectionClosing);
            
            RayCastSensor.EventEnter += RayCastSensorOnEventEnter;
            RayCastSensor.EventExit += RayCastSensorOnEventExit;
            
            // Grip
            grip = Global.AddComponentIfNotExisting<Grip>(gameObject);
            grip.PartToGrip = RayCastSensor;
            base.Awake();
        }

        private void RayCastSensorOnEventExit(GameObject obj)
        {
            var mu = obj.GetComponent<MU>();
            if (mu == null) return;
            if (mu == muingripper)
                muingripper = null;
        }
        

        private void RayCastSensorOnEventEnter(GameObject obj)
        {
            var mu = obj.GetComponent<MU>();
            if (mu == null) return;
            
            if (usefingers)
            {
                if (Close == true)
                {
                    grippedonclosing = true;
                    GrippedMU = mu;
                    grip.Fix(mu);
                    gripdistancerel = (posabs + RayCastSensor.RayCastDistance) / (GripperWidth / 2);
                }
            }
            else
            {
                muingripper = mu;
                gripdistancerel = 1;
            }
           
        }
        
        
        [Button("Close")]
        public void GripperClose()
        {
            Close = true;
            Open = false;
        }

        public void Stop()
        {
            Close = false;
            Open = false;
        }

        [Button("Open")]
        public void GripperOpen()
        {
            Open = true;
            Close = false;
        }

        
        // Start is called before the first frame update
        void Start()
        {
            FullyOpened = true;
            FullyClosed = false;
            Opening = false;
            Closing = false;
            posrel = 0;
        }

        void UpdateGripper()
        {

            bool isstopped = false;

            if (Open && Close)
                return;
            if (usefingers && Close && grippedonclosing && posrel >= gripdistancerel)
            {
                posrel = gripdistancerel;
                Closing = false;
                isstopped = true;
             
            }

            if (!usefingers && Close && posrel >= gripdistancerel)
            {
                if (muingripper != null && GrippedMU == null)
                {
                    GrippedMU = muingripper;
                    grip.Fix(muingripper);
                    muingripper = null;
                }
            }

            if (Open && GrippedMU != null)
            {
                grip.Unfix(GrippedMU);
                GrippedMU = null;
            }

            if (Open)
            {
                posrel = posrel - ((GripperWidth / 2) / TimeClosing * Time.fixedDeltaTime) / 2;
            }
            
            if (Close && !isstopped)
            {
                posrel = posrel + ((GripperWidth / 2) / TimeClosing * Time.fixedDeltaTime) / 2;
            }

            if (Close && !isstopped)
                Closing = true;
            
            if (Open && posrel <= 0)
            {
                Opening = false;
            }

            if (Close && posrel >= 1)
            {
                Closing = false;
            }

            if (posrel > 1)
                posrel = 1;
            if (posrel < 0)
                posrel = 0;


            if (posrel == 0)
                FullyOpened = true;
            else
                FullyOpened = false;

            if (posrel == 1)
                FullyClosed = true;
            else
                FullyClosed = false;

            // Update Positions
            posabs = (GripperWidth / 2) * posrel;
            if (usefingers)
            {
                leftfinger.transform.localPosition =
                    leftstartpos + DirectionClosing * (posabs + OpenPosOffset) / realvirtualController.Scale;
                rightfinger.transform.localPosition =
                    rightstartpos - DirectionClosing * (posabs + OpenPosOffset) / realvirtualController.Scale;
            }
        }

        // Update is called once per frame
        void FixedUpdate()
        {
            // Get PLCOutput Signals
            if (isclosegrippernotnull)
                Close = CloseGripper.Value;

            if (isopengrippernotnull)
                Open = OpenGripper.Value;

            // Calculate Positions, Status and so on
            UpdateGripper();

            // Set PLCInput Signals
            if (isclosingnotnull)
                IsClosing.Value = Closing;

            if (isopeningnotnull)
                IsOpening.Value = Opening;

            if (isfullyclosednotnull)
                IsFullyClosed.Value = FullyClosed;

            if (isfullyopenednotnull)
                IsFullyOpened.Value = FullyOpened;
            
        }
    }
}