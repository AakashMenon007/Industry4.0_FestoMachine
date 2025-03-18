// realvirtual (R) Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 realvirtual GmbH - Usage of this source code only allowed based on License conditions see https://realvirtual.io/en/company/license

using realvirtual;
using UnityEngine;

namespace realvirtual
{
	[RequireComponent(typeof(Drive))]
	//! This drive is only for test purposes. It is moving constantly two erratic positions between MinPos and MaxPos. 
	[HelpURL("https://doc.realvirtual.io/components-and-scripts/motion/drive-behavior")]
	public class Drive_ErraticPosition : BehaviorInterface
	{
		public float MinPos = 0; //!< Minimum position of the range where the drive is allowed to move to.
		public float MaxPos = 100; //!< Maximum position of the range where the drive is allowed to move to.
		public float Speed = 100; //!< Speed of the drive in millimeter / second.
		public bool Driving = false; //!< Set to true if Drive should drive to erratic positions.
		public bool IterateBetweenMaxAndMin = false; //!< If true, the drive will only iterate between MinPos and MaxPos. If false, the drive will iterate between random positions
		private Drive Drive;
		private float _destpos;
		
		void Reset()
		{
			Drive = GetComponent<Drive>();
			if (Drive.UseLimits)
			{
				MinPos = Drive.LowerLimit;
				MaxPos = Drive.UpperLimit;
			}
		}
		
		// Use this for initialization
		void Start()
		{
			Drive = GetComponent<Drive>();
		}

		// Update is called once per frame
		void FixedUpdate()
		{
			if (Driving && !Drive.IsRunning && Drive.CurrentPosition !=_destpos)
			{
				Drive.TargetPosition = _destpos;
				Drive.TargetStartMove = true;
			}	
			
			if (Driving == false)
			{
				Drive.TargetSpeed = Speed;
				if (!IterateBetweenMaxAndMin)
					Drive.TargetPosition = Random.Range(MinPos,MaxPos);
				else
				{
					if (Drive.CurrentPosition == MaxPos)
						Drive.TargetPosition = MinPos;
					else
						Drive.TargetPosition = MaxPos;
				}
				Drive.TargetStartMove = true;
				Driving = true;
				_destpos = Drive.TargetPosition;
			} else
			if (Drive.IsRunning && Driving == true)
			{
				Drive.TargetStartMove = false;
			}
			
			if (Drive.CurrentPosition == _destpos && Driving == true)
			{
				Driving = false;
			}
		
		}
	}
}
