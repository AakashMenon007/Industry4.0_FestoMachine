// realvirtual (R) Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 realvirtual GmbH - Usage of this source code only allowed based on License conditions see https://realvirtual.io/en/company/license

using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

namespace realvirtual
{
 
    public class MoveMU : MonoBehaviour
    {
        #region Public Attributes

        public PhysicMaterial MaterialStop;
        [ReadOnly] public Vector3 Direction;
       public bool Align;
        [ReadOnly] public float Velocity;
        [ReadOnly] public BoxCollider BoxCollider;
        [ReadOnly] public Rigidbody Rigidbody;
        #endregion
        
        private Rigidbody _rigidbody;
        private Vector3 lastDirection;
        private PhysicMaterial physicMat_move, physicMat_stop;
        private float angle;
        private Vector3 rot;
        private float time;
        private Vector3 curVelocity;

        void Start()
        {
            Rigidbody = gameObject.GetComponent<Rigidbody>();
            BoxCollider = gameObject.GetComponentInChildren<BoxCollider>();
            physicMat_move = BoxCollider.material;
            physicMat_stop = MaterialStop;
        }
        
        public void Move()
        {
            if(lastDirection == Vector3.zero)
            {
                lastDirection = Direction;
            }
		
            if (Direction != Vector3.zero)
            {
                if (Velocity != 0)
                {
                    if (Rigidbody.IsSleeping())
                    {
                        Rigidbody.WakeUp();
                    }
                    BoxCollider.material = physicMat_move;
                    Direction = Direction.normalized;

                    if(Align)
                    {
                        lastDirection.y = Direction.y;
                        angle = Vector3.Angle(Direction,lastDirection);
                        angle = -1* angle * Mathf.Sign(Vector3.Cross(Direction, lastDirection).y);
					
                        rot = Quaternion.Euler(0, angle, 0) * Rigidbody.transform.right;
                        float step = 2.0f * Time.deltaTime;
                        Rigidbody.transform.right = Vector3.MoveTowards(Rigidbody.transform.right, rot, step);
                        lastDirection = Direction;
                    }

                    curVelocity = Rigidbody.velocity;
                    curVelocity.x = Velocity * Direction.x;
                    curVelocity.z = Velocity * Direction.z;
                    Rigidbody.velocity = curVelocity;
                    Rigidbody.angularVelocity = Vector3.zero;
                }
                else
                {
                    BoxCollider.material = physicMat_stop;
                    Rigidbody.velocity = Vector3.zero;
                    Rigidbody.angularVelocity = Vector3.zero;
                }
            }
            Velocity = 0;
            Direction = Vector3.zero;
            Align = false;
        }
        
        public virtual void Update ()
        {
            if (!Rigidbody.isKinematic) {
                Move ();
            }
            else
            {
                Velocity = 0;
                Direction = Vector3.zero;
                lastDirection = Vector3.zero;
            }
        }

    }
}