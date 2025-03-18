// realvirtual.io (formerly game4automation) (R) a Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 realvirtual GmbH - Usage of this source code only allowed based on License conditions see https://realvirtual.io/unternehmen/lizenz    

using UnityEngine;

namespace realvirtual
{

    public class GuideLine : realvirtualBehavior, IGuide
    {
        public float Length=1.0f;
        public bool ShowGizmos = true;

        public void OnDrawGizmos()
        {
            if (!ShowGizmos) return;
            var start = this.transform.position;
            var end = this.transform.position + this.transform.right * Length;
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(start, 0.02f);
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(start, end);
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(end, 0.02f);

        }


        public bool IsActive()
        {
            return this.enabled;
        }

        public Vector3 GetClosestDirection(Vector3 position)
        {
            return this.transform.right;
        }

        public Vector3 GetClosestPoint(Vector3 position)
        {
            var origin = this.transform.position;
            var end = origin + this.transform.right * Length;
            Vector3 heading = (end - origin);
            float magnitudeMax = heading.magnitude;
            heading.Normalize();

            //Do projection from the point but clamp it
            Vector3 lhs = position - origin;
            float dotP = Vector3.Dot(lhs, heading);
            dotP = Mathf.Clamp(dotP, 0f, magnitudeMax);
            return origin + heading * dotP;
        }
    }


    public interface IGuide
    {
        public Vector3 GetClosestDirection(Vector3 position);
        public Vector3 GetClosestPoint(Vector3 position);

        public bool IsActive();
    }
}
