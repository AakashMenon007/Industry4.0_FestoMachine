// realvirtual (R) Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 realvirtual GmbH - Usage of this source code only allowed based on License conditions see https://realvirtual.io/en/company/license

using realvirtual;
using UnityEngine;

namespace realvirtual
{
    [RequireComponent(typeof(Drive))]
    //! Behavior model of a cylinder movement which can be connected to a Drive.
    //! The cylinder is defined by a maximum (*MaxPos*) and minimum (*MinPos*) position in millimeter
    [HelpURL("https://doc.realvirtual.io/components-and-scripts/motion/drive-behavior")]
    public class Drive_Simple : BehaviorInterface
    {
       
        [Header("Settings")] public float ScaleSpeed = 1;  //!< Scale factor for the input and output speed and acceleration

        [Header("PLC IOs")] public PLCOutputFloat Speed; //!< PLCOutput for the speed of the drive in millimeter / second, can be scaled by Scale factor.
        public PLCOutputFloat Accelaration; //!< PLCOutput for the speed of the drive in millimeter / second, can be scaled by Scale factor.
        public PLCOutputBool Forward; //!< Signal to move the drive forward
        public PLCOutputBool Backward; //!< Signal to move the drive backward
        public PLCInputFloat IsAtPosition; //!< Signal for current position of the drive (in millimeter).
        public PLCInputFloat IsAtSpeed; //!< Signal for current speed of the drive (in millimeter/s).
        public PLCInputBool IsDriving; //!< Signal is true if Drive is driving.

        private Drive Drive;
        private bool _isSpeedNotNull;
        private bool _isIsAtPositionNotNull;
        private bool _isForwardNotNull;
        private bool _isBackwardNotNull;
        private bool _isIsDrivingNotNull;
        private bool _isIsAtSpeedNotNull;
        private bool _isAccelerationNotNull;

        // Use this for initialization
        void Start()
        {
            _isIsDrivingNotNull = IsDriving != null;
            _isBackwardNotNull = Backward != null;
            _isForwardNotNull = Forward != null;
            _isIsAtPositionNotNull = IsAtPosition != null;
            _isIsAtSpeedNotNull = IsAtSpeed != null;
            _isSpeedNotNull = Speed != null;
            _isAccelerationNotNull = Accelaration != null;
            Drive = GetComponent<Drive>();
        }

        // Update is called once per frame
        void FixedUpdate()
        {
            // Get external PLC Outputs
            if (_isSpeedNotNull)
                Drive.TargetSpeed  = Speed.Value* ScaleSpeed;
            if (_isForwardNotNull)
                Drive.JogForward = Forward.Value;
            if (_isBackwardNotNull)
                Drive.JogBackward = Backward.Value;
            if (_isAccelerationNotNull)
                Drive.Acceleration = Accelaration.Value*ScaleSpeed;
        
            // Set external PLC Outpits
            if (_isIsAtPositionNotNull)
                IsAtPosition.Value = Drive.CurrentPosition/ ScaleSpeed;
            if (_isIsAtSpeedNotNull)
                IsAtSpeed.Value = Drive.CurrentSpeed/ ScaleSpeed ;
            if (_isIsDrivingNotNull)
                IsDriving.Value = Drive.IsRunning;
        }
    }
}