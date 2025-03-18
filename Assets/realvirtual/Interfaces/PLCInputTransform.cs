﻿// realvirtual (R) Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 realvirtual GmbH - Usage of this source code only allowed based on License conditions see https://realvirtual.io/en/company/license


 using System;
 using UnityEngine;
 
namespace realvirtual
{
    //! PLC INT INPUT Signal
    [HelpURL("https://doc.realvirtual.io/components-and-scripts/interfaces")]
    [SelectionBase]
    public class PLCInputTransform : Signal
    {
        public StatusTransform Status;
       
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
                if (oldvalue != value)
                {
                    SignalChangedEvent(this);
                }
            }
        }
	
        // When Script is added or reset ist pushed
        private void Reset()
        {
            UpdateEnable = true;
            Settings.Active= true;
            Settings.Override = false;
            Status.Value =  new Pose(transform.position, transform.rotation);
        }
	

        public override void SetStatusConnected(bool status)
        {
            Status.Connected = status;
        }

        public override bool GetStatusConnected()
        {
            return Status.Connected;
        }

        public override string GetVisuText()
        {
            
                return Value.position.x.ToString("0.0") + " " + Value.position.y.ToString("0.0") + " " +
                       Value.position.z.ToString("0.0");
        }
	
        public override bool IsInput()
        {
            return true;
        }


        public override void SetValue(string value)
        {
            throw new NotImplementedException();
        }

        public override void SetValue(object value)
        {
            Value = (Pose)value;
        }
        
        public override object GetValue()
        {
            return Value;
        }
        
     
        public void SetValue(Pose value)
        {
            Value = value;
        }

        public void SetValueToTransform()
        {
            Value = new Pose(transform.localPosition, transform.localRotation);
        }
        
        public void SetTransformFromValue()
        {
            transform.localPosition= Value.position;
            transform.localRotation = Value.rotation;
        }

        private void FixedUpdate()
        {
           SetValueToTransform();
        }

        public void Update()
        {
            if (Status.OldValue != Status.Value)
            {
                if (EventSignalChanged!=null)
                   EventSignalChanged.Invoke(this);
                Status.OldValue = Status.Value;
            }		
        }
	
    }
}
