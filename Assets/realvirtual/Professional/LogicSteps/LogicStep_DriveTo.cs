// realvirtual.io (formerly game4automation) (R) a Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 realvirtual GmbH - Usage of this source code only allowed based on License conditions see https://realvirtual.io/unternehmen/lizenz  

using System;
using UnityEngine;
using NaughtyAttributes;

namespace realvirtual
{
    [HelpURL("https://doc.realvirtual.io/components-and-scripts/defining-logic/logicsteps")]
    public class LogicStep_DriveTo : LogicStep
    {
        public Drive drive;
        [OnValueChanged("EditorPosition")] public float Destination;
        public bool Relative = false;
        [OnValueChanged("LiveEditStart")] public bool LiveEdit = false;
        private float startpos = 0;
        private float delta = 0;

        private float des = 0;
        
        protected override void OnStarted()
        {
            LiveEdit = false;
            State = 0;
            if (drive != null)
            {
                drive.OnAtPosition += DriveOnOnAtPosition; 
                des = Destination;
                startpos = drive.CurrentPosition;
                if (Relative)
                    des = drive.CurrentPosition + Destination;
                 delta = Mathf.Abs(drive.CurrentPosition - Destination);
                drive.DriveTo(des);
            }
            else
            {
                NextStep();
            }
        }

        private void OnDestroy()
        {
            Debug.Log("Destory");
        }

        private void LiveEditStart()
        {
            if (drive!=null)
                if (LiveEdit)
                {
               
                    drive.StartEditorMoveMode();
                    EditorPosition();
                }
                else
                    drive.EndEditorMoveMode();
        }
        

        private void EditorPosition()
        {
            if (drive != null)
            {
                if (LiveEdit)
                {
              
                    drive.SetPositionEditorMoveMode(Destination);
                }
            }
        }

        public void FixedUpdate()
        {
            if (StepActive)
            {
                var currdelta =  Mathf.Abs(drive.CurrentPosition - des);
                State = ((delta-currdelta) / delta * 100);
            }
        }

        private void DriveOnOnAtPosition(Drive drive1)
        {
            drive.OnAtPosition -= DriveOnOnAtPosition;
            NextStep();
        }
    }

}

