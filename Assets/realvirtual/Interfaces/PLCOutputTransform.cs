﻿// realvirtual (R) Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 realvirtual GmbH - Usage of this source code only allowed based on License conditions see https://realvirtual.io/en/company/license

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;

 namespace realvirtual
{
    [HelpURL("https://doc.realvirtual.io/components-and-scripts/interfaces")]
    [System.Serializable]
    [SelectionBase]
    public class PLCOutputTransform : Signal
    {
        public StatusTransform Status;
        [SerializeField] Pose _value;
        private bool settransform = false;
        public Pose Value
        {
            get
            {
                if (Settings.Override)
                {
                    return Status.ValueOverride;;
                }
                else
                {
                    return Status.Value;
                }
            }
            set
            {
                var oldvalue = Status.Value;
                Status.Value = value;
                settransform = true;
                if (oldvalue != value)
                {
                    SignalChangedEvent(this);
                }
            }
        }

        public override void SetStatusConnected(bool status)
        {
            Status.Connected = status;
        }

        public override bool GetStatusConnected()
        {
            return Status.Connected;
        }

        // When Script is added or reset ist pushed
        private void Reset()
        {
            UpdateEnable = true;
            Settings.Active = true;
            Settings.Override = false;
            // get gameobject
            Status.Value = new Pose(transform.position, transform.rotation);
        }
        
        public override void SetValue(String value)
        {
            Debug.Log("Not implemented to set value " + value);
        }
        
        public override void SetValue(System.Object value)
        {
            Value = (Pose) value;
        }


        public void SetValue(Pose value)
        {
            Value = value;
        }

        public override object GetValue()
        {
            return Value;
        }

        public override string GetVisuText()
        {
          
            return Value.position.x.ToString("0.0") + " " + Value.position.y.ToString("0.0") + " " +
                   Value.position.z.ToString("0.0");
        }

    
        public void Update()
        {
            if (Status.OldValue != Status.Value)
            {
                EventSignalChanged.Invoke(this);
                Status.OldValue = Status.Value;
            }		
        }

        public void SetValueToTransoform()
        {
            Value = new Pose(transform.localPosition, transform.localRotation);
        }
        
        public void SetTransformFromValue()
        {
            transform.localPosition= Value.position;
            transform.localRotation = Value.rotation;
        }

        public void FixedUpdate()
        {
            if (settransform)
            {
                settransform = false;
                SetTransformFromValue();
                SignalChangedEvent(this);
            }
            
        }
    }
}