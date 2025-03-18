﻿// realvirtual (R) Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 realvirtual GmbH - Usage of this source code only allowed based on License conditions see https://realvirtual.io/en/company/license

using NaughtyAttributes;
using UnityEngine;

 namespace realvirtual
{
	
	//! PLC BOOL INPUT Signal
	[HelpURL("https://doc.realvirtual.io/components-and-scripts/interfaces")]
	public class PLCInputBool : Signal
	{
		//! Status struct of the bool
	
		public StatusBool Status;
		

		//! Sets and gets the value
		public bool Value
		{
			get
			{
				if (Settings.Override)
				{
					return Status.ValueOverride;
				} else
				{
					return Status.Value;
				}
			}
			set
			{   var oldvalue = Status.Value;
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
			Settings.Active = true;
			Settings.Override = false;
			Status.Value = false;
			Status.OldValue = false;
		}
		

		public override void OnToggleHierarchy()
		{
			if (Settings.Override == false)
				Settings.Override = true;
			Status.ValueOverride = !Status.ValueOverride;
			EventSignalChanged.Invoke(this);
			SignalChangedEvent(this);
		}
	
		//! Sets the Status connected
		public override void SetStatusConnected(bool status)
		{
			Status.Connected = status;
		}

		//! Gets the status connected
		public override bool GetStatusConnected()
		{
			return Status.Connected;
		}

		//! Gets the text for displaying it in the hierarchy view
		public override string GetVisuText()
		{
			return Value.ToString();
		}
	
		//! True if signal is input
		public override bool IsInput()
		{
			return true;
		}

		//! Sets the Value as a string
		public override void SetValue(string value)
		{
			if (value != "")
			{
				if (value == "0")
				{
					Value = false;
					return;
				}

				if (value == "1")
				{
					Value = true;
					return;
				}
				Value = bool.Parse(value);
			}
			else
				Value = false;
		}
	
		public override void SetValue(object value)
		{
			if (value != null )
				Value = (bool)value;
		}
		
		public override object GetValue()
		{
			return Value;
		}
		
		//! Sets the Value as a bool
		public void SetValue(bool value)
		{
			Value = value;
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
