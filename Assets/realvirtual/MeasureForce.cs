using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace realvirtual
{
    public class MeasureForce : BehaviorInterface
    {
        public Vector3 Force;
        public Vector3 Torque;

        public float AbsForce;
        public float AbsTorque;
        
        // Start is called before the first frame update
        private UnityEngine.Joint rb;

        public PLCInputFloat ForceX;
        public PLCInputFloat ForceY;
        public PLCInputFloat ForceZ;
        public PLCInputFloat ForceAbs;
        
        public PLCInputFloat TorqueX;
        public PLCInputFloat TorqueY;
        public PLCInputFloat TorqueZ;
        public PLCInputFloat TorqueAbs;

        private bool fx, fy, fz,fa, tx, ty, tz,ta;
        
        void Start()
        {
            rb = GetComponent<UnityEngine.Joint>();

            fx = ForceX != null;
            fy = ForceY != null;
            fz = ForceZ != null;
            fa = ForceAbs!= null;
            
            tx = TorqueX != null;
            ty = TorqueY != null;
            tz = TorqueZ != null;
            ta = TorqueAbs != null;
        }

        // Update is called once per frame
        void FixedUpdate()
        {
            Force = rb.currentForce;
            Torque = rb.currentTorque;
            AbsForce = Force.magnitude;
            AbsTorque = Torque.magnitude;
            
            if (fx)
                ForceX.Value = Force.x;
            if (fy)
                ForceY.Value = Force.y;
            if (fz)
                ForceZ.Value = Force.z;
            if (fa)
                ForceAbs.Value = AbsForce;

            
            if (tx)
                TorqueX.Value = Torque.x;
            if (ty)
                TorqueY.Value = Torque.y;
            if (tz)
                TorqueZ.Value = Torque.z;
            if (ta)
                TorqueAbs.Value = AbsTorque;
        }
    }

}
